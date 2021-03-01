using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;

namespace DotNetARX
{
    /// <summary>
    /// 实体操作类
    /// </summary>
    public static class EntTools
    {
        /// <summary>
        /// 移动实体
        /// </summary>
        /// <param name="id">实体的ObjectId</param>
        /// <param name="sourcePt">移动的源点</param>
        /// <param name="targetPt">移动的目标点</param>
        public static void Move(this ObjectId id, Point3d sourcePt, Point3d targetPt)
        {
            //构建用于移动实体的矩阵
            Vector3d vector = sourcePt.GetVectorTo(targetPt);
            Matrix3d mt = Matrix3d.Displacement(vector);
            //以写的方式打开id表示的实体对象
            Entity ent = (Entity)id.GetObject(OpenMode.ForWrite);
            //对实体实施移动
            ent.TransformBy(mt);
            //切换实体为读的状态
            ent.DowngradeOpen();
        }

        /// <summary>
        /// 移动实体
        /// </summary>
        /// <param name="ent">实体</param>
        /// <param name="sourcePt">移动的源点</param>
        /// <param name="targetPt">移动的目标点</param>
        public static void Move(this Entity ent, Point3d sourcePt, Point3d targetPt)
        {
            //如果是还未被添加到数据库中的新实体
            if (ent.IsNewObject)
            {
                //构建用于移动实体的矩阵
                Vector3d vector = sourcePt.GetVectorTo(targetPt);
                Matrix3d mt = Matrix3d.Displacement(vector);
                //对实体实施移动
                ent.TransformBy(mt);
            }
            //如果是已经添加到数据库中的实体
            else
                ent.ObjectId.Move(sourcePt, targetPt);
        }

        /// <summary>
        /// 移动实体
        /// </summary>
        /// <param name="sourcePt">移动的源点</param>
        /// <param name="targetPt">移动的目标点</param>
        /// <param name="ids"></param>
        public static void Move(Point3d sourcePt, Point3d targetPt, params ObjectId[] ids)
        {
            //构建用于移动实体的矩阵
            Vector3d vector = sourcePt.GetVectorTo(targetPt);
            Matrix3d mt = Matrix3d.Displacement(vector);
            foreach (var id in ids)
            {
                //以写的方式打开id表示的实体对象
                Entity ent = (Entity)id.GetObject(OpenMode.ForWrite);
                //对实体实施移动
                ent.TransformBy(mt);
                //切换实体为读的状态
                ent.DowngradeOpen();
            }
        }

        /// <summary>
        /// 复制实体
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="sourcePt">复制的源点</param>
        /// <param name="targetPt">复制的目标点</param>
        /// <param name="ids">复制对象</param>
        /// <returns>返回复制实体的ObjectId列表</returns>
        public static Dictionary<ObjectId, ObjectId> Copy(this Database db, Point3d sourcePt, Point3d targetPt, params ObjectId[] ids)
        {
            Dictionary<ObjectId, ObjectId> dictId = new Dictionary<ObjectId, ObjectId>();
            //构建用于复制实体的矩阵
            Vector3d vector = sourcePt.GetVectorTo(targetPt);
            Matrix3d mt = Matrix3d.Displacement(vector);
            //获取id表示的实体对象
            ObjectIdCollection srcIds = new ObjectIdCollection(ids);
            //
            var map = new IdMapping();
            db.DeepCloneObjects(srcIds, db.CurrentSpaceId, map, false);
            //
            foreach (var id in ids)
            {
                var idpair = map[id];
                //
                Entity copyEntity = idpair.Value.GetObject(OpenMode.ForWrite) as Entity;
                copyEntity.TransformBy(mt);
                //
                dictId.Add(idpair.Key, idpair.Value);
            }
            return dictId;
        }

        /// <summary>
        /// 旋转实体
        /// </summary>
        /// <param name="id">实体的ObjectId</param>
        /// <param name="basePt">旋转基点</param>
        /// <param name="angle">旋转角度</param>
        public static void Rotate(this ObjectId id, Point3d basePt, double angle)
        {
            Matrix3d mt = Matrix3d.Rotation(angle, Vector3d.ZAxis, basePt);
            Entity ent = (Entity)id.GetObject(OpenMode.ForWrite);
            ent.TransformBy(mt);
            ent.DowngradeOpen();
        }

        /// <summary>
        /// 旋转实体（多个）
        /// </summary>
        public static void Rotate(this ObjectId[] ids, Point3d basePt, double angle)
        {
            foreach (ObjectId id in ids)
            {
                id.Rotate(basePt, angle);
            }
        }

        /// <summary>
        /// 旋转实体
        /// </summary>
        /// <param name="ent">实体</param>
        /// <param name="basePt">旋转基点</param>
        /// <param name="angle">旋转角度</param>
        public static void Rotate(this Entity ent, Point3d basePt, double angle)
        {
            if (ent.IsNewObject)
            {
                Matrix3d mt = Matrix3d.Rotation(angle, Vector3d.ZAxis, basePt);
                ent.TransformBy(mt);
            }
            else
                ent.ObjectId.Rotate(basePt, angle);
        }

        /// <summary>
        /// 缩放实体
        /// </summary>
        /// <param name="id">实体的ObjectId</param>
        /// <param name="basePt">缩放基点</param>
        /// <param name="scaleFactor">缩放比例</param>
        public static void Scale(this ObjectId id, Point3d basePt, double scaleFactor)
        {
            Matrix3d mt = Matrix3d.Scaling(scaleFactor, basePt);
            Entity ent = (Entity)id.GetObject(OpenMode.ForWrite);
            ent.TransformBy(mt);
            ent.DowngradeOpen();
        }

        /// <summary>
        /// 缩放实体
        /// </summary>
        /// <param name="ent">实体</param>
        /// <param name="basePt">缩放基点</param>
        /// <param name="scaleFactor">缩放比例</param>
        public static void Scale(this Entity ent, Point3d basePt, double scaleFactor)
        {
            if (ent.IsNewObject)
            {
                Matrix3d mt = Matrix3d.Scaling(scaleFactor, basePt);
                ent.TransformBy(mt);
            }
            else
                ent.ObjectId.Scale(basePt, scaleFactor);
        }

        /// <summary>
        /// 镜像实体
        /// </summary>
        /// <param name="id">实体的ObjectId</param>
        /// <param name="mirrorPt1">镜像轴的第一点</param>
        /// <param name="mirrorPt2">镜像轴的第二点</param>
        /// <param name="eraseSourceObject">是否删除源对象</param>
        /// <returns>返回镜像实体的ObjectId</returns>
        public static ObjectId Mirror(this ObjectId id, Point3d mirrorPt1, Point3d mirrorPt2, bool eraseSourceObject)
        {
            //镜像线
            Line3d miLine = new Line3d(mirrorPt1, mirrorPt2);
            //镜像矩阵
            Matrix3d mt = Matrix3d.Mirroring(miLine);
            ObjectId mirrorId = id;
            Entity ent = (Entity)id.GetObject(OpenMode.ForWrite);
            //如果删除源对象，则直接对源对象实行镜像变换
            if (eraseSourceObject == true)
                ent.TransformBy(mt);
            //如果不删除源对象，则镜像复制源对象
            else
            {
                Entity entCopy = ent.GetTransformedCopy(mt);
                mirrorId = id.Database.AddToCurrentSpace(entCopy);
            }
            return mirrorId;
        }

        /// <summary>
        /// 镜像实体
        /// </summary>
        /// <param name="ent">实体</param>
        /// <param name="mirrorPt1">镜像轴的第一点</param>
        /// <param name="mirrorPt2">镜像轴的第二点</param>
        /// <param name="eraseSourceObject">是否删除源对象</param>
        /// <returns>返回镜像实体的ObjectId</returns>
        public static ObjectId Mirror(this Entity ent, Point3d mirrorPt1, Point3d mirrorPt2, bool eraseSourceObject)
        {
            //镜像线
            Line3d miLine = new Line3d(mirrorPt1, mirrorPt2);
            //镜像矩阵
            Matrix3d mt = Matrix3d.Mirroring(miLine);
            ObjectId mirrorId = ObjectId.Null;
            if (ent.IsNewObject)
            {
                //如果删除源对象，则直接对源对象实行镜像变换
                if (eraseSourceObject == true)
                    ent.TransformBy(mt);
                else //如果不删除源对象，则镜像复制源对象
                {
                    Entity entCopy = ent.GetTransformedCopy(mt);
                    mirrorId = ent.Database.AddToCurrentSpace(entCopy);
                }
            }
            else
                mirrorId = ent.ObjectId.Mirror(mirrorPt1, mirrorPt2, eraseSourceObject);
            return mirrorId;
        }

        /// <summary>
        /// 镜像实体
        /// </summary>
        /// <param name="id">实体的ObjectId</param>
        /// <param name="mirrorPt1">镜像轴的第一点</param>
        /// <param name="mirrorPt2">镜像轴的第二点</param>
        /// <returns>返回镜像实体的ObjectId</returns>
        public static ObjectId Mirror(this ObjectId id, Point3d mirrorPt1, Point3d mirrorPt2)
        {
            //镜像线
            Line3d miLine = new Line3d(mirrorPt1, mirrorPt2);
            //镜像矩阵
            Matrix3d mt = Matrix3d.Mirroring(miLine);
            //
            Entity ent = (Entity)id.GetObject(OpenMode.ForWrite);
            ent.TransformBy(mt);
            //
            return id;
        }

        /// <summary>
        /// 镜像实体（多个）
        /// </summary>
        public static ObjectId[] Mirror(this ObjectId[] ids, Point3d mirrorPt1, Point3d mirrorPt2)
        {
            List<ObjectId> mirrorIds = new List<ObjectId>();
            foreach (ObjectId id in ids)
            {
                mirrorIds.Add(id.Mirror(mirrorPt1, mirrorPt2));
            }
            return mirrorIds.ToArray();
        }

        /// <summary>
        /// 偏移实体
        /// </summary>
        /// <param name="id">实体的ObjectId</param>
        /// <param name="dis">偏移距离</param>
        /// <returns>返回偏移后的实体Id集合</returns>
        public static List<ObjectId> Offset(this ObjectId id, double dis)
        {
            List<ObjectId> ids = new List<ObjectId>();
            Curve cur = id.GetObject(OpenMode.ForWrite) as Curve;
            if (cur != null)
            {
                try
                {
                    //获取偏移的对象集合
                    DBObjectCollection offsetCurves = cur.GetOffsetCurves(dis);
                    //将对象集合类型转换为实体类的数组，以方便加入实体的操作
                    Entity[] offsetEnts = new Entity[offsetCurves.Count];
                    offsetCurves.CopyTo(offsetEnts, 0);
                    //将偏移的对象加入到数据库
                    ids = id.Database.AddToCurrentSpace(offsetEnts);
                }
                catch
                {
                    Application.ShowAlertDialog("无法偏移！");
                }
            }
            else
                Application.ShowAlertDialog("无法偏移！");
            //返回偏移后的实体Id集合
            return ids;
        }

        /// <summary>
        /// 偏移实体
        /// </summary>
        /// <param name="ent">实体</param>
        /// <param name="dis">偏移距离</param>
        /// <returns>返回偏移后的实体集合</returns>
        public static DBObjectCollection Offset(this Entity ent, double dis)
        {
            DBObjectCollection offsetCurves = new DBObjectCollection();
            Curve cur = ent as Curve;
            if (cur != null)
            {
                try
                {
                    offsetCurves = cur.GetOffsetCurves(dis);
                    Entity[] offsetEnts = new Entity[offsetCurves.Count];
                    offsetCurves.CopyTo(offsetEnts, 0);
                }
                catch
                {
                    Application.ShowAlertDialog("无法偏移！");
                }
            }
            else
                Application.ShowAlertDialog("无法偏移！");
            return offsetCurves;
        }

        /// <summary>
        /// 矩形阵列实体
        /// </summary>
        /// <param name="id">实体的ObjectId</param>
        /// <param name="numRows">矩形阵列的行数,该值必须为正数</param>
        /// <param name="numCols">矩形阵列的列数,该值必须为正数</param>
        /// <param name="disRows">行间的距离</param>
        /// <param name="disCols">列间的距离</param>
        /// <returns>返回阵列后的实体集合的ObjectId</returns>
        public static ObjectIdCollection ArrayRectang(this ObjectId id, int numRows, int numCols, double disRows, double disCols)
        {
            // 用于返回阵列后的实体集合的ObjectId
            ObjectIdCollection ids = new ObjectIdCollection();
            // 以读的方式打开实体
            Entity ent = (Entity)id.GetObject(OpenMode.ForRead);
            for (int m = 0; m < numRows; m++)
            {
                for (int n = 0; n < numCols; n++)
                {
                    //获取平移矩阵
                    Matrix3d mt = Matrix3d.Displacement(new Vector3d(n * disCols, m * disRows, 0));
                    //复制实体
                    Entity entCopy = ent.GetTransformedCopy(mt);
                    //将复制的实体添加到模型空间
                    ObjectId entCopyId = id.Database.AddToCurrentSpace(entCopy);
                    //将复制实体的ObjectId添加到集合中
                    ids.Add(entCopyId);
                }
            }
            //切换实体为写的状态
            ent.UpgradeOpen();
            //删除实体
            ent.Erase();
            //返回阵列后的实体集合的ObjectId
            return ids;
        }

        /// <summary>
        /// 环形阵列实体
        /// </summary>
        /// <param name="id">实体的ObjectId</param>
        /// <param name="cenPt">环形阵列的中心点</param>
        /// <param name="numObj">在环形阵列中所要创建的对象数量</param>
        /// <param name="angle">以弧度表示的填充角度，正值表示逆时针方向旋转，负值表示顺时针方向旋转，如果角度为0则出错</param>
        /// <returns>返回阵列后的实体集合的ObjectId</returns>
        public static ObjectIdCollection ArrayPolar(this ObjectId id, Point3d cenPt, int numObj, double angle)
        {
            ObjectIdCollection ids = new ObjectIdCollection();
            Entity ent = (Entity)id.GetObject(OpenMode.ForRead);
            for (int i = 0; i < numObj - 1; i++)
            {
                Matrix3d mt = Matrix3d.Rotation(angle * (i + 1) / numObj, Vector3d.ZAxis, cenPt);
                Entity entCopy = ent.GetTransformedCopy(mt);
                ObjectId entCopyId = id.Database.AddToCurrentSpace(entCopy);
                ids.Add(entCopyId);
            }
            return ids;
        }

        /// <summary>
        /// 删除实体
        /// </summary>
        /// <param name="id">实体的ObjectId</param>
        public static void Erase(this ObjectId id)
        {
            DBObject ent = id.GetObject(OpenMode.ForWrite);
            ent.Erase();
        }

        /// <summary>
        /// 分解实体，并删除源对象
        /// </summary>
        /// <param name="objectIds">要分解的对象</param>
        /// <returns>分解成功</returns>
        public static bool ExplodEnts(this Database db, params ObjectId[] objectIds)
        {
            foreach (ObjectId id in objectIds)
            {
                try
                {
                    DBObjectCollection objs = new DBObjectCollection();
                    Entity ent = (Entity)id.GetObject(OpenMode.ForWrite);
                    ent.Explode(objs);
                    ent.Erase();
                    foreach (Entity obj in objs)
                        if (obj.Visible)
                            db.AddToCurrentSpace(obj);
                }
                catch { return false; }
            }
            return true;
        }

        /// <summary>
        /// 计算图形数据库模型空间中所有实体的包围框
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <returns>返回模型空间中所有实体的包围框</returns>
        public static Extents3d GetAllEntsExtent(this Database db)
        {
            Extents3d ext = new Extents3d();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId entId in btr)
                {
                    Entity ent = trans.GetObject(entId, OpenMode.ForRead) as Entity;
                    ext.AddExtents(ent.GeometricExtents);
                }
            }
            return ext;
        }
    }
}
