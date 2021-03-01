using System.IO;
using System.Net;
using System.Windows.Forms;

namespace DotNetARX
{
    /// <summary>
    /// 文件或文件夹的位置管理类
    /// </summary>
    public static class FileLocation
    {
        /// <summary>
        /// 文件是否存在
        /// </summary>
        /// <param name="file">完整的文件名</param>
        /// <param name="location">文件位置（互联网、局域网或本地）</param>
        /// <returns>返回是否存在</returns>
        public static bool FileExists(this string file, string location)
        {
            if (location == "互联网")
                return PathExistsOnline(file);
            else if (File.Exists(file))
                return true;
            else
            {
                MessageBox.Show("请确认下列文件存在：\n\n" + file, "程序退出", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }
        }

        /// <summary>
        /// 文件夹是否存在
        /// </summary>
        /// <param name="dir">完整的文件夹名</param>
        /// <param name="location">文件夹位置（互联网、局域网或本地）</param>
        /// <returns>返回是否存在</returns>
        public static bool DirectoryExists(this string dir, string location)
        {
            if (location == "互联网")
                return PathExistsOnline(dir);
            else if (Directory.Exists(dir))
                return true;
            else
            {
                MessageBox.Show("请确认下列文件夹存在：\n\n" + dir, "程序退出", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }
        }

        /// <summary>
        /// 网络路径是否存在
        /// </summary>
        /// <param name="path">网络路径</param>
        /// <returns>返回是否存在</returns>
        public static bool PathExistsOnline(string path)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(path);
                request.Method = "HEAD";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    response.Close();
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}
