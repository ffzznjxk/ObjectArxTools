using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Customization;
using Autodesk.AutoCAD.EditorInput;
using System.IO;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace DotNetARX
{
    /// <summary>
    /// 加载CUI类
    /// </summary>
    public static class CuiLoader
    {
        /// <summary>
        /// 加载cuix文件
        /// </summary>
        /// <param name="cuiFile">cuix文件名</param>
        public static void LoadPartialCuix(string cuiFile)
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            if (!File.Exists(cuiFile))
            {
                ed.WriteMessage($"\n文件：{cuiFile} 不存在。");
                return;
            }

            //主cuix
            string cuix = AcadApp.GetSystemVariable("MENUNAME") + ".cuix";
            CustomizationSection cs = new CustomizationSection(cuix);
            //若不包含则进行加载
            if (!cs.PartialCuiFiles.Contains(cuiFile))
            {
                //备份系统变量
                object bCmdEcho = AcadApp.GetSystemVariable("CMDECHO");
                object bFileDia = AcadApp.GetSystemVariable("FILEDIA");
                //设置新的系统变量
                AcadApp.SetSystemVariable("CMDECHO", 0);
                AcadApp.SetSystemVariable("FILEDIA", 0);
                try
                {
                    //加载
                    doc.SendStringToExecute("_.cuiload \"" + cuiFile + "\" ", false, false, false);
                    //刷新
                    doc.SendStringToExecute("_.wscurrent \n", false, false, false);
                }
                catch
                {
                    ed.WriteMessage($"\n文件：{cuiFile} 自定义组加载失败。");
                }
                finally
                {
                    //还原系统变量
                    doc.SendStringToExecute("(setvar 'FILEDIA' " + bFileDia.ToString() + ")(princ) ", false, false, false);
                    doc.SendStringToExecute("(setvar 'CMDECHO' " + bCmdEcho.ToString() + ")(princ) ", false, false, false);
                }
            }
        }

        /// <summary>
        /// 卸载cuix文件
        /// </summary>
        /// <param name="cuiFile">cuix文件名</param>
        public static void UnLoadPartialCuix(string cuiFile)
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            if (!File.Exists(cuiFile))
            {
                ed.WriteMessage($"\n文件：{cuiFile} 不存在。");
                return;
            }
            //主cuix
            string cuix = AcadApp.GetSystemVariable("MENUNAME") + ".cuix";
            CustomizationSection cs = new CustomizationSection(cuix);
            //若不包含则进行加载
            if (cs.PartialCuiFiles.Contains(cuiFile))
            {
                //备份系统变量
                object bCmdEcho = AcadApp.GetSystemVariable("CMDECHO");
                object bFileDia = AcadApp.GetSystemVariable("FILEDIA");
                //设置新的系统变量
                AcadApp.SetSystemVariable("CMDECHO", 0);
                AcadApp.SetSystemVariable("FILEDIA", 0);
                try
                {
                    //加载
                    doc.SendStringToExecute("_.cuiunload \"" + Path.GetFileNameWithoutExtension(cuiFile) + "\" ", false, false, false);
                    //刷新
                    doc.SendStringToExecute("_.wscurrent \n", false, false, false);
                }
                catch
                {
                    ed.WriteMessage($"\n文件：{cuiFile} 自定义组卸载失败。");
                }
                finally
                {
                    //还原系统变量
                    doc.SendStringToExecute("(setvar 'FILEDIA' " + bFileDia.ToString() + ")(princ) ", false, false, false);
                    doc.SendStringToExecute("(setvar 'CMDECHO' " + bCmdEcho.ToString() + ")(princ) ", false, false, false);
                }
            }
        }
    }
}
