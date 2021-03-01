using Autodesk.AutoCAD.DatabaseServices;

namespace DotNetARX
{
    /// <summary>
    /// 字典操作类，包括扩展字典与有名对象字典
    /// </summary>
    public static class DictTools
    {
        /// <summary>
        /// 添加扩展记录
        /// </summary>
        /// <param name="id">对象的Id</param>
        /// <param name="searchKey">扩展记录名称</param>
        /// <param name="values">扩展记录的内容</param>
        /// <returns>返回添加的扩展记录的Id</returns>
        public static ObjectId AddXrecord(this ObjectId id, string searchKey, TypedValueList values)
        {
            //打开对象
            DBObject obj = id.GetObject(OpenMode.ForRead);
            if (obj.ExtensionDictionary.IsNull)
            {
                obj.UpgradeOpen();
                //为对象创建扩展字典
                obj.CreateExtensionDictionary();
                obj.DowngradeOpen();
            }
            //打开对象的扩展字典
            DBDictionary dict = (DBDictionary)obj.ExtensionDictionary.GetObject(OpenMode.ForRead);
            //如果扩展字典中已包含指定的扩展记录对象，则返回
            if (dict.Contains(searchKey))
                return ObjectId.Null;
            //为对象新建一个扩展记录
            Xrecord xrec = new Xrecord();
            //指定扩展记录的内容
            xrec.Data = values;
            //将扩展字典切换成写的状态
            dict.UpgradeOpen();
            //在扩展字典中加入新建的扩展记录，并指定它的搜索关键字
            ObjectId idXrec = dict.SetAt(searchKey, xrec);
            id.Database.TransactionManager.AddNewlyCreatedDBObject(xrec, true);
            dict.DowngradeOpen();
            //返回添加的扩展记录的的Id
            return idXrec;
        }

        /// <summary>
        /// 获取扩展记录
        /// </summary>
        /// <param name="id">对象的Id</param>
        /// <param name="searchKey">扩展记录名称</param>
        /// <returns>返回扩展记录的内容</returns>
        public static TypedValueList GetXrecord(this ObjectId id, string searchKey)
        {
            DBObject obj = id.GetObject(OpenMode.ForRead);
            //获取对象的扩展字典
            ObjectId dictId = obj.ExtensionDictionary;
            //若对象没有扩展字典，则返回
            if (dictId.IsNull)
                return null;
            DBDictionary dict = (DBDictionary)dictId.GetObject(OpenMode.ForRead);
            //在扩展字典中搜索指定关键字的扩展记录，如果没找到则返回
            if (!dict.Contains(searchKey))
                return null;
            //获取扩展记录的Id
            ObjectId xrecordId = dict.GetAt(searchKey);
            //打开扩展记录并获取扩展记录的内容
            Xrecord xrecord = (Xrecord)xrecordId.GetObject(OpenMode.ForRead);
            //返回扩展记录的内容
            return xrecord.Data;
        }

        /// <summary>
        /// 添加有名对象字典项
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="searchKey">有名对象字典项的名称</param>
        /// <returns>返回添加的有名对象字典项的Id</returns>
        public static ObjectId AddNamedDictionary(this Database db, string searchKey)
        {
            //存储添加的命名字典项的Id
            ObjectId id = ObjectId.Null;
            //打开数据库的有名对象字典
            DBDictionary dicts = (DBDictionary)db.NamedObjectsDictionaryId.GetObject(OpenMode.ForRead);
            //如果不存在指定关键字的字典项，则新建字典项
            if (!dicts.Contains(searchKey))
            {
                DBDictionary dict = new DBDictionary();
                dicts.UpgradeOpen();
                //设置新建字典项的搜索关键字
                id = dicts.SetAt(searchKey, dict);
                dicts.DowngradeOpen();
                //将新建的字典项添加到事务处理中
                db.TransactionManager.AddNewlyCreatedDBObject(dict, true);
            }
            return id;
        }

        /// <summary>
        /// 添加有名对象字典项
        /// </summary>
        /// <param name="parentId">上一级字典</param>
        /// <param name="searchKey">有名对象字典项的名称</param>
        /// <returns>返回添加的有名对象字典项的Id</returns>
        public static ObjectId AddSubDictionary(this ObjectId parentId, string searchKey)
        {
            //存储添加的命名字典项的Id
            ObjectId id = ObjectId.Null;
            //打开数据库的有名对象字典
            DBDictionary parentDict = (DBDictionary)parentId.GetObject(OpenMode.ForRead);
            //如果不存在指定关键字的字典项，则新建字典项
            if (!parentDict.Contains(searchKey))
            {
                DBDictionary dict = new DBDictionary();
                parentDict.UpgradeOpen();
                //设置新建字典项的搜索关键字
                parentDict.SetAt(searchKey, dict);
                parentDict.DowngradeOpen();
                //将新建的字典项添加到事务处理中
                parentId.Database.TransactionManager.AddNewlyCreatedDBObject(dict, true);
            }
            id = parentDict.GetAt(searchKey);
            return id;
        }

        /// <summary>
        /// 获取扩展记录
        /// </summary>
        /// <param name="id">对象的Id</param>
        /// <param name="searchKey">扩展记录名称</param>
        /// <param name="defaultValues"></param>
        /// <returns>返回扩展记录的内容</returns>
        public static TypedValueList ReadXrecord(this ObjectId id, string searchKey, TypedValueList defaultValues = null)
        {
            DBDictionary dict = (DBDictionary)id.GetObject(OpenMode.ForRead);
            if (dict == null)
                return null;
            //在扩展字典中搜索指定关键字的扩展记录，如果没找到则返回
            if (dict.Contains(searchKey))
            {
                //获取扩展记录的Id
                ObjectId xrecordId = dict.GetAt(searchKey);
                //打开扩展记录并获取扩展记录的内容
                Xrecord xrecord = (Xrecord)xrecordId.GetObject(OpenMode.ForRead);
                //返回扩展记录的内容
                return xrecord.Data;
            }
            else if (defaultValues != null)
            {
                //为对象新建一个扩展记录
                Xrecord xrec = new Xrecord();
                //指定扩展记录的内容
                xrec.Data = defaultValues;
                //将扩展字典切换成写的状态
                dict.UpgradeOpen();
                //在扩展字典中加入新建的扩展记录，并指定它的搜索关键字
                dict.SetAt(searchKey, xrec);
                id.Database.TransactionManager.AddNewlyCreatedDBObject(xrec, true);
                dict.DowngradeOpen();
            }
            //
            return defaultValues;
        }

        /// <summary>
        /// 获取扩展记录
        /// </summary>
        /// <param name="id">对象的Id</param>
        /// <param name="searchKey">扩展记录名称</param>
        /// <param name="valus"></param>
        /// <returns>返回扩展记录的内容</returns>
        public static bool WriteXrecord(this ObjectId id, string searchKey, TypedValueList valus)
        {
            DBDictionary dict = (DBDictionary)id.GetObject(OpenMode.ForRead);
            if (dict == null)
                return false;
            try
            {
                dict.UpgradeOpen();
                Xrecord xrec;
                //在扩展字典中搜索指定关键字的扩展记录，如果没找到则返回
                if (dict.Contains(searchKey))
                {
                    //获取扩展记录的Id
                    ObjectId xrecordId = dict.GetAt(searchKey);
                    //打开扩展记录并获取扩展记录的内容
                    xrec = (Xrecord)xrecordId.GetObject(OpenMode.ForWrite);
                    xrec.Data = valus;
                }
                else
                {
                    //为对象新建一个扩展记录
                    xrec = new Xrecord();
                    //指定扩展记录的内容
                    xrec.Data = valus;
                    //在扩展字典中加入新建的扩展记录，并指定它的搜索关键字
                    dict.SetAt(searchKey, xrec);
                    id.Database.TransactionManager.AddNewlyCreatedDBObject(xrec, true);
                }
                dict.DowngradeOpen();
                //
                return true;
            }
            catch (System.Exception ex)
            {
                return false;
            }
        }
    }
}
