using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Windows.Data;
using System.IO;
using System.Windows.Media.Imaging;

namespace DotNetARX
{
    /// <summary>
    /// 块预览类
    /// </summary>
    public static class BlockPreview
    {
        /// <summary>
        /// 获取块的预览图形
        /// </summary>
        /// <param name="tr">事务处理</param>
        /// <param name="bt">块表</param>
        /// <param name="blockName">需要生成预览的块名</param>
        /// <returns>返回块预览的bmp图像</returns>
        public static System.Drawing.Image ExtractThumbnail(Transaction tr, BlockTable bt, string blockName)
        {
            if (!bt.Has(blockName))
                return null;
            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForRead);
            var imgsrc = CMLContentSearchPreviews.GetBlockTRThumbnail(btr);
            return ImageSourceToGDI(imgsrc as BitmapSource);
        }

        /// <summary>
        /// 从BitmapSource产生图像
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static System.Drawing.Image ImageSourceToGDI(BitmapSource src)
        {
            var ms = new MemoryStream();
            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(src));
            encoder.Save(ms);
            ms.Flush();
            return System.Drawing.Image.FromStream(ms);
        }
    }
}
