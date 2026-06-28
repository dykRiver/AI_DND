using System.Text;

public static class PacketConst
{
    static PacketConst()
    {
        Encoding = Encoding.UTF8;
    }

    public static Encoding Encoding { get; }
}