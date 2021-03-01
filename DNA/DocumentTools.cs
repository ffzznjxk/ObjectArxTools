using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using System;

namespace DotNetARX
{
    /// <summary>
    /// 文档操作类
    /// </summary>
    public static class DocumentTools
    {
        /// <summary>
        /// 按图层读取文档
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="layers">图层名</param>
        /// <returns></returns>
        public static void PartialOpenDatabase(this Database db, params string[] layers)
        {
            if (db != null)
            {
                //
                SpatialFilter spatialFilter = new SpatialFilter
                {
                    Definition = new SpatialFilterDefinition()
                };
                //
                LayerFilter layerFilter = new LayerFilter();
                foreach (string layer in layers)
                    layerFilter.Add(layer);
                //
                db.ApplyPartialOpenFilters(spatialFilter, layerFilter);
                db.CloseInput(true);
            }
        }

        /// <summary>
        /// 确定文档中是否有未保存的修改。
        /// </summary>
        /// <param name="doc">文档对象</param>
        /// <returns>如果有未保存的修改则返回true，否则返回false</returns>
        public static bool Saved(this Document doc)
        {
            //获取DBMOD系统变量，它用来指示图形的修改状态
            object dbmod = Application.GetSystemVariable("DBMOD");
            //若DBMOD不为0，则表示图形已被修改但还未被保存
            if (Convert.ToInt16(dbmod) != 0)
                return true;
            //图形没有未保存的修改
            else
                return false;
        }

        /// <summary>
        /// 保存已有文档
        /// </summary>
        /// <param name="doc">文档对象</param>
        public static void Save(this Document doc)
        {
            doc.Database.SaveAs(doc.Name, DwgVersion.Current);
        }
    }
}