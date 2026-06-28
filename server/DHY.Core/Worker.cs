using Microsoft.Extensions.Logging;

namespace DHY.Core
{
    /// <summary>
    /// 使用System.Threading实现的可控制工程线程
    /// </summary>
    public class Worker
    {
        private readonly object _lockObject = new object();
        private readonly string _actionName;
        private readonly string _actionCode;
        private readonly int _workerLoopT;
        private readonly Action _action;
        private readonly ILogger _logger;
        private Status _status;
        private bool _disableLogError;
        /// <summary>
        /// 默认循环间隔时长
        /// </summary>
        public static int DefualtWorkerLoopT { set; get; } = 200;

        /// <summary>获取work名称.
        /// </summary>
        public string ActionName
        {
            get { return _actionName; }
        }

        /// <summary>获取work循环间隔时间.
        /// </summary>
        public int WorkerLoopT
        {
            get { return _workerLoopT; }
        }

        /// <summary>初始化一个work.
        /// </summary>
        /// <param name="actionName">名称.</param>
        /// <param name="action">动作.</param>
        public Worker(string actionCode, Action action, ILogger logger, string actionName = null,int? workerLoopT = null)
        {
            _actionName = actionName?? actionCode;
            _actionCode = actionCode;
            _action = action;
            _status = Status.Initial;
            _logger = logger;
            _workerLoopT = workerLoopT?? DefualtWorkerLoopT;
        }

        /// <summary>启动.
        /// </summary>
        public Worker Start()
        {
            lock (_lockObject)
            {
                if (_status == Status.Running) return this;

                _status = Status.Running;
                new Thread(Loop)
                {
                    Name = string.Format("{0}.Worker", _actionCode),
                    IsBackground = true
                }.Start(this);

                return this;
            }
        }
        /// <summary>停止.
        /// </summary>
        public Worker Stop()
        {
            lock (_lockObject)
            {
                if (_status == Status.StopRequested) return this;

                _status = Status.StopRequested;

                return this;
            }
        }

        private void Loop(object data)
        {
            var worker = (Worker)data;

            while (worker._status == Status.Running)
            {
                try
                {
                    _action();
                    _disableLogError = false;
                }
                catch (ThreadAbortException)
                {
                    _logger.LogInformation("Worker thread caught ThreadAbortException, try to resetting, actionName:{0}", _actionCode);

                    Thread.ResetAbort();
                    _logger.LogInformation("Worker thread ThreadAbortException resetted, actionName:{0}", _actionCode);
                }
                catch (Exception ex)
                {
                    if (_disableLogError == false)
                    {
                        _disableLogError = true;

                        _logger.LogError(string.Format("Worker thread has exception, actionName:{0},message:{1},stack:{2}", _actionCode, ex.Message, ex.StackTrace), ex);
                        Console.WriteLine(string.Format("Worker thread has exception, actionName:{0},message:{1},stack:{2}", _actionCode, ex.Message, ex.StackTrace), ex);
                    }
                }
            }
        }

        enum Status
        {
            Initial,
            Running,
            StopRequested
        }
    }
}
