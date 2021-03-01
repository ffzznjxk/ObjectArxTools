﻿using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DotNetARX
{
    /// <summary>
    /// 辅助操作类
    /// </summary>
    public static partial class Tools
    {
        /// <summary>
        /// 判断字符串是否为数字
        /// </summary>
        /// <param name="value">字符串</param>
        /// <returns>如果字符串为数字则返回true，否则返回false</returns>
        public static bool IsNumeric(this string value)
        {
            return Regex.IsMatch(value, @"^[+-]?\d*[.]?\d*$");
        }

        /// <summary>
        /// 判断字符串是否为整数
        /// </summary>
        /// <param name="value">字符串</param>
        /// <returns>如果字符串为整数则返回true，否则返回false</returns>
        public static bool IsInt(this string value)
        {
            return Regex.IsMatch(value, @"^[+-]?\d*$");
        }

        /// <summary>
        /// 判断字符串是否为空或空白
        /// </summary>
        /// <param name="value">字符串</param>
        /// <returns>如果字符串为空或空白则返回true，否则返回false</returns>
        public static bool IsNullOrWhiteSpace(this string value)
        {
            if (value == null)
                return false;
            return string.IsNullOrEmpty(value.Trim());
        }

        /// <summary>
        /// 获取当前.NET程序所在的目录
        /// </summary>
        /// <returns>返回当前.NET程序所在的目录</returns>
        public static string GetCurrentPath()
        {
            var module = System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0];
            return System.IO.Path.GetDirectoryName(module.FullyQualifiedName);
        }

        /// <summary>
        /// 获取模型空间的ObjectId
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <returns>返回模型空间的ObjectId</returns>
        public static ObjectId GetModelSpaceId(this Database db)
        {
            return SymbolUtilityServices.GetBlockModelSpaceId(db);
        }

        /// <summary>
        /// 获取图纸空间的ObjectId
        /// </summary>
        /// <param name="db"></param>
        /// <returns>返回图纸空间的ObjectId</returns>
        public static ObjectId GetPaperSpaceId(this Database db)
        {
            return SymbolUtilityServices.GetBlockPaperSpaceId(db);
        }

        /// <summary>
        /// 将实体添加到模型空间
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="ent">要添加的实体</param>
        /// <returns>返回添加到模型空间中实体的ObjectId</returns>
        public static ObjectId AddToModelSpace(this Database db, Entity ent)
        {
            //用于返回的ObjectId
            ObjectId entId;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //以读方式打开块表
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                //以写方式打开模型空间块表记录
                BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                //将图形对象的信息添加到块表记录
                entId = btr.AppendEntity(ent);
                //把对象添加到事务处理中
                trans.AddNewlyCreatedDBObject(ent, true);
                //提交事务处理
                trans.Commit();
            }
            //返回ObjectId
            return entId;
        }

        /// <summary>
        /// 将实体添加到模型空间
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="ents">要添加的多个实体</param>
        /// <returns>返回添加到模型空间中实体的ObjectId集合</returns>
        public static ObjectIdCollection AddToModelSpace(this Database db, params Entity[] ents)
        {
            ObjectIdCollection ids = new ObjectIdCollection();
            var trans = db.TransactionManager;
            BlockTableRecord btr = (BlockTableRecord)trans.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
            foreach (var ent in ents)
            {
                ids.Add(btr.AppendEntity(ent));
                trans.AddNewlyCreatedDBObject(ent, true);
            }
            btr.DowngradeOpen();
            return ids;
        }

        /// <summary>
        /// 将实体添加到图纸空间
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="ent">要添加的实体</param>
        /// <returns>返回添加到图纸空间中实体的ObjectId</returns>
        public static ObjectId AddToPaperSpace(this Database db, Entity ent)
        {
            var trans = db.TransactionManager;
            BlockTableRecord btr = (BlockTableRecord)trans.GetObject(SymbolUtilityServices.GetBlockPaperSpaceId(db), OpenMode.ForWrite);
            ObjectId id = btr.AppendEntity(ent);
            trans.AddNewlyCreatedDBObject(ent, true);
            btr.DowngradeOpen();
            return id;
        }

        /// <summary>
        /// 将实体添加到图纸空间
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="ents">要添加的多个实体</param>
        /// <returns>返回添加到图纸空间中实体的ObjectId集合</returns>
        public static ObjectIdCollection AddToPaperSpace(this Database db, params Entity[] ents)
        {
            ObjectIdCollection ids = new ObjectIdCollection();
            var trans = db.TransactionManager;
            BlockTableRecord btr = (BlockTableRecord)trans.GetObject(SymbolUtilityServices.GetBlockPaperSpaceId(db), OpenMode.ForWrite);
            foreach (var ent in ents)
            {
                ids.Add(btr.AppendEntity(ent));
                trans.AddNewlyCreatedDBObject(ent, true);
            }
            btr.DowngradeOpen();
            return ids;
        }

        /// <summary>
        /// 将实体添加到当前空间
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="ent">要添加的实体</param>
        /// <returns>返回添加到当前空间中的实体ObjectId</returns>
        public static ObjectId AddToCurrentSpace(this Database db, Entity ent)
        {
            var trans = db.TransactionManager;
            BlockTableRecord btr = (BlockTableRecord)trans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
            ObjectId id = btr.AppendEntity(ent);
            trans.AddNewlyCreatedDBObject(ent, true);
            btr.DowngradeOpen();
            return id;
        }

        ///// <summary>
        ///// 将实体添加到当前空间
        ///// </summary>
        ///// <param name="db">数据库对象</param>
        ///// <param name="ents">要添加的实体集</param>
        ///// <returns>返回添加到当前空间中的实体ObjectId集合</returns>
        //public static ObjectIdCollection AddToCurrentSpace(this Database db, params Entity[] ents)
        //{
        //    ObjectIdCollection ids = new ObjectIdCollection();
        //    var trans = db.TransactionManager;
        //    BlockTableRecord btr = (BlockTableRecord)trans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
        //    foreach (var ent in ents)
        //    {
        //        ids.Add(btr.AppendEntity(ent));
        //        trans.AddNewlyCreatedDBObject(ent, true);
        //    }
        //    btr.DowngradeOpen();
        //    return ids;
        //}

        /// <summary>
        /// 将实体添加到当前空间
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="ents">要添加的实体集</param>
        /// <returns>返回添加到当前空间中的实体ObjectId集合</returns>
        public static List<ObjectId> AddToCurrentSpace(this Database db, params Entity[] ents)
        {
            List<ObjectId> ids = new List<ObjectId>();
            var trans = db.TransactionManager;
            BlockTableRecord btr = (BlockTableRecord)trans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
            foreach (var ent in ents)
            {
                ids.Add(btr.AppendEntity(ent));
                trans.AddNewlyCreatedDBObject(ent, true);
            }
            btr.DowngradeOpen();
            return ids;
        }

        /// <summary>
        /// 将字符串形式的句柄转化为ObjectId
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="handleString">句柄字符串</param>
        /// <returns>返回实体的ObjectId</returns>
        public static ObjectId HandleToObjectId(this Database db, string handleString)
        {
            Handle handle = new Handle(Convert.ToInt64(handleString, 16));
            ObjectId id = db.GetObjectId(false, handle, 0);
            return id;
        }

        /// <summary>
        /// 亮显实体
        /// </summary>
        /// <param name="ids">实体的Id集合</param>
        public static void HighlightEntities(this ObjectIdCollection ids)
        {
            if (ids.Count == 0) return;
            var trans = ids[0].Database.TransactionManager;
            foreach (ObjectId id in ids)
            {
                Entity ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent != null)
                {
                    ent.Highlight();
                }
            }
        }

        /// <summary>
        /// 亮显选择集中的实体
        /// </summary>
        /// <param name="selectionSet">选择集</param>
        public static void HighlightEntities(this SelectionSet selectionSet)
        {
            if (selectionSet == null) return;
            ObjectIdCollection ids = new ObjectIdCollection(selectionSet.GetObjectIds());
            ids.HighlightEntities();
        }

        /// <summary>
        /// 取消亮显实体
        /// </summary>
        /// <param name="ids">实体的Id集合</param>
        public static void UnHighlightEntities(this ObjectIdCollection ids)
        {
            if (ids.Count == 0) return;
            var trans = ids[0].Database.TransactionManager;
            foreach (ObjectId id in ids)
            {
                Entity ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent != null)
                {
                    ent.Unhighlight();
                }
            }
        }

        /// <summary>
        /// 将字符串格式的点转换为Point3d格式
        /// </summary>
        /// <param name="stringPoint">字符串格式的点</param>
        /// <returns>返回对应的Point3d</returns>
        public static Point3d StringToPoint3d(this string stringPoint)
        {
            string[] strPoint = stringPoint.Trim().Split(new char[] { '(', ',', ')' }, StringSplitOptions.RemoveEmptyEntries);
            double x = Convert.ToDouble(strPoint[0]);
            double y = Convert.ToDouble(strPoint[1]);
            double z = Convert.ToDouble(strPoint[2]);
            return new Point3d(x, y, z);
        }

        /// <summary>
        /// 获取数据库对应的文档对象
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <returns>返回数据库对应的文档对象</returns>
        public static Document GetDocument(this Database db)
        {
            return Application.DocumentManager.GetDocument(db);
        }

        /// <summary>
        /// 根据数据库获取命令行对象
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <returns>返回命令行对象</returns>
        public static Editor GetEditor(this Database db)
        {
            return Application.DocumentManager.GetDocument(db).Editor;
        }

        /// <summary>
        /// 在命令行输出信息
        /// </summary>
        /// <param name="ed">命令行对象</param>
        /// <param name="message">要输出的信息</param>
        public static void WriteMessage(this Editor ed, object message)
        {
            ed.WriteMessage(message.ToString());
        }

        /// <summary>
        /// 在命令行输出信息，信息显示在新行上
        /// </summary>
        /// <param name="ed">命令行对象</param>
        /// <param name="message">要输出的信息</param>
        public static void WriteMessageWithReturn(this Editor ed, object message)
        {
            ed.WriteMessage("\n" + message.ToString());
        }

        /// <summary>
        /// 绘制折断线
        /// </summary>
        /// <param name="startPt">起点</param>
        /// <param name="endPt">终点</param>
        /// <param name="layer">层名</param>
        /// <param name="scale">比例</param>
        /// <param name="ext">延伸长度</param>
        /// <returns>返回折断线</returns>
        public static Polyline DrawBreakPLine(Point3d startPt, Point3d endPt, string layer, int scale, int ext)
        {
            //两点距离
            double dist = startPt.DistanceTo(endPt);
            //先在水平线上画
            Point3d _distPt = startPt + new Vector3d(dist, 0, 0);
            //两头
            Point3d _pt1 = startPt + new Vector3d(-ext, 0, 0);
            Point3d _pt6 = _distPt + new Vector3d(ext, 0, 0);
            //水平线上的中点
            Point3d midPt = GeoTools.MidPoint(startPt, _distPt);
            //
            Point3d _pt2 = midPt + new Vector3d(-scale / 2, 0, 0);
            Point3d _pt5 = midPt + new Vector3d(scale / 2, 0, 0);
            //
            Point3d _pt3 = midPt + new Vector3d(-scale / 6, scale, 0);
            Point3d _pt4 = midPt + new Vector3d(scale / 6, -scale, 0);
            //
            Point3d[] pts = new Point3d[] { _pt1, _pt2, _pt3, _pt4, _pt5, _pt6 };
            Polyline breakLine = new Polyline
            {
                Layer = layer
            };
            breakLine.CreatePolyline(new Point3dCollection(pts));
            //两点角度
            double angle = endPt.AngleFromXAxis(startPt);
            //旋转
            breakLine.Rotate(startPt, angle);
            //返回
            return breakLine;
        }
    }
}
