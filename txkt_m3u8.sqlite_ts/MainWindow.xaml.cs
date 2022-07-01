using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;

namespace txkt_m3u8.sqlite_ts
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
            this.CurrentPercent.Text = "";
            // 清空总进度
            this.TotalProgress.Value = 0;
            this.TotalPercent.Text = "";

            log.Info("初始化完成");
            ShowStatus("初始化完成");
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
                log.Info("选定目标文件夹" + TargetFolder.Text);
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
                log.Info("选定源文件夹" + SourceFolder.Text);
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
                log.Error("请输入或选择源文件夹");
                return;
            }
            if (!Directory.Exists(sourceFolder))
            {
                log.Error("源文件夹不存在");
                return;
            }

            if (string.IsNullOrEmpty(targetFolder))
            {
                log.Error("请输入或选择目标文件夹");
                return;
            }
            if (!Directory.Exists(targetFolder))
            {
                log.Error("目标文件夹不存在");
                return;
            }

            // 获取文件列表
            string[] filePaths = Directory.GetFiles(sourceFolder, "*.m3u8.sqlite", SearchOption.AllDirectories);
            if (null == filePaths || filePaths.Length <= 0)
            {
                log.Error("没有找到需要解码的文件（*.m3u8.sqlite）");
                return;
            }

            Task.Run(() => {
                // 遍历文件
                int index = 1;
                int total = filePaths.Length;

                // 初始化进度条总数
                RefreshProgress(ProgressType.总进度, 0, total);
                ShowStatus("扫描文件。总数：" + total);

                foreach (string filePath in filePaths)
                {
                    log.Info(filePath);

                    try
                    {
                        // 解析元数据
                        string[] metadata = FetchOneMetadata(filePath);
                        string uin = metadata[1];
                        string termId = metadata[2];

                        // 获取ts
                        FetchOneTs(filePath, uin, termId);

                        // 刷新总进度
                        RefreshProgress(ProgressType.总进度, index, total);
                    }
                    catch (Exception ex)
                    {
                        log.Error("文件损坏：" + filePath);
                    }
                    index++;
                }
            });
        }

        /// <summary>
        /// 刷新总进度
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="value"></param>
        /// <param name="total"></param>
        private void RefreshProgress(ProgressType pt, float value, float total)
        {
            void Refresh()
            {
                ProgressBar progressBar = pt == ProgressType.当前进度 ? CurrentProgress : TotalProgress;
                TextBlock percentBar = pt == ProgressType.当前进度 ? CurrentPercent : TotalPercent;
                progressBar.Maximum = total;
                progressBar.Value = value;
                percentBar.Text = String.Format("{0}%  [{1}/{2}]", (value / total * 100).ToString("#.00"), value, total);
            }
            try
            {
                Refresh();
            }
            catch
            {
                Dispatcher.InvokeAsync(() =>
                {
                    Refresh();
                });
            }
        }

        /// <summary>
        /// ShowStatus
        /// </summary>
        /// <param name="logtxt"></param>
        private void ShowStatus(string text)
        {
            void Append()
            {
                this.WorkStatus.Content = text;
            }
            try
            {
                Append();
            }
            catch
            {
                Dispatcher.InvokeAsync(() =>
                {
                    Append();
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="uin"></param>
        /// <param name="termId"></param>
        private void FetchOneTs(string filePath, string uin, string termId)
        {
            DateTime beginTime = DateTime.Now;
            using (SQLite sqlite = new SQLite(filePath))
            {
                ShowStatus("读取caches => " + filePath);
                string cachesTableName = "caches";
                int total = sqlite.GetRowsCount(cachesTableName);

                string[] caches = sqlite.GetRows(cachesTableName, "value");

                RefreshProgress(ProgressType.当前进度, 0, total);
                for (int i = 0; i < total; i++)
                {
                    List<string[]> ss = sqlite.GetLines(cachesTableName, i + "", "value");

                    RefreshProgress(ProgressType.当前进度, i + 1, total);
                    ShowStatus(string.Format("解析ts进度 [{0} / {1}] => {2}", i, total, filePath));
                }
            }
        }

        /// <summary>
        /// 获取一个元数据
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string[] FetchOneMetadata(string filePath)
        {
            // 读取数据库
            using (SQLite sqlite = new SQLite(filePath))
            {
                ShowStatus("解析元数据 => " + filePath);
                string[] metadatas = sqlite.GetRows("metadata", "value");
                if (null == metadatas || metadatas.Length <= 0)
                {
                    log.Error("metadata 不存在 => " + filePath);
                    return null;
                }
                string metadata = metadatas[0];
                Extend extend = ExtractFromMetadata(metadata);
                string uin = extend.Tokens["uin"];
                string termId = extend.Tokens["term_id"];
                string psKey = extend.Tokens["pskey"];
                string fileName = Path.GetFileName(filePath);

                var result = new string[] { fileName, uin, termId, psKey };

                log.Info("metadata " + fileName);
                log.Info(string.Format("uin：{0}，term_id：{1}，pskey：{2}", uin, termId, psKey));
                log.Info(string.Format("args：{0}", extend.Queries));
                log.Info(result);
                return result;
            }
        }

        /// <summary>
        /// 从元数据获取
        /// </summary>
        /// <param name="metadata"></param>
        private Extend ExtractFromMetadata(string metadata)
        {
            Uri uri = new Uri(metadata);
            string path = uri.AbsolutePath;
            log.Info(path);

            // 获取token
            Match matchToken = new Regex(@"token\.([\S]+)\.v").Match(path);
            if (!matchToken.Success)
            {
                log.Error("token解析失败");
                return null;
            }
            string tokenRaw = matchToken.Value;
            tokenRaw = tokenRaw.Substring(6);
            tokenRaw = tokenRaw.Substring(0, tokenRaw.Length - 2);

            string tokenDecode = Base64.Decode(tokenRaw);

            return new Extend()
            {
                Netloc = uri.Host,
                Path = uri.LocalPath,
                TokenRaw = tokenRaw,
                Tokens = ParseQueryString(tokenDecode, ";"),
                Params = "",
                Fragment = uri.Fragment,
                Queries = ParseQueryString(uri.Query)
            };
        }

        public static Dictionary<string, string> ParseQueryString(string query, string separator = "&")
        {
            return Regex.Matches(query, "([^?=&]+)(=([^" + separator + "]*))?").Cast<Match>().ToDictionary(x => x.Groups[1].Value.Replace(separator, ""), x => HttpUtility.UrlDecode(x.Groups[3].Value));
        }

        class Extend
        {
            public string Netloc { get; set; }
            public string Path { get; set; }
            public string TokenRaw { get; set; }
            public Dictionary<string, string> Tokens { get; set; }
            public string Params { get; set; }
            public string Fragment { get; set; }
            public Dictionary<string, string> Queries { get; set; }
        }

        /// <summary>
        /// 进度类型
        /// </summary>
        enum ProgressType
        { 
            当前进度,
            总进度
        }

    }
}
