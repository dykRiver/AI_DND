using System.Runtime.CompilerServices;

namespace DHY.Game.AI.Models;

/// <summary>
/// 【VIP流式预生成·中途接管】可重放的叙事流缓冲。
/// 生产者（后台预生成）逐 chunk 写入；消费者（玩家点选）从游标 0 回放：
/// 已缓冲的 chunk 立即吐出（首字≈0），追平后继续等同一生产流的后续 chunk，直到生产完成。
/// 全程只有一次 AI 请求——点选不再新开请求，而是接管这个正在进行的流。
///
/// 并发模型：单生产者 + 单消费者（每个选项至多一个点选者）。
/// 历史 chunk 全量保留（支持回放），用 SemaphoreSlim 作"有新 chunk / 已完成"信号；
/// 消费循环每轮先重查游标与 Count 再决定是否等待，残留信号只会让等待立即返回并重新检查，不漏 chunk、不死锁。
/// </summary>
public sealed class NarrativeStreamBuffer
{
    private readonly List<string> _chunks = new();
    private readonly object _lock = new();
    private readonly SemaphoreSlim _signal = new(0);
    private int _charCount;
    private bool _completed;
    private Exception? _error;

    /// <summary>生产者：追加一个流式 chunk。</summary>
    public void Write(string chunk)
    {
        if (string.IsNullOrEmpty(chunk))
            return;
        lock (_lock)
        {
            _chunks.Add(chunk);
            _charCount += chunk.Length;
        }
        _signal.Release();
    }

    /// <summary>生产者：正常结束（含被取消时视为正常收尾，未选选项无人消费）。</summary>
    public void Complete()
    {
        lock (_lock)
        {
            if (_completed) return;
            _completed = true;
        }
        _signal.Release();
    }

    /// <summary>生产者：失败结束。消费者回放完已缓冲 chunk 后据此收尾（不抛给玩家，见 D2）。</summary>
    public void Fail(Exception error)
    {
        lock (_lock)
        {
            if (_completed) return;
            _error = error;
            _completed = true;
        }
        _signal.Release();
    }

    /// <summary>是否已结束（完成或失败）。</summary>
    public bool IsCompleted
    {
        get { lock (_lock) return _completed; }
    }

    /// <summary>是否以失败结束（点选据此决定接管收尾还是降级）。</summary>
    public bool IsFaulted
    {
        get { lock (_lock) return _error != null; }
    }

    /// <summary>已缓冲的累计字数（供日志与完成快照）。</summary>
    public int CharCount
    {
        get { lock (_lock) return _charCount; }
    }

    /// <summary>当前已缓冲的全文快照。</summary>
    public string FullText
    {
        get { lock (_lock) return string.Concat(_chunks); }
    }

    /// <summary>
    /// 消费者：游标独立地回放历史 + 续读未来。
    /// 追平且生产未完成时等待新 chunk；生产结束后吐完剩余即退出。
    /// 生产失败时，已缓冲 chunk 照常吐完，随后静默结束（不抛异常，由 Hub 补发 IsLast，见 D2）。
    /// </summary>
    public async IAsyncEnumerable<string> ReplayAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        var cursor = 0;
        while (true)
        {
            string? chunk = null;
            var done = false;
            lock (_lock)
            {
                if (cursor < _chunks.Count)
                    chunk = _chunks[cursor++];
                else
                    done = _completed;
            }

            if (chunk != null)
            {
                yield return chunk;
                continue;
            }

            if (done)
                yield break;

            // 追平但未完成：等待下一个 chunk（或结束信号）。消费者断线由 ct 兜底退出。
            await _signal.WaitAsync(ct).ConfigureAwait(false);
        }
    }
}
