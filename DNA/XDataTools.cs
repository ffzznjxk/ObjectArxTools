using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Linq;

namespace DotNetARX
{
    /// <summary>
    /// 扩展数据操作类
    /// </summary>
    public static class XDataTools
    {
        /// <summary>
        /// 添加扩展数据
        /// </summary>
        /// <param name="id">对象的ObjectId</param>
        /// <param name="regAppName">注册应用程序名</param>
        /// <param name="values">要添加的扩展数据项列表</param>
        public static void AddXData(this ObjectId id, string regAppName, TypedValueList values)
        {
            //获取实体所属的数据库
            Database db = id.Database;
            //获取数据库的注册应用程序表
            RegAppTable regAppTable = (RegAppTable)db.RegAppTableId.GetObject(OpenMode.ForWrite);
            //如里不存在名为regAppName的记录，则创建新的注册应用程序表记录
            if (!regAppTable.Has(regAppName))
            {
                //创建一个注册应用程序表记录用来表示扩展数据
                RegAppTableRecord ratRecord = new RegAppTableRecord();
                //设置扩展数据的名字
                ratRecord.Name = regAppName;
                //在注册应用程序表加入扩展数据，并通知事务处理
                regAppTable.Add(ratRecord);
                db.TransactionManager.AddNewlyCreatedDBObject(ratRecord, true);
            }
            //以写的方式打开要添加扩展数据的实体
            DBObject obj = id.GetObject(OpenMode.ForWrite);
            //将扩展数据的应用程序名添加到扩展数据项列表的第一项
            values.Insert(0, new TypedValue((int)DxfCode.ExtendedDataRegAppName, regAppName));
            //将新建的扩展数据附加到实体中
            obj.XData = values;
            //切换为读的状态
            regAppTable.DowngradeOpen();
        }

        /// <summary>
        /// 添加扩展数据
        /// </summary>
        /// <param name="id">对象的ObjectId</param>
        /// <param name="regAppName">注册应用程序名</param>
        /// <param name="data">字符串值</param>
        public static void AddXData(this ObjectId id, string regAppName, string data)
        {
            TypedValueList values = new TypedValueList();
            values.Add(DxfCode.ExtendedDataAsciiString, data);
            AddXData(id, regAppName, values);
        }

        /// <summary>
        /// 添加扩展数据
        /// </summary>
        /// <param name="id">对象的ObjectId</param>
        /// <param name="kvs"></param>
        public static void AddXDatas(this ObjectId id, Dictionary<string, string> kvs)
        {
            foreach (var kv in kvs)
                id.AddXData(kv.Key, kv.Value);
        }

        /// <summary>
        /// 获取扩展数据
        /// </summary>
        /// <param name="id">对象的ObjectId</param>
        /// <param name="regAppName">注册应用程序名</param>
        /// <returns>返回获得的扩展数据</returns>
        public static TypedValueList GetXData(this ObjectId id, string regAppName)
        {
            TypedValueList xdata = new TypedValueList();
            //打开对象
            DBObject obj = id.GetObject(OpenMode.ForRead);
            //获取对象中名为regAppName的扩展数据
            xdata = obj.GetXDataForApplication(regAppName);
            //返回扩展数据
            return xdata;
        }

        /// <summary>
        /// 获取扩展数据的唯一值
        /// </summary>
        /// <param name="id">对象的ObjectId</param>
        /// <param name="regAppNames"></param>
        /// <returns>返回获得的扩展数据的唯一值</returns>
        public static Dictionary<string, string> GetXDataValues(this ObjectId id, List<string> regAppNames)
        {
            return regAppNames.ToDictionary(d => d, d => id.GetXDataValue(d));
        }

        /// <summary>
        /// 获取扩展数据的唯一值
        /// </summary>
        /// <param name="id">对象的ObjectId</param>
        /// <param name="regAppName">注册应用程序名</param>
        /// <returns>返回获得的扩展数据的唯一值</returns>
        public static string GetXDataValue(this ObjectId id, string regAppName)
        {
            string xDataValue = "";
            try
            {
                TypedValueList xdata = id.GetXData(regAppName);
                xDataValue = xdata[1].Value.ToString();
            }
            catch (System.Exception)
            {

            }
            return xDataValue;
        }

        /// <summary>
        /// 修改对象的扩展数据
        /// </summary>
        /// <param name="id">对象的ObjectId</param>
        /// <param name="regAppName">注册应用程序名</param>
        /// <param name="code">扩展数据的类型</param>
        /// <param name="oldValue">原扩展数据值</param>
        /// <param name="newValue">新扩展数据值</param>
        public static void ModXData(this ObjectId id, string regAppName, DxfCode code, object oldValue, object newValue)
        {
            //以写的方式打开对象
            DBObject obj = id.GetObject(OpenMode.ForWrite);
            //获取regAppName注册应用程序下的扩展数据列表
            TypedValueList xdata = obj.GetXDataForApplication(regAppName);
            //遍历扩展数据列表
            for (int i = 0; i < xdata.Count; i++)
            {
                //扩展数据
                TypedValue tv = xdata[i];
                //判断扩展数据的类型和值是否满足条件
                if (tv.TypeCode == (short)code && tv.Value.Equals(oldValue))
                {
                    //设置新的扩展数据值
                    xdata[i] = new TypedValue(tv.TypeCode, newValue);
                    //跳出循环
                    break;
                }
            }
            //覆盖原扩展数据
            obj.XData = xdata;
            //切换为读的状态
            obj.DowngradeOpen();
        }

        /// <summary>
        /// 删除指定注册应用程序下的扩展数据
        /// </summary>
        /// <param name="id">对象的ObjectId</param>
        /// <param name="regAppName">注册应用程序名</param>
        public static void RemoveXData(this ObjectId id, string regAppName)
        {
            //以写的方式打开对象
            DBObject obj = id.GetObject(OpenMode.ForWrite);
            //获取regAppName注册应用程序下的扩展数据列表
            TypedValueList xdata = obj.GetXDataForApplication(regAppName);
            //如果有扩展数据
            if (xdata != null)
            {
                //新建一个TypedValue列表，并只添加注册应用程序名扩展数据项
                TypedValueList newXdata = new TypedValueList();
                newXdata.Add(DxfCode.ExtendedDataRegAppName, regAppName);
                //为对象的XData属性重新赋值，从而删除扩展数据
                obj.XData = newXdata;
            }
            //切换为读的状态
            obj.DowngradeOpen();
        }

        /// <summary>
        /// 删除所有扩展数据
        /// </summary>
        /// <param name="id">对象的ObjectId</param>
        public static void RemoveXData(this ObjectId id)
        {
            //以写的方式打开对象
            DBObject obj = id.GetObject(OpenMode.ForWrite);
            //如果有扩展数据
            if (obj.XData != null)
            {
                IEnumerable<string> appNames = from TypedValue tv in obj.XData.AsArray()
                                               where tv.TypeCode == (short)DxfCode.ExtendedDataRegAppName
                                               select tv.Value.ToString();
                foreach (string appName in appNames)
                    obj.XData = new ResultBuffer(new TypedValue((short)DxfCode.ExtendedDataRegAppName, appName));
            }
            //切换为读的状态
            obj.DowngradeOpen();
        }
    }
}
