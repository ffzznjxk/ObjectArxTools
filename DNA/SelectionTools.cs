using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace DotNetARX
{
    /// <summary>
    /// 选择操作类
    /// </summary>
    public static class SelectionTools
    {
        /// <summary>
        /// 图层过滤器
        /// </summary>
        /// <param name="layers">图层</param>
        /// <returns></returns>
        public static SelectionFilter LayerFilters(params string[] layers)
        {
            //构建列表
            List<TypedValue> tv = new List<TypedValue>
            {
                new TypedValue((int)DxfCode.Operator, "<or")
            };
            //增加图层到列表中
            foreach (string layer in layers)
                tv.Add(new TypedValue((int)DxfCode.LayerName, layer));
            //结束
            tv.Add(new TypedValue((int)DxfCode.Operator, "or>"));
            //返回过滤器
            return new SelectionFilter(tv.ToArray());
        }

        /// <summary>
        /// 选择过一点的所有实体
        /// </summary>
        /// <param name="ed">命令行对象</param>
        /// <param name="point">点</param>
        /// <param name="filter">选择过滤器</param>
        /// <returns>返回过指定点的所有实体</returns>
        public static PromptSelectionResult SelectAtPoint(this Editor ed, Point3d point, SelectionFilter filter)
        {
            return ed.SelectCrossingWindow(point, point, filter);
        }

        /// <summary>
        /// 选择过一点的所有实体
        /// </summary>
        /// <param name="ed">命令行对象</param>
        /// <param name="point">点</param>
        /// <returns>返回过指定点的所有实体</returns>
        public static PromptSelectionResult SelectAtPoint(this Editor ed, Point3d point)
        {
            return ed.SelectCrossingWindow(point, point);
        }

    }
}
