using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace DotNetARX
{
    /// <summary>
    /// 表格操作类
    /// </summary>
    public static class TableTools
    {
        /// <summary>
        /// 所有行的标志位（包括标题行、数据行）
        /// </summary>
        public static int AllRows
        {
            get { return (int)(RowType.DataRow | RowType.HeaderRow | RowType.TitleRow); }
        }

        /// <summary>
        /// 创建表格
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="position">表格位置</param>
        /// <param name="numRows">表格行数</param>
        /// <param name="numCols">表格列数</param>
        /// <returns>返回创建的表格的Id</returns>
        public static ObjectId CreateTable(this Database db, Point3d position, int numRows, int numCols)
        {
            ObjectId tableId;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                Table table = new Table();
                //设置表格的行数和列数
                table.SetSize(numRows, numCols);
                //设置表格放置的位置
                table.Position = position;
                //根据当前样式更新表格（不加此句会导致AutoCAD崩溃）
                table.GenerateLayout();
                //表格添加到模型空间
                tableId = db.AddToModelSpace(table);
                //提交
                trans.Commit();
            }
            return tableId;
        }

        /// <summary>
        /// 设置单元格中文本的高度
        /// </summary>
        /// <param name="table">表格对象</param>
        /// <param name="height">文本高度</param>
        /// <param name="rowType">行的标志位</param>
        public static void SetTextHeight(this Table table, double height, RowType rowType)
        {
            table.SetTextHeight(height, rowType);
        }

        /// <summary>
        /// 设置表格中所有单元格中文本为同一高度
        /// </summary>
        /// <param name="table">表格对象</param>
        /// <param name="height">文本高度</param>
        public static void SetTextHeight(this Table table, double height)
        {
            table.Cells.TextHeight = height;
        }

        /// <summary>
        /// 设置单元格中文本的对齐方式
        /// </summary>
        /// <param name="table">表格对象</param>
        /// <param name="align">单元格对齐方式</param>
        /// <param name="rowType">行的标志位</param>
        public static void SetAlignment(this Table table, CellAlignment align, RowType rowType)
        {
            table.Cells.Alignment = align;
            //table.SetAlignment(align, (int)rowType);
        }

        /// <summary>
        /// 设置表格中所有单元格的对齐方式
        /// </summary>
        /// <param name="table">表格对象</param>
        /// <param name="align">单元格对齐方式</param>
        public static void SetAlignment(this Table table, CellAlignment align)
        {
            table.Cells.Alignment = align;
        }

        /// <summary>
        /// 一次性按行设置单元格文本
        /// </summary>
        /// <param name="table">表格对象</param>
        /// <param name="rowIndex">行号</param>
        /// <param name="data">文本内容</param>
        /// <returns>如果设置成功则返回true，否则返回false</returns>
        public static bool SetRowTextString(this Table table, int rowIndex, params string[] data)
        {
            if (data.Length > table.Columns.Count)
                return false;
            for (int j = 0; j < data.Length; j++)
                table.Cells[rowIndex, j].TextString = data[j];
            return true;
        }

        /// <summary>
        /// 添加表格样式
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="styleName">表格样式的名称</param>
        /// <returns>返回表格样式的Id</returns>
        public static ObjectId AddTableStyle(this Database db, string styleName)
        {
            ObjectId styleId;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //打开表格样式字典
                DBDictionary dict = (DBDictionary)tr.GetObject(db.TableStyleDictionaryId, OpenMode.ForRead);
                //判断是否存在指定的表格样式，如果存在则返回表格样式的Id
                if (dict.Contains(styleName))
                    styleId = dict.GetAt(styleName);
                else
                {
                    //新建一个表格样式
                    TableStyle tStyle = new TableStyle();
                    //切换表格样式字典为写的状态
                    dict.UpgradeOpen();
                    //将新的表格样式添加到样式字典并获取其Id
                    styleId = dict.SetAt(styleName, tStyle);
                    //将新建的表格样式添加到事务处理中
                    tr.AddNewlyCreatedDBObject(tStyle, true);
                    //提交
                    tr.Commit();
                }
            }
            //返回表格样式的Id
            return styleId;
        }

        /// <summary>
        /// 添加表格样式
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="styleName">表格样式的名称</param>
        /// <param name="textStyle">表格字体样式</param>
        /// <param name="textHeight">表格字高</param>
        /// <param name="align">对齐方式</param>
        /// <param name="margin">边距</param>
        /// <param name="hideTitleLine"></param>
        /// <returns>返回表格样式的Id</returns>
        public static ObjectId AddTableStyle(this Database db, string styleName, string textStyle, double textHeight, CellAlignment align, double margin, bool hideTitleLine = false)
        {
            ObjectId styleId;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //打开表格样式字典
                DBDictionary dict = (DBDictionary)tr.GetObject(db.TableStyleDictionaryId, OpenMode.ForRead);
                //判断是否存在指定的表格样式，如果存在则返回表格样式的Id
                if (dict.Contains(styleName))
                    styleId = dict.GetAt(styleName);
                else
                {
                    //新建一个表格样式
                    TableStyle tStyle = new TableStyle();
                    //字体样式
                    TextStyleTable textStyleTable = tr.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                    tStyle.SetTextStyle(textStyleTable[textStyle], AllRows);
                    //字高
                    tStyle.SetTextHeight(textHeight, AllRows);
                    //对齐
                    tStyle.SetAlignment(align, AllRows);
                    //边距
                    tStyle.HorizontalCellMargin = margin;
                    tStyle.VerticalCellMargin = margin;
                    //首行边界隐藏
                    if (hideTitleLine)
                    {
                        tStyle.SetGridVisibility(false, (int)GridLineType.AllGridLines, (int)RowType.TitleRow);
                        tStyle.SetGridVisibility(true, (int)GridLineType.HorizontalBottom, (int)RowType.TitleRow);
                    }
                    //切换表格样式字典为写的状态
                    dict.UpgradeOpen();
                    //将新的表格样式添加到样式字典并获取其Id
                    styleId = dict.SetAt(styleName, tStyle);
                    //将新建的表格样式添加到事务处理中
                    tr.AddNewlyCreatedDBObject(tStyle, true);
                    //提交
                    tr.Commit();
                }
            }
            //返回表格样式的Id
            return styleId;
        }
    }
}
