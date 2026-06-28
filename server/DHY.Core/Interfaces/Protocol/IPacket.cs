using DHY.Core.Interfaces.Protocol;

public interface IPacket
{
    bool Checked { get; }

    bool Analysis(ArraySegment<byte> buffer);
    bool Deserialize();
    bool Deserialize(ref PackReader reader);
    TDeviceCommand GetData<TDeviceCommand>() where TDeviceCommand : IDeviceCommand;
    TDeviceCommand GetMultiPackData<TDeviceCommand>() where TDeviceCommand : MultiPackCommand;
    bool Serialize();
}