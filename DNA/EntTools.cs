using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;

namespace DotNetARX
{
    /// <summary>
    /// ʵ�������
    /// </summary>
    public static class EntTools
    {
        /// <summary>
        /// �ƶ�ʵ��
        /// </summary>
        /// <param name="id">ʵ���ObjectId</param>
        /// <param name="sourcePt">�ƶ���Դ��</param>
        /// <param name="targetPt">�ƶ���Ŀ���</param>
        public static void Move(this ObjectId id, Point3d sourcePt, Point3d targetPt)
        {
            //���������ƶ�ʵ��ľ���
            Vector3d vector = sourcePt.GetVectorTo(targetPt);
            Matrix3d mt = Matrix3d.Displacement(vector);
            //��д�ķ�ʽ��id��ʾ��ʵ�����
            Entity ent = (Entity)id.GetObject(OpenMode.ForWrite);
            //��ʵ��ʵʩ�ƶ�
            ent.TransformBy(mt);
            //�л�ʵ��Ϊ����״̬
            ent.DowngradeOpen();
        }

        /// <summary>
        /// �ƶ�ʵ��
        /// </summary>
        /// <param name="ent">ʵ��</param>
        /// <param name="sourcePt">�ƶ���Դ��</param>
        /// <param name="targetPt">�ƶ���Ŀ���</param>
        public static void Move(this Entity ent, Point3d sourcePt, Point3d targetPt)
        {
            //����ǻ�δ����ӵ����ݿ��е���ʵ��
            if (ent.IsNewObject)
            {
                //���������ƶ�ʵ��ľ���
                Vector3d vector = sourcePt.GetVectorTo(targetPt);
                Matrix3d mt = Matrix3d.Displacement(vector);
                //��ʵ��ʵʩ�ƶ�
                ent.TransformBy(mt);
            }
            //������Ѿ���ӵ����ݿ��е�ʵ��
            else
                ent.ObjectId.Move(sourcePt, targetPt);
        }

        /// <summary>
        /// �ƶ�ʵ��
        /// </summary>
        /// <param name="sourcePt">�ƶ���Դ��</param>
        /// <param name="targetPt">�ƶ���Ŀ���</param>
        /// <param name="ids"></param>
        public static void Move(Point3d sourcePt, Point3d targetPt, params ObjectId[] ids)
        {
            //���������ƶ�ʵ��ľ���
            Vector3d vector = sourcePt.GetVectorTo(targetPt);
            Matrix3d mt = Matrix3d.Displacement(vector);
            foreach (var id in ids)
            {
                //��д�ķ�ʽ��id��ʾ��ʵ�����
                Entity ent = (Entity)id.GetObject(OpenMode.ForWrite);
                //��ʵ��ʵʩ�ƶ�
                ent.TransformBy(mt);
                //�л�ʵ��Ϊ����״̬
                ent.DowngradeOpen();
            }
        }

        /// <summary>
        /// ����ʵ��
        /// </summary>
        /// <param name="db">���ݿ����</param>
        /// <param name="sourcePt">���Ƶ�Դ��</param>
        /// <param name="targetPt">���Ƶ�Ŀ���</param>
        /// <param name="ids">���ƶ���</param>
        /// <returns>���ظ���ʵ���ObjectId�б�</returns>
        public static Dictionary<ObjectId, ObjectId> Copy(this Database db, Point3d sourcePt, Point3d targetPt, params ObjectId[] ids)
        {
            Dictionary<ObjectId, ObjectId> dictId = new Dictionary<ObjectId, ObjectId>();
            //�������ڸ���ʵ��ľ���
            Vector3d vector = sourcePt.GetVectorTo(targetPt);
            Matrix3d mt = Matrix3d.Displacement(vector);
            //��ȡid��ʾ��ʵ�����
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
        /// ��תʵ��
        /// </summary>
        /// <param name="id">ʵ���ObjectId</param>
        /// <param name="basePt">��ת����</param>
        /// <param name="angle">��ת�Ƕ�</param>
        public static void Rotate(this ObjectId id, Point3d basePt, double angle)
        {
            Matrix3d mt = Matrix3d.Rotation(angle, Vector3d.ZAxis, basePt);
            Entity ent = (Entity)id.GetObject(OpenMode.ForWrite);
            ent.TransformBy(mt);
            ent.DowngradeOpen();
        }

        /// <summary>
        /// ��תʵ�壨�����
        /// </summary>
        public static void Rotate(this ObjectId[] ids, Point3d basePt, double angle)
        {
            foreach (ObjectId id in ids)
            {
                id.Rotate(basePt, angle);
            }
        }

        /// <summary>
        /// ��תʵ��
        /// </summary>
        /// <param name="ent">ʵ��</param>
        /// <param name="basePt">��ת����</param>
        /// <param name="angle">��ת�Ƕ�</param>
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
        /// ����ʵ��
        /// </summary>
        /// <param name="id">ʵ���ObjectId</param>
        /// <param name="basePt">���Ż���</param>
        /// <param name="scaleFactor">���ű���</param>
        public static void Scale(this ObjectId id, Point3d basePt, double scaleFactor)
        {
            Matrix3d mt = Matrix3d.Scaling(scaleFactor, basePt);
            Entity ent = (Entity)id.GetObject(OpenMode.ForWrite);
            ent.TransformBy(mt);
            ent.DowngradeOpen();
        }

        /// <summary>
        /// ����ʵ��
        /// </summary>
        /// <param name="ent">ʵ��</param>
        /// <param name="basePt">���Ż���</param>
        /// <param name="scaleFactor">���ű���</param>
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
        /// ����ʵ��
        /// </summary>
        /// <param name="id">ʵ���ObjectId</param>
        /// <param name="mirrorPt1">������ĵ�һ��</param>
        /// <param name="mirrorPt2">������ĵڶ���</param>
        /// <param name="eraseSourceObject">�Ƿ�ɾ��Դ����</param>
        /// <returns>���ؾ���ʵ���ObjectId</returns>
        public static ObjectId Mirror(this ObjectId id, Point3d mirrorPt1, Point3d mirrorPt2, bool eraseSourceObject)
        {
            //������
            Line3d miLine = new Line3d(mirrorPt1, mirrorPt2);
            //�������
            Matrix3d mt = Matrix3d.Mirroring(miLine);
            ObjectId mirrorId = id;
            Entity ent = (Entity)id.GetObject(OpenMode.ForWrite);
            //���ɾ��Դ������ֱ�Ӷ�Դ����ʵ�о���任
            if (eraseSourceObject == true)
                ent.TransformBy(mt);
            //�����ɾ��Դ����������Դ����
            else
            {
                Entity entCopy = ent.GetTransformedCopy(mt);
                mirrorId = id.Database.AddToCurrentSpace(entCopy);
            }
            return mirrorId;
        }

        /// <summary>
        /// ����ʵ��
        /// </summary>
        /// <param name="ent">ʵ��</param>
        /// <param name="mirrorPt1">������ĵ�һ��</param>
        /// <param name="mirrorPt2">������ĵڶ���</param>
        /// <param name="eraseSourceObject">�Ƿ�ɾ��Դ����</param>
        /// <returns>���ؾ���ʵ���ObjectId</returns>
        public static ObjectId Mirror(this Entity ent, Point3d mirrorPt1, Point3d mirrorPt2, bool eraseSourceObject)
        {
            //������
            Line3d miLine = new Line3d(mirrorPt1, mirrorPt2);
            //�������
            Matrix3d mt = Matrix3d.Mirroring(miLine);
            ObjectId mirrorId = ObjectId.Null;
            if (ent.IsNewObject)
            {
                //���ɾ��Դ������ֱ�Ӷ�Դ����ʵ�о���任
                if (eraseSourceObject == true)
                    ent.TransformBy(mt);
                else //�����ɾ��Դ����������Դ����
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
        /// ����ʵ��
        /// </summary>
        /// <param name="id">ʵ���ObjectId</param>
        /// <param name="mirrorPt1">������ĵ�һ��</param>
        /// <param name="mirrorPt2">������ĵڶ���</param>
        /// <returns>���ؾ���ʵ���ObjectId</returns>
        public static ObjectId Mirror(this ObjectId id, Point3d mirrorPt1, Point3d mirrorPt2)
        {
            //������
            Line3d miLine = new Line3d(mirrorPt1, mirrorPt2);
            //�������
            Matrix3d mt = Matrix3d.Mirroring(miLine);
            //
            Entity ent = (Entity)id.GetObject(OpenMode.ForWrite);
            ent.TransformBy(mt);
            //
            return id;
        }

        /// <summary>
        /// ����ʵ�壨�����
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
        /// ƫ��ʵ��
        /// </summary>
        /// <param name="id">ʵ���ObjectId</param>
        /// <param name="dis">ƫ�ƾ���</param>
        /// <returns>����ƫ�ƺ��ʵ��Id����</returns>
        public static List<ObjectId> Offset(this ObjectId id, double dis)
        {
            List<ObjectId> ids = new List<ObjectId>();
            Curve cur = id.GetObject(OpenMode.ForWrite) as Curve;
            if (cur != null)
            {
                try
                {
                    //��ȡƫ�ƵĶ��󼯺�
                    DBObjectCollection offsetCurves = cur.GetOffsetCurves(dis);
                    //�����󼯺�����ת��Ϊʵ��������飬�Է������ʵ��Ĳ���
                    Entity[] offsetEnts = new Entity[offsetCurves.Count];
                    offsetCurves.CopyTo(offsetEnts, 0);
                    //��ƫ�ƵĶ�����뵽���ݿ�
                    ids = id.Database.AddToCurrentSpace(offsetEnts);
                }
                catch
                {
                    Application.ShowAlertDialog("�޷�ƫ�ƣ�");
                }
            }
            else
                Application.ShowAlertDialog("�޷�ƫ�ƣ�");
            //����ƫ�ƺ��ʵ��Id����
            return ids;
        }

        /// <summary>
        /// ƫ��ʵ��
        /// </summary>
        /// <param name="ent">ʵ��</param>
        /// <param name="dis">ƫ�ƾ���</param>
        /// <returns>����ƫ�ƺ��ʵ�弯��</returns>
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
                    Application.ShowAlertDialog("�޷�ƫ�ƣ�");
                }
            }
            else
                Application.ShowAlertDialog("�޷�ƫ�ƣ�");
            return offsetCurves;
        }

        /// <summary>
        /// ��������ʵ��
        /// </summary>
        /// <param name="id">ʵ���ObjectId</param>
        /// <param name="numRows">�������е�����,��ֵ����Ϊ����</param>
        /// <param name="numCols">�������е�����,��ֵ����Ϊ����</param>
        /// <param name="disRows">�м�ľ���</param>
        /// <param name="disCols">�м�ľ���</param>
        /// <returns>�������к��ʵ�弯�ϵ�ObjectId</returns>
        public static ObjectIdCollection ArrayRectang(this ObjectId id, int numRows, int numCols, double disRows, double disCols)
        {
            // ���ڷ������к��ʵ�弯�ϵ�ObjectId
            ObjectIdCollection ids = new ObjectIdCollection();
            // �Զ��ķ�ʽ��ʵ��
            Entity ent = (Entity)id.GetObject(OpenMode.ForRead);
            for (int m = 0; m < numRows; m++)
            {
                for (int n = 0; n < numCols; n++)
                {
                    //��ȡƽ�ƾ���
                    Matrix3d mt = Matrix3d.Displacement(new Vector3d(n * disCols, m * disRows, 0));
                    //����ʵ��
                    Entity entCopy = ent.GetTransformedCopy(mt);
                    //�����Ƶ�ʵ����ӵ�ģ�Ϳռ�
                    ObjectId entCopyId = id.Database.AddToCurrentSpace(entCopy);
                    //������ʵ���ObjectId��ӵ�������
                    ids.Add(entCopyId);
                }
            }
            //�л�ʵ��Ϊд��״̬
            ent.UpgradeOpen();
            //ɾ��ʵ��
            ent.Erase();
            //�������к��ʵ�弯�ϵ�ObjectId
            return ids;
        }

        /// <summary>
        /// ��������ʵ��
        /// </summary>
        /// <param name="id">ʵ���ObjectId</param>
        /// <param name="cenPt">�������е����ĵ�</param>
        /// <param name="numObj">�ڻ�����������Ҫ�����Ķ�������</param>
        /// <param name="angle">�Ի��ȱ�ʾ�����Ƕȣ���ֵ��ʾ��ʱ�뷽����ת����ֵ��ʾ˳ʱ�뷽����ת������Ƕ�Ϊ0�����</param>
        /// <returns>�������к��ʵ�弯�ϵ�ObjectId</returns>
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
        /// ɾ��ʵ��
        /// </summary>
        /// <param name="id">ʵ���ObjectId</param>
        public static void Erase(this ObjectId id)
        {
            DBObject ent = id.GetObject(OpenMode.ForWrite);
            ent.Erase();
        }

        /// <summary>
        /// �ֽ�ʵ�壬��ɾ��Դ����
        /// </summary>
        /// <param name="objectIds">Ҫ�ֽ�Ķ���</param>
        /// <returns>�ֽ�ɹ�</returns>
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
        /// ����ͼ�����ݿ�ģ�Ϳռ�������ʵ��İ�Χ��
        /// </summary>
        /// <param name="db">���ݿ����</param>
        /// <returns>����ģ�Ϳռ�������ʵ��İ�Χ��</returns>
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
