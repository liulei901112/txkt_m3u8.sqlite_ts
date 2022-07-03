using System.Windows;

namespace txkt_m3u8.sqlite_ts.ViewModel
{
    /// <summary>
    /// 主窗口数据Model
    /// </summary>
    public class MainModel
    {
        /// <summary>
        /// 源文件夹
        /// </summary>
        public string SourceFolder { get; set; }

        /// <summary>
        /// 目标文件夹
        /// </summary>
        public string TargetFolder { get; set; }

        /// <summary>
        /// 当前进度
        /// </summary>
        public float CurrentProgress { get; set; }
        /// <summary>
        /// 当前进度最大值
        /// </summary>
        public float CurrentMax { get; set; }
        /// <summary>
        /// 当前进度百分比
        /// </summary>
        public string CurrentPercent { get; set; }

        /// <summary>
        /// 总进度
        /// </summary>
        public float TotalProgress { get; set; }
        /// <summary>
        /// 总进度
        /// </summary>
        public float TotalMax { get; set; }
        /// <summary>
        /// 当前进度百分比
        /// </summary>
        public string TotalPercent { get; set; }
    }
}
