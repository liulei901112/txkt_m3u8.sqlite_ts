using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace txkt_m3u8.sqlite_ts
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 窗口初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 清空当前进度
            this.CurrentProgress.Value = 0;
            this.CurrentPercent.Text = "0 / 100";
            // 清空总进度
            this.TotalProgress.Value = 0;
            this.TotalPercent.Text = "0 / 100";

            Log("初始化完成");
        }

        /// <summary>
        /// 选择目标文件夹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSelectTargetFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFileDialog = new System.Windows.Forms.FolderBrowserDialog(); // 选择文件夹
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) // 注意，此处一定要手动引入System.Window.Forms空间，否则你如果使用默认的DialogResult会发现没有OK属性
            {
                TargetFolder.Text = openFileDialog.SelectedPath;
                Log("选定目标文件夹" + TargetFolder.Text);
            }
        }

        /// <summary>
        /// 选择源文件夹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSelectSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFileDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SourceFolder.Text = openFileDialog.SelectedPath;
                Log("选定源文件夹" + SourceFolder.Text);
            }
        }

        /// <summary>
        /// 开始转换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BeginConvert_Click(object sender, RoutedEventArgs e)
        {
            string sourceFolder = SourceFolder.Text.Trim();
            string targetFolder = TargetFolder.Text.Trim();
            if (string.IsNullOrEmpty(sourceFolder))
            {
                Log("请输入或选择源文件夹");
                return;
            }
            if (!Directory.Exists(sourceFolder))
            {
                Log("源文件夹不存在");
                return;
            }

            if (string.IsNullOrEmpty(targetFolder))
            {
                Log("请输入或选择目标文件夹");
                return;
            }
            if (!Directory.Exists(targetFolder))
            {
                Log("目标文件夹不存在");
                return;
            }

            // 获取文件列表
            string[] filePaths = Directory.GetFiles(sourceFolder, "*.m3u8.sqlite", SearchOption.AllDirectories);
            if (null == filePaths || filePaths.Length <= 0)
            {
                Log("没有找到需要解码的文件（*.m3u8.sqlite）");
                return;
            }

            // 遍历文件
            int index = 1;
            int total = filePaths.Length;
            foreach (string filePath in filePaths)
            {
                Log("[+]" + filePath);
                RefreshTotalProgress(index, total);
                index++;
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        /// <summary>
        /// 刷新总进度
        /// </summary>
        /// <param name="value"></param>
        /// <param name="total"></param>
        private void RefreshTotalProgress(float value, float total)
        {
            this.TotalProgress.Value = value / total * 100;
            this.TotalPercent.Text = String.Format("{0} / {1}", value, total);
        }

        /// <summary>
        /// log
        /// </summary>
        /// <param name="logtxt"></param>
        private void Log(string logtxt)
        {
            this.Logtxt.AppendText(string.Format("[{0}] - {1}\r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), logtxt));
            this.Logtxt.ScrollToEnd();
        }
    }
}
