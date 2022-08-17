using log4net;
using System;
using System.Threading;
using System.Windows;

namespace txkt_m3u8.sqlite_ts
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 重写OnStartup
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            /// <summary>
            /// 最小工作线程（CPU线程数 * 2）
            /// </summary>
            int workerThreadMinSize = Environment.ProcessorCount * 2;
            /// <summary>
            /// 最大工作线程（CPU线程数 * 4）
            /// </summary>
            int workerThreadMaxSize = Environment.ProcessorCount * 4;
            /// <summary>
            /// 最小IO线程（CPU线程数）
            /// </summary>
            int ioThreadMinSize = Environment.ProcessorCount;
            /// <summary>
            /// 最大IO线程（CPU线程数 * 2）
            /// </summary>
            int ioThreadMaxSize = Environment.ProcessorCount * 2;

            bool setMinThread = ThreadPool.SetMinThreads(workerThreadMinSize, ioThreadMinSize);
            log.Debug(string.Format("设置线程池最小工作线程数：{0}，最小IO线程数：{1}，结果：{2}", workerThreadMinSize, ioThreadMinSize, setMinThread));
            bool setMaxThread = ThreadPool.SetMaxThreads(workerThreadMaxSize, ioThreadMaxSize);
            log.Debug(string.Format("设置线程池最大工作线程数：{0}，最大IO线程数：{1}，结果：{2}", workerThreadMaxSize, ioThreadMaxSize, setMaxThread));
        }
    }
}
