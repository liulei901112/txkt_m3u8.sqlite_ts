using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SQLite sqlite = new SQLite(@"D:\1f7e38e3e1528e53505b152790d04725.m3u8.sqlite");
            // 获取表名
            string[] tableNames = sqlite.GetTablesName();
            // 遍历表名
            foreach (string tableName in tableNames) {
                // 获取数据
                Console.WriteLine(tableName);
                
            }

            List<string[]> allLines = sqlite.GetAllLines("caches");
            foreach(string[] lines in allLines)
            {
                string keyValue = "";
                foreach(string line in lines)
                {
                    Console.Write(line + " ");
                    keyValue += line;
                }
                Console.WriteLine();
                
                
                List<string[]> lineValues = sqlite.GetLines("caches", "value", keyValue);
                foreach(string[] lineValue in lineValues)
                {
                    foreach(string lv in lineValue)
                    {
                        Console.Write(lv + " ");
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}
