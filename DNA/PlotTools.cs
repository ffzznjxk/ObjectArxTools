using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Xml.Linq;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace DotNetARX
{
    /// <summary>
    /// 打印操作类
    /// </summary>
    public static class PlotTools
    {
        /// <summary>
        /// 单一布局打印
        /// </summary>
        /// <param name="engine">打印引擎对象</param>
        /// <param name="layout">要打印的布局</param>
        /// <param name="ps">打印设置</param>
        /// <param name="fileName">打印文件名</param>
        /// <param name="copies">打印份数</param>
        /// <param name="isPreview">是否预览打印</param>
        /// <param name="showProgressDialog">是否显示打印进度框</param>
        /// <param name="plotToFile">是否打印到文件</param>
        /// <returns>返回打印状态</returns>
        public static PreviewEndPlotStatus Plot(this PlotEngine engine, Layout layout, PlotSettings ps, string fileName, int copies, bool isPreview, bool showProgressDialog, bool plotToFile)
        {
            //声明一个打印进度框对象
            PlotProgressDialog plotDlg = null;
            //如果需要显示，则创建打印进度框
            if (showProgressDialog)
            {
                plotDlg = new PlotProgressDialog(isPreview, 1, true);
                //获取去除扩展名后的文件名（不含路径）
                string plotFileName = SymbolUtilityServices.GetSymbolNameFromPathName(fileName, "dwg");
                //在打印进度框中显示的图纸名称：当前打印布局的名称（含文档名）
                plotDlg.set_PlotMsgString(PlotMessageIndex.SheetName, "正在处理图纸：" + layout.LayoutName + "（" + plotFileName + ".dwg）");
            }
            //设置打印的状态为停止
            PreviewEndPlotStatus status = PreviewEndPlotStatus.Cancel;
            //创建打印信息
            PlotInfo pi = new PlotInfo();
            //要打印的布局
            pi.Layout = layout.ObjectId;
            //使用ps中的打印设置
            pi.OverrideSettings = ps;
            //验证打印信息是否有效
            PlotInfoValidator piv = new PlotInfoValidator();
            piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
            piv.Validate(pi);
            //启动打印进度框
            if (showProgressDialog)
                plotDlg.StartPlotProgress(isPreview);
            //开启打印引擎进行打印任务
            engine.BeginPlot(plotDlg, null);
            //开始打印文档
            engine.BeginDocument(pi, layout.Database.GetDocument().Name, null, copies, plotToFile, fileName);
            //启动打印图纸进程
            if (plotDlg != null)
                plotDlg.StartSheetProgress();
            //开始打印页面
            PlotPageInfo ppi = new PlotPageInfo();
            engine.BeginPage(ppi, pi, true, null);
            //开始内容打印
            engine.BeginGenerateGraphics(null);
            //设置打印进度
            if (plotDlg != null)
                plotDlg.SheetProgressPos = 50;
            //结束内容打印
            engine.EndGenerateGraphics(null);
            //结束页面打印
            PreviewEndPlotInfo pepi = new PreviewEndPlotInfo();
            engine.EndPage(pepi);
            //打印预览结束时的状态
            status = pepi.Status;
            //终止显示打印图纸进程
            if (plotDlg != null)
                plotDlg.EndSheetProgress();
            //结束文档打印
            engine.EndDocument(null);
            //终止显示打印进度框
            if (plotDlg != null)
                plotDlg.EndPlotProgress();
            //结束打印任务
            engine.EndPlot(null);
            //打印预览结束时的状态
            return status;
        }

        /// <summary>
        /// 多布局打印
        /// </summary>
        /// <param name="engine">打印引擎对象</param>
        /// <param name="layouts">要打印的布局列表</param>
        /// <param name="ps">打印设置</param>
        /// <param name="fileName">打印文件名</param>
        /// <param name="previewNum">预览的布局号，小于1表示打印</param>
        /// <param name="copies">打印份数</param>
        /// <param name="showProgressDialog">是否显示打印进度框</param>
        /// <param name="plotToFile">是否打印到文件</param>
        /// <returns>返回打印状态</returns>
        public static PreviewEndPlotStatus MPlot(this PlotEngine engine, List<Layout> layouts, PlotSettings ps, string fileName, int previewNum, int copies, bool showProgressDialog, bool plotToFile)
        {
            //表示当前打印的图纸序号
            int numSheet = 1;
            //设置是否为预览
            bool isPreview = previewNum >= 1
                ? true
                : false;
            Document doc = Application.DocumentManager.MdiActiveDocument;
            //声明一个打印进度框对象
            PlotProgressDialog plotDlg = null;
            //如果需要显示打印进度框，则创建
            if (showProgressDialog)
                plotDlg = new PlotProgressDialog(isPreview, layouts.Count, true);
            //设置打印状态为停止
            PreviewEndPlotStatus status = PreviewEndPlotStatus.Cancel;
            //重建一个布局列表，因为打印预览只能是单页的
            List<Layout> layoutList = new List<Layout>();
            //如果为打印预览，则只对预览的布局进行操作
            if (isPreview && previewNum >= 1)
                layoutList.Add(layouts[previewNum - 1]);
            //如果为打印，则需对所有的布局进行操作
            else
                layoutList.AddRange(layouts);
            //遍历布局
            foreach (Layout layout in layoutList)
            {
                //创建打印信息
                PlotInfo pi = new PlotInfo();
                //要打印的布局
                pi.Layout = layout.ObjectId;
                //多文档打印，必须将要打印的布局设置为当前布局
                LayoutManager.Current.CurrentLayout = layout.LayoutName;
                //使用ps中的打印设置
                pi.OverrideSettings = ps;
                //验证打印信息是否有效
                PlotInfoValidator piv = new PlotInfoValidator();
                piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                piv.Validate(pi);
                //如果显示打印进度框
                if (plotDlg != null)
                {
                    //获取去除扩展名后的文件名（不含路径）
                    string plotFileName = SymbolUtilityServices.GetSymbolNameFromPathName(doc.Name, "dwg");
                    //在打印进度框中显示的图纸名称：当前打印布局的名称（含文档名）
                    plotDlg.set_PlotMsgString(PlotMessageIndex.SheetName, plotFileName + "-" + layout.LayoutName);
                }
                //如果打印的是第一张图纸则启动下面的操作，以后就不再需要再次进行
                if (numSheet == 1)
                {
                    //启动打印进度框
                    if (showProgressDialog)
                        plotDlg.StartPlotProgress(isPreview);
                    //开启打印引擎进行打印任务
                    engine.BeginPlot(plotDlg, null);
                    //开始打印文档
                    engine.BeginDocument(pi, doc.Name, null, copies, plotToFile, fileName);
                }
                //启动打印图纸进程
                if (plotDlg != null)
                    plotDlg.StartSheetProgress();
                //开始打印页面
                PlotPageInfo ppi = new PlotPageInfo();
                engine.BeginPage(ppi, pi, (numSheet == layoutList.Count), null);
                //开始打印内容
                engine.BeginGenerateGraphics(null);
                //设置打印进度
                if (plotDlg != null)
                    plotDlg.SheetProgressPos = 50;
                //结束内容打印
                engine.EndGenerateGraphics(null);
                //结束页面打印
                PreviewEndPlotInfo pepi = new PreviewEndPlotInfo();
                engine.EndPage(pepi);
                //打印预览结束时的状态
                status = pepi.Status;
                //终止显示打印图纸进程
                if (plotDlg != null)
                    plotDlg.EndSheetProgress();
                //将要打印的图纸序号设置为下一个
                numSheet++;
                //更新打印进程
                if (plotDlg != null)
                    plotDlg.PlotProgressPos += (100 / layouts.Count);
            }
            //结束文档打印
            engine.EndDocument(null);
            //终止显示打印进度框
            if (plotDlg != null)
                plotDlg.EndPlotProgress();
            //结束打印任务
            engine.EndPlot(null);
            //返回打印预览结束时的状态
            return status;
        }

        /// <summary>
        /// 启动打印图纸进程
        /// </summary>
        /// <param name="plotDlg">打印进度框对象</param>
        public static void StartSheetProgress(this PlotProgressDialog plotDlg)
        {
            //开始时的打印进度
            plotDlg.LowerSheetProgressRange = 0;
            //结束时的打印进度
            plotDlg.UpperSheetProgressRange = 100;
            //当前进度为0，表示开始
            plotDlg.SheetProgressPos = 0;
            //图纸打印开始，进度框开始工作
            plotDlg.OnBeginSheet();
        }

        /// <summary>
        /// 终止打印图纸进程
        /// </summary>
        /// <param name="plotDlg">打印进度框对象</param>
        public static void EndSheetProgress(this PlotProgressDialog plotDlg)
        {
            //当前进度为100，表示结束
            plotDlg.SheetProgressPos = 100;
            //图纸打印结束，进度框停止工作
            plotDlg.OnEndSheet();
        }

        /// <summary>
        /// 启动打印进度框
        /// </summary>
        /// <param name="plotDlg">打印进度框对象</param>
        /// <param name="isPreview">是否为预览</param>
        public static void StartPlotProgress(this PlotProgressDialog plotDlg, bool isPreview)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            //获取去除扩展名后的文件名（不含路径）
            string plotFileName = SymbolUtilityServices.GetSymbolNameFromPathName(doc.Name, "dwg");
            //设置打印进度框中的提示信息
            string dialogTitle = isPreview
                ? "预览作业进度"
                : "打印作业进度";
            plotDlg.set_PlotMsgString(PlotMessageIndex.DialogTitle, dialogTitle);
            plotDlg.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "正在处理图纸：" + plotFileName);
            plotDlg.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "取消打印");
            plotDlg.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "取消文档");
            plotDlg.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "进度：");
            //开始时的打印进度
            plotDlg.LowerPlotProgressRange = 0;
            //结束时的打印进度
            plotDlg.UpperPlotProgressRange = 100;
            //当前进度为0，表示开始
            plotDlg.PlotProgressPos = 0;
            //打印开始，进程框开始工作
            plotDlg.OnBeginPlot();
            //显示打印进度框
            plotDlg.IsVisible = true;
        }

        /// <summary>
        /// 终止打印进度框
        /// </summary>
        /// <param name="plotDlg">打印进度框对象</param>
        public static void EndPlotProgress(this PlotProgressDialog plotDlg)
        {
            //当前进度为100，表示结束
            plotDlg.PlotProgressPos = 100;
            //结束打印
            plotDlg.OnEndPlot();
            //销毁打印进度框
            plotDlg.Dispose();
        }

        /// <summary>
        /// 将打印设备及标准图纸尺寸清单存储到XML文件
        /// </summary>
        /// <param name="fileName">XML文件名</param>
        public static void DeviceMeidaToXML(string fileName)
        {
            //创建一个XML根元素
            XElement xroot = new XElement("Root");
            //获取当前打印设备列表
            PlotSettingsValidator psv = PlotSettingsValidator.Current;
            StringCollection devices = psv.GetPlotDeviceList();
            //创建打印设置对象，以获取设备拥有的图纸尺寸
            PlotSettings ps = new PlotSettings(true);
            //遍历打印设备
            foreach (string device in devices)
            {
                //创建一个名为Device的新元素
                XElement xDevice = new XElement("Device");
                //在Device元素下添加表示设备名称的属性
                xDevice.Add(new XAttribute("Name", device));
                //更新打印设备、图纸尺寸，以反映当前系统状态。
                psv.SetPlotConfigurationName(ps, device, null);
                psv.RefreshLists(ps);
                //获取打印设备的所有可用标准图纸尺寸的名称
                StringCollection medias = psv.GetCanonicalMediaNameList(ps);
                foreach (string media in medias)
                {
                    //如果为用户自定义图纸尺寸，则结束本次循环
                    if (media == "UserDefinedMetric")
                        continue;
                    //创建一个名为Media的新元素
                    XElement xMedia = new XElement("Media");
                    //在Media元素下添加表示图纸尺寸的属性
                    xMedia.Add(new XAttribute("Name", media));
                    //添加Media元素到Device元素中
                    xDevice.Add(xMedia);
                }
                //添加Device元素到根元素中
                xroot.Add(xDevice);
            }
            //保存XML文件
            xroot.Save(fileName);
        }

        /// <summary>
        /// 从XML文件中读取打印设备及标准图纸尺寸清单
        /// </summary>
        /// <param name="fileName">XML文件名</param>
        /// <param name="deviceName">打印设备名</param>
        /// <returns>返回打印设备及其对应的标准图纸尺寸清单</returns>
        public static List<string> MeidasFromXML(string fileName, string deviceName)
        {
            List<string> medias = new List<string>();
            //载入XML文件
            XElement xroot = XElement.Load(fileName);
            var devices = from d in xroot.Elements("Device")
                          where d.Attribute("Name").Value == deviceName
                          select d;
            if (devices.Count() != 1)
                return medias;
            medias = (from m in devices.First().Elements("Media")
                      select m.Attribute("Name").Value).ToList();
            //返回标准图纸尺寸清单的字典对象
            return medias;
        }

        /// <summary>
        /// 从XML文件读取打印设备名列表
        /// </summary>
        /// <param name="fileName">XML文件名</param>
        /// <returns>返回打印设备名列表</returns>
        public static List<string> DevicesFromXML(string fileName)
        {
            //载入XML文件
            XElement xroot = XElement.Load(fileName);
            List<string> devices = (from d in xroot.Elements("Device")
                                    select d.Attribute("Name").Value).ToList();
            return devices;
        }

        /// <summary>
        /// 从其它图形数据库中复制打印设置
        /// </summary>
        /// <param name="destdb">目的图形数据库</param>
        /// <param name="sourceDb">源图形数据库</param>
        /// <param name="plotSettingName">打印设置名称</param>
        public static void CopyPlotSettings(this Database destdb, Database sourceDb, string plotSettingName)
        {
            using (Transaction trSource = sourceDb.TransactionManager.StartTransaction())
            {
                using (Transaction trDest = destdb.TransactionManager.StartTransaction())
                {
                    //获取源图形数据库的打印设置字典
                    DBDictionary dict = trSource.GetObject(sourceDb.PlotSettingsDictionaryId, OpenMode.ForRead) as DBDictionary;
                    if (dict != null && dict.Contains(plotSettingName))
                    {
                        //获取指定名称的打印设置
                        ObjectId settingsId = dict.GetAt(plotSettingName);
                        PlotSettings settings = trSource.GetObject(settingsId, OpenMode.ForRead) as PlotSettings;
                        //新建一个打印设置对象
                        PlotSettings newSettings = new PlotSettings(settings.ModelType);
                        //复制打印设置
                        newSettings.CopyFrom(settings);
                        //将新建的打印设置对象添加到目的数据库的打印设置字典中
                        newSettings.AddToPlotSettingsDictionary(destdb);
                        trDest.AddNewlyCreatedDBObject(newSettings, true);
                    }
                    trDest.Commit();
                }
                trSource.Commit();
            }
        }

        /// <summary>
        /// 复制另一个图形数据库中的所有打印设置
        /// </summary>
        /// <param name="destdb">目的图形数据库</param>
        /// <param name="sourceDb">源图形数据库</param>
        public static void CopyPlotSettings(this Database destdb, Database sourceDb)
        {
            using (Transaction trSource = sourceDb.TransactionManager.StartTransaction())
            {
                using (Transaction trDest = destdb.TransactionManager.StartTransaction())
                {
                    //获取源图形数据库的打印设置字典
                    DBDictionary dict = trSource.GetObject(sourceDb.PlotSettingsDictionaryId, OpenMode.ForRead) as DBDictionary;
                    //对打印设置字典中的条目进行遍历
                    foreach (DBDictionaryEntry entry in dict)
                    {
                        //获取指定名称的打印设置
                        ObjectId settingsId = entry.Value;
                        PlotSettings settings = trSource.GetObject(settingsId, OpenMode.ForRead) as PlotSettings;
                        //新建一个打印设置对象
                        PlotSettings newSettings = new PlotSettings(settings.ModelType);
                        //复制打印设置
                        newSettings.CopyFrom(settings);
                        //将新建的打印设置对象添加到目的数据库的打印设置字典中
                        newSettings.AddToPlotSettingsDictionary(destdb);
                        trDest.AddNewlyCreatedDBObject(newSettings, true);
                    }
                    trDest.Commit();
                }
                trSource.Commit();
            }
        }

        /// <summary>
        /// 打印单张图纸（来自typepad）
        /// </summary>
        [CommandMethod("simplot")]
        public static void simplot()
        {
            //备份系统变量
            short bgPlot = (short)AcadApp.GetSystemVariable("BACKGROUNDPLOT");
            //设置新值，关闭后台打印
            AcadApp.SetSystemVariable("BACKGROUNDPLOT", 0);

            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //打印当前布局
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
                Layout layout = (Layout)tr.GetObject(btr.LayoutId, OpenMode.ForRead);

                //创建【打印设置】（基于布局）
                PlotSettings ps = new PlotSettings(layout.ModelType);
                ps.CopyFrom(layout);

                //创建【打印设置验证】
                PlotSettingsValidator psv = PlotSettingsValidator.Current;
                //设置打印窗口
                Extents2d window = new Extents2d(Point2d.Origin, new Point2d(84100, 59400));
                //按窗口打印
                psv.SetPlotWindowArea(ps, window);
                psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                //按比例缩放
                psv.SetUseStandardScale(ps, true);
                psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);
                //居中
                psv.SetPlotCentered(ps, true);
                //打印到PDF，纸张为A3
                psv.SetPlotConfigurationName(ps, "DWG To PDF.pc3", "ISO_full_bleed_A3_(420.00_x_297.00_MM)");

                //创建【打印信息】
                PlotInfo pi = new PlotInfo();
                //关联布局
                pi.Layout = btr.LayoutId;
                //关联【打印设置】
                pi.OverrideSettings = ps;

                //创建【打印信息验证】
                PlotInfoValidator piv = new PlotInfoValidator();
                piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                piv.Validate(pi);

                //当没有打印任务进行时进行打印
                if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
                    //创建【打印引擎】
                    using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                    //创建【打印对话框】（不预览、1份、显示取消按钮），提供信息
                    using (PlotProgressDialog ppd = new PlotProgressDialog(false, 1, true))
                    {
                        //设置打印进度对话框
                        ppd.set_PlotMsgString(PlotMessageIndex.DialogTitle, "打印进度");
                        ppd.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "图纸进度");
                        ppd.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "图纸集进度");
                        ppd.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "取消图纸");
                        ppd.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "取消任务");
                        //开始打印（设置进度条）
                        ppd.OnBeginPlot();
                        ppd.IsVisible = true;
                        ppd.LowerPlotProgressRange = 0;
                        ppd.UpperPlotProgressRange = 100;
                        ppd.PlotProgressPos = 0;
                        pe.BeginPlot(ppd, null);
                        //开始文档（1份，打印到文件，文件名）
                        pe.BeginDocument(pi, doc.Name, null, 1, true, "c:\\test");
                        //开始图纸（设置进度条）
                        ppd.OnBeginSheet();
                        ppd.LowerSheetProgressRange = 0;
                        ppd.UpperSheetProgressRange = 100;
                        ppd.SheetProgressPos = 0;
                        //开始页面
                        PlotPageInfo ppi = new PlotPageInfo();
                        pe.BeginPage(ppi, pi, true, null);
                        pe.BeginGenerateGraphics(null);
                        pe.EndGenerateGraphics(null);
                        //结束页面
                        pe.EndPage(null);
                        //结束图纸（设置进度条）
                        ppd.SheetProgressPos = 100;
                        ppd.OnEndSheet();
                        //结束文档
                        pe.EndDocument(null);
                        //结束打印（设置进度条）
                        ppd.PlotProgressPos = 100;
                        ppd.OnEndPlot();
                        pe.EndPlot(null);
                    }
                //否则输出提示信息
                else
                    AcadApp.ShowAlertDialog("正在进行另一打印任务！");
                //恢复系统变量
                AcadApp.SetSystemVariable("BACKGROUNDPLOT", bgPlot);
            }
        }
    }
}