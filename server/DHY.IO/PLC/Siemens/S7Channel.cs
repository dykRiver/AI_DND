using DHY.Core.Drivers;

namespace DHY.IO.PLC.Siemens
{
    /// <summary>
    /// 西门子S7通信通道
    /// </summary>
    public class S7Channel : IChannel
    {
        /// <summary>
        /// 驱动
        /// </summary>
        public IDriver Driver { get; }

        public bool Pause()
        {
            throw new NotImplementedException();
        }

        public bool ReStart()
        {
            throw new NotImplementedException();
        }

        public bool Resume()
        {
            throw new NotImplementedException();
        }

        public bool Setup(params object[] para)
        {
            throw new NotImplementedException();
        }

        public bool ShutDown()
        {
            throw new NotImplementedException();
        }

        public bool Start()
        {
            throw new NotImplementedException();
        }
    }
}
