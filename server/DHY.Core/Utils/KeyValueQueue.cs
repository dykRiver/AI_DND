using System.Collections.Concurrent;

namespace DHY.Core.Utils;

public class KeyValueQueue<Tkey, TValue>
{
    private Dictionary<Tkey, TValue> _dictionary = new Dictionary<Tkey, TValue>();
    private ConcurrentQueue<Tkey> _queue = new ConcurrentQueue<Tkey>();

    public void Enqueue(Tkey key, TValue value)
    {
        if (_dictionary.ContainsKey(key))
        {
            return;
        }
        //else
        //{
        //    throw new 
        //}

        _dictionary[key] = value;
        _queue.Enqueue(key);
    }

    public (Tkey, TValue) Dequeue()
    {
        var tresult = _queue.TryDequeue(out Tkey tkey);

        if (tresult)
        {
            return (tkey, _dictionary[tkey]);
        }

        return default;
    }
}
