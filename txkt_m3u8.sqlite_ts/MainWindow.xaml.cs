using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using txkt_m3u8.sqlite_ts.ViewModel;

namespace txkt_m3u8.sqlite_ts
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 数据模型
        /// </summary>
        private MainViewModel _viewModel = new MainViewModel();

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = _viewModel;
        }

        /// <summary>
        /// 窗口初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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
                _viewModel.TargetFolder = openFileDialog.SelectedPath;
                log.Info("选定目标文件夹" + _viewModel.TargetFolder);
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
                _viewModel.SourceFolder = openFileDialog.SelectedPath;
                log.Info("选定源文件夹" + _viewModel.SourceFolder);
            }
        }

        /// <summary>
        /// 开始转换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BeginConvert_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_viewModel.SourceFolder))
            {
                log.Error("请输入或选择源文件夹");
                MessageCore.ShowWarning("请输入或选择源文件夹");
                return;
            }
            if (!Directory.Exists(_viewModel.SourceFolder))
            {
                log.Error("源文件夹不存在 => " + _viewModel.SourceFolder);
                MessageCore.ShowWarning("源文件夹不存在");
                return;
            }

            if (string.IsNullOrEmpty(_viewModel.TargetFolder))
            {
                log.Error("请输入或选择目标文件夹");
                MessageCore.ShowWarning("请输入或选择目标文件夹");
                return;
            }

            // 获取文件列表
            string[] filePaths = Directory.GetFiles(_viewModel.SourceFolder, "*.m3u8.sqlite", SearchOption.AllDirectories);
            if (null == filePaths || filePaths.Length <= 0)
            {
                log.Error("没有找到需要解码的文件（*.m3u8.sqlite）");
                MessageCore.ShowError("没有找到需要解码的文件（*.m3u8.sqlite）");
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
                        log.Error("文件损坏 => " + filePath + "; " + ex.Message, ex);
                        MessageCore.ShowError("文件损坏 => " + filePath);
                    }
                    index++;
                }
            });
        }

        /// <summary>
        /// 获取一个ts
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="uin"></param>
        /// <param name="termId"></param>
        private void FetchOneTs(string filePath, string uin, string termId)
        {
            string fileName = Path.GetFileName(filePath);

            DateTime beginTime = DateTime.Now;
            using (SQLite sqlite = new SQLite(filePath))
            {
                string msg = "读取caches => " + filePath;
                ShowStatus(msg);
                log.Info(msg);
                string cachesTableName = "caches";
                int total = sqlite.GetRowsCount(cachesTableName);
                int index = 1;

                RefreshProgress(ProgressType.当前进度, 0, total * 2);

                List<long[]> tsIndex = new List<long[]>();
                List<string> aesKeys = new List<string>();

                int pageSize = 100;
                int totalPage = (total + pageSize - 1) / pageSize;
                

                for (int i = 1; i <= totalPage; i++)
                {
                    string pageLimit = string.Format("LIMIT {0}, {1}", (i - 1) * pageSize, pageSize);
                    List<object[]> caches = sqlite.GetRows(cachesTableName, "*", pageLimit);
                    foreach (object[] cache in caches)
                    {
                        try
                        {
                            string key = cache[0].ToString();
                            object value = cache[1];

                            Extend keyExtend = ExtractFromMetadata(key);
                            string[] keyQueriesExtendKeys = keyExtend.Queries.Keys.ToArray();

                            if (Encoding.UTF8.GetString(value as byte[]).Contains("#EXTM3U"))
                            { }
                            else if (keyQueriesExtendKeys.Contains("edk"))
                            {
                                aesKeys.Add(value.ToString());
                                string hex = ToHexString(value as byte[]);
                                log.Info(string.Format("[KEY]：{0}, length：{1}", hex, hex.Length));
                            }
                            else
                            {
                                long start = long.Parse(keyExtend.Queries["start"]);
                                long end = long.Parse(keyExtend.Queries["end"]);
                                tsIndex.Add(new long[] { index - 1, start, end });
                            }

                            RefreshProgress(ProgressType.当前进度, index, total * 2);
                            msg = string.Format("解码进度 [{0} / {1}] => {2}", index, total, fileName);
                            ShowStatus(msg);
                            log.Info(msg);
                        }
                        finally
                        {
                            Interlocked.Increment(ref index);
                        }
                    }
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }          

                // 重新排序
                long matchTime = -1;
                List<long[]> orderedTsIndex = new List<long[]>();
                for (int x = 0; x < tsIndex.Count; x++)
                {
                    for (int y = 0; y < tsIndex.Count; y++)
                    {
                        if (tsIndex[y][1] == matchTime + 1)
                        {
                            orderedTsIndex.Add(tsIndex[y]);
                            matchTime = tsIndex[y][2];
                        }
                    }
                }
                msg = string.Format("重新排序 => " + fileName);
                ShowStatus(msg);

                // 保存文件
                string sourceFolder = _viewModel.SourceFolder.Trim();
                string targetFolder = _viewModel.TargetFolder.Trim();
                if (".".Equals(targetFolder))
                {
                    targetFolder = sourceFolder;
                }
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }
                string targetFileName = fileName.Replace(".m3u8.sqlite", ".ts");
                string targetFilePath = Path.Combine(targetFolder, targetFileName);

                using (FileStream fs = new FileStream(targetFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    int orderIndex = 0;
                    int orderTotal = orderedTsIndex.Count;

                    List<object[]> cacheOne;
                    string limit;
                    object raw;
                    byte[] chip;
                    // 合并片段
                    foreach (long[] tsOne in orderedTsIndex)
                    {
                        limit = string.Format("LIMIT {0}, {1}", tsOne[0], 1);
                        cacheOne = sqlite.GetRows(cachesTableName, "*", limit);
                        raw = cacheOne[0][1];
                        chip = AESDecrypt(raw as byte[], aesKeys[0]);
                        fs.Write(chip, 0, chip.Length);
                        orderIndex += 1;

                        RefreshProgress(ProgressType.当前进度, index, total + orderTotal);
                        msg = string.Format("合并进度 [{0} / {1}] => {2}", orderIndex, orderTotal, fileName);
                        ShowStatus(msg);
                        log.Info(msg);
                        index++;
                    }
                }

                /*List<object[]> cacheOne;
                string limit;
                object raw;
                byte[] chip;
                // 合并片段
                foreach (long[] tsOne in orderedTsIndex)
                {
                    limit = string.Format("LIMIT {0}, {1}", tsOne[0], 1);
                    cacheOne = sqlite.GetRows(cachesTableName, "*", limit);
                    raw = cacheOne[0][1];
                    chip= AESDecrypt(raw as byte[], aesKeys[0]);
                    plain.AddRange(chip);
                    orderIndex += 1;

                    RefreshProgress(ProgressType.当前进度, index, total + orderTotal);
                    msg = string.Format("合并进度 [{0} / {1}] => {2}", orderIndex, orderTotal, fileName);
                    ShowStatus(msg);
                    log.Info(msg);
                    index++;
                }

                // 保存文件
                string sourceFolder = SourceFolder.Text.Trim();
                string targetFolder = TargetFolder.Text.Trim();
                if (".".Equals(targetFolder))
                {
                    targetFolder = sourceFolder;
                }
                if (Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }
                string targetFileName = fileName.Replace(".m3u8.sqlite", ".ts");
                string targetFilePath = Path.Combine(targetFolder, targetFileName);

                using (FileStream fs = new FileStream(targetFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    fs.Write(plain.ToArray(), 0, plain.Count);
                }*/

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            log.Info(fileName + "文件解码完成，耗时：" + (DateTime.Now - beginTime).TotalMilliseconds + "ms");
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
                List<object[]> metadatas = sqlite.GetRows("metadata", "value");
                if (null == metadatas || metadatas.Count <= 0)
                {
                    log.Error("metadata 不存在 => " + filePath);
                    return null;
                }
                string metadata = metadatas[0][0].ToString();
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
            string tokenRaw = "";
            Dictionary<string, string> tokens = null;
            Match matchToken = new Regex(@"token\.([\S]+)\.v").Match(path);
            if (matchToken.Success)
            {
                tokenRaw = matchToken.Value;
                tokenRaw = tokenRaw.Substring(6);
                tokenRaw = tokenRaw.Substring(0, tokenRaw.Length - 2);
                string tokenDecode = Base64Decode(tokenRaw);
                tokens = ParseQueryString(tokenDecode, ";");
            }           

            return new Extend()
            {
                Netloc = uri.Host,
                Path = uri.LocalPath,
                TokenRaw = tokenRaw,
                Tokens = tokens,
                Params = "",
                Fragment = uri.Fragment,
                Queries = ParseQueryString(uri.Query)
            };
        }

        public string ToHexString(byte[] bytes)
        {
            string hexString = string.Empty;
            if (bytes != null)
            {
                StringBuilder strB = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    strB.Append(" " + bytes[i].ToString("X2"));
                }
                hexString = strB.ToString();

            }
            return hexString;
        }

        /// <summary>
        /// 查询参数解析为词典
        /// </summary>
        /// <param name="query"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        private Dictionary<string, string> ParseQueryString(string query, string separator = "&")
        {
            return Regex.Matches(query, "([^?=&]+)(=([^" + separator + "]*))?").Cast<Match>().ToDictionary(x => x.Groups[1].Value.Replace(separator, ""), x => HttpUtility.UrlDecode(x.Groups[3].Value));
        }


        /// <summary>
        /// 扩展信息
        /// </summary>
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

        #region UI更新封装

        /// <summary>
        /// 进度类型
        /// </summary>
        enum ProgressType
        {
            当前进度,
            总进度
        }
        /// <summary>
        /// 刷新进度
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="value"></param>
        /// <param name="total"></param>
        private void RefreshProgress(ProgressType pt, float value, float total)
        {
            switch (pt)
            {
                case ProgressType.当前进度:
                    _viewModel.CurrentMax = total;
                    _viewModel.CurrentProgress = value;
                    _viewModel.CurrentPercent = String.Format("{0}%", (value / total * 100).ToString("0.00"));
                    break;
                case ProgressType.总进度:
                    _viewModel.TotalMax = total;
                    _viewModel.TotalProgress = value;
                    _viewModel.TotalPercent = String.Format("{0} / {1}", value, total);
                    break;
            }
        }

        /// <summary>
        /// ShowStatus
        /// </summary>
        /// <param name="logtxt"></param>
        private void ShowStatus(string text)
        {
            _viewModel.WorkStatus = text;
        }
        #endregion

        #region MessageCore
        /// <summary>
        /// 消息盒子
        /// </summary>
        public class MessageCore
        {
            /// <summary>
            /// Rubyer.Message参数containerIdentifier
            /// </summary>
            private const string MESSAGE_CONTAINER = "MessageContainers";
            /// <summary>
            /// Rubyer.MessageBoxR参数containerIdentifier
            /// </summary>
            private const string CONFIRM_CONTAINER = "ConfirmContainers";

            /// <summary>
            /// 警告
            /// </summary>
            /// <param name="message"></param>
            public static void ShowWarning(string message)
            {
                Rubyer.Message.ShowWarning(MESSAGE_CONTAINER, message);
            }

            /// <summary>
            /// 成功
            /// </summary>
            /// <param name="message"></param>
            public static void ShowSuccess(string message)
            {
                Rubyer.Message.ShowSuccess(MESSAGE_CONTAINER, message);
            }

            /// <summary>
            /// 错误
            /// </summary>
            /// <param name="message"></param>
            public static void ShowError(string message)
            {
                Rubyer.Message.ShowError(MESSAGE_CONTAINER, message);
            }

            /// <summary>
            /// 信息
            /// </summary>
            /// <param name="message"></param>
            public static void ShowInfo(string message)
            {
                Rubyer.Message.ShowInfo(MESSAGE_CONTAINER, message);
            }

            /// <summary>
            /// 确认提示
            /// </summary>
            /// <param name="message"></param>
            /// <param name="title"></param>
            /// <param name="button"></param>
            /// <returns></returns>
            public static Task<MessageBoxResult> Confirm(string message, string title, MessageBoxButton button = MessageBoxButton.OKCancel)
            {
                return Rubyer.MessageBoxR.ConfirmInContainer(CONFIRM_CONTAINER, message, title, button);
            }
        }
        #endregion

        #region Base64
        /// <summary>
        /// Base64解码
        /// </summary>
        /// <param name="base64"></param>
        /// <returns></returns>
        private string Base64Decode(string base64)
        {
            return Base64Decode(base64, Encoding.UTF8);
        }

        /// <summary>
        /// Base64解码
        /// </summary>
        /// <param name="base64"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        private string Base64Decode(string base64, Encoding encoding)
        {
            string dummyData = base64.Trim().Replace("%", "").Replace(",", "").Replace(" ", "+");
            if (dummyData.Length % 4 > 0)
            {
                dummyData = dummyData.PadRight(dummyData.Length + 4 - dummyData.Length % 4, '=');
            }
            string text = string.Empty;
            byte[] bytes = Convert.FromBase64String(dummyData);
            try
            {
                text = encoding.GetString(bytes);
            }
            catch
            {
                text = base64;
            }
            return text;
        }
        #endregion

        #region AES
        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="raw">被解密的密文</param>
        /// <param name="key">密钥</param>
        /// <param name="iv">向量</param>
        /// <returns>明文</returns>
        private byte[] AESDecrypt(byte[] raw, string key, string iv = "0000000000000000")
        {
            byte[] bkey = new byte[32];
            Array.Copy(Encoding.UTF8.GetBytes(key.PadRight(bkey.Length)), bkey, bkey.Length);
            byte[] biv = Encoding.UTF8.GetBytes(iv);
            
            RijndaelManaged aes = new RijndaelManaged()
            {
                Key = bkey,
                IV = biv,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.Zeros,
                BlockSize = 128
            };
            ICryptoTransform cTransform = aes.CreateDecryptor();
            byte[] rs = cTransform.TransformFinalBlock(raw, 0, raw.Length);
            return rs;
        }
        #endregion

        #region SQLite
        class SQLite : IDisposable
        {
            private SQLiteConnection _connection;
            private SQLiteDataReader _reader;
            private SQLiteCommand _command;
            private string queryString;
            public SQLite(string path)
            {
                if (File.Exists(path))
                {
                    _connection = new SQLiteConnection("data source = " + path);
                    _connection.Open();
                }
                else
                {
                    throw new Exception("db数据库不存在:" + path);
                }
            }
            // region IsExists        
            /// <summary>        
            /// 是否存在数据库        
            /// </summary>        
            /// <param name="path">目录</param>        
            /// <returns></returns>        
            public static bool IsExistsDateBase(string path)
            {
                if (Path.GetExtension(path).Equals(".sqlite"))
                {
                    throw new SQLiteException("不是.sqlite后缀,你查什么!!!");
                }
                return File.Exists(path);
            }
            /// <summary>
            /// 表中是否存在字段
            /// </summary>
            /// <param name="tableName">表名</param>
            /// <param name="key">区分大小写</param>
            /// <returns></returns>        
            public bool IsExisTableFile(string tableName, string key)
            {
                bool isExis = false;
                foreach (var item in GetFiles(tableName))
                {
                    {
                        isExis = true;
                        break;
                    }
                }
                return isExis;
            }
            /// <summary>
            /// 查询表是否存在
            /// </summary>
            /// <param name="tableName">表名</param>
            /// <returns></returns>        
            public bool IsExistsTable(string tableName)
            {
                queryString = "SELECT name FROM sqlite_master WHERE name='" + tableName + "'";
                ExecuteQuery();
                if (!_reader.Read())
                {
                    return false;
                }
                _reader.Close();
                return true;
            }
            // endregion                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           // region Create
            /// <summary>
            /// 创建数据库
            /// </summary>
            /// <param name="path">目录</param>        
            public static void CreateDateBase(string path)
            {
                if (Path.GetExtension(path).Equals(".sqlite"))
                {
                    SQLiteConnection.CreateFile(path);
                }
                else
                {
                    throw new Exception("要创建数据库，则文件后缀必须为.sqlite");
                }
            }
            // endregion        
            // region Delete
            /// <summary>
            /// 删除一张表
            /// </summary>
            /// <param name="tableName">表名</param>
            /// <returns>是否成功删除</returns>        
            public bool DeleteTable(string tableName)
            {
                if (IsExistsTable(tableName))
                {
                    queryString = "DROP TABLE " + tableName;
                    ExecuteQuery();
                }
                if (IsExistsTable(tableName))
                {
                    return false;
                }
                return true;
            }
            /// <summary>
            /// 删除一行数据
            /// </summary>
            /// <param name="tableName">表名</param>
            /// <param name="key">key</param>
            /// <param name="value">值</param>        
            public void DeleteLine(string tableName, string key, string value)
            {
                queryString = "DELETE FROM " + tableName + " WHERE " + key + " = " + "'" + value + "'";
                ExecuteQuery();
            }
            /// <summary>
            /// 删除数据库
            /// </summary>
            /// <param name="path"></param>        
            public void DeleteDateBase(string path)
            {
                if (File.Exists(path))
                {
                    if (Path.GetExtension(path).Equals(".sqlite"))
                    {
                        File.Delete(path);
                    }
                    else
                    {
                        throw new FileNotFoundException("让你删除数据库文件，删啥呢");
                    }
                }
            }
            // endregion         // region Get
            /// <summary>
            /// 表中多少列
            /// </summary>
            /// <param name="tableName">表名</param>
            /// <returns>列数</returns>        
            public int GetRowsCount(string tableName)
            {
                queryString = "SELECT count(*) FROM " + tableName;
                ExecuteQuery();
                _reader.Read();
                return _reader.GetInt32(0);
            }
            /// <summary>
            /// 获取数据库中表数量
            /// </summary>
            /// <returns>返回数据库中表数量</returns>        
            public int GetTablesCount()
            {
                return GetTablesName().Length;
            }
            /// <summary>
            /// 获取表中字段都有那些
            /// </summary>
            /// <param name="tableName">表名</param>
            /// <returns>字段数组</returns>        
            public string[] GetFiles(string tableName)
            {
                queryString = "Pragma Table_Info(" + tableName + ")";
                ExecuteQuery();
                List<string> tablesFiles = new List<string>();
                while (_reader.Read())
                {
                    tablesFiles.Add(_reader["Name"].ToString());
                }
                return tablesFiles.ToArray();
            }
            /// <summary>
            /// 获取数据库中所有表名
            /// </summary>
            /// <returns>所有表名数组</returns>        
            public string[] GetTablesName()
            {
                queryString = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
                ExecuteQuery();
                List<string> tablesName = new List<string>();
                while (_reader.Read())
                {
                    // 表名
                    tablesName.Add(_reader["Name"].ToString());
                }
                return tablesName.ToArray();
            }
            /// <summary>
            /// 获取表中一列
            /// </summary>
            /// <param name="tableName">表名</param>
            /// <param name="fieldName">字段名</param>
            /// <param name="limit">分页limit（例如：limit 0,500）</param>
            /// <returns>列数组</returns>        
            public List<object[]> GetRows(string tableName, string fieldName, string limit = "")
            {
                if (!IsExistsTable(tableName))
                {
                    throw new FileNotFoundException("表不存在:" + tableName);
                }
                if (!IsExisTableFile(tableName, fieldName))
                {
                    throw new Exception("表中不存在字段:" + fieldName);
                }
                queryString = string.Format("SELECT {0} FROM {1} {2}", fieldName, tableName, limit);

                SQLiteDataAdapter sda = new SQLiteDataAdapter(queryString, _connection);
                DataTable dt = new DataTable();
                sda.Fill(dt);
                List<object[]> rows = new List<object[]>();
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    rows.Add(dt.Rows[i].ItemArray);
                }
                sda.Dispose();
                dt.Dispose();
                return rows;
            }
            /// <summary>
            /// 获取行数据，可能会有同名字段，会返回多条数据
            /// </summary>
            /// <param name="tableName">表名</param>
            /// <param name="key"></param>
            /// <param name="keyValue"></param>
            /// <returns></returns>        
            public List<string[]> GetLines(string tableName, string key, string keyValue)
            {
                queryString = string.Format("SELECT * FROM {0} WHERE {1} = '{2}'", tableName, key, keyValue);
                ExecuteQuery();
                string[] array = new string[_reader.FieldCount];
                List<string[]> result = new List<string[]>();
                while (_reader.Read())
                {
                    for (int i = 0; i < _reader.FieldCount - 1; i++)
                    {
                        array[i] = Convert.ToString(_reader[i]);
                    }
                    result.Add(array);
                }
                return result;
            }
            public List<object[]> GetAllLines(string tableName)
            {
                queryString = string.Format("SELECT * FROM {0}", tableName);
                ExecuteQuery();
                object[] array = new object[_reader.FieldCount];
                List<object[]> result = new List<object[]>();
                while (_reader.Read())
                {
                    for (int i = 0; i < _reader.FieldCount - 1; i++)
                    {
                        array[i] = _reader[i];
                    }
                    result.Add(array);
                }
                return result;
            }
            // endregion         
            // region Update
            /// <summary>
            /// 更新某一行的，某一个值
            /// </summary>
            /// <param name="tableName"></param>
            /// <param name="key"></param>
            /// <param name="keyVale"></param>
            /// <param name="updateKey">更新key</param>
            /// <param name="updateValue"></param>        
            public void UpdatePropety(string tableName, string key, string keyVale, string updateKey, string updateValue)
            {
                queryString = string.Format("UPDATE {0} SET {1} = '{2}' WHERE {3} = '{4}'", tableName, updateKey, updateValue, key, keyVale);
                ExecuteQuery();
            }
            /// <summary>
            /// 列数据为同一个
            /// </summary>
            /// <param name="tableName"></param>
            /// <param name="key"></param>
            /// <param name="value"></param>        
            public bool UpdateColumns(string tableName, string updateKey, string UpdateValue)
            {
                if (!IsExistsTable(tableName))
                {
                    throw new FileNotFoundException("没有表: " + tableName);
                }
                if (!IsExisTableFile(tableName, updateKey))
                {
                    throw new Exception("没有关键key:" + updateKey);
                }
                queryString = "UPDATE " + tableName + " SET " + updateKey + " = '" + UpdateValue + "'";
                ExecuteQuery();
                foreach (var item in GetRows(tableName, updateKey))
                {
                    if (!item.Equals(UpdateValue))
                    {
                        return false;
                    }
                }
                return true;
            }
            // endregion         // region Insert
            /// <summary>
            /// 表中插入一列
            /// </summary>
            /// <param name="tableName">表名</param>
            /// <param name="key">key</param>
            /// <returns>是否操作成功</returns>        
            public bool InsertRows(string tableName, string key, SQLiteType sQLiteType)
            {
                if (IsExistsTable(tableName))
                {
                    if (IsExisTableFile(tableName, key))
                    {
                        throw new Exception("key值已存在,不可重复添加 :" + key);
                    }
                    else
                    {
                        queryString = "ALTER TABLE " + tableName + " ADD COLUMN " + key + " " + sQLiteType.ToString();
                        ExecuteQuery();
                    }
                }
                else
                {
                    throw new NullReferenceException("不存在表名:" + tableName);
                }
                if (IsExisTableFile(tableName, key))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            // endregion         // region Rename
            /// <summary>
            /// 重命名表名
            /// </summary>
            /// <param name="tableName">要修改的表名</param>
            /// <param name="newName">新表名</param>
            /// <returns>是否重命名成功</returns>        
            public bool RenameTable(string tableName, string newName)
            {
                if (IsExistsTable(tableName))
                {
                    queryString = "ALTER TABLE " + tableName + " RENAME TO " + newName;
                    ExecuteQuery();
                }
                else
                {
                    throw new FileNotFoundException("无法重命名不存在的表:" + tableName);
                }
                if (IsExistsTable(newName))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            /// <summary>
            /// 重命名字段
            /// </summary>
            /// <param name="tableName"></param>
            /// <param name="fileName"></param>
            /// <param name="newFileName"></param>
            /// <returns></returns>        
            public bool RenameTableFile(string tableName, string fileName, string newFileName)
            {
                if (!IsExistsTable(tableName))
                {
                    throw new FileNotFoundException("表不存在:" + tableName);
                }
                return false;
            }
            // endregion         
            // region Todo        
            //创建表        
            //插入行        
            //删除列         
            //获取第N行、第一行、最后一行数据                
            public void CreateTables(string tables)
            {
            }
            public void InsertLines(string tablesName, string[] values)
            {
            }
            /// <summary>
            /// 删除列
            /// </summary>
            /// <param name="key"></param>        
            public void DeleteRows(string key)
            {
            }
            // endregion        
            public void Dispose()
            {
                _connection?.Close();
                _connection?.Dispose();
                _reader?.Dispose();
                _command?.Dispose();
            }
            private void ExecuteQuery()
            {
                _command = _connection.CreateCommand();
                _command.CommandText = queryString;
                _reader = _command.ExecuteReader();
            }
            public enum SQLiteType
            {
                blob, integer, varchar, text,
            }
        }
        #endregion
    }
}
