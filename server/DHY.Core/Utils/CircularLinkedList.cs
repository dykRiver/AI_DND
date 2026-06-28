using Furion.DependencyInjection;

namespace DHY.Core.Utils;

/// <summary>
/// 循环链表
/// </summary>
[SuppressSniffer]
public class CircularLinkedList<T>
{
    private ListNode<T> _head;
    private ListNode<T> _last;
    private ListNode<T> _current;

    public T First => _head.Value;
    public T Current => _current.Value;

    public CircularLinkedList(IEnumerable<T> list)
    {
        ListNode<T> previous = null;
        ListNode<T> current = null;

        foreach (var item in list)
        {
            current = new ListNode<T>(item);

            if (_head == null)
            {
                _head = current;
                previous = current;
            }

            previous.Next = current;
            previous = current;
        }

        current.Next = _head;
        _current = _head;
        _last = current;
    }

    public void Add(T item)
    {
        var node = new ListNode<T>(item);
        _current = node;

        //头节点为第一个节点
        if (_head == null)
        {
            _head = node;
        }
        else
        {
            //找到队尾
            var currentNode = _head;

            while (currentNode.Next != _head)
            {
                currentNode = currentNode.Next;
            }

            currentNode.Next = node;
        }

        //首尾相连
        node.Next = _head;
        _last = node;
    }

    public void Remove(T item)
    {
        if (_head == null)
        {
            return;
        }

        if (_head.Value.Equals(item))
        {
            _head = null;
            return;
        }

        var previous = _head;
        var currentNode = _head;

        while (!currentNode.Value.Equals(item))
        {
            previous = currentNode;
            currentNode = currentNode.Next;

            //找一圈没找到
            if (currentNode.Next == _head)
            {
                return;
            }
        }

        previous.Next = currentNode.Next;
    }

    /// <summary>
    /// 移除所有符合条件的项，链表较长时性能会变差
    /// </summary>
    /// <param name="predicate"></param>
    public void RemoveAll(Predicate<T> predicate)
    {
        var previous = _head;
        var current =  _head;

        while (true)
        {
            if (predicate(current.Value))
            {
                //说明是第一个元素
                if (previous == current)
                {
                    //只有一个元素
                    if (current.Next == _head)
                    {
                        _head = null;
                        _last = null;
                        _current = null;
                        break;
                    }

                    //最后一个元素链接到第二个元素
                    _last.Next = current.Next;
                    //第二个元素变成队首
                    _head = current.Next;
                    previous = current.Next;
                }
                else
                {
                    previous.Next = current.Next;
                }

                //如果当前指针指向被删除的元素，当前指针后移
                if (_current == current)
                {
                    _current = current.Next;
                }

                current = current.Next;
                continue;
            }

            previous = current;
            current = current.Next;

            if (current == _head)
            {
                break;
            }
        }
    }

    /// <summary>
    /// 修改链表中与参数值相等的项，并且不会移动当前指针
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Update(T item)
    {
        var node = Fetch(item);

        if (node != null)
        {
            node.Value = item;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 修改符合条件的值，并将当前指针指向修改后的位置
    /// </summary>
    /// <param name="predicate">修改条件</param>
    /// <param name="item">修改后的值</param>
    /// <returns></returns>
    public bool UpdateAndMoveTo(Predicate<T> predicate, T item)
    {
        if (!predicate(_current.Value))
        {
            _current = _current.Next;

            if (_current == _head)
            {
                return false;
            }

            return UpdateAndMoveTo(predicate, item);
        }

        _current.Value = item;

        return true;
    }

    /// <summary>
    /// 修改链表中第一个符合查询条件的元素，并不会改变当前指针位置
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="item">修改后的值</param>
    /// <param name="startPosition">查找的起始点</param>
    /// <returns></returns>
    public bool Update(Predicate<T> predicate, T item, ListNode<T> startPosition = null)
    {
        var current = startPosition ?? _head;
        if (!predicate(current.Value))
        {
            current = current.Next;

            if (current == _head)
            {
                return false;
            }

            return Update(predicate, item, current);
        }

        current.Value = item;

        return true;
    }

    /// <summary>
    ///  在链表中查找结果并保留当前指针位置不做调整
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="startPosition">查找的起始点</param>
    /// <returns></returns>
    public T FirstOrDefault(Predicate<T> predicate, ListNode<T> startPosition = null)
    {
        var current = startPosition ?? _head;

        if (!predicate(current.Value))
        {
            current = current.Next;

            if (current == _head)
            {
                return default;
            }

            return FirstOrDefault(predicate, current);
        }

        return current.Value;
    }

    /// <summary>
    /// 在链表中查找并将当前指针指向查找到的元素位置（链表较长时查询性能会下降）
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public T FindOneAndMoveTo(Predicate<T> predicate)
    {
        if (!predicate(_current.Value))
        {
            _current = _current.Next;

            if (_current == _head)
            {
                return default;
            }

            return FirstOrDefault(predicate, _current);
        }

        return _current.Value;
    }

    private ListNode<T> Fetch(T item)
    {
        if (_head == null)
        {
            return null;
        }

        if (_head.Value.Equals(item))
        {
            return _head;
        }

        var previous = _head;
        var currentNode = _head;

        while (!currentNode.Value.Equals(item))
        {
            previous = currentNode;
            currentNode = currentNode.Next;

            //找一圈没找到
            if (currentNode.Next == _head)
            {
                return null;
            }
        }

        return currentNode;
    }

    /// <summary>
    /// 查找下一个元素
    /// </summary>
    /// <returns></returns>
    public T? Next()
    {
        if (_current == null)
        {
            return default;
        }

        var result = _current.Value;
        _current = _current.Next;
        return result;
    }

    /// <summary>
    /// 查找第一个符合条件的元素并将指针指向下一个元素
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public T? Next(Predicate<T> predicate)
    {
        if (_current == null)
        {
            return default;
        }

        if (!predicate(_current.Value))
        {
            _current = _current.Next;

            if (_current == _head)
            {
                return default;
            }

            return Next(predicate);
        }

        var result = _current.Value;
        _current = _current.Next;

        return result;
    }
}
