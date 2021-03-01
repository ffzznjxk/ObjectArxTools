using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace DotNetARX
{
    /// <summary>
    /// 块Jig工具类
    /// </summary>
    public class BlockJigTools : EntityJig
    {
        ///成员变量
        private Point3d m_position;
        private double m_angle;
        private Dictionary<AttributeReference, AttributeDefinition> m_dict;
        private Dictionary<string, string> m_atts;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="br">块参照</param>
        /// <param name="dict">属性参照与定义的映射</param>
        /// <param name="atts">属性的键值</param>
        public BlockJigTools(BlockReference br,
            Dictionary<AttributeReference, AttributeDefinition> dict,
            Dictionary<string, string> atts)
            : base(br)
        {
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            br.TransformBy(ed.CurrentUserCoordinateSystem);
            m_position = br.Position;
            m_angle = br.Rotation;
            m_dict = dict;
            m_atts = atts;
        }

        /// <summary>
        /// 动态更新
        /// </summary>
        /// <returns></returns>
        protected override bool Update()
        {
            var br = (BlockReference)Entity;
            br.Position = m_position;
            br.Rotation = m_angle;
            if (br.AttributeCollection.Count == 0)
                return true;
            foreach (var dic in m_dict)
            {
                var attRef = dic.Key;
                var attDef = dic.Value;
                attRef.SetAttributeFromBlock(attDef, br.BlockTransform);
                attRef.AdjustAlignment(attDef.Database);
                //判断是否包含指定的属性名称
                if (m_atts.ContainsKey(attDef.Tag))
                {
                    //设置属性值
                    attDef.TextString = m_atts[attDef.Tag];
                    attRef.TextString = attDef.TextString;
                }
            }
            return true;
        }

        /// <summary>
        /// 用户交互
        /// </summary>
        /// <param name="prompts"></param>
        /// <returns></returns>
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var ppo = new JigPromptPointOptions("\n指定插入点");
            ppo.BasePoint = Point3d.Origin;
            ppo.UserInputControls = UserInputControls.GovernedByUCSDetect | UserInputControls.NoZeroResponseAccepted;
            var ppr = prompts.AcquirePoint(ppo);
            if (m_position == ppr.Value)
                return SamplerStatus.NoChange;
            m_position = ppr.Value;
            return SamplerStatus.OK;
        }

        /// <summary>
        /// 调用Jig
        /// </summary>
        /// <returns></returns>
        public PromptStatus Run()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            if (doc == null)
                return PromptStatus.Error;
            return ed.Drag(this).Status;
        }
    }

    /// <summary>
    /// 块Jig类
    /// </summary>
    public static class BlockJig
    {
        /// <summary>
        /// 拖动并插入块参照
        /// </summary>
        /// <param name="spaceId">空间ID</param>
        /// <param name="layer">图层名</param>
        /// <param name="blockName">块名</param>
        /// <param name="scale">缩放比例</param>
        /// <param name="angle">角度</param>
        /// <param name="atts">属性集</param>
        /// <param name="colorIndex">颜色索引</param>
        /// <returns></returns>
        public static ObjectId JigBlockReference(this ObjectId spaceId,
             string layer, string blockName, Scale3d scale, double angle, Dictionary<string, string> atts, int colorIndex = 256)
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(blockName))
                    return ObjectId.Null;
                //以写的方式打开空间
                BlockTableRecord space = (BlockTableRecord)tr.GetObject(spaceId, OpenMode.ForWrite);
                //获取块表记录的Id
                ObjectId btrID = bt[blockName];
                //打开块表记录
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrID, OpenMode.ForRead);
                //插入块参照
                BlockReference br = new BlockReference(new Point3d(), btr.ObjectId);
                br.Layer = layer;
                br.ScaleFactors = scale;
                br.Rotation = angle;
                //颜色随层
                br.ColorIndex = colorIndex;
                space.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);
                //建立属性参照和定义间的映射
                var dict = new Dictionary<AttributeReference, AttributeDefinition>();
                if (btr.HasAttributeDefinitions)
                {
                    foreach (ObjectId id in btr)
                    {
                        AttributeDefinition attDef
                            = tr.GetObject(id, OpenMode.ForWrite) as AttributeDefinition;
                        if (attDef != null)
                        {
                            //创建一个新的属性对象
                            AttributeReference attRef = new AttributeReference();
                            //从属性定义获得属性对象的对象特性
                            attRef.SetAttributeFromBlock(attDef, br.BlockTransform);
                            //添加属性到块参照
                            br.AttributeCollection.AppendAttribute(attRef);
                            //添加属性到事务处理
                            tr.AddNewlyCreatedDBObject(attRef, true);
                            //字典中添加属性参照和属性定义的ObjectId
                            dict.Add(attRef, attDef);
                        }
                    }
                }
                //运行Jig
                BlockJigTools jig = new BlockJigTools(br, dict, atts);
                if (jig.Run() != PromptStatus.OK)
                    return ObjectId.Null;
                tr.Commit();
                return br.ObjectId;
            }
        }
    }

    /// <summary>
    /// 线JIG
    /// </summary>
    public class LineJigger : EntityJig
    {
        /// <summary>
        /// 
        /// </summary>
        public Point3d endPnt = new Point3d();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="line"></param>
        public LineJigger(Line line)
            : base(line)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override bool Update()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            (Entity as Line).EndPoint = endPnt;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prompts"></param>
        /// <returns></returns>
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            JigPromptPointOptions prOptions1 = new JigPromptPointOptions("\nNext point:");
            prOptions1.BasePoint = (Entity as Line).StartPoint;
            prOptions1.UseBasePoint = true;
            prOptions1.UserInputControls = UserInputControls.Accept3dCoordinates
                | UserInputControls.AnyBlankTerminatesInput
                | UserInputControls.GovernedByOrthoMode
                | UserInputControls.GovernedByUCSDetect
                | UserInputControls.UseBasePointElevation
                | UserInputControls.InitialBlankTerminatesInput
                | UserInputControls.NullResponseAccepted;
            PromptPointResult prResult1 = prompts.AcquirePoint(prOptions1);
            if (prResult1.Status == PromptStatus.Cancel)
                return SamplerStatus.Cancel;

            if (prResult1.Value.Equals(endPnt))
            {
                return SamplerStatus.NoChange;
            }
            else
            {
                endPnt = prResult1.Value;
                return SamplerStatus.OK;
            }
        }

        #region

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool LineJig(out Line line)
        {
            line = new Line();
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;

                PromptPointResult ppr = doc.Editor.GetPoint("\nStart point:");
                if (ppr.Status != PromptStatus.OK)
                    return false;
                Point3d pt = ppr.Value;
                line = new Line(pt, pt);
                line.TransformBy(doc.Editor.CurrentUserCoordinateSystem);

                LineJigger jigger = new LineJigger(line);
                PromptResult pr = doc.Editor.Drag(jigger);
                if (pr.Status == PromptStatus.OK)
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = tr.GetObject(db.BlockTableId,
                            OpenMode.ForRead) as BlockTable;
                        BlockTableRecord modelSpace = tr.GetObject(
                            bt[BlockTableRecord.ModelSpace],
                            OpenMode.ForWrite) as BlockTableRecord;

                        modelSpace.AppendEntity(jigger.Entity);
                        tr.AddNewlyCreatedDBObject(jigger.Entity, true);
                        tr.Commit();
                    }
                }
                else
                {
                    line.Dispose();
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
