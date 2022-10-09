using System.Windows;

namespace txkt_m3u8.sqlite_ts.ViewModel
{
    /// <summary>
    /// MainWindow模型
    /// </summary>
    public class MainViewModel: NotificationObject
    {
        private MainModel model;

        public MainViewModel()
        {
            model = new MainModel()
            {
#if DEBUG
                SourceFolder = "D:\\TXEDU_TEST",
#endif
                TargetFolder = ".",

                CurrentProgress = 0,
                CurrentMax = 100,
                CurrentPercent = "",

                TotalProgress = 0,
                TotalMax = 100,
                TotalPercent = ""
            };
        }
        /// <summary>
        /// 源文件夹
        /// </summary>
        public string SourceFolder
        {
            get { return model.SourceFolder; }
            set
            {
                model.SourceFolder = value;
                RaisePropertyChanged("SourceFolder");
            }
        }

        /// <summary>
        /// 目标文件夹
        /// </summary>
        public string TargetFolder
        {
            get { return model.TargetFolder; }
            set
            {
                model.TargetFolder = value;
                RaisePropertyChanged("TargetFolder");
            }
        }

        /// <summary>
        /// 当前进度
        /// </summary>
        public float CurrentProgress
        {
            get { return model.CurrentProgress; }
            set
            {
                model.CurrentProgress = value;
                RaisePropertyChanged("CurrentProgress");
            }
        }
        /// <summary>
        /// 当前进度最大值
        /// </summary>
        public float CurrentMax
        {
            get { return model.CurrentMax; }
            set
            {
                model.CurrentMax = value;
                RaisePropertyChanged("CurrentMax");
            }
        }
        /// <summary>
        /// 当前进度百分比
        /// </summary>
        public string CurrentPercent
        {
            get { return model.CurrentPercent; }
            set
            {
                model.CurrentPercent = value;
                RaisePropertyChanged("CurrentPercent");
            }
        }

        /// <summary>
        /// 总进度
        /// </summary>
        public float TotalProgress
        {
            get { return model.TotalProgress; }
            set
            {
                model.TotalProgress = value;
                RaisePropertyChanged("TotalProgress");
            }
        }
        /// <summary>
        /// 总进度
        /// </summary>
        public float TotalMax
        {
            get { return model.TotalMax; }
            set
            {
                model.TotalMax = value;
                RaisePropertyChanged("TotalMax");
            }
        }
        /// <summary>
        /// 当前进度百分比
        /// </summary>
        public string TotalPercent
        {
            get { return model.TotalPercent; }
            set
            {
                model.TotalPercent = value;
                RaisePropertyChanged("TotalPercent");
            }
        }

        /// <summary>
        /// 工作状态
        /// </summary>
        public string WorkStatus {
            get { return model.WorkStatus; }
            set
            {
                model.WorkStatus = value;
                RaisePropertyChanged("WorkStatus");
            }
        }
    }
}
