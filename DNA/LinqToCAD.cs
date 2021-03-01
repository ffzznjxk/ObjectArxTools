using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using System.Linq;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace DotNetARX
{
    /// <summary>
    /// LINQ操作类
    /// </summary>
    public static partial class LinqToCAD
    {
        /// <summary>
        /// 创建块参照过滤器
        /// </summary>
        /// <param name="blocks">块名列表</param>
        /// <returns>返回块名列表中的块参照</returns>
        public static SelectionFilter BlockFilters(List<string> blocks)
        {
            //构建列表
            List<TypedValue> tv = new List<TypedValue> { (new TypedValue((int)DxfCode.Start, "Insert")) };
            //块名部分开始
            tv.Add(new TypedValue((int)DxfCode.Operator, "<or"));
            //增加块名到列表中
            foreach (string b in blocks)
                tv.Add(new TypedValue((int)DxfCode.BlockName, b));
            //块名部分结束
            tv.Add(new TypedValue((int)DxfCode.Operator, "or>"));
            //返回过滤器
            return new SelectionFilter(tv.ToArray());
        }

        /// <summary>
        /// 创建块参照过滤器
        /// </summary>
        /// <param name="blocks">块名</param>
        /// <returns>返回块名列表中的块参照</returns>
        public static SelectionFilter BlockFilters(params string[] blocks)
        {
            return BlockFilters(blocks.ToList());
        }

        /// <summary>
        /// 获取用户选择的指定名称列表中的块参照
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="blocks">块名列表</param>
        /// <returns>返回指定名称列表中的块参照</returns>
        public static List<BlockReference> GetBlocks(this Database db, List<string> blocks)
        {
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            var trans = db.TransactionManager;
            List<BlockReference> blockrefs = new List<BlockReference>();
            var filter = BlockFilters(blocks);
            var psr = ed.GetSelection(filter);
            if (psr.Status == PromptStatus.OK)
                foreach (var id in psr.Value.GetObjectIds())
                {
                    BlockReference b = (BlockReference)trans.GetObject(id, OpenMode.ForRead, false);
                    blockrefs.Add(b);
                }
            return blockrefs;
        }

        /// <summary>
        /// 获取数据库中所有的实体
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="mode">打开方式</param>
        /// <param name="openErased">打开已删除的实体</param>
        /// <param name="openLocked">打开锁定的图层</param>
        /// <returns>返回数据库中所有的实体</returns>
        public static List<Entity> GetEntsInDatabase(this Database db, OpenMode mode, bool openErased, bool openLocked = false)
        {
            var trans = db.TransactionManager;
            //声明一个List类的变量，用于返回所有实体
            List<Entity> ents = new List<Entity>();
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            var psr = ed.SelectAll();
            if (psr.Status == PromptStatus.OK)
                //循环遍历符合条件的实体
                foreach (var id in psr.Value.GetObjectIds())
                {
                    Entity ent = (Entity)trans.GetObject(id, mode, openErased, openLocked);
                    ents.Add(ent);
                }
            return ents;
        }

        /// <summary>
        /// 获取数据库中所有的实体
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <returns>返回数据库中所有的实体</returns>
        public static List<Entity> GetEntsInDatabase(this Database db)
        {
            return GetEntsInDatabase(db, OpenMode.ForRead, false);
        }

        /// <summary>
        ///  获取数据库中类型为T的所有实体
        /// </summary>
        /// <typeparam name="T">实体的类型</typeparam>
        /// <param name="db">数据库对象</param>
        /// <param name="mode">打开方式</param>
        /// <param name="openErased">打开已删除的实体</param>
        /// <param name="openLocked">打开锁定的图层</param>
        /// <returns>返回数据库中类型为T的实体</returns>
        public static List<T> GetEntsInDatabase<T>(this Database db, OpenMode mode, bool openErased, bool openLocked = false) where T : Entity
        {
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            //声明一个List类的变量，用于返回类型为T为的实体列表
            List<T> ents = new List<T>();
            //获取类型T代表的DXF代码名，用于构建选择集过滤器
            string dxfname = RXClass.GetClass(typeof(T)).DxfName;
            //构建选择集过滤器
            TypedValue[] values = { new TypedValue((int)DxfCode.Start, dxfname) };
            SelectionFilter filter = new SelectionFilter(values);
            //选择符合条件的所有实体
            PromptSelectionResult psr = ed.SelectAll(filter);
            if (psr.Status == PromptStatus.OK)
                //循环遍历符合条件的实体
                foreach (var id in psr.Value.GetObjectIds())
                {
                    //将实体强制转化为T类型的对象
                    T t = (T)id.GetObject(mode, openErased, openLocked);
                    //将实体添加到返回列表中
                    ents.Add(t);
                }
            return ents;
        }

        /// <summary>
        /// 获取数据库中类型为T的所有实体（对象打开为读）
        /// </summary>
        /// <typeparam name="T">实体的类型</typeparam>
        /// <param name="db">数据库对象</param>
        /// <returns>返回数据库中类型为T的实体</returns>
        public static List<T> GetEntsInDatabase<T>(this Database db) where T : Entity
        {
            return GetEntsInDatabase<T>(db, OpenMode.ForRead, false);
        }

        /// <summary>
        /// 获取用户选择的实体
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="mode">打开方式</param>
        /// <param name="openErased">打开已删除的实体</param>
        /// <returns>返回用户选择的实体</returns>
        public static List<Entity> GetSelection(this Database db, OpenMode mode, bool openErased)
        {
            var trans = db.TransactionManager;
            List<Entity> ents = new List<Entity>();
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            var psr = ed.GetSelection();
            if (psr.Status == PromptStatus.OK)
                foreach (var id in psr.Value.GetObjectIds())
                {
                    Entity ent = (Entity)trans.GetObject(id, mode, openErased);
                    ents.Add(ent);
                }
            return ents;
        }

        /// <summary>
        /// 获取用户选择的实体
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <returns>返回用户选择的实体</returns>
        public static List<Entity> GetSelection(this Database db)
        {
            return GetSelection(db, OpenMode.ForRead, false);
        }

        /// <summary>
        /// 获取用户选择的类型为T的所有实体
        /// </summary>
        /// <typeparam name="T">实体的类型</typeparam>
        /// <param name="db">数据库对象</param>
        /// <param name="mode">打开方式</param>
        /// <param name="openErased">打开已删除的实体</param>
        /// <param name="prompt">提示字符串</param>
        /// <returns>返回类型为T的实体</returns>
        public static List<T> GetSelection<T>(this Database db, OpenMode mode, bool openErased, string prompt) where T : Entity
        {
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            var trans = db.TransactionManager;
            string dxfname = RXObject.GetClass(typeof(T)).DxfName;
            List<T> ents = new List<T>();
            TypedValue[] values = { new TypedValue((int)DxfCode.Start, dxfname) };
            var filter = new SelectionFilter(values);
            var pso = new PromptSelectionOptions();
            pso.MessageForAdding = prompt;
            var psr = ed.GetSelection(pso, filter);
            if (psr.Status == PromptStatus.OK)
                foreach (var id in psr.Value.GetObjectIds())
                {
                    T t = (T)trans.GetObject(id, mode, openErased);
                    ents.Add(t);
                }
            return ents;
        }

        /// <summary>
        /// 获取用户选择的类型为T的所有实体
        /// </summary>
        /// <typeparam name="T">实体的类型</typeparam>
        /// <param name="db">数据库对象</param>
        /// <param name="prompt">提示字符串</param>
        /// <returns>返回类型为T的实体</returns>
        public static List<T> GetSelection<T>(this Database db, string prompt) where T : Entity
        {
            return GetSelection<T>(db, OpenMode.ForRead, false, prompt);
        }

        /// <summary>
        /// 获取用户选择的类型为T的所有实体
        /// </summary>
        /// <typeparam name="T">实体的类型</typeparam>
        /// <param name="db">数据库对象</param>
        /// <returns>返回类型为T的实体</returns>
        public static List<T> GetSelection<T>(this Database db) where T : Entity
        {
            return GetSelection<T>(db, OpenMode.ForRead, false, "");
        }

        /// <summary>
        /// 选择窗口中及和窗口四条边界相交的实体
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="pt1">窗口的一个角点</param>
        /// <param name="pt2">窗口的另一个角点</param>
        /// <param name="mode">打开方式</param>
        /// <param name="openErased">打开已删除的实体</param>
        /// <returns>返回选择的实体</returns>
        public static List<Entity> SelectCrossingWindow(this Database db, Point3d pt1, Point3d pt2, OpenMode mode, bool openErased)
        {
            var trans = db.TransactionManager;
            List<Entity> ents = new List<Entity>();
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            var psr = ed.SelectCrossingWindow(pt1, pt2);
            if (psr.Status == PromptStatus.OK)
                foreach (var id in psr.Value.GetObjectIds())
                {
                    Entity ent = (Entity)trans.GetObject(id, mode, openErased);
                    ents.Add(ent);
                }
            return ents;
        }

        /// <summary>
        /// 选择窗口中及和窗口四条边界相交的实体
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="pt1">窗口的一个角点</param>
        /// <param name="pt2">窗口的另一个角点</param>
        /// <returns>返回选择的实体</returns>
        public static List<Entity> SelectCrossingWindow(this Database db, Point3d pt1, Point3d pt2)
        {
            return SelectCrossingWindow(db, pt1, pt2, OpenMode.ForRead, false);
        }

        /// <summary>
        /// 选择窗口中及和窗口四条边界相交的实体，实体类型为T
        /// </summary>
        /// <typeparam name="T">实体的类型</typeparam>
        /// <param name="db">数据库对象</param>
        /// <param name="pt1">窗口的一个角点</param>
        /// <param name="pt2">窗口的另一个角点</param>
        /// <returns>返回类型为T的实体</returns>
        public static List<T> SelectCrossingWindow<T>(this Database db, Point3d pt1, Point3d pt2) where T : Entity
        {
            return SelectCrossingWindow<T>(db, pt1, pt2, OpenMode.ForRead, false);
        }

        /// <summary>
        /// 选择窗口中及和窗口四条边界相交的实体，实体类型为T
        /// </summary>
        /// <typeparam name="T">实体的类型</typeparam>
        /// <param name="db">数据库对象</param>
        /// <param name="pt1">窗口的一个角</param>
        /// <param name="pt2">窗口的另一个角点</param>
        /// <param name="mode">打开方式</param>
        /// <param name="openErased">打开已删除的实体</param>
        /// <returns>返回类型为T的实体</returns>
        public static List<T> SelectCrossingWindow<T>(this Database db, Point3d pt1, Point3d pt2, OpenMode mode, bool openErased) where T : Entity
        {
            var trans = db.TransactionManager;
            string dxfname = RXObject.GetClass(typeof(T)).DxfName;
            List<T> ents = new List<T>();
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            TypedValue[] values = { new TypedValue((int)DxfCode.Start, dxfname) };
            var filter = new SelectionFilter(values);
            var psr = ed.SelectCrossingWindow(pt1, pt2, filter);
            if (psr.Status == PromptStatus.OK)
                foreach (var id in psr.Value.GetObjectIds())
                {
                    T t = (T)trans.GetObject(id, mode, openErased);
                    ents.Add(t);
                }
            return ents;
        }


        /// <summary>
        /// 选择多边形窗口中及和窗口四条边界相交的实体，实体类型为T
        /// </summary>
        /// <typeparam name="T">实体的类型</typeparam>
        /// <param name="db">数据库对象</param>
        /// <param name="pts"></param>
        /// <returns>返回类型为T的实体</returns>
        public static List<T> SelectCrossingPolygon<T>(this Database db, Point3dCollection pts) where T : Entity
        {
            return SelectCrossingPolygon<T>(db, pts, OpenMode.ForRead, false);
        }

        /// <summary>
        /// 选择多边形窗口中及和窗口四条边界相交的实体，实体类型为T
        /// </summary>
        /// <typeparam name="T">实体的类型</typeparam>
        /// <param name="db">数据库对象</param>
        /// <param name="pts">窗口的另一个角点</param>
        /// <param name="mode">打开方式</param>
        /// <param name="openErased">打开已删除的实体</param>
        /// <returns>返回类型为T的实体</returns>
        public static List<T> SelectCrossingPolygon<T>(this Database db, Point3dCollection pts, OpenMode mode, bool openErased) where T : Entity
        {
            var trans = db.TransactionManager;
            string dxfname = RXObject.GetClass(typeof(T)).DxfName;
            List<T> ents = new List<T>();
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            TypedValue[] values = { new TypedValue((int)DxfCode.Start, dxfname) };
            var filter = new SelectionFilter(values);
            var psr = ed.SelectCrossingPolygon(pts, filter);
            if (psr.Status == PromptStatus.OK)
                foreach (var id in psr.Value.GetObjectIds())
                {
                    T t = (T)trans.GetObject(id, mode, openErased);
                    ents.Add(t);
                }
            return ents;
        }

        /// <summary>
        /// 获取模型空间中的所有实体
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="mode">打开方式</param>
        /// <param name="openErased">打开已删除的实体</param>
        /// <returns>返回模型空间中的所有实体</returns>
        public static List<Entity> GetEntsInModelSpace(this Database db, OpenMode mode, bool openErased)
        {
            var trans = db.TransactionManager;
            List<Entity> ents = new List<Entity>();
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            TypedValue[] values = { new TypedValue((int)DxfCode.ViewportVisibility, 0) };
            var filter = new SelectionFilter(values);
            var psr = ed.SelectAll(filter);
            if (psr.Status == PromptStatus.OK)
                foreach (var id in psr.Value.GetObjectIds())
                {
                    Entity ent = (Entity)trans.GetObject(id, mode, openErased);
                    ents.Add(ent);
                }
            return ents;
        }

        /// <summary>
        /// 获取模型空间中的所有实体
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <returns>返回模型空间中的所有实体</returns>
        public static List<Entity> GetEntsInModelSpace(this Database db)
        {
            return GetEntsInModelSpace(db, OpenMode.ForRead, false);
        }

        /// <summary>
        /// 获取模型空间中类型为T的所有实体
        /// </summary>
        /// <typeparam name="T">实体的类型</typeparam>
        /// <param name="db">数据库对象</param>
        /// <param name="mode">打开方式</param>
        /// <param name="openErased">打开已删除的实体</param>
        /// <returns>返回模型空间中类型为T的实体</returns>
        public static List<T> GetEntsInModelSpace<T>(this Database db, OpenMode mode, bool openErased) where T : Entity
        {
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            //声明一个List类的变量，用于返回类型为T为的实体列表
            List<T> ents = new List<T>();
            //获取类型T代表的DXF代码名用于构建选择集过滤器
            string dxfname = RXClass.GetClass(typeof(T)).DxfName;
            //构建选择集过滤器        
            TypedValue[] values = { new TypedValue((int)DxfCode.Start, dxfname),
                                    new TypedValue((int)DxfCode.LayoutName, "Model") };
            SelectionFilter filter = new SelectionFilter(values);
            //选择符合条件的所有实体
            PromptSelectionResult psr = ed.SelectAll(filter);
            if (psr.Status == PromptStatus.OK)
                //循环遍历符合条件的实体
                foreach (var id in psr.Value.GetObjectIds())
                {
                    //将实体强制转化为T类型的对象
                    T t = (T)id.GetObject(mode, openErased);
                    //将实体添加到返回列表中
                    ents.Add(t);
                }
            //返回类型为T为的实体列表
            return ents;
        }

        /// <summary>
        /// 获取模型空间中类型为T的所有实体(对象打开为读）
        /// </summary>
        /// <typeparam name="T">实体的类型</typeparam>
        /// <param name="db">数据库对象</param>
        /// <returns>返回模型空间中类型为T的实体</returns>
        public static List<T> GetEntsInModelSpace<T>(this Database db) where T : Entity
        {
            return GetEntsInModelSpace<T>(db, OpenMode.ForRead, false);
        }

        /// <summary>
        /// 获取图纸空间中类型为T的所有实体
        /// </summary>
        /// <typeparam name="T">实体的类型</typeparam>
        /// <param name="db">数据库对象</param>
        /// <param name="mode">打开方式</param>
        /// <param name="openErased">打开已删除的实体</param>
        /// <returns>返回图纸空间中类型为T的实体</returns>
        public static List<T> GetEntsInPaperSpace<T>(this Database db, OpenMode mode, bool openErased) where T : Entity
        {
            var trans = db.TransactionManager;
            string dxfname = RXClass.GetClass(typeof(T)).DxfName;
            List<T> ents = new List<T>();
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            TypedValue[] values = { new TypedValue((int)DxfCode.Start, dxfname),
                                    new TypedValue((int)DxfCode.ViewportVisibility, 1) };
            var filter = new SelectionFilter(values);
            var psr = ed.SelectAll(filter);
            if (psr.Status == PromptStatus.OK)
                foreach (var id in psr.Value.GetObjectIds())
                {
                    T t = (T)trans.GetObject(id, mode, openErased);
                    ents.Add(t);
                }
            return ents;
        }

        /// <summary>
        /// 获取图纸空间中类型为T的所有实体
        /// </summary>
        /// <typeparam name="T">实体的类型</typeparam>
        /// <param name="db">数据库对象</param>
        /// <returns>返回图纸空间中类型为T的实体</returns>
        public static List<T> GetEntsInPaperSpace<T>(this Database db) where T : Entity
        {
            return GetEntsInPaperSpace<T>(db, OpenMode.ForRead, false);
        }

        /// <summary>
        /// 获取图纸空间中的所有实体
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="mode">打开方式</param>
        /// <param name="openErased">打开已删除的实体</param>
        /// <returns>返回图纸空间中的所有实体</returns>
        public static List<Entity> GetEntsInPaperSpace(this Database db, OpenMode mode, bool openErased)
        {
            var trans = db.TransactionManager;
            List<Entity> ents = new List<Entity>();
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            TypedValue[] values = { new TypedValue((int)DxfCode.ViewportVisibility, 1) };
            var filter = new SelectionFilter(values);
            var psr = ed.SelectAll(filter);
            if (psr.Status == PromptStatus.OK)
                foreach (var id in psr.Value.GetObjectIds())
                {
                    Entity ent = (Entity)trans.GetObject(id, mode, openErased);
                    ents.Add(ent);
                }
            return ents;
        }

        /// <summary>
        /// 获取图纸空间中的所有实体
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <returns>返回图纸空间中的所有实体</returns>
        public static List<Entity> GetEntsInPaperSpace(this Database db)
        {
            return GetEntsInPaperSpace(db, OpenMode.ForRead, false);
        }

        /// <summary>
        /// 获取当前空间中的所有实体
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="mode">打开方式</param>
        /// <param name="openErased">打开已删除的实体</param>
        /// <returns>返回当前空间中的所有实体</returns>
        public static List<Entity> GetEntsInCurrentSpace(this Database db, OpenMode mode, bool openErased)
        {
            var trans = db.TransactionManager;
            List<Entity> ents = new List<Entity>();
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            TypedValue[] values = { new TypedValue((int)DxfCode.LayoutName, AcadApp.GetSystemVariable("CTAB")) };
            var filter = new SelectionFilter(values);
            var psr = ed.SelectAll(filter);
            if (psr.Status == PromptStatus.OK)
                foreach (var id in psr.Value.GetObjectIds())
                {
                    Entity ent = (Entity)trans.GetObject(id, mode, openErased, true);
                    ents.Add(ent);
                }
            return ents;
        }

        /// <summary>
        /// 获取当前空间中的所有实体
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <returns>返回当前空间中的所有实体</returns>
        public static List<Entity> GetEntsInCurrentSpace(this Database db)
        {
            return GetEntsInCurrentSpace(db, OpenMode.ForRead, false);
        }

        /// <summary>
        /// 获取当前空间中类型为T的所有实体
        /// </summary>
        /// <typeparam name="T">实体的类型</typeparam>
        /// <param name="db">数据库对象</param>
        /// <param name="mode">打开方式</param>
        /// <param name="openErased">打开已删除的实体</param>
        /// <param name="openLocked">打开锁定的图层</param>
        /// <returns>返回当前空间中类型为T的实体</returns>
        public static List<T> GetEntsInCurrentSpace<T>(this Database db, OpenMode mode, bool openErased, bool openLocked = false) where T : Entity
        {
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            //声明一个List类的变量，用于返回类型为T为的实体列表
            List<T> ents = new List<T>();
            //获取类型T代表的DXF代码名用于构建选择集过滤器
            string dxfname = RXClass.GetClass(typeof(T)).DxfName;
            //构建选择集过滤器        
            TypedValue[] values = { new TypedValue((int)DxfCode.Start, dxfname),
                                    new TypedValue((int)DxfCode.LayoutName, AcadApp.GetSystemVariable("CTAB")) };
            SelectionFilter filter = new SelectionFilter(values);
            //选择符合条件的所有实体
            PromptSelectionResult psr = ed.SelectAll(filter);
            if (psr.Status == PromptStatus.OK)
                //循环遍历符合条件的实体
                foreach (var id in psr.Value.GetObjectIds())
                {
                    //将实体强制转化为T类型的对象
                    T t = (T)id.GetObject(mode, openErased, openLocked);
                    //将实体添加到返回列表中
                    ents.Add(t);
                }
            //返回类型为T为的实体列表
            return ents;
        }

        /// <summary>
        /// 获取当前空间中类型为T的所有实体(对象打开为读）
        /// </summary>
        /// <typeparam name="T">实体的类型</typeparam>
        /// <param name="db">数据库对象</param>
        /// <returns>返回当前空间中类型为T的实体</returns>
        public static List<T> GetEntsInCurrentSpace<T>(this Database db) where T : Entity
        {
            return GetEntsInCurrentSpace<T>(db, OpenMode.ForRead, false);
        }

        #region 按块名选择块参照

        /// <summary>
        /// 获得指定块名的所有块参照（当前文件）
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="isFuzzy">模糊查找</param>
        /// <param name="blockNames">块名</param>
        /// <returns>返回当前文件内所有块参照</returns>
        public static List<BlockReference> GetBlockRefsInDatabase(this Database db, bool isFuzzy, params string[] blockNames)
        {
            List<BlockReference> blockRefs = new List<BlockReference>();
            var allBlockRefs = db.GetEntsInDatabase<BlockReference>();
            foreach (BlockReference b in allBlockRefs)
            {
                try
                {
                    if (FuzzyLookup(isFuzzy, b.GetBlockName(), blockNames))
                        blockRefs.Add(b);
                }
                catch
                {

                }
            }
            return blockRefs;
        }

        /// <summary>
        /// 模糊选中
        /// </summary>
        /// <param name="isFuzzy">允许参照形式</param>
        /// <param name="name">名称</param>
        /// <param name="names">名称列表</param>
        /// <returns>选中与否</returns>
        public static bool FuzzyLookup(bool isFuzzy, string name, params string[] names)
        {
            if (isFuzzy)
            {
                foreach (string bn in names)
                    //块名后缀（大写）包含列表内（大写）
                    if (name.ToUpper().EndsWith(bn.ToUpper()))
                        return true;
            }
            else
            {
                return names.Contains(name);
            }
            return false;
        }

        /// <summary>
        /// 获得指定块名的所有块参照（当前空间）
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="isFuzzy">模糊</param>
        /// <param name="blockNames">块名</param>
        /// <returns>返回当前空间内所有块参照</returns>
        public static List<BlockReference> GetBlockRefsInCurrentSpace(this Database db,
            bool isFuzzy, params string[] blockNames)
        {
            List<BlockReference> blockRefs = new List<BlockReference>();
            var allBlockRefs = db.GetEntsInCurrentSpace<BlockReference>();
            foreach (BlockReference b in allBlockRefs)
            {
                try
                {
                    if (FuzzyLookup(isFuzzy, b.GetBlockName(), blockNames))
                        blockRefs.Add(b);
                }
                catch
                {

                }
            }
            return blockRefs;
        }

        /// <summary>
        /// 选择指定块名的块参照
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="prompt">提示文字</param>
        /// <param name="isFuzzy"></param>
        /// <param name="blockNames">块名</param>
        /// <returns>返回选择的块参照</returns>
        public static List<BlockReference> GetSelectionOfBlockRefs(this Database db, string prompt,
            bool isFuzzy, params string[] blockNames)
        {
            List<BlockReference> blockRefs = new List<BlockReference>();
            var allBlockRefs = db.GetSelection<BlockReference>(prompt);
            foreach (BlockReference b in allBlockRefs)
            {
                try
                {
                    if (FuzzyLookup(isFuzzy, b.GetBlockName(), blockNames))
                        blockRefs.Add(b);
                }
                catch
                {

                }
            }
            return blockRefs;
        }

        /// <summary>
        /// 选择指定块名的块参照
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="prompt">提示文字</param>
        /// <param name="includeNestedBlock">是否包含嵌套块</param>
        /// <param name="isFuzzy"></param>
        /// <param name="blockNames">块名</param>
        /// <returns>返回选择的块参照</returns>
        public static List<BlockReference> GetSelectionOfBlockRefs(this Database db, string prompt,
            bool includeNestedBlock, bool isFuzzy, params string[] blockNames)
        {
            List<BlockReference> blockRefs = new List<BlockReference>();
            var allBlockRefs = db.GetSelection<BlockReference>(prompt);
            foreach (BlockReference b in allBlockRefs)
            {
                try
                {
                    if (FuzzyLookup(isFuzzy, b.GetBlockName(), blockNames))
                        blockRefs.Add(b);
                    //若包含嵌套块
                    else if (includeNestedBlock)
                        GetNestedBlockreferenceList(b, blockRefs, isFuzzy, blockNames);
                }
                catch
                {

                }
            }
            return blockRefs;
        }

        private static void GetNestedBlockreferenceList(BlockReference br, List<BlockReference> blockRefs,
            bool isFuzzy, params string[] blockNames)
        {
            ObjectId btrId = br.BlockTableRecord;
            BlockTableRecord btr = btrId.GetObject(OpenMode.ForRead) as BlockTableRecord;
            foreach (ObjectId id in btr)
            {
                BlockReference nestBr = id.GetObject(OpenMode.ForRead) as BlockReference;
                if (nestBr != null)
                {
                    if (FuzzyLookup(isFuzzy, nestBr.GetBlockName(), blockNames))
                        blockRefs.Add(nestBr);
                    else
                        GetNestedBlockreferenceList(nestBr, blockRefs, isFuzzy, blockNames);
                }
            }
        }
        #endregion
    }
}
