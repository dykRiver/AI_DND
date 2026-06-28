public class ByteHelper
{
    public static long CombineInt64(uint higher, uint lower)
    {
        return ((long)higher << 32) | lower;
    }

    public static uint SplitToUint32High(long v)
    {
        return (uint)(v >> 32);
    }
    public static uint SplitToUint32Low(long v)
    {
        return (uint)v;
    }
}