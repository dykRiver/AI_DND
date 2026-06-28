using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Yitter.IdGenerator;

public static class LoggerExtensions
{
    private static readonly Dictionary<DDCSTrackModel, Stopwatch> DictTrack;
    private static readonly Semaphore SemaphoreTrack;

    private const int TimeoutTrack = 1000;

    static LoggerExtensions()
    {
        DictTrack = new Dictionary<DDCSTrackModel, Stopwatch>();
        SemaphoreTrack = new Semaphore(1, 1);
    }

    public static void Track(this ILogger logger, TrackEvent trackEvent, long pid, string prescriptionNo, object trackContent) => logger.LogTrace($"[{trackEvent}] 处方id：【{pid}】| 处方号：{prescriptionNo}|{trackContent.ToJson()}");

    private static void Track(this ILogger logger, DDCSTrackModel track)
    {
        track.CreateTime = DateTime.Now;

        var logContent = new StringBuilder();

        //logContent.Append($"[{track.CreateTime:yyyy-MM-dd HH:mm:ss:fff}]");
        logContent.Append($"[{track.TrackEvent.ToString()}] ");

        if (track.Pid != 0)
        {
            logContent.Append($" 处方id：【{track.Pid}】|");
        }

        if (!string.IsNullOrWhiteSpace(track.PrescriptionNo))
        {
            logContent.Append($"处方号：{track.PrescriptionNo}|");
        }

        if (track.DDCSPid.HasValue)
        {
            logContent.Append($"拆方号：{track.DDCSPid}|");
        }

        if (track.ContainerNo.HasValue)
        {
            logContent.Append($"桶号：{track.ContainerNo}|");
        }

        if (track.DeviceNo.HasValue)
        {
            logContent.Append($"设备号：{track.DeviceNo}|");
        }

        if (track.TaskNo.HasValue)
        {
            logContent.Append($"任务Id：{track.TaskNo}|");
        }

        if (!string.IsNullOrWhiteSpace(track.OperUser))
        {
            logContent.Append($"操作人：{track.OperUser}|");
        }

        if (!string.IsNullOrWhiteSpace(track.Extra1))
        {
            logContent.Append($"{track.Extra1}|");
        }

        if (!string.IsNullOrWhiteSpace(track.Extra2))
        {
            logContent.Append($"{track.Extra2}|");
        }

        if (!string.IsNullOrWhiteSpace(track.Extra3))
        {
            logContent.Append($"{track.Extra3}|");
        }

        if (!string.IsNullOrWhiteSpace(track.Extra4))
        {
            logContent.Append($"{track.Extra4}|");
        }

        if (!string.IsNullOrWhiteSpace(track.Extra5))
        {
            logContent.Append($"{track.Extra5}|");
        }

        if (!string.IsNullOrWhiteSpace(track.Extra6))
        {
            logContent.Append($"{track.Extra6}|");
        }

        if (!string.IsNullOrWhiteSpace(track.Extra7))
        {
            logContent.Append($"{track.Extra7}|");
        }

        if (!string.IsNullOrWhiteSpace(track.Extra8))
        {
            logContent.Append($"{track.Extra8}|");
        }

        if (!string.IsNullOrWhiteSpace(track.Extra9))
        {
            logContent.Append($"{track.Extra9} ");
        }

        if (track.Elapsed.HasValue)
        {
            logContent.Append($"{Environment.NewLine}用时：{track.Elapsed} ");
        }

        logger.LogTrace(logContent.ToString());
    }

    public static DDCSTrackModel StartTrack(this ILogger logger, TrackEvent trackEvent, object extra1 = null, object extra2 = null, object extra3 = null, object extra4 = null, object extra5 = null, object extra6 = null, object extra7 = null, object extra8 = null, object extra9 = null)
    {
        var wait = false;

        try
        {
            var track = new DDCSTrackModel
            {
                Id = YitIdHelper.NextId(),
                TrackEvent = trackEvent,
                Extra1 = extra1?.ToJson(),
                Extra2 = extra2?.ToJson(),
                Extra3 = extra3?.ToJson(),
                Extra4 = extra4?.ToJson(),
                Extra5 = extra5?.ToJson(),
                Extra6 = extra6?.ToJson(),
                Extra7 = extra7?.ToJson(),
                Extra8 = extra8?.ToJson(),
                Extra9 = extra9?.ToJson()
            }; ;

            if (!SemaphoreTrack.WaitOne(TimeoutTrack))
            {
                return track;
            }

            wait = true;

            DictTrack.Add(track, Stopwatch.StartNew());
            return track;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            return null;
        }
        finally
        {
            if (wait)
            {
                SemaphoreTrack.Release();
            }
        }
    }

    public static DDCSTrackModel StartTrack(this ILogger logger, TrackEvent trackEvent, long pid, string prescriptionNo, long? ddcsPid = null, ushort? containerNo = null, ushort? deviceNo = null, long? taskId = null, object extra1 = null, object extra2 = null, object extra3 = null, object extra4 = null, object extra5 = null, object extra6 = null, object extra7 = null, object extra8 = null, object extra9 = null)
    {
        var wait = false;

        try
        {
            var track = CreateTrack(trackEvent, pid, prescriptionNo, ddcsPid, containerNo, deviceNo, taskId, extra1, extra2, extra3, extra4, extra5, extra6, extra7, extra8, extra9);

            if (!SemaphoreTrack.WaitOne(TimeoutTrack))
            {
                return track;
            }

            wait = true;

            DictTrack.Add(track, Stopwatch.StartNew());

            logger.LogInformation(track.ToJson());

            return track;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            return null;
        }
        finally
        {
            if (wait)
            {
                SemaphoreTrack.Release();
            }
        }
    }

    public static void StopTrack(this ILogger logger, DDCSTrackModel track, object extra1 = null, object extra2 = null, object extra3 = null, object extra4 = null, object extra5 = null, object extra6 = null, object extra7 = null, object extra8 = null, object extra9 = null)
    {
        var wait = false;

        try
        {
            if (!SemaphoreTrack.WaitOne(TimeoutTrack))
            {
                return;
            }

            wait = true;

            if (track == null || !DictTrack.ContainsKey(track))
            {
                return;
            }

            if (extra1 != null)
            {
                track.Extra1 = extra1.ToJson();
            }

            if (extra2 != null)
            {
                track.Extra2 = extra2.ToJson();
            }

            if (extra3 != null)
            {
                track.Extra3 = extra3.ToJson();
            }

            if (extra4 != null)
            {
                track.Extra4 = extra4.ToJson();
            }

            if (extra5 != null)
            {
                track.Extra5 = extra5.ToJson();
            }

            if (extra6 != null)
            {
                track.Extra6 = extra6.ToJson();
            }

            if (extra7 != null)
            {
                track.Extra7 = extra7.ToJson();
            }

            if (extra8 != null)
            {
                track.Extra8 = extra8.ToJson();
            }

            if (extra9 != null)
            {
                track.Extra9 = extra9.ToJson();
            }

            var sw = DictTrack[track];

            sw?.Stop();

            track.Elapsed = sw?.ElapsedMilliseconds;

            DictTrack.Remove(track);
            Track(logger, track);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
        }
        finally
        {
            if (wait)
            {
                SemaphoreTrack.Release();
            }
        }
    }

    public static void Track(this ILogger logger, TrackEvent trackEvent, long pid, string prescriptionNo, long? ddcsPid = null, ushort? containerNo = null, ushort? deviceNo = null, long? taskId = null, object extra1 = null, object extra2 = null, object extra3 = null, object extra4 = null, object extra5 = null, object extra6 = null, object extra7 = null, object extra8 = null, object extra9 = null)
    {
        var track = CreateTrack(trackEvent, pid, prescriptionNo, ddcsPid, containerNo, deviceNo, taskId, extra1, extra2, extra3, extra4, extra5, extra6, extra7, extra8, extra9);
        Track(logger, track);
    }

    private static DDCSTrackModel CreateTrack(TrackEvent trackEvent, long pid, string prescriptionNo, long? ddcsPid = null, ushort? containerNo = null, ushort? deviceNo = null, long? taskId = null, object extra1 = null, object extra2 = null, object extra3 = null, object extra4 = null, object extra5 = null, object extra6 = null, object extra7 = null, object extra8 = null, object extra9 = null)
    {
        return new DDCSTrackModel
        {
            Id = YitIdHelper.NextId(),
            TrackEvent = trackEvent,
            Pid = pid,
            PrescriptionNo = prescriptionNo,
            DDCSPid = ddcsPid,
            ContainerNo = containerNo,
            DeviceNo = deviceNo,
            TaskNo = taskId,
            Extra1 = extra1?.ToJson(),
            Extra2 = extra2?.ToJson(),
            Extra3 = extra3?.ToJson(),
            Extra4 = extra4?.ToJson(),
            Extra5 = extra5?.ToJson(),
            Extra6 = extra6?.ToJson(),
            Extra7 = extra7?.ToJson(),
            Extra8 = extra8?.ToJson(),
            Extra9 = extra9?.ToJson()
        };
    }
}