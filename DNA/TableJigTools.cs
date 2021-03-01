using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace DotNetARX
{
    /// <summary>
    /// 表格Jig工具类
    /// </summary>
    public class TableJigTools : EntityJig
    {
        ///成员变量
        private Point3d m_position;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="table">表格</param>
        public TableJigTools(Table table) : base(table)
        {
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            table.TransformBy(ed.CurrentUserCoordinateSystem);
            m_position = table.Position;
        }

        /// <summary>
        /// 动态更新
        /// </summary>
        /// <returns></returns>
        protected override bool Update()
        {
            Table table = (Table)Entity;
            table.Position = m_position;
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
}
