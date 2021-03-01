using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace DotNetARX
{
    /// <summary>
    /// 图层操作类
    /// </summary>
    public static class LayerTools
    {
        /// <summary>
        /// 创建新图层
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="layerName">图层名</param>
        /// <param name="layerDescription">图层描述</param>
        /// <returns>返回新建图层的ObjectId</returns>
        public static ObjectId AddLayer(this Database db, string layerName, string layerDescription = "")
        {
            //打开层表
            LayerTable lt = (LayerTable)db.LayerTableId.GetObject(OpenMode.ForRead);
            //如果存在名为layerName的图层则返回空值
            if (lt.Has(layerName))
                return ObjectId.Null;
            //定义一个新的层表记录
            LayerTableRecord ltr = new LayerTableRecord();
            //设置图层名
            ltr.Name = layerName;
            //添加图层
            lt.UpgradeOpen();
            lt.Add(ltr);
            //增加说明
            ltr.Description = layerDescription;
            //把层表记录添加到事务处理中
            db.TransactionManager.AddNewlyCreatedDBObject(ltr, true);
            lt.DowngradeOpen();
            //返回新添加的层表记录的ObjectId
            return lt[layerName];
        }

        /// <summary>
        /// 修改图层名
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="oldName">旧图层名</param>
        /// <param name="newName">新图层名</param>
        public static bool ChangeLayerName(this Database db, string oldName, string newName)
        {
            LayerTable lt = (LayerTable)db.LayerTableId.GetObject(OpenMode.ForRead);
            if (!lt.Has(oldName))
                return false;
            ObjectId layerId = lt[oldName];
            LayerTableRecord ltr = (LayerTableRecord)layerId.GetObject(OpenMode.ForWrite);
            ltr.Name = newName;
            ltr.DowngradeOpen();
            return true;
        }

        /// <summary>
        /// 设置图层的颜色
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="layerName">图层名</param>
        /// <param name="colorIndex">颜色索引</param>
        /// <returns>如果成功设置图层颜色则返回True，否则返回False</returns>
        public static bool SetLayerColor(this Database db, string layerName, short colorIndex)
        {
            LayerTable lt = (LayerTable)db.LayerTableId.GetObject(OpenMode.ForRead);
            if (!lt.Has(layerName))
                return false;
            ObjectId layerId = lt[layerName];
            LayerTableRecord ltr = (LayerTableRecord)layerId.GetObject(OpenMode.ForWrite);
            ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);
            ltr.DowngradeOpen();
            return true;
        }

        /// <summary>
        /// 设置图层的颜色
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="layerName">图层名</param>
        /// <param name="color">颜色</param>
        /// <returns>如果成功设置图层颜色则返回True，否则返回False</returns>
        public static bool SetLayerColor(this Database db, string layerName, Color color)
        {
            LayerTable lt = (LayerTable)db.LayerTableId.GetObject(OpenMode.ForRead);
            if (!lt.Has(layerName))
                return false;
            ObjectId layerId = lt[layerName];
            LayerTableRecord ltr = (LayerTableRecord)layerId.GetObject(OpenMode.ForWrite);
            ltr.Color = color;
            ltr.DowngradeOpen();
            return true;
        }

        /// <summary>
        /// 将指定的图层设置为当前层
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="layerName">图层名</param>
        /// <returns>如果设置成功则返回True</returns>
        public static bool SetCurrentLayer(this Database db, string layerName)
        {
            //打开层表
            LayerTable lt = (LayerTable)db.LayerTableId.GetObject(OpenMode.ForRead);
            //如果不存在名为layerName的图层则返回
            if (!lt.Has(layerName))
                return false;
            //获取名为layerName的层表记录的Id
            ObjectId layerId = lt[layerName];
            //如果指定的图层为当前层，则返回
            if (db.Clayer == layerId)
                return false;
            //指定当前层
            db.Clayer = layerId;
            //指定当前图层成功
            return true;
        }

        /// <summary>
        /// 获取当前图形中所有的图层
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <returns>返回所有的层表记录</returns>
        public static List<LayerTableRecord> GetAllLayers(this Database db)
        {
            LayerTable lt = (LayerTable)db.LayerTableId.GetObject(OpenMode.ForRead);
            //用于返回层表记录的列表
            List<LayerTableRecord> ltrs = new List<LayerTableRecord>();
            //遍历层表
            foreach (ObjectId id in lt)
            {
                //打开层表记录
                LayerTableRecord ltr = (LayerTableRecord)id.GetObject(OpenMode.ForRead);
                //添加到返回列表中
                ltrs.Add(ltr);
            }
            //返回所有的层表记录
            return ltrs;
        }

        /// <summary>
        /// 获取当前图形中所有的图层名称
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <returns>返回所有的图层名称</returns>
        public static List<string> GetAllLayerNames(this Database db)
        {
            List<string> layerNames = new List<string>();
            //层表
            LayerTable lt = (LayerTable)db.LayerTableId.GetObject(OpenMode.ForRead);
            //遍历层表
            foreach (ObjectId id in lt)
            {
                //打开层表记录
                LayerTableRecord ltr = (LayerTableRecord)id.GetObject(OpenMode.ForRead);
                //添加到返回列表中
                layerNames.Add(ltr.Name);
            }
            //返回所有的图层名称
            return layerNames;
        }

        /// <summary>
        /// 删除指定名称的图层
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="layerName">图层名</param>
        /// <returns>如果删除成功则返回True，否则返回False</returns>
        public static bool DeleteLayer(this Database db, string layerName)
        {
            LayerTable lt = (LayerTable)db.LayerTableId.GetObject(OpenMode.ForRead);
            //如果层名为0或Defpoints则返回
            if (layerName == "0" || layerName == "Defpoints")
                return false;
            //如果不存在名为layerName的图层则返回
            if (!lt.Has(layerName))
                return false;
            //获取名为layerName的层表记录的Id
            ObjectId layerId = lt[layerName];
            //如果要删除的图层为当前层则返回
            if (layerId == db.Clayer)
                return false;
            //打开名为layerName的层表记录
            LayerTableRecord ltr = (LayerTableRecord)layerId.GetObject(OpenMode.ForRead);
            //如果要删除的图层包含对象或依赖外部参照则返回
            lt.GenerateUsageData();
            if (ltr.IsUsed)
                return false;
            //切换层表记录为写的状态
            ltr.UpgradeOpen();
            //删除名为layerName的图层
            ltr.Erase(true);
            //删除图层成功
            return true;
        }

        /// <summary>
        /// 获取所有图层的ObjectId
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <returns>返回所有图层的ObjectId</returns>
        public static List<ObjectId> GetAllLayerObjectIds(this Database db)
        {
            LayerTable lt = (LayerTable)db.LayerTableId.GetObject(OpenMode.ForRead);
            //用于返回层表记录ObjectId的列表
            List<ObjectId> ltrs = new List<ObjectId>();
            //遍历层表
            foreach (ObjectId id in lt)
                //添加到返回列表中
                ltrs.Add(id);
            //返回所有的层表记录的ObjectId
            return ltrs;
        }

        /// <summary>
        /// 打开/关闭图层
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="layerName">图层名</param>
        /// <param name="closeOrOpen"></param>
        /// <returns>如果设置成功则返回True</returns>
        public static bool ToggleOnOffLayer(this Database db, string layerName, object closeOrOpen = null)
        {
            //打开层表
            LayerTable lt = (LayerTable)db.LayerTableId.GetObject(OpenMode.ForRead);
            //如果不存在名为layerName的图层则返回
            if (!lt.Has(layerName))
                return false;
            //获取名为layerName的层表记录的Id
            ObjectId layerId = lt[layerName];
            //打开名为layerName的层表记录
            LayerTableRecord ltr = (LayerTableRecord)layerId.GetObject(OpenMode.ForWrite);
            //切换
            if (closeOrOpen != null)
                ltr.IsOff = (bool)closeOrOpen;
            else
                ltr.IsOff = !ltr.IsOff;
            ltr.DowngradeOpen();
            return true;
        }

        /// <summary>
        /// 锁定/开启图层
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="layerName">图层名</param>
        /// <returns>如果设置成功则返回True</returns>
        public static bool ToggleLockLayer(this Database db, string layerName)
        {
            //打开层表
            LayerTable lt = (LayerTable)db.LayerTableId.GetObject(OpenMode.ForRead);
            //如果不存在名为layerName的图层则返回
            if (!lt.Has(layerName))
                return false;
            //获取名为layerName的层表记录的Id
            ObjectId layerId = lt[layerName];
            //打开名为layerName的层表记录
            LayerTableRecord ltr = (LayerTableRecord)layerId.GetObject(OpenMode.ForWrite);
            //切换
            ltr.IsLocked = !ltr.IsLocked;
            ltr.DowngradeOpen();
            return true;
        }

        /// <summary>
        /// 冻结/解冻图层
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="layerName">图层名</param>
        /// <returns>如果设置成功则返回True</returns>
        public static bool ToggleFreezeLayer(this Database db, string layerName)
        {
            //打开层表
            LayerTable lt = (LayerTable)db.LayerTableId.GetObject(OpenMode.ForRead);
            //如果不存在名为layerName的图层则返回
            if (!lt.Has(layerName))
                return false;
            //获取名为layerName的层表记录的Id
            ObjectId layerId = lt[layerName];
            //是当前层，返回
            if (db.Clayer == layerId)
                return false;
            //打开名为layerName的层表记录
            LayerTableRecord ltr = (LayerTableRecord)layerId.GetObject(OpenMode.ForWrite);
            //切换
            ltr.IsFrozen = !ltr.IsFrozen;
            ltr.DowngradeOpen();
            return true;
        }
    }
}