using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace DotNetARX
{
    /// <summary>
    /// 组操作类
    /// </summary>
    public static class GroupTools
    {
        /// <summary>
        /// 创建组
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="groupName">组名</param>
        /// <param name="groupDesc">组描述</param>
        /// <param name="ids">要加入实体的ObjectId集合</param>
        /// <returns>返回组的Id</returns>
        public static ObjectId CreateGroup(this Database db, string groupName, string groupDesc, ObjectIdList ids)
        {
            //打开当前数据库的组字典对象
            DBDictionary groupDict = (DBDictionary)db.GroupDictionaryId.GetObject(OpenMode.ForRead);
            //如果已经存在指定名称的组，则返回
            if (groupDict.Contains(groupName)) return ObjectId.Null;
            //新建一个组对象
            Group group = new Group(groupDesc, true);
            groupDict.UpgradeOpen(); //切换组字典为写的状态
            //在组字典中加入新创建的组对象，并指定它的搜索关键字为groupName
            groupDict.SetAt(groupName, group);
            //通知事务处理完成组对象的加入
            db.TransactionManager.AddNewlyCreatedDBObject(group, true);
            group.Append(ids); // 在组对象中加入实体对象
            groupDict.DowngradeOpen(); //为了安全，将组字典切换成写
            return group.ObjectId; //返回组的Id
        }

        /// <summary>
        /// 向已有组中添加对象
        /// </summary>
        /// <param name="groupId">组对象的Id</param>
        /// <param name="entIds">要加入到组中的实体的ObjectId集合</param>
        public static void AppendEntityToGroup(this ObjectId groupId, ObjectIdList entIds)
        {
            //打开组对象
            Group group = groupId.GetObject(OpenMode.ForRead) as Group;
            if (group == null) return; //如果不是组对象，则返回
            group.UpgradeOpen(); // 切换组为写状态
            group.Append(entIds); // 在组中添加实体对象
            group.DowngradeOpen(); //为了安全，将组切换成写
        }

        /// <summary>
        /// 获取实体对象所在的组
        /// </summary>
        /// <param name="entId">实体的Id</param>
        /// <returns>返回实体所在的组</returns>
        public static List<ObjectId> GetGroupIds(this ObjectId entId)
        {
            List<ObjectId> groupIds = new List<ObjectId>();
            DBObject obj = entId.GetObject(OpenMode.ForRead);//打开实体
            //获取实体对象所拥有的永久反应器（组也属性永久反应器之一）
            ObjectIdCollection ids = obj.GetPersistentReactorIds();
            if (ids != null && ids.Count > 0)
                foreach (ObjectId id in ids)
                    if (id.GetObject(OpenMode.ForRead) is Group)
                        groupIds.Add(id);
            return groupIds;
        }

        /// <summary>
        /// 删除组
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="groupName">组名</param>
        public static void RemoveGroup(this Database db, string groupName)
        {
            //获取数据库的组字典对象
            DBDictionary groupDict = (DBDictionary)db.GroupDictionaryId.GetObject(OpenMode.ForRead);
            //在组字典中搜索关键字为groupName的组对象
            if (groupDict.Contains(groupName)) //如果找到名为groupName的组
            {
                groupDict.UpgradeOpen();
                groupDict.Remove(groupName);  //从组字典中删除组
                groupDict.DowngradeOpen();
            }
        }

        /// <summary>
        /// 设置组
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="groupName">组名</param>
        /// <param name="groupDesc"></param>
        /// <param name="ids"></param>
        public static ObjectId SetGroup(this Database db, string groupName, string groupDesc, ObjectIdList ids)
        {
            //清空同名组
            db.ClearGroup(groupName);
            //创建组
            return db.CreateGroup(groupName, groupDesc, ids);
        }

        /// <summary>
        /// 设置组
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="sourcePt"></param>
        /// <param name="targetPt"></param>
        public static void MoveGroup(this ObjectId groupId, Point3d sourcePt, Point3d targetPt)
        {
            var grp = (Group)groupId.GetObject(OpenMode.ForRead);
            EntTools.Move(sourcePt, targetPt, grp.GetAllEntityIds().Distinct().ToArray());
        }

        /// <summary>
        /// 设置组
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="groupName">组名</param>
        public static void ClearGroup(this Database db, string groupName)
        {
            //获取数据库的组字典对象
            DBDictionary groupDict = (DBDictionary)db.GroupDictionaryId.GetObject(OpenMode.ForRead);
            //在组字典中搜索关键字为groupName的组对象
            if (groupDict.Contains(groupName))
            {
                var grp = (Group)((ObjectId)groupDict[groupName]).GetObject(OpenMode.ForRead);
                if (grp != null)
                    foreach (var id in grp.GetAllEntityIds().Distinct())
                        try { id.Erase(); }
                        catch { }
                db.RemoveGroup(groupName);
            }
        }

        /// <summary>
        /// 设置组
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="groupDesc"></param>
        /// <param name="groupName">组名</param>
        public static bool RedescGroup(this Database db, string groupName, string groupDesc)
        {
            //获取数据库的组字典对象
            DBDictionary groupDict = (DBDictionary)db.GroupDictionaryId.GetObject(OpenMode.ForRead);
            //在组字典中搜索关键字为groupName的组对象
            if (groupDict.Contains(groupName))
            {
                var grp = (Group)((ObjectId)groupDict[groupName]).GetObject(OpenMode.ForWrite);
                grp.Description = groupDesc;
                grp.DowngradeOpen();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 设置组
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="groupDesc"></param>
        /// <param name="groupName">组名</param>
        public static bool RenameGroup(this Database db, string groupDesc, string groupName)
        {
            //获取数据库的组字典对象
            DBDictionary groupDict = (DBDictionary)db.GroupDictionaryId.GetObject(OpenMode.ForRead);
            foreach (DBDictionaryEntry de in groupDict)
            {
                var grp = (Group)de.Value.GetObject(OpenMode.ForRead);
                if (grp != null && grp.Description == groupDesc)
                {
                    groupDict.UpgradeOpen();
                    grp.Name = groupName;
                    groupDict.DowngradeOpen();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 通过名称获得组
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="groupName">组名</param>
        public static Group GetGroupFromName(this Database db, string groupName)
        {
            //获取数据库的组字典对象
            DBDictionary groupDict = (DBDictionary)db.GroupDictionaryId.GetObject(OpenMode.ForRead);
            //在组字典中搜索关键字为groupName的组对象
            if (!groupDict.Contains(groupName)) return null;
            return (Group)((ObjectId)groupDict[groupName]).GetObject(OpenMode.ForRead);
        }

        /// <summary>
        /// 通过描述获得组
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="groupDesc"></param>
        public static List<Group> GetGroupFromDesc(this Database db, string groupDesc)
        {
            //获取数据库的组字典对象
            DBDictionary groupDict = (DBDictionary)db.GroupDictionaryId.GetObject(OpenMode.ForRead);
            List<Group> groups = new List<Group>();
            foreach (DBDictionaryEntry de in groupDict)
            {
                var grp = (Group)de.Value.GetObject(OpenMode.ForRead);
                if (grp != null && grp.Description == groupDesc)
                    groups.Add(grp);
            }
            return groups;
        }
    }
}