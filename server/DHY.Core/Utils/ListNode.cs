namespace DHY.Core.Utils;

public class ListNode<T>
{
    public T Value { get; set; }
    public ListNode<T> Next { get; set; }

    public ListNode(T value)
    {
        Value = value;
        Next = null;
    }
}