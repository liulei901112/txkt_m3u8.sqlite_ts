using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace txkt_m3u8.sqlite_ts
{
    /// <summary>
    /// Base64 相关操作
    /// </summary>
    public class Base64
    {
        /// <summary>
        /// 解码
        /// </summary>
        /// <param name="base64"></param>
        /// <returns></returns>
        public static string Decode(string base64)
        {
            return Decode(base64, Encoding.UTF8);
        }

        /// <summary>
        /// 解码
        /// </summary>
        /// <param name="base64"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string Decode(string base64, Encoding encoding)
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

        /// <summary>
        /// 编码
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string Encode(string source)
        {
            return Encode(source, Encoding.UTF8);
        }

        /// <summary>
        /// 编码
        /// </summary>
        /// <param name="source"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string Encode(string source, Encoding encoding)
        {
            string text = string.Empty;
            byte[] bytes = encoding.GetBytes(source);
            try
            {
                text = Convert.ToBase64String(bytes);
            }
            catch
            {
                text = source;
            }
            return text;
        }
    }
}
