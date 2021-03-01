using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DotNetARX
{
    /// <summary>
    /// 配置文件
    /// </summary>
    public class IniClass
    {
        /// <summary>
        /// 定义文件路径
        /// </summary>
        private string iniPath;

        /// <summary>
        /// 写入配置需要的参数
        /// </summary>
        /// <param name="section">段落</param>
        /// <param name="key">键</param>
        /// <param name="val">值</param>
        /// <param name="filePath">ini文件的完整路径和文件名</param>
        /// <returns></returns>
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key,
            string val, string filePath);

        /// <summary>
        /// 读入配置需要的参数
        /// </summary>
        /// <param name="section">段落</param>
        /// <param name="key">键</param>
        /// <param name="def">缺省值</param>
        /// <param name="retVal">读取数值</param>
        /// <param name="size">数值大小</param>
        /// <param name="filePath">ini文件的完整路径和文件名</param>
        /// <returns></returns>
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key,
            string def, StringBuilder retVal, int size, string filePath);

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public IniClass(string filePath)
        {
            string path = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            iniPath = filePath;
        }

        /// <summary>
        /// 写入ini文件
        /// </summary>
        /// <param name="section">段落</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public void Write(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, iniPath);
        }

        /// <summary>
        /// 读取ini文件
        /// </summary>
        /// <param name="section">段落</param>
        /// <param name="key">键</param>
        /// <returns>值</returns>
        public string Read(string section, string key)
        {
            StringBuilder tmp = new StringBuilder(255);
            int i = GetPrivateProfileString(section, key, "", tmp, 255, iniPath);
            return tmp.ToString();
        }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string IniPath
        {
            get { return iniPath; }
            set { iniPath = value; }
        }
    }
}
