using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace DotNetARX
{
    /// <summary>
    /// 块操作类
    /// </summary>
    public static partial class BlockTools
    {
        #region 创建块

        /// <summary>
        /// 创建一个块表记录并添加到数据库中
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="blockName">块名</param>
        /// <param name="ents">加入块中的实体列表</param>
        /// <returns>返回块表记录的Id</returns>
        public static ObjectId AddBlockTableRecord(this Database db, string blockName, List<Entity> ents)
        {
            BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            if (!bt.Has(blockName))
            {
                BlockTableRecord btr = new BlockTableRecord();
                btr.Name = blockName;
                ents.ForEach(ent => btr.AppendEntity(ent));
                bt.UpgradeOpen();
                bt.Add(btr);
                db.TransactionManager.AddNewlyCreatedDBObject(btr, true);
                bt.DowngradeOpen();
            }
            return bt[blockName];
        }

        /// <summary>
        /// 创建一个块表记录并添加到数据库中
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="blockName">块名</param>
        /// <param name="ents">加入块中的实体列表</param>
        /// <returns>返回块表记录的Id</returns>
        public static ObjectId AddBlockTableRecord(this Database db, string blockName, params Entity[] ents)
        {
            return AddBlockTableRecord(db, blockName, ents.ToList());
        }

        #endregion

        #region 插入块

        /// <summary>
        /// 在AutoCAD图形中插入块参照
        /// </summary>
        /// <param name="spaceID">当前空间的Id</param>
        /// <param name="layer">图层名</param>
        /// <param name="blockName">块名</param>
        /// <param name="position">插入点</param>
        /// <param name="scale">缩放比例</param>
        /// <param name="angle">旋转角度</param>
        /// <returns>返回块参照的Id</returns>
        public static ObjectId InsertBlockReference(this ObjectId spaceID, string layer, string blockName,
            Point3d position, Scale3d scale, double angle)
        {
            Database db = spaceID.Database;
            BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            if (!bt.Has(blockName))
                return ObjectId.Null;
            //以写的方式打开当前空间
            BlockTableRecord space = (BlockTableRecord)spaceID.GetObject(OpenMode.ForWrite);
            //获取块表记录的Id
            ObjectId btrID = bt[blockName];
            //创建一个块参照并设置插入点
            BlockReference br = new BlockReference(position, btrID)
            {
                ScaleFactors = scale,
                Layer = layer,
                Rotation = angle
            };
            space.AppendEntity(br);
            db.TransactionManager.AddNewlyCreatedDBObject(br, true);
            space.DowngradeOpen();
            return br.ObjectId;
        }

        /// <summary>
        /// 在AutoCAD图形中插入块参照
        /// </summary>
        /// <param name="spaceID">当前空间的Id</param>
        /// <param name="layer">图层名</param>
        /// <param name="blockName">块名</param>
        /// <param name="position">插入点</param>
        /// <param name="scale">缩放比例</param>
        /// <param name="angle">旋转角度</param>
        /// <param name="atts">属性的名称与取值</param>
        /// <returns>返回块参照的Id</returns>
        public static ObjectId InsertBlockReference(this ObjectId spaceID, string layer, string blockName,
            Point3d position, Scale3d scale, double angle, Dictionary<string, string> atts = null)
        {
            Database db = spaceID.Database;
            BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            if (!bt.Has(blockName))
                return ObjectId.Null;
            //以写的方式打开当前空间
            BlockTableRecord space = (BlockTableRecord)spaceID.GetObject(OpenMode.ForWrite);
            //获取块表记录的Id
            ObjectId btrID = bt[blockName];
            //创建一个块参照并设置插入点
            BlockReference br = new BlockReference(position, btrID)
            {
                ScaleFactors = scale,
                Layer = layer,
                Rotation = angle
            };
            space.AppendEntity(br);
            //判断块表记录是否包含属性定义
            if (atts != null && atts.Count > 0)
            {
                //打开块表记录
                BlockTableRecord btr = (BlockTableRecord)btrID.GetObject(OpenMode.ForRead);
                if (btr.HasAttributeDefinitions)
                {
                    //若包含属性定义，则遍历属性定义
                    foreach (ObjectId id in btr)
                    {
                        //检查是否是属性定义
                        AttributeDefinition attDef = id.GetObject(OpenMode.ForRead) as AttributeDefinition;
                        if (attDef != null)
                        {
                            //创建一个新的属性对象
                            AttributeReference attRef = new AttributeReference();
                            //从属性定义获得属性对象的对象特性
                            attRef.SetAttributeFromBlock(attDef, br.BlockTransform);
                            //设置属性对象的其它特性
                            attRef.Position = attDef.Position.TransformBy(br.BlockTransform);
                            attRef.Rotation = attDef.Rotation;
                            attRef.AdjustAlignment(db);
                            //判断是否包含指定的属性名称
                            if (atts.ContainsKey(attDef.Tag.ToUpper()))
                                //设置属性值
                                attRef.TextString = atts[attDef.Tag.ToUpper()];
                            //向块参照添加属性对象
                            br.AttributeCollection.AppendAttribute(attRef);
                            db.TransactionManager.AddNewlyCreatedDBObject(attRef, true);
                        }
                    }
                }
            }
            db.TransactionManager.AddNewlyCreatedDBObject(br, true);
            space.DowngradeOpen();
            return br.ObjectId;
        }

        #endregion
        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockRef"></param>
        /// <param name="tr"></param>
        /// <returns></returns>
        public static IDictionary<string, AttributeReference> AppendAttributes(this BlockReference blockRef, Transaction tr)
        {
            if (blockRef == null)
                throw new ArgumentNullException("blockRef");
            ErrorStatus.NoDatabase.Check(blockRef.Database != null);
            ErrorStatus.NotOpenForWrite.Check(blockRef.IsWriteEnabled);
            tr = tr ?? blockRef.Database.TransactionManager.TopTransaction;
            ErrorStatus.NoActiveTransactions.Check(tr != null);
            RXClass rxclass = RXObject.GetClass(typeof(AttributeDefinition));
            var result = new Dictionary<string, AttributeReference>(
               StringComparer.InvariantCultureIgnoreCase);
            var btr = (BlockTableRecord)tr.GetObject(
               blockRef.DynamicBlockTableRecord, OpenMode.ForRead);
            if (!btr.HasAttributeDefinitions)
                return result;
            var xform = blockRef.BlockTransform;
            var collection = blockRef.AttributeCollection;
            foreach (ObjectId id in btr)
            {
                if (id.ObjectClass.IsDerivedFrom(rxclass))
                {
                    var attdef = (AttributeDefinition)tr.GetObject(id, OpenMode.ForRead);
                    if (!attdef.Constant)
                    {
                        var attref = new AttributeReference();
                        try
                        {
                            attref.SetAttributeFromBlock(attdef, xform);
                            collection.AppendAttribute(attref);
                            tr.AddNewlyCreatedDBObject(attref, true);
                            result.Add(attref.Tag, attref);
                        }
                        catch
                        {
                            attref.Dispose();
                            throw;
                        }
                    }
                }
            }
            return result;
        }
        #region 块属性

        /// <summary>
        /// 获取块的属性名和属性值
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="blockName">块名</param>
        /// <param name="showAll">显示所有属性</param>
        /// <returns>返回块的属性名和属性值</returns>
        public static SortedDictionary<string, string> GetAttributesInBlock(this Database db, string blockName, bool showAll)
        {
            SortedDictionary<string, string> dicAttDef = new SortedDictionary<string, string>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (bt.Has(blockName))
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForRead);
                    if (btr.HasAttributeDefinitions)
                    {
                        foreach (ObjectId id in btr)
                        {
                            AttributeDefinition attDef = tr.GetObject(id, OpenMode.ForRead) as AttributeDefinition;
                            if (attDef != null)
                                if (showAll || attDef.Visible)
                                    dicAttDef.Add(attDef.Tag, attDef.TextString);
                        }
                    }
                    btr.Dispose();
                }
                bt.Dispose();
            }
            return dicAttDef;
        }

        /// <summary>
        /// 得到块参照的指定属性值
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="br">块参照</param>
        /// <param name="att">属性名</param>
        /// <returns>返回块参照的指定属性值</returns>
        public static string GetAttValue(this Database db, BlockReference br, string att)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in br.AttributeCollection)
                {
                    try
                    {
                        AttributeReference attRef = (AttributeReference)tr.GetObject(id, OpenMode.ForRead);
                        if (attRef.Tag == att)
                            return attRef.TextString;
                    }
                    catch { }
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// 获取指定名称的块参照的属性名和属性值
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="blockName">块名</param>
        /// <returns>返回块参照的属性名和属性值</returns>
        public static SortedDictionary<string, string> GetAttributesInBlockReference(this Database db, string blockName)
        {
            SortedDictionary<string, string> attributes = new SortedDictionary<string, string>();
            //筛选指定名称的块参照
            TypedValue[] values = { new TypedValue((int)DxfCode.Start, "INSERT"), new TypedValue((int)DxfCode.BlockName, blockName) };
            var filter = new SelectionFilter(values);
            Editor ed = Application.DocumentManager.GetDocument(db).Editor;
            var entSelected = ed.SelectAll(filter);
            //如果数据库不存在指定名称的块参照则返回
            if (entSelected.Status != PromptStatus.OK)
                return null;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //遍历块参照
                foreach (var id in entSelected.Value.GetObjectIds())
                {
                    BlockReference bref = (BlockReference)trans.GetObject(id, OpenMode.ForRead);
                    //遍历块参照中的属性
                    foreach (ObjectId attId in bref.AttributeCollection)
                    {
                        AttributeReference attRef = (AttributeReference)trans.GetObject(attId, OpenMode.ForRead);
                        //将块参照的属性名和属性值添加到字典中
                        attributes.Add(attRef.Tag, attRef.TextString);
                    }
                }
                trans.Commit();
            }
            //返回指定名称的块参照的属性名和属性值
            return attributes;
        }

        /// <summary>
        /// 获取块参照的属性名和属性值
        /// </summary>
        /// <param name="blockRefId">块参照的Id</param>
        /// <returns>返回块参照的属性名和属性值</returns>
        public static SortedDictionary<string, string> GetAttributesInBlockReference(this ObjectId blockRefId)
        {
            SortedDictionary<string, string> attributes = new SortedDictionary<string, string>();
            Database db = blockRefId.Database;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //获取块参照
                BlockReference bref = (BlockReference)trans.GetObject(blockRefId, OpenMode.ForRead);
                //遍历块参照的属性，并将其属性名和属性值添加到字典中
                foreach (ObjectId attId in bref.AttributeCollection)
                {
                    AttributeReference attRef = (AttributeReference)trans.GetObject(attId, OpenMode.ForRead);
                    attributes.Add(attRef.Tag, attRef.TextString);
                }
                trans.Commit();
            }
            //返回块参照的属性名和属性值
            return attributes;
        }

        /// <summary>
        /// 获取指定名称的块属性值
        /// </summary>
        /// <param name="blockRefId">块参照的Id</param>
        /// <param name="attributeName">属性名</param>
        /// <returns>返回指定名称的块属性值</returns>
        public static string GetAttributeInBlockReference(this ObjectId blockRefId, string attributeName)
        {
            //属性值
            Database db = HostApplicationServices.WorkingDatabase;
            string value = string.Empty;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //获取块参照
                BlockReference blockRef = (BlockReference)trans.GetObject(blockRefId, OpenMode.ForRead);
                //遍历块参照的属性
                foreach (ObjectId attId in blockRef.AttributeCollection)
                {
                    //获取块参照属性对象
                    AttributeReference attRef = (AttributeReference)trans.GetObject(attId, OpenMode.ForRead);
                    //判断属性名是否为指定的属性名
                    if (attRef.Tag.ToUpper() == attributeName.ToUpper())
                    {
                        //获取属性值
                        value = attRef.TextString;
                        break;
                    }
                }
                trans.Commit();
            }
            //返回属性值
            return value;
        }

        /// <summary>
        /// 获取指定名称的块属性的位置
        /// </summary>
        /// <param name="blockRefId">块参照的Id</param>
        /// <param name="attributeName">属性名</param>
        /// <returns>返回指定名称的块属性的位置</returns>
        public static Point3d GetAttributePositionInBlockReference(this ObjectId blockRefId, string attributeName)
        {
            //属性值
            Database db = HostApplicationServices.WorkingDatabase;
            Point3d value = Point3d.Origin;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //获取块参照
                BlockReference blockRef = (BlockReference)trans.GetObject(blockRefId, OpenMode.ForRead);
                //遍历块参照的属性
                foreach (ObjectId attId in blockRef.AttributeCollection)
                {
                    //获取块参照属性对象
                    AttributeReference attRef = (AttributeReference)trans.GetObject(attId, OpenMode.ForRead, true);
                    //判断属性名是否为指定的属性名
                    if (attRef.Tag.ToUpper() == attributeName.ToUpper())
                    {
                        //获取属性位置
                        value = attRef.AlignmentPoint;
                        break;
                    }
                }
                trans.Commit();
            }
            //返回属性值
            return value;
        }

        /// <summary>
        /// 更新块参照中的属性值
        /// </summary>
        /// <param name="blockRefId">块参照的Id</param>
        /// <param name="attNameValues">需要更新的属性名称与取值</param>
        public static void SetFieldInBlock(this Database db, ObjectId blockRefId, Dictionary<string, string> attNameValues)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                //获取块参照对象
                BlockReference blockRef = blockRefId.GetObject(OpenMode.ForRead) as BlockReference;
                if (blockRef != null)
                {
                    //遍历块参照中的属性
                    foreach (ObjectId id in blockRef.AttributeCollection)
                    {
                        try
                        {
                            //获取属性
                            AttributeReference attRef = id.GetObject(OpenMode.ForRead) as AttributeReference;
                            //判断是否包含指定的属性名称
                            if (attNameValues.ContainsKey(attRef.Tag.ToUpper()))
                            {
                                Field field = new Field(attNameValues[attRef.Tag.ToUpper()]);
                                field.Evaluate();
                                FieldEvaluationStatusResult fieldEval = field.EvaluationStatus;
                                if (fieldEval.Status == FieldEvaluationStatus.Success)
                                {
                                    try
                                    {
                                        attRef.UpgradeOpen();
                                        //set the field to attribute reference 
                                        attRef.SetField(field);
                                        attRef.DowngradeOpen();
                                        tr.AddNewlyCreatedDBObject(field, true);
                                    }
                                    catch (System.Exception ex)
                                    {
                                        field.Dispose();
                                    }
                                }
                            }
                        }
                        catch (System.Exception ex) { }
                    }
                }
                tr.Commit();
            }
        }

        /// <summary>
        /// 更新块参照中的属性值
        /// </summary>
        /// <param name="blockRefId">块参照的Id</param>
        /// <param name="attNameValues">需要更新的属性名称与取值</param>
        public static void UpdateAttributesInBlock(this ObjectId blockRefId, Dictionary<string, string> attNameValues)
        {
            //获取块参照对象
            BlockReference blockRef = blockRefId.GetObject(OpenMode.ForRead) as BlockReference;
            if (blockRef != null)
            {
                //遍历块参照中的属性
                foreach (ObjectId id in blockRef.AttributeCollection)
                {
                    try
                    {
                        //获取属性
                        AttributeReference attRef = id.GetObject(OpenMode.ForRead) as AttributeReference;
                        //判断是否包含指定的属性名称
                        if (attNameValues.ContainsKey(attRef.Tag.ToUpper()))
                        {
                            attRef.UpgradeOpen();
                            //设置属性值
                            attRef.TextString = attNameValues[attRef.Tag.ToUpper()].ToString();
                            attRef.DowngradeOpen();
                        }
                    }
                    catch (System.Exception) { }
                }
            }
        }

        /// <summary>
        /// 更新块参照中的属性值
        /// </summary>
        /// <param name="blockRefId">块参照的Id</param>
        /// <param name="attName">需要更新的属性名称</param>
        /// <param name="attValue">属性取值</param>
        public static void UpdateAttributesInBlock(this ObjectId blockRefId, string attName, string attValue)
        {
            Dictionary<string, string> att = new Dictionary<string, string> { { attName, attValue } };
            blockRefId.UpdateAttributesInBlock(att);
        }

        /// <summary>
        /// 更新块参照中属性的外部数据
        /// </summary>
        /// <param name="blockRefId">块参照的Id</param>
        /// <param name="attNameXdata">需要更新的属性名称与取值</param>
        public static void UpdateAttXdataInBlock(this ObjectId blockRefId, Dictionary<string, string> attNameXdata)
        {
            //获取块参照对象
            BlockReference blockRef = blockRefId.GetObject(OpenMode.ForRead) as BlockReference;
            if (blockRef != null)
            {
                //遍历块参照中的属性
                foreach (ObjectId id in blockRef.AttributeCollection)
                {
                    try
                    {
                        //获取属性
                        AttributeReference attRef = id.GetObject(OpenMode.ForRead) as AttributeReference;
                        //判断是否包含指定的属性名称
                        if (attNameXdata.ContainsKey(attRef.Tag.ToUpper()))
                        {
                            attRef.UpgradeOpen();
                            //删除外部数据
                            attRef.ObjectId.RemoveXData();
                            //添加外部数据
                            attRef.ObjectId.AddXData(
                                attRef.Tag.ToUpper(),
                                attNameXdata[attRef.Tag.ToUpper()].ToString());
                            //
                            attRef.DowngradeOpen();
                        }
                    }
                    catch (System.Exception) { }
                }
            }
        }

        /// <summary>
        /// 获取指定名称的块属性的外部数据
        /// </summary>
        /// <param name="blockRefId">块参照的Id</param>
        /// <param name="attributeName">属性名</param>
        /// <returns>返回指定名称的块属性的外部数据</returns>
        public static string GetAttXdataInBlockReference(this ObjectId blockRefId, string attributeName)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            string value = string.Empty;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //获取块参照
                BlockReference blockRef = (BlockReference)trans.GetObject(blockRefId, OpenMode.ForRead);
                //遍历块参照的属性
                foreach (ObjectId attId in blockRef.AttributeCollection)
                {
                    //获取块参照属性对象
                    AttributeReference attRef = (AttributeReference)trans.GetObject(attId, OpenMode.ForRead);
                    //判断属性名是否为指定的属性名
                    if (attRef.Tag.ToUpper() == attributeName.ToUpper())
                    {
                        //获取外部数据
                        value = attId.GetXDataValue(attRef.Tag.ToUpper());
                        break;
                    }
                }
                trans.Commit();
            }
            //返回属性值
            return value;
        }

        /// <summary>
        /// 设置属性文字的角度（在-90 + deviationAngle ~ 90 + deviationAngle之间）属性角度随块角度一致
        /// </summary>
        /// <param name="blockRefId">块参照的Id</param>
        /// <param name="ucsAngle">UCS偏转角度，WCS偏转角为0</param>
        /// <param name="deviationAngle">偏离角度，默认5°</param>
        public static void SetAttributeRotationWithBlockReference(this ObjectId blockRefId, double ucsAngle = 0, double deviationAngle = 5)
        {
            BlockReference blockRef = (BlockReference)blockRefId.GetObject(OpenMode.ForRead);
            foreach (ObjectId id in blockRef.AttributeCollection)
            {
                try
                {
                    AttributeReference attRef = (AttributeReference)id.GetObject(OpenMode.ForRead);
                    attRef.UpgradeOpen();
                    attRef.Rotation = blockRef.Rotation;
                    double angle = attRef.Rotation - ucsAngle;
                    if (angle < 0)
                    {
                        int m = Convert.ToInt32(Math.Ceiling(Math.Abs(angle / 2 / Math.PI)));
                        angle += m * 2 * Math.PI;
                    }
                    else
                        angle %= (2 * Math.PI);
                    if (angle > Math.PI * (90 + deviationAngle) / 180 && angle <= Math.PI * (270 + deviationAngle) / 180)
                    {
                        attRef.Rotation += Math.PI;
                    }
                    attRef.IsMirroredInX = false;
                    attRef.IsMirroredInY = false;
                    attRef.DowngradeOpen();
                }
                catch { }
            }
        }

        /// <summary>
        /// 设置属性文字的角度
        /// </summary>
        /// <param name="blockRefId">块参照的Id</param>
        /// <param name="angle">角度</param>
        public static void SetAttributeAngle(this ObjectId blockRefId, double angle)
        {
            BlockReference blockRef = (BlockReference)blockRefId.GetObject(OpenMode.ForRead);
            foreach (ObjectId id in blockRef.AttributeCollection)
            {
                try
                {
                    AttributeReference attRef = (AttributeReference)id.GetObject(OpenMode.ForRead);
                    attRef.UpgradeOpen();
                    attRef.Rotation = angle;
                    attRef.IsMirroredInX = false;
                    attRef.IsMirroredInY = false;
                    attRef.DowngradeOpen();
                }
                catch { }
            }
        }

        /// <summary>
        /// 设置属性文字的角度
        /// </summary>
        /// <param name="blockRefId">块参照的Id</param>
        /// <param name="angle">角度</param>
        public static void SetAttributeAngle(this ObjectId blockRefId, double angle, params string[] attTag)
        {
            BlockReference blockRef = (BlockReference)blockRefId.GetObject(OpenMode.ForRead);
            foreach (ObjectId id in blockRef.AttributeCollection)
            {
                try
                {
                    AttributeReference attRef = (AttributeReference)id.GetObject(OpenMode.ForRead);
                    attRef.UpgradeOpen();
                    if (attTag.Contains(attRef.Tag))
                    {
                        attRef.Rotation = angle;
                        attRef.IsMirroredInX = false;
                        attRef.IsMirroredInY = false;
                    }
                    attRef.DowngradeOpen();
                }
                catch { }
            }
        }

        /// <summary>
        /// 读取属性文字的高度
        /// </summary>
        /// <param name="blockRefId">块参照的Id</param>
        /// <param name="attTag">属性名称</param>
        public static double GetAttributeHeight(this ObjectId blockRefId, string attTag)
        {
            BlockReference blockRef = (BlockReference)blockRefId.GetObject(OpenMode.ForRead);
            foreach (ObjectId id in blockRef.AttributeCollection)
            {
                try
                {
                    AttributeReference attRef = (AttributeReference)id.GetObject(OpenMode.ForRead);
                    if (attRef.Tag == attTag)
                        return attRef.Height;
                }
                catch (System.Exception)
                {

                }
            }
            return 0;
        }

        /// <summary>
        /// 读取属性文字的高度
        /// </summary>
        /// <param name="blockRefId">块参照的Id</param>
        /// <param name="attTag">属性名称</param>
        public static double GetAttributeAngle(this ObjectId blockRefId, string attTag)
        {
            BlockReference blockRef = (BlockReference)blockRefId.GetObject(OpenMode.ForRead);
            foreach (ObjectId id in blockRef.AttributeCollection)
            {
                try
                {
                    AttributeReference attRef = (AttributeReference)id.GetObject(OpenMode.ForRead);
                    if (attRef.Tag == attTag)
                        return attRef.Rotation;
                }
                catch (System.Exception)
                {

                }
            }
            return 0;
        }

        /// <summary>
        /// 设置属性文字的高度
        /// </summary>
        /// <param name="blockRefId">块参照的Id</param>
        /// <param name="attTag">属性名称</param>
        /// <param name="height">文字高度</param>
        public static void SetAttributeHeight(this ObjectId blockRefId, string attTag, double height)
        {
            BlockReference blockRef = (BlockReference)blockRefId.GetObject(OpenMode.ForRead);
            foreach (ObjectId id in blockRef.AttributeCollection)
            {
                try
                {
                    AttributeReference attRef = (AttributeReference)id.GetObject(OpenMode.ForRead);
                    if (attRef.Tag == attTag)
                    {
                        attRef.UpgradeOpen();
                        attRef.Height = height;
                        attRef.DowngradeOpen();
                        break;
                    }
                }
                catch (System.Exception)
                {

                }
            }
        }

        /// <summary>
        /// 设置块内所有属性文字的高度
        /// </summary>
        /// <param name="blockRefId">块参照的Id</param>
        /// <param name="height">文字高度</param>
        /// <param name="includeNestedBlock">是否包含嵌套块</param>
        public static void SetAttributeHeight(this ObjectId blockRefId, double height, bool includeNestedBlock)
        {
            BlockReference blockRef = (BlockReference)blockRefId.GetObject(OpenMode.ForRead);
            //当前块内的属性文字
            foreach (ObjectId id in blockRef.AttributeCollection)
            {
                try
                {
                    AttributeReference attRef = (AttributeReference)id.GetObject(OpenMode.ForWrite);
                    attRef.Height = height;
                }
                catch (System.Exception)
                {

                }
            }
            //查找是否有嵌套块
            if (includeNestedBlock)
            {
                ObjectId btrId = blockRef.BlockTableRecord;
                BlockTableRecord btr = btrId.GetObject(OpenMode.ForRead) as BlockTableRecord;
                foreach (ObjectId id in btr)
                {
                    BlockReference nestBr = id.GetObject(OpenMode.ForRead) as BlockReference;
                    //是嵌套块
                    if (nestBr != null)
                    {
                        SetAttributeHeight(nestBr.ObjectId, height, true);
                    }
                }
            }
        }

        /// <summary>
        /// 更改属性定义（未用）
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="blockName">块名</param>
        /// <param name="attName">属性名</param>
        /// <param name="textStyleId">字体样式的ID</param>
        /// <param name="height">字高</param>
        /// <param name="widthFactor">宽度系数</param>
        public static void UpdateMTextAttDef(this Database db, string blockName, string attName, ObjectId textStyleId, double height, double widthFactor)
        {
            BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            //获取块表记录的Id
            ObjectId btrID = bt[blockName];
            //打开块表记录
            BlockTableRecord btr = (BlockTableRecord)btrID.GetObject(OpenMode.ForRead);
            //遍历属性定义
            foreach (ObjectId id in btr)
            {
                //检查是否是属性定义
                AttributeDefinition attDef = id.GetObject(OpenMode.ForRead) as AttributeDefinition;
                if (attDef != null && attDef.Tag == attName)
                {
                    attDef.UpgradeOpen();
                    attDef.TextStyleId = textStyleId;
                    attDef.Height = height;
                    attDef.WidthFactor = widthFactor;
                    attDef.DowngradeOpen();
                }
            }
        }

        /// <summary>
        /// 更改属性文字的宽度系数
        /// </summary>
        /// <param name="blockRefId">块参照的ObjectId</param>
        /// <param name="attTag">属性标签</param>
        /// <param name="widthFactor">宽度系数</param>
        public static void AttributeWidthFactor(this ObjectId blockRefId, string attTag, double widthFactor)
        {
            BlockReference blockRef = (BlockReference)blockRefId.GetObject(OpenMode.ForRead);
            foreach (ObjectId id in blockRef.AttributeCollection)
            {
                try
                {
                    AttributeReference attRef = id.GetObject(OpenMode.ForRead) as AttributeReference;
                    if (attRef.Tag == attTag)
                    {
                        attRef.UpgradeOpen();
                        attRef.WidthFactor = widthFactor;
                        attRef.DowngradeOpen();
                    }
                }
                catch (System.Exception)
                {

                }
            }
        }

        /// <summary>
        /// 获得块参照中属性文字的实际宽度
        /// </summary>
        /// <param name="blockRefId">块参照ObjectId</param>
        /// <param name="attTag">属性标记</param>
        /// <param name="isPaperSpace">是否图纸空间</param>
        /// <returns>属性文字的实际宽度</returns>
        public static double GetAttTextWidth(this ObjectId blockRefId, string attTag, bool isPaperSpace)
        {
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            //
            BlockReference blockRef = blockRefId.GetObject(OpenMode.ForRead) as BlockReference;
            //得到文字的实际宽度
            foreach (ObjectId attId in blockRef.AttributeCollection)
            {
                //获取属性
                AttributeReference attRef = attId.GetObject(OpenMode.ForRead) as AttributeReference;
                //判断是否包含指定的属性名称
                if (attRef.Tag == attTag)
                {
                    Extents2d ext = ed.TranslateUCSExtents(attRef, isPaperSpace);
                    return ext.MaxPoint.X - ext.MinPoint.X;
                }
            }
            return 0;
        }

        /// <summary>
        /// 为块表记录添加属性
        /// </summary>
        /// <param name="blockId">块表记录的Id</param>
        /// <param name="atts">要加入的块属性列表</param>
        public static void AddAttsToBlock(this ObjectId blockId, List<AttributeDefinition> atts)
        {
            //获取数据库对象
            Database db = blockId.Database;
            //打开块表记录为写
            BlockTableRecord btr = (BlockTableRecord)blockId.GetObject(OpenMode.ForWrite);
            //遍历属性定义对象列表，为块表记录添加属性，通知事务处理
            foreach (AttributeDefinition att in atts)
            {
                btr.AppendEntity(att);
                db.TransactionManager.AddNewlyCreatedDBObject(att, true);
            }
            btr.DowngradeOpen();
        }

        /// <summary>
        /// 为块表记录添加属性
        /// </summary>
        /// <param name="blockId">块表记录的Id</param>
        /// <param name="atts">要加入的块属性列表</param>
        public static void AddAttsToBlock(this ObjectId blockId, params AttributeDefinition[] atts)
        {
            blockId.AddAttsToBlock(atts.ToList());
        }

        #endregion

        #region 通过块名获取块参照

        /// <summary>
        /// 获取指定块名的块参照
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="blockName">块名</param>
        /// <returns>返回指定块名的块参照</returns>
        public static List<BlockReference> GetAllBlockReferences(this Database db, string blockName)
        {
            List<BlockReference> blocks = new List<BlockReference>();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //打开块表
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                //打开指定块名的块表记录
                BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[blockName], OpenMode.ForRead);
                //获取指定块名的块参照集合的Id
                ObjectIdCollection blockIds = btr.GetBlockReferenceIds(true, true);
                //遍历块参照的Id
                foreach (ObjectId id in blockIds)
                {
                    //获取块参照
                    BlockReference block = (BlockReference)trans.GetObject(id, OpenMode.ForRead);
                    //将块参照添加到返回列表
                    blocks.Add(block);
                }
                trans.Commit();
            }
            //返回块参照列表
            return blocks;
        }

        /// <summary>
        /// 返回指定块名的动态块参照
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="blockName">动态块的块名</param>
        /// <returns>返回指定块名的动态块参照</returns>
        public static List<BlockReference> GetAllDynBlockReferences(this Database db, string blockName)
        {
            List<BlockReference> blocks = new List<BlockReference>();
            var trans = db.TransactionManager;
            BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
            BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[blockName], OpenMode.ForRead);
            blocks = (from b in db.GetEntsInDatabase<BlockReference>()
                      where b.GetBlockName() == blockName
                      select b).ToList();
            return blocks;
        }

        #endregion

        #region 导入块

        /// <summary>
        /// 导入外部文件中的块
        /// </summary>
        /// <param name="db">目标数据库</param>
        /// <param name="fileName">包含完整路径的外部文件名</param>
        /// <param name="clone">复制模式</param>
        public static void ImportBlockFromDwg(this Database db, string fileName, DuplicateRecordCloning clone)
        {
            //创建一个临时数据库对象，以读入外部文件中的对象
            Database tempDb = new Database(false, true);
            //把fileName读入到临时数据库中
            tempDb.ReadDwgFile(fileName, FileOpenMode.OpenForReadAndAllShare, true, null);
            tempDb.CloseInput(true);
            //创建一个变量，用来存储块的ObjectId列表
            ObjectIdCollection blockIds = new ObjectIdCollection();
            //在临时数据库中开始事务处理
            using (Transaction trans = tempDb.TransactionManager.StartTransaction())
            {
                //打开临时数据库的块表
                BlockTable bt = (BlockTable)trans.GetObject(tempDb.BlockTableId, OpenMode.ForRead, false);
                //遍历块
                foreach (ObjectId id in bt)
                {
                    BlockTableRecord btr = (BlockTableRecord)trans.GetObject(id, OpenMode.ForRead, false);
                    //只加入命名块和非布局块到复制列表中
                    if (!btr.IsAnonymous && !btr.IsLayout)
                        blockIds.Add(id);
                    btr.Dispose();
                }
                bt.Dispose();
            }
            //定义一个IdMapping对象
            IdMapping map = new IdMapping();
            //从临时数据库向当前数据库复制块表记录
            tempDb.WblockCloneObjects(blockIds, db.BlockTableId, map, clone, false);
            //操作完成，销毁临时数据库
            tempDb.Dispose();
        }

        /// <summary>
        /// 导入外部文件中的块
        /// </summary>
        /// <param name="db">目标数据库</param>
        /// <param name="fileName">包含完整路径的外部文件名</param>
        /// <param name="blockName">要求导入的块名</param>
        /// <param name="clone">复制模式</param>
        /// <returns>若导入成功返回true，否则返回false</returns>
        public static bool ImportBlockFromDwg(this Database db, string fileName, string blockName, DuplicateRecordCloning clone)
        {
            bool imported = true;
            string errMsg = "";
            //创建一个临时数据库对象，以读入外部文件中的对象
            Database tempDb = new Database(false, true);
            //把fileName读入到临时数据库中
            tempDb.ReadDwgFile(fileName, FileOpenMode.OpenForReadAndAllShare, true, null);
            tempDb.CloseInput(true);
            //创建一个变量，用来存储块的ObjectId列表
            ObjectIdCollection blockIds = new ObjectIdCollection();
            //在临时数据库中开始事务处理
            using (Transaction trans = tempDb.TransactionManager.StartTransaction())
            {
                //打开临时数据库的块表
                BlockTable bt = (BlockTable)trans.GetObject(tempDb.BlockTableId, OpenMode.ForRead, false);
                if (bt.Has(blockName))
                {
                    blockIds.Add(bt[blockName]);
                    //定义一个IdMapping对象
                    IdMapping map = new IdMapping();
                    //从临时数据库向当前数据库复制块表记录
                    try
                    {
                        tempDb.WblockCloneObjects(blockIds, db.BlockTableId, map, clone, false);
                    }
                    catch (System.Exception ex)
                    {
                        errMsg = ex.Message == "eNotAllowedForThisProxy"
                            ? "本图块由天正或其他第三方软件绘制，请用相关软件导入。"
                            : "发生未知错误，导入图块失败！";
                        imported = false;
                    }
                }
                else
                {
                    errMsg = "图块【" + blockName + "】不存在，导入失败！";
                    imported = false;
                }
                bt.Dispose();
            }
            //操作完成，销毁临时数据库
            tempDb.Dispose();
            //导入不成功，显示消息
            if (!imported)
                System.Windows.Forms.MessageBox.Show(
                    errMsg, "错误",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            return imported;
        }

        #endregion

        #region 读取及修改块名

        /// <summary>
        /// 获取文件中的块名列表
        /// </summary>
        /// <param name="db">数据库</param>
        public static List<string> GetBlockNamesFromDwg(this Database db)
        {
            //创建一个列表，用来存储块名
            List<string> blockNames = new List<string>();
            //在临时数据库中开始事务处理
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //打开临时数据库的块表
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead, false);
                //遍历块
                foreach (ObjectId id in bt)
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead, false);
                    //只加入命名块和非布局块到复制列表中
                    if (!btr.IsAnonymous && !btr.IsLayout)
                        blockNames.Add(btr.Name);
                }
            }
            //排序
            if (blockNames.Count() != 0)
                blockNames = blockNames.OrderBy(b => b).ToList();
            //返回列表
            return blockNames;
        }

        /// <summary>
        /// 获取外部文件中的块名列表
        /// </summary>
        /// <param name="fileName">包含完整路径的外部文件名</param>
        public static List<string> GetBlockNamesFromDwg(string fileName)
        {
            //创建一个列表，用来存储块名
            List<string> blockNames = new List<string>();
            //创建一个临时数据库对象，以读入外部文件中的对象
            Database tempDb = new Database(false, true);
            //把fileName读入到临时数据库中
            tempDb.ReadDwgFile(fileName, FileOpenMode.OpenForReadAndAllShare, true, null);
            tempDb.CloseInput(true);
            //在临时数据库中开始事务处理
            using (Transaction tr = tempDb.TransactionManager.StartTransaction())
            {
                //打开临时数据库的块表
                BlockTable bt = (BlockTable)tr.GetObject(tempDb.BlockTableId, OpenMode.ForRead, false);
                //遍历块
                foreach (ObjectId id in bt)
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead, false);
                    //只加入命名块和非布局块到复制列表中
                    if (!btr.IsAnonymous && !btr.IsLayout)
                        blockNames.Add(btr.Name);
                }
            }
            //操作完成，销毁临时数据库
            tempDb.Dispose();
            //排序
            if (blockNames.Count() != 0)
                blockNames = blockNames.Distinct().OrderBy(b => b).ToList();
            //返回列表
            return blockNames;
        }

        /// <summary>
        /// 修改块名
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="oldName">旧块名</param>
        /// <param name="newName">新块名</param>
        public static bool ChangeBlockName(this Database db, string oldName, string newName)
        {
            BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            if (!bt.Has(oldName))
                return false;
            ObjectId blockId = bt[oldName];
            BlockTableRecord btr = (BlockTableRecord)blockId.GetObject(OpenMode.ForWrite);
            btr.Name = newName;
            btr.DowngradeOpen();
            return true;
        }

        #endregion

        #region 获取块名

        /// <summary>
        /// 获取块参照的块名（包括动态块）
        /// </summary>
        /// <param name="id">块参照的Id</param>
        /// <returns>返回块名</returns>
        public static string GetBlockName(this ObjectId id)
        {
            //获取块参照
            BlockReference bref = id.GetObject(OpenMode.ForRead) as BlockReference;
            //如果是块参照
            if (bref != null)
                return GetBlockName(bref);
            else
                return null;
        }

        /// <summary>
        /// 获取块参照的块名（包括动态块）
        /// </summary>
        /// <param name="bref">块参照</param>
        /// <returns>返回块名</returns>
        public static string GetBlockName(this BlockReference bref)
        {
            //如果块参照不存在则返回
            if (bref == null)
                return null;
            //获取动态块所属的动态块表记录
            ObjectId idDyn = bref.DynamicBlockTableRecord;
            //打开动态块表记录
            BlockTableRecord btr = (BlockTableRecord)idDyn.GetObject(OpenMode.ForRead);
            //获取块名
            return btr.Name;
        }

        #endregion

        #region 动态块

        /// <summary>
        /// 获取块的动态属性名和属性值
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="blockName">块名</param>
        /// <param name="showAll">是否显示全部</param>
        /// <returns>返回块的动态属性名和属性值</returns>
        public static SortedDictionary<string, List<string>> GetDynPropsInBlock(this Database db, string blockName, bool showAll)
        {
            SortedDictionary<string, List<string>> dicDynProps = new SortedDictionary<string, List<string>>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (bt.Has(blockName))
                {
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    BlockReference br = new BlockReference(new Point3d(), bt[blockName]);
                    ms.AppendEntity(br);
                    tr.AddNewlyCreatedDBObject(br, false);
                    if (br.IsDynamicBlock)
                    {
                        DynamicBlockReferencePropertyCollection props = br.DynamicBlockReferencePropertyCollection;
                        //遍历动态属性
                        foreach (DynamicBlockReferenceProperty prop in props)
                        {
                            if (showAll || prop.Show)
                            {
                                List<string> listDynProps = new List<string>();
                                //类型
                                listDynProps.Insert(0, prop.PropertyTypeCode.ToString());
                                //描述
                                listDynProps.Insert(1, prop.Description.ToString());
                                //允许值
                                string allowedValue = "";
                                foreach (var value in prop.GetAllowedValues())
                                    allowedValue += value + "#";
                                listDynProps.Insert(2, allowedValue.Trim('#'));
                                //当前值
                                listDynProps.Insert(3, prop.Value.ToString());
                                //添加到字典
                                dicDynProps.Add(prop.PropertyName, listDynProps);
                            }
                        }
                    }
                    br.Dispose();
                }
                bt.Dispose();
            }
            return dicDynProps;
        }

        /// <summary>
        /// 获得动态块的所有动态属性
        /// </summary>
        /// <param name="blockId">动态块的Id</param>
        /// <returns>返回动态块的所有属性</returns>
        public static DynamicBlockReferencePropertyCollection GetDynProperties(this ObjectId blockId)
        {
            //获取块参照
            BlockReference br = blockId.GetObject(OpenMode.ForRead) as BlockReference;
            //如果不是动态块则返回
            if (br == null && !br.IsDynamicBlock)
                return null;
            //返回动态块的动态属性
            return br.DynamicBlockReferencePropertyCollection;
        }

        /// <summary>
        /// 获取动态块的动态属性值
        /// </summary>
        /// <param name="blockId">动态块的Id</param>
        /// <param name="propName">需要查找的动态属性名</param>
        /// <returns>返回指定动态属性的值</returns>
        public static string GetDynBlockValue(this ObjectId blockId, string propName)
        {
            string propValue = null;
            //获得动态块的所有动态属性
            var props = blockId.GetDynProperties();
            //遍历动态属性
            foreach (DynamicBlockReferenceProperty prop in props)
            {
                //如果动态属性的名称与输入的名称相同
                if (prop.PropertyName == propName)
                {
                    //获取动态属性值并结束遍历
                    propValue = prop.Value.ToString();
                    break;
                }
            }
            return propValue;
        }

        /// <summary>
        /// 设置动态块的动态属性
        /// </summary>
        /// <param name="blockId">动态块的ObjectId</param>
        /// <param name="propName">动态属性的名称</param>
        /// <param name="propValue">动态属性的值</param>
        public static void SetDynBlockValue(this ObjectId blockId, string propName, object propValue)
        {
            //获得动态块的所有动态属性
            DynamicBlockReferencePropertyCollection props = blockId.GetDynProperties();
            //遍历动态属性
            foreach (DynamicBlockReferenceProperty prop in props)
            {
                //如果动态属性为可写，且名称相同
                if (!prop.ReadOnly && prop.PropertyName == propName)
                {
                    prop.Value = propValue;
                    break;
                }
            }
        }

        /// <summary>
        /// 设置动态块的动态属性
        /// </summary>
        /// <param name="blockId">动态块的ObjectId</param>
        /// <param name="dynAtts">动态属性字典</param>
        public static void SetDynBlockValues(this ObjectId blockId, Dictionary<string, object> dynAtts)
        {
            //获得动态块的所有动态属性
            DynamicBlockReferencePropertyCollection props = blockId.GetDynProperties();
            foreach (var att in dynAtts)
            {
                //遍历动态属性
                foreach (DynamicBlockReferenceProperty prop in props)
                {
                    //如果动态属性为可写，且名称相同
                    if (!prop.ReadOnly && prop.PropertyName == att.Key)
                    {
                        prop.Value = att.Value;
                        break;
                    }
                }
            }
        }

        #endregion

        #region 杂项

        /// <summary>
        /// 
        /// </summary>
        /// <param name="br"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetAttField(this BlockReference br, string key)
        {
            //遍历块参照中的属性
            foreach (ObjectId att in br.AttributeCollection)
            {
                try
                {
                    //获取属性
                    AttributeReference attRef = att.GetObject(OpenMode.ForRead) as AttributeReference;
                    //判断是否包含指定的属性名称
                    if (attRef.Tag == key)
                    {
                        return $"%<\\AcObjProp Object(%<\\_ObjId {att.OldIdPtr}>%).TextString>%";
                    }
                }
                catch { }
            }
            return string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetAttField(this ObjectId id, string key)
        {
            var br = (BlockReference)id.GetObject(OpenMode.ForRead);
            if (br != null)
                return br.GetAttField(key);
            return string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="br"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static string GetDynLookupField(this BlockReference br, int parameter)
        {
            return $"%<\\AcObjProp Object(%<\\_ObjId {br.ObjectId.OldIdPtr}>%).Parameter({parameter}).lookupString>%";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static string GetDynLookupField(this ObjectId id, int parameter)
        {
            var br = (BlockReference)id.GetObject(OpenMode.ForRead);
            if (br != null)
                return br.GetDynLookupField(parameter);
            return string.Empty;
        }

        /// <summary>
        /// 块的可分解性
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="blockName">块名</param>
        /// <returns>返回块的可分解性</returns>
        public static bool BlockExplodable(this Database db, string blockName)
        {
            bool result = false;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (bt.Has(blockName))
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForRead);
                    result = btr.Explodable;
                    btr.Dispose();
                }
                bt.Dispose();
            }
            return result;
        }

        /// <summary>
        /// 得到外部文件中块参照的图层及其颜色
        /// </summary>
        /// <param name="fileName">包含完整路径的外部文件名</param>
        /// <param name="blockName">块名</param>
        public static Dictionary<string, Color> GetBlockReferenceLayerNameColor(string fileName, string blockName)
        {
            string layerName = "";
            Color layerColor = new Color();
            using (Database tempDb = new Database(false, true))
            {
                //读取文件
                tempDb.ReadDwgFile(fileName, FileOpenMode.OpenForReadAndAllShare, true, null);
                tempDb.CloseInput(true);
                using (var tr = tempDb.TransactionManager.StartTransaction())
                {
                    //查找层名，有可能不存在图层，因为没有实际的块参照存在
                    BlockTable bt = (BlockTable)tr.GetObject(tempDb.BlockTableId, OpenMode.ForRead);
                    foreach (ObjectId id in bt)
                    {
                        //遍历各个空间查找，但只返回第一个块参照的层名
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                        if (btr.IsLayout)
                        {
                            foreach (ObjectId bid in btr)
                            {
                                try
                                {
                                    if (bid.GetObject(OpenMode.ForRead) is BlockReference)
                                    {
                                        BlockReference block = bid.GetObject(OpenMode.ForRead) as BlockReference;
                                        if (block.GetBlockName() == blockName)
                                        {
                                            layerName = block.Layer;
                                            break;
                                        }
                                    }
                                }
                                catch
                                {

                                }
                            }
                        }
                        if (layerName != "")
                            break;
                    }
                    //当有块参照时，得到层颜色
                    if (layerName != "")
                    {
                        LayerTable lt = (LayerTable)tr.GetObject(tempDb.LayerTableId, OpenMode.ForRead);
                        LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForRead);
                        layerColor = ltr.Color;
                    }
                }
            }
            //无结果
            return new Dictionary<string, Color> { { layerName, layerColor } };
        }

        /// <summary>
        /// 块剪切
        /// </summary>
        /// <param name="db"></param>
        /// <param name="newBr"></param>
        /// <param name="pts"></param>
        public static void BlockreferenceXclip(this Database db, BlockReference br, Point2dCollection pts)
        {
            const string filterDictName = "ACAD_FILTER";
            const string spatialName = "SPATIAL";
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Create spatial filter
                // Just zeroes for the elevation and clip depths
                SpatialFilterDefinition sfd = new SpatialFilterDefinition(pts, Vector3d.ZAxis, 0.0, 0.0, 0.0, true);
                SpatialFilter sf = new SpatialFilter();
                sf.Definition = sfd;

                // Create extension dictionary if doesn't exist
                if (br.ExtensionDictionary == ObjectId.Null)
                {
                    br.UpgradeOpen();
                    br.CreateExtensionDictionary();
                    br.DowngradeOpen();
                }

                // Add spatial filter to extension dictionary
                DBDictionary xDict = (DBDictionary)tr.GetObject(br.ExtensionDictionary, OpenMode.ForWrite);

                if (xDict.Contains(filterDictName))
                {
                    DBDictionary fDict = (DBDictionary)tr.GetObject(xDict.GetAt(filterDictName), OpenMode.ForWrite);

                    if (fDict.Contains(spatialName))
                        fDict.Remove(spatialName);
                    fDict.SetAt(spatialName, sf);
                }
                else
                {
                    DBDictionary fDict = new DBDictionary();
                    xDict.SetAt(filterDictName, fDict);
                    tr.AddNewlyCreatedDBObject(fDict, true);
                    fDict.SetAt(spatialName, sf);
                }
                tr.AddNewlyCreatedDBObject(sf, true);
                tr.Commit();
            }
            //db.GetEditor().Regen();
        }

        /// <summary>
        /// 块剪切
        /// </summary>
        /// <param name="db"></param>
        /// <param name="br"></param>
        /// <param name="pl"></param>
        public static void BlockreferenceXclip(this Database db, BlockReference br, Polyline pl)
        {
            //
            Point3dCollection pts3d = new Point3dCollection();
            for (int i = 0; i < pl.NumberOfVertices; i++)
                pts3d.Add(pl.GetPoint3dAt(i).TransformBy(br.BlockTransform.Inverse()));
            //
            Point2dCollection pts2d = new Point2dCollection();
            foreach (Point3d pt in pts3d)
                pts2d.Add(new Point2d(pt.X, pt.Y));

            db.BlockreferenceXclip(br, pts2d);
        }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public static class RuntimeExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="es"></param>
        /// <param name="condition"></param>
        /// <param name="message"></param>
        public static void Check(this ErrorStatus es, bool condition, string message = null)
        {
            if (!condition)
            {
                if (!string.IsNullOrEmpty(message))
                    throw new Autodesk.AutoCAD.Runtime.Exception(es, message);
                else
                    throw new Autodesk.AutoCAD.Runtime.Exception(es);
            }
        }
    }
}
