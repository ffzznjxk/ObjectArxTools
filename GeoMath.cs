using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetARX
{
    class GeoMath
    {
        #region 几何计算

        /* 常用的常量定义 */
        const double INF = 1E200;
        const double EP = 1E-10;
        const int MAXV = 300;
        const double PI = 3.14159265;

        /* 基本几何结构 */
        public struct Point2d
        {
            public double X;
            public double Y;
            public Point2d(double a = 0, double b = 0) { X = a; Y = b; } //constructor 
        };
        public struct Line2d
        {
            public Point2d StartPoint;
            public Point2d EndPoint;
            public Line2d(Point2d a, Point2d b) { StartPoint = a; EndPoint = b; }
            //public Line2d() { }
        };
        struct LINE           // 直线的解析方程 a*x+b*y+c=0  为统一表示，约定 a >= 0 
        {
            public double a;
            public double b;
            public double c;
            LINE(double d1 = 1, double d2 = -1, double d3 = 0) { a = d1; b = d2; c = d3; }
        };

        /**********************
         *                    * 
         *   点的基本运算     * 
         *                    * 
         **********************/

        public static double dist(Point2d p1, Point2d p2)                // 返回两点之间欧氏距离 
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }
        public static bool equal_point(Point2d p1, Point2d p2)           // 判断两个点是否重合  
        {
            return (Math.Abs(p1.X - p2.X) < EP) && (Math.Abs(p1.Y - p2.Y) < EP);
        }
        /****************************************************************************** 
        r=multiply(sp,ep,op),得到(sp-op)和(ep-op)的叉积 
        r>0：ep在矢量opsp的逆时针方向； 
        r=0：opspep三点共线； 
        r<0：ep在矢量opsp的顺时针方向 
        *******************************************************************************/
        public static double multiply(Point2d sp, Point2d ep, Point2d op)
        {
            return ((sp.X - op.X) * (ep.Y - op.Y) - (ep.X - op.X) * (sp.Y - op.Y));
        }
        /* 
        r=dotmultiply(p1,p2,op),得到矢量(p1-op)和(p2-op)的点积，如果两个矢量都非零矢量 
        r<0：两矢量夹角为钝角；
        r=0：两矢量夹角为直角；
        r>0：两矢量夹角为锐角 
        *******************************************************************************/
        public static double dotmultiply(Point2d p1, Point2d p2, Point2d p0)
        {
            return ((p1.X - p0.X) * (p2.X - p0.X) + (p1.Y - p0.Y) * (p2.Y - p0.Y));
        }
        /****************************************************************************** 
        判断点p是否在线段l上
        条件：(p在线段l所在的直线上) && (点p在以线段l为对角线的矩形内)
        *******************************************************************************/
        public static bool online(Line2d l, Point2d p)
        {
            return (multiply(l.EndPoint, p, l.StartPoint) == 0) && (((p.X - l.StartPoint.X) * (p.X - l.EndPoint.X) <= 0) && ((p.Y - l.StartPoint.Y) * (p.Y - l.EndPoint.Y) <= 0));
        }
        // 返回点p以点o为圆心逆时针旋转alpha(单位：弧度)后所在的位置 
        public static Point2d rotate(Point2d o, double alpha, Point2d p)
        {
            Point2d tp;
            p.X -= o.X;
            p.Y -= o.Y;
            tp.X = p.X * Math.Cos(alpha) - p.Y * Math.Sin(alpha) + o.X;
            tp.Y = p.Y * Math.Cos(alpha) + p.X * Math.Sin(alpha) + o.Y;
            return tp;
        }
        /* 返回顶角在o点，起始边为os，终止边为oe的夹角(单位：弧度) 
         角度小于pi，返回正值 
         角度大于pi，返回负值 
         可以用于求线段之间的夹角 
        原理：
         r = dotmultiply(s,e,o) / (dist(o,s)*dist(o,e))
         r'= multiply(s,e,o)

         r >= 1 angle = 0;
         r <= -1 angle = -PI
         -1<r<1 && r'>0 angle = arccos(r)
         -1<r<1 && r'<=0 angle = -arccos(r)
        */
        double angle(Point2d o, Point2d s, Point2d e)
        {
            double cosfi, fi, norm;
            double dsx = s.X - o.X;
            double dsy = s.Y - o.Y;
            double dex = e.X - o.X;
            double dey = e.Y - o.Y;

            cosfi = dsx * dex + dsy * dey;
            norm = (dsx * dsx + dsy * dsy) * (dex * dex + dey * dey);
            cosfi /= Math.Sqrt(norm);

            if (cosfi >= 1.0) return 0;
            if (cosfi <= -1.0) return -3.1415926;

            fi = Math.Acos(cosfi);
            if (dsx * dey - dsy * dex > 0) return fi;      // 说明矢量os 在矢量 oe的顺时针方向 
            return -fi;
        }
        /*****************************\ 
        *                             * 
        *      线段及直线的基本运算   * 
        *                             * 
        \*****************************/

        /* 判断点与线段的关系,用途很广泛 
        本函数是根据下面的公式写的，P是点C到线段AB所在直线的垂足 

                        AC dot AB 
                r =     --------- 
                         ||AB||^2 
                     (Cx-Ax)(Bx-Ax) + (Cy-Ay)(By-Ay) 
                  = ------------------------------- 
                                  L^2 

            r has the following meaning: 

                r=0      P = A 
                r=1      P = B 
                r<0   P is on the backward extension of AB 
          r>1      P is on the forward extension of AB 
                0<r<1  P is interior to AB 
        */
        public static double relation(Point2d p, Line2d l)
        {
            Line2d tl;
            tl.StartPoint = l.StartPoint;
            tl.EndPoint = p;
            return dotmultiply(tl.EndPoint, l.EndPoint, l.StartPoint) / (dist(l.StartPoint, l.EndPoint) * dist(l.StartPoint, l.EndPoint));
        }
        // 求点C到线段AB所在直线的垂足 P 
        public static Point2d perpendicular(Point2d p, Line2d l)
        {
            double r = relation(p, l);
            Point2d tp;
            tp.X = l.StartPoint.X + r * (l.EndPoint.X - l.StartPoint.X);
            tp.Y = l.StartPoint.Y + r * (l.EndPoint.Y - l.StartPoint.Y);
            return tp;
        }
        /* 求点p到线段l的最短距离,并返回线段上距该点最近的点np 
        注意：np是线段l上到点p最近的点，不一定是垂足 */
        public static double ptolinesegdist(Point2d p, Line2d l, Point2d np)
        {
            double r = relation(p, l);
            if (r < 0)
            {
                np = l.StartPoint;
                return dist(p, l.StartPoint);
            }
            if (r > 1)
            {
                np = l.EndPoint;
                return dist(p, l.EndPoint);
            }
            np = perpendicular(p, l);
            return dist(p, np);
        }
        // 求点p到线段l所在直线的距离,请注意本函数与上个函数的区别  
        double ptoldist(Point2d p, Line2d l)
        {
            return Math.Abs(multiply(p, l.EndPoint, l.StartPoint)) / dist(l.StartPoint, l.EndPoint);
        }
        /* 计算点到折线集的最近距离,并返回最近点. 
        注意：调用的是ptolineseg()函数 */
        double ptopointset(int vcount, Point2d[] pointset, Point2d p, Point2d q)
        {
            int i;
            double cd = (double)INF, td;
            Line2d l = new Line2d();
            Point2d tq = new Point2d();
            Point2d cq = new Point2d();

            for (i = 0; i < vcount - 1; i++)
            {
                l.StartPoint = pointset[i];

                l.EndPoint = pointset[i + 1];
                td = ptolinesegdist(p, l, tq);
                if (td < cd)
                {
                    cd = td;
                    cq = tq;
                }
            }
            q = cq;
            return cd;
        }
        /* 判断圆是否在多边形内.ptolineseg()函数的应用2 */
        bool CircleInsidePolygon(int vcount, Point2d center, double radius, Point2d[] polygon)
        {
            Point2d q;
            double d;
            q.X = 0;
            q.Y = 0;
            d = ptopointset(vcount, polygon, center, q);
            if (d < radius || Math.Abs(d - radius) < EP)
                return true;
            else
                return false;
        }
        /* 返回两个矢量l1和l2的夹角的余弦(-1 --- 1)注意：如果想从余弦求夹角的话，注意反余弦函数的定义域是从 0到pi */
        double cosine(Line2d l1, Line2d l2)
        {
            return (((l1.EndPoint.X - l1.StartPoint.X) * (l2.EndPoint.X - l2.StartPoint.X) +
            (l1.EndPoint.Y - l1.StartPoint.Y) * (l2.EndPoint.Y - l2.StartPoint.Y)) / (dist(l1.EndPoint, l1.StartPoint) * dist(l2.EndPoint, l2.StartPoint)));
        }
        // 返回线段l1与l2之间的夹角 单位：弧度 范围(-pi，pi) 
        double lsangle(Line2d l1, Line2d l2)
        {
            Point2d o, s, e;
            o.X = o.Y = 0;
            s.X = l1.EndPoint.X - l1.StartPoint.X;
            s.Y = l1.EndPoint.Y - l1.StartPoint.Y;
            e.X = l2.EndPoint.X - l2.StartPoint.X;
            e.Y = l2.EndPoint.Y - l2.StartPoint.Y;
            return angle(o, s, e);
        }
        // 如果线段u和v相交(包括相交在端点处)时，返回true 
        //
        //判断P1P2跨立Q1Q2的依据是：( P1 - Q1 ) × ( Q2 - Q1 ) * ( Q2 - Q1 ) × ( P2 - Q1 ) >= 0。
        //判断Q1Q2跨立P1P2的依据是：( Q1 - P1 ) × ( P2 - P1 ) * ( P2 - P1 ) × ( Q2 - P1 ) >= 0。
        bool intersect(Line2d u, Line2d v)
        {
            return ((Math.Max(u.StartPoint.X, u.EndPoint.X) >= Math.Min(v.StartPoint.X, v.EndPoint.X)) &&                     //排斥实验 
              (Math.Max(v.StartPoint.X, v.EndPoint.X) >= Math.Min(u.StartPoint.X, u.EndPoint.X)) &&
              (Math.Max(u.StartPoint.Y, u.EndPoint.Y) >= Math.Min(v.StartPoint.Y, v.EndPoint.Y)) &&
              (Math.Max(v.StartPoint.Y, v.EndPoint.Y) >= Math.Min(u.StartPoint.Y, u.EndPoint.Y)) &&
              (multiply(v.StartPoint, u.EndPoint, u.StartPoint) * multiply(u.EndPoint, v.EndPoint, u.StartPoint) >= 0) &&         //跨立实验 
              (multiply(u.StartPoint, v.EndPoint, v.StartPoint) * multiply(v.EndPoint, u.EndPoint, v.StartPoint) >= 0));
        }
        //  (线段u和v相交)&&(交点不是双方的端点) 时返回true    
        bool intersect_A(Line2d u, Line2d v)
        {
            return ((intersect(u, v)) &&
              (!online(u, v.StartPoint)) &&
              (!online(u, v.EndPoint)) &&
              (!online(v, u.EndPoint)) &&
              (!online(v, u.StartPoint)));
        }

        // 线段v所在直线与线段u相交时返回true；方法：判断线段u是否跨立线段v  
        bool intersect_l(Line2d u, Line2d v)
        {
            return multiply(u.StartPoint, v.EndPoint, v.StartPoint) * multiply(v.EndPoint, u.EndPoint, v.StartPoint) >= 0;
        }
        // 根据已知两点坐标，求过这两点的直线解析方程： a*x+b*y+c = 0  (a >= 0)  
        LINE makeline(Point2d p1, Point2d p2)
        {
            LINE tl;
            int sign = 1;
            tl.a = p2.Y - p1.Y;
            if (tl.a < 0)
            {
                sign = -1;
                tl.a = sign * tl.a;
            }
            tl.b = sign * (p1.X - p2.X);
            tl.c = sign * (p1.Y * p2.X - p1.X * p2.Y);
            return tl;
        }
        // 根据直线解析方程返回直线的斜率k,水平线返回 0,竖直线返回 1e200 
        double slope(LINE l)
        {
            if (Math.Abs(l.a) < 1e-20)
                return 0;
            if (Math.Abs(l.b) < 1e-20)
                return INF;
            return -(l.a / l.b);
        }
        // 返回直线的倾斜角alpha ( 0 - pi) 
        double alpha(LINE l)
        {
            if (Math.Abs(l.a) < EP)
                return 0;
            if (Math.Abs(l.b) < EP)
                return PI / 2;
            double k = slope(l);
            if (k > 0)
                return Math.Atan(k);
            else
                return PI + Math.Atan(k);
        }
        // 求点p关于直线l的对称点  
        Point2d symmetry(LINE l, Point2d p)
        {
            Point2d tp;
            tp.X = ((l.b * l.b - l.a * l.a) * p.X - 2 * l.a * l.b * p.Y - 2 * l.a * l.c) / (l.a * l.a + l.b * l.b);
            tp.Y = ((l.a * l.a - l.b * l.b) * p.Y - 2 * l.a * l.b * p.X - 2 * l.b * l.c) / (l.a * l.a + l.b * l.b);
            return tp;
        }
        // 如果两条直线 l1(a1*x+b1*y+c1 = 0), l2(a2*x+b2*y+c2 = 0)相交，返回true，且返回交点p  
        bool lineintersect(LINE l1, LINE l2, Point2d p) // 是 L1，L2 
        {
            double d = l1.a * l2.b - l2.a * l1.b;
            if (Math.Abs(d) < EP) // 不相交 
                return false;
            p.X = (l2.c * l1.b - l1.c * l2.b) / d;
            p.Y = (l2.a * l1.c - l1.a * l2.c) / d;
            return true;
        }
        // 如果线段l1和l2相交，返回true且交点由(inter)返回，否则返回false 
        bool intersection(Line2d l1, Line2d l2, Point2d inter)
        {
            LINE ll1, ll2;
            ll1 = makeline(l1.StartPoint, l1.EndPoint);
            ll2 = makeline(l2.StartPoint, l2.EndPoint);
            if (lineintersect(ll1, ll2, inter))
                return online(l1, inter) && online(l2, inter);
            else
                return false;
        }

        /******************************\ 
        *         * 
        * 多边形常用算法模块    * 
        *         * 
        \******************************/

        // 如果无特别说明，输入多边形顶点要求按逆时针排列 

        /* 
        返回值：输入的多边形是简单多边形，返回true 
        要 求：输入顶点序列按逆时针排序 
        说 明：简单多边形定义： 
        1：循环排序中相邻线段对的交是他们之间共有的单个点 
        2：不相邻的线段不相交 
        本程序默认第一个条件已经满足 
        */
        //bool issimple(int vcount, Point2d[] polygon)
        //{
        //    int i, cn;
        //    Line2d l1, l2;
        //    for (i = 0; i < vcount; i++)
        //    {
        //        l1.s = polygon[i];
        //        l1.e = polygon[(i + 1) % vcount];
        //        cn = vcount - 3;
        //        while (cn)
        //        {
        //            l2.s = polygon[(i + 2) % vcount];
        //            l2.e = polygon[(i + 3) % vcount];
        //            if (intersect(l1, l2))
        //                break;
        //            cn--;
        //        }
        //        if (cn)
        //            return false;
        //    }
        //    return true;
        //}
        //// 返回值：按输入顺序返回多边形顶点的凸凹性判断，bc[i]=1,iff:第i个顶点是凸顶点 
        //void checkconvex(int vcount, Point2d[] polygon, bool[] bc)
        //{
        //    int i, index = 0;
        //    Point2d tp = polygon[0];
        //    for (i = 1; i < vcount; i++) // 寻找第一个凸顶点 
        //    {
        //        if (polygon[i].y < tp.y || (polygon[i].y == tp.y && polygon[i].x < tp.x))
        //        {
        //            tp = polygon[i];
        //            index = i;
        //        }
        //    }
        //    int count = vcount - 1;
        //    bc[index] = 1;
        //    while (count) // 判断凸凹性 
        //    {
        //        if (multiply(polygon[(index + 1) % vcount], polygon[(index + 2) % vcount], polygon[index]) >= 0)
        //            bc[(index + 1) % vcount] = 1;
        //        else
        //            bc[(index + 1) % vcount] = 0;
        //        index++;
        //        count--;
        //    }
        //}
        //// 返回值：多边形polygon是凸多边形时，返回true  
        //bool isconvex(int vcount, Point2d[] polygon)
        //{
        //    bool bc[MAXV];
        //    checkconvex(vcount, polygon, bc);
        //    for (int i = 0; i < vcount; i++) // 逐一检查顶点，是否全部是凸顶点 
        //        if (!bc[i])
        //            return false;
        //    return true;
        //}
        //// 返回多边形面积(signed)；输入顶点按逆时针排列时，返回正值；否则返回负值 
        //double area_of_polygon(int vcount, Point2d[] polygon)
        //{
        //    int i;
        //    double s;
        //    if (vcount < 3)
        //        return 0;
        //    s = polygon[0].y * (polygon[vcount - 1].x - polygon[1].x);
        //    for (i = 1; i < vcount; i++)
        //        s += polygon[i].y * (polygon[(i - 1)].x - polygon[(i + 1) % vcount].x);
        //    return s / 2;
        //}
        //// 如果输入顶点按逆时针排列，返回true 
        //bool isconterclock(int vcount, Point2d[] polygon)
        //{
        //    return area_of_polygon(vcount, polygon) > 0;
        //}
        //// 另一种判断多边形顶点排列方向的方法  
        //bool isccwize(int vcount, Point2d[] polygon)
        //{
        //    int i, index;
        //    Point2d a, b, v;
        //    v = polygon[0];
        //    index = 0;
        //    for (i = 1; i < vcount; i++) // 找到最低且最左顶点，肯定是凸顶点 
        //    {
        //        if (polygon[i].y < v.y || polygon[i].y == v.y && polygon[i].x < v.x)
        //        {
        //            index = i;
        //        }
        //    }
        //    a = polygon[(index - 1 + vcount) % vcount]; // 顶点v的前一顶点 
        //    b = polygon[(index + 1) % vcount]; // 顶点v的后一顶点 
        //    return multiply(v, b, a) > 0;
        //}
        /********************************************************************************************   
        射线法判断点q与多边形polygon的位置关系，要求polygon为简单多边形，顶点逆时针排列 
           如果点在多边形内：   返回0 
           如果点在多边形边上： 返回1 
           如果点在多边形外： 返回2 
        *********************************************************************************************/
        int insidepolygon(int vcount, Point2d[] Polygon, Point2d q)
        {
            int c = 0, i, n;
            Line2d l1 = new Line2d(), l2 = new Line2d();
            bool bintersect_a, bonline1, bonline2, bonline3;
            double r1, r2;

            l1.StartPoint = q;
            l1.EndPoint = q;
            l1.EndPoint.X = (double)INF;
            n = vcount;
            for (i = 0; i < vcount; i++)
            {
                l2.StartPoint = Polygon[i];
                l2.EndPoint = Polygon[(i + 1) % n];
                if (online(l2, q))
                    return 1; // 如果点在边上，返回1 
                if ((bintersect_a = intersect_A(l1, l2)) || // 相交且不在端点 
                ((bonline1 = online(l1, Polygon[(i + 1) % n])) && // 第二个端点在射线上 
                ((!(bonline2 = online(l1, Polygon[(i + 2) % n]))) && /* 前一个端点和后一个端点在射线两侧 */
                ((r1 = multiply(Polygon[i], Polygon[(i + 1) % n], l1.StartPoint) * multiply(Polygon[(i + 1) % n], Polygon[(i + 2) % n], l1.StartPoint)) > 0) ||
                (bonline3 = online(l1, Polygon[(i + 2) % n])) &&     /* 下一条边是水平线，前一个端点和后一个端点在射线两侧  */
                 ((r2 = multiply(Polygon[i], Polygon[(i + 2) % n], l1.StartPoint) * multiply(Polygon[(i + 2) % n],
                Polygon[(i + 3) % n], l1.StartPoint)) > 0)
                  )
                 )
                ) c++;
            }
            if (c % 2 == 1)
                return 0;
            else
                return 2;
        }
        //点q是凸多边形polygon内时，返回true；注意：多边形polygon一定要是凸多边形  
        bool InsideConvexPolygon(int vcount, Point2d[] polygon, Point2d q) // 可用于三角形！ 
        {
            Point2d p;
            Line2d l;
            int i;
            p.X = 0; p.Y = 0;
            for (i = 0; i < vcount; i++) // 寻找一个肯定在多边形polygon内的点p：多边形顶点平均值 
            {
                p.X += polygon[i].X;
                p.Y += polygon[i].Y;
            }
            p.X /= vcount;
            p.Y /= vcount;

            for (i = 0; i < vcount; i++)
            {
                l.StartPoint = polygon[i]; l.EndPoint = polygon[(i + 1) % vcount];
                if (multiply(p, l.EndPoint, l.StartPoint) * multiply(q, l.EndPoint, l.StartPoint) < 0) /* 点p和点q在边l的两侧，说明点q肯定在多边形外 */
                    break;
            }
            return (i == vcount);
        }
        /********************************************** 
        寻找凸包的graham 扫描法 
        PointSet为输入的点集； 
        ch为输出的凸包上的点集，按照逆时针方向排列; 
        n为PointSet中的点的数目 
        len为输出的凸包上的点的个数 
        **********************************************/
        void Graham_scan(Point2d[] PointSet, Point2d[] ch, int n, int len)
        {
            int i, j, k = 0, top = 2;
            Point2d tmp;
            // 选取PointSet中y坐标最小的点PointSet[k]，如果这样的点有多个，则取最左边的一个 
            for (i = 1; i < n; i++)
                if (PointSet[i].Y < PointSet[k].Y || (PointSet[i].Y == PointSet[k].Y) && (PointSet[i].X < PointSet[k].X))
                    k = i;
            tmp = PointSet[0];
            PointSet[0] = PointSet[k];
            PointSet[k] = tmp; // 现在PointSet中y坐标最小的点在PointSet[0] 
            for (i = 1; i < n - 1; i++) /* 对顶点按照相对PointSet[0]的极角从小到大进行排序，极角相同的按照距离PointSet[0]从近到远进行排序 */
            {
                k = i;
                for (j = i + 1; j < n; j++)
                    if (multiply(PointSet[j], PointSet[k], PointSet[0]) > 0 ||  // 极角更小    
                     (multiply(PointSet[j], PointSet[k], PointSet[0]) == 0) && /* 极角相等，距离更短 */
                     dist(PointSet[0], PointSet[j]) < dist(PointSet[0], PointSet[k])
                       )
                        k = j;
                tmp = PointSet[i];
                PointSet[i] = PointSet[k];
                PointSet[k] = tmp;
            }
            ch[0] = PointSet[0];
            ch[1] = PointSet[1];
            ch[2] = PointSet[2];
            for (i = 3; i < n; i++)
            {
                while (multiply(PointSet[i], ch[top], ch[top - 1]) >= 0)
                    top--;
                ch[++top] = PointSet[i];
            }
            len = top + 1;
        }
        //// 卷包裹法求点集凸壳，参数说明同graham算法    
        //void ConvexClosure(Point2d[] PointSet, Point2d[] ch, int n, int len)
        //{
        //    int top = 0, i, index, first;
        //    double curmax, curcos, curdis;
        //    Point2d tmp;
        //    Line2d l1, l2;
        //    bool use[MAXV];
        //    tmp = PointSet[0];
        //    index = 0;
        //    // 选取y最小点，如果多于一个，则选取最左点 
        //    for (i = 1; i < n; i++)
        //    {
        //        if (PointSet[i].y < tmp.y || PointSet[i].y == tmp.y && PointSet[i].x < tmp.x)
        //        {
        //            index = i;
        //        }
        //        use[i] = false;
        //    }
        //    tmp = PointSet[index];
        //    first = index;
        //    use[index] = true;

        //    index = -1;
        //    ch[top++] = tmp;
        //    tmp.x -= 100;
        //    l1.s = tmp;
        //    l1.e = ch[0];
        //    l2.s = ch[0];

        //    while (index != first)
        //    {
        //        curmax = -100;
        //        curdis = 0;
        //        // 选取与最后一条确定边夹角最小的点，即余弦值最大者 
        //        for (i = 0; i < n; i++)
        //        {
        //            if (use[i]) continue;
        //            l2.e = PointSet[i];
        //            curcos = cosine(l1, l2); // 根据Math.Cos值求夹角余弦，范围在 （-1 -- 1 ） 
        //            if (curcos > curmax || Math.Abs(curcos - curmax) < 1e-6 && dist(l2.s, l2.e) > curdis)
        //            {
        //                curmax = curcos;
        //                index = i;
        //                curdis = dist(l2.s, l2.e);
        //            }
        //        }
        //        use[first] = false;            //清空第first个顶点标志，使最后能形成封闭的hull 
        //        use[index] = true;
        //        ch[top++] = PointSet[index];
        //        l1.s = ch[top - 2];
        //        l1.e = ch[top - 1];
        //        l2.s = ch[top - 1];
        //    }
        //    len = top - 1;
        //}
        /*********************************************************************************************  
         判断线段是否在简单多边形内(注意：如果多边形是凸多边形，下面的算法可以化简) 
            必要条件一：线段的两个端点都在多边形内； 
         必要条件二：线段和多边形的所有边都不内交； 
         用途： 1. 判断折线是否在简单多边形内 
           2. 判断简单多边形是否在另一个简单多边形内 
        **********************************************************************************************/
        //bool LinesegInsidePolygon(int vcount, Point2d[] polygon, Line2d l)
        //{
        //    // 判断线端l的端点是否不都在多边形内 
        //    if (!insidepolygon(vcount, polygon, l.s) || !insidepolygon(vcount, polygon, l.e))
        //        return false;
        //    int top = 0, i, j;
        //    Point2d PointSet[300], tmp;
        //    Line2d s;

        //    for (i = 0; i < vcount; i++)
        //    {
        //        s.s = polygon[i];
        //        s.e = polygon[(i + 1) % vcount];
        //        if (online(s, l.s)) //线段l的起始端点在线段s上 
        //            PointSet[top++] = l.s;
        //        else if (online(s, l.e)) //线段l的终止端点在线段s上 
        //            PointSet[top++] = l.e;
        //        else
        //        {
        //            if (online(l, s.s)) //线段s的起始端点在线段l上 
        //                PointSet[top++] = s.s;
        //            else if (online(l, s.e)) // 线段s的终止端点在线段l上 
        //                PointSet[top++] = s.e;
        //            else
        //            {
        //                if (intersect(l, s)) // 这个时候如果相交，肯定是内交，返回false 
        //                    return false;
        //            }
        //        }
        //    }

        //    for (i = 0; i < top - 1; i++) /* 冒泡排序，x坐标小的排在前面；x坐标相同者，y坐标小的排在前面 */
        //    {
        //        for (j = i + 1; j < top; j++)
        //        {
        //            if (PointSet[i].x > PointSet[j].x || Math.Abs(PointSet[i].x - PointSet[j].x) < EP && PointSet[i].y > PointSet[j].y)
        //            {
        //                tmp = PointSet[i];
        //                PointSet[i] = PointSet[j];
        //                PointSet[j] = tmp;
        //            }
        //        }
        //    }

        //    for (i = 0; i < top - 1; i++)
        //    {
        //        tmp.x = (PointSet[i].x + PointSet[i + 1].x) / 2; //得到两个相邻交点的中点 
        //        tmp.y = (PointSet[i].y + PointSet[i + 1].y) / 2;
        //        if (!insidepolygon(vcount, polygon, tmp))
        //            return false;
        //    }
        //    return true;
        //}
        /*********************************************************************************************  
        求任意简单多边形polygon的重心 
        需要调用下面几个函数： 
         void AddPosPart(); 增加右边区域的面积 
         void AddNegPart(); 增加左边区域的面积 
         void AddRegion(); 增加区域面积 
        在使用该程序时，如果把xtr,ytr,wtr,xtl,ytl,wtl设成全局变量就可以使这些函数的形式得到化简,
        但要注意函数的声明和调用要做相应变化 
        **********************************************************************************************/
        void AddPosPart(double x, double y, double w, double xtr, double ytr, double wtr)
        {
            if (Math.Abs(wtr + w) < 1e-10) return; // detect zero regions 
            xtr = (wtr * xtr + w * x) / (wtr + w);
            ytr = (wtr * ytr + w * y) / (wtr + w);
            wtr = w + wtr;
            return;
        }
        void AddNegPart(double x, double y, double w, double xtl, double ytl, double wtl)
        {
            if (Math.Abs(wtl + w) < 1e-10)
                return; // detect zero regions 

            xtl = (wtl * xtl + w * x) / (wtl + w);
            ytl = (wtl * ytl + w * y) / (wtl + w);
            wtl = w + wtl;
            return;
        }
        void AddRegion(double x1, double y1, double x2, double y2, double xtr, double ytr,
          double wtr, double xtl, double ytl, double wtl)
        {
            if (Math.Abs(x1 - x2) < 1e-10)
                return;

            if (x2 > x1)
            {
                AddPosPart((x2 + x1) / 2, y1 / 2, (x2 - x1) * y1, xtr, ytr, wtr); /* rectangle 全局变量变化处 */
                AddPosPart((x1 + x2 + x2) / 3, (y1 + y1 + y2) / 3, (x2 - x1) * (y2 - y1) / 2, xtr, ytr, wtr);
                // triangle 全局变量变化处 
            }
            else
            {
                AddNegPart((x2 + x1) / 2, y1 / 2, (x2 - x1) * y1, xtl, ytl, wtl);
                // rectangle 全局变量变化处 
                AddNegPart((x1 + x2 + x2) / 3, (y1 + y1 + y2) / 3, (x2 - x1) * (y2 - y1) / 2, xtl, ytl, wtl);
                // triangle  全局变量变化处 
            }
        }
        Point2d cg_simple(int vcount, Point2d[] polygon)
        {
            double xtr, ytr, wtr, xtl, ytl, wtl;
            //注意： 如果把xtr,ytr,wtr,xtl,ytl,wtl改成全局变量后这里要删去 
            Point2d p1, p2, tp;
            xtr = ytr = wtr = 0.0;
            xtl = ytl = wtl = 0.0;
            for (int i = 0; i < vcount; i++)
            {
                p1 = polygon[i];
                p2 = polygon[(i + 1) % vcount];
                AddRegion(p1.X, p1.Y, p2.X, p2.Y, xtr, ytr, wtr, xtl, ytl, wtl); //全局变量变化处 
            }
            tp.X = (wtr * xtr + wtl * xtl) / (wtr + wtl);
            tp.Y = (wtr * ytr + wtl * ytl) / (wtr + wtl);
            return tp;
        }
        // 求凸多边形的重心,要求输入多边形按逆时针排序 
        Point2d gravitycenter(int vcount, Point2d[] polygon)
        {
            Point2d tp;
            double x, y, s, x0, y0, cs, k;
            x = 0; y = 0; s = 0;
            for (int i = 1; i < vcount - 1; i++)
            {
                x0 = (polygon[0].X + polygon[i].X + polygon[i + 1].X) / 3;
                y0 = (polygon[0].Y + polygon[i].Y + polygon[i + 1].Y) / 3; //求当前三角形的重心 
                cs = multiply(polygon[i], polygon[i + 1], polygon[0]) / 2;
                //三角形面积可以直接利用该公式求解 
                if (Math.Abs(s) < 1e-20)
                {
                    x = x0; y = y0; s += cs; continue;
                }
                k = cs / s; //求面积比例 
                x = (x + k * x0) / (1 + k);
                y = (y + k * y0) / (1 + k);
                s += cs;
            }
            tp.X = x;
            tp.Y = y;
            return tp;
        }

        /************************************************
        给定一简单多边形，找出一个肯定在该多边形内的点 
        定理1 ：每个多边形至少有一个凸顶点 
        定理2 ：顶点数>=4的简单多边形至少有一条对角线 
        结论 ： x坐标最大，最小的点肯定是凸顶点 
         y坐标最大，最小的点肯定是凸顶点            
        ************************************************/
        //Point2d a_point_insidepoly(int vcount, Point2d[] polygon)
        //{
        //    Point2d v, a, b, r;
        //    int i, index;
        //    v = polygon[0];
        //    index = 0;
        //    for (i = 1; i < vcount; i++) //寻找一个凸顶点 
        //    {
        //        if (polygon[i].y < v.y)
        //        {
        //            v = polygon[i];
        //            index = i;
        //        }
        //    }
        //    a = polygon[(index - 1 + vcount) % vcount]; //得到v的前一个顶点 
        //    b = polygon[(index + 1) % vcount]; //得到v的后一个顶点 
        //    Point2d tri[3], q;
        //    tri[0] = a; tri[1] = v; tri[2] = b;
        //    double md = INF;
        //    int in1 = index;
        //    bool bin = false;
        //    for (i = 0; i < vcount; i++) //寻找在三角形avb内且离顶点v最近的顶点q 
        //    {
        //        if (i == index) continue;
        //        if (i == (index - 1 + vcount) % vcount) continue;
        //        if (i == (index + 1) % vcount) continue;
        //        if (!InsideConvexPolygon(3, tri, polygon[i])) continue;
        //        bin = true;
        //        if (dist(v, polygon[i]) < md)
        //        {
        //            q = polygon[i];
        //            md = dist(v, q);
        //        }
        //    }
        //    if (!bin) //没有顶点在三角形avb内，返回线段ab中点 
        //    {
        //        r.x = (a.x + b.x) / 2;
        //        r.y = (a.y + b.y) / 2;
        //        return r;
        //    }
        //    r.x = (v.x + q.x) / 2; //返回线段vq的中点 
        //    r.y = (v.y + q.y) / 2;
        //    return r;
        //}
        /***********************************************************************************************
        求从多边形外一点p出发到一个简单多边形的切线,如果存在返回切点,其中rp点是右切点,lp是左切点 
        注意：p点一定要在多边形外 ,输入顶点序列是逆时针排列 
        原 理： 如果点在多边形内肯定无切线;凸多边形有唯一的两个切点,凹多边形就可能有多于两个的切点; 
          如果polygon是凸多边形，切点只有两个只要找到就可以,可以化简此算法 
          如果是凹多边形还有一种算法可以求解:先求凹多边形的凸包,然后求凸包的切线 
        /***********************************************************************************************/
        void pointtangentpoly(int vcount, Point2d[] polygon, Point2d p, Point2d rp, Point2d lp)
        {
            Line2d ep, en;
            bool blp, bln;
            rp = polygon[0];
            lp = polygon[0];
            for (int i = 1; i < vcount; i++)
            {
                ep.StartPoint = polygon[(i + vcount - 1) % vcount];
                ep.EndPoint = polygon[i];
                en.StartPoint = polygon[i];
                en.EndPoint = polygon[(i + 1) % vcount];
                blp = multiply(ep.EndPoint, p, ep.StartPoint) >= 0;                // p is to the left of pre edge 
                bln = multiply(en.EndPoint, p, en.StartPoint) >= 0;                // p is to the left of next edge 
                if (!blp && bln)
                {
                    if (multiply(polygon[i], rp, p) > 0)           // polygon[i] is above rp 
                        rp = polygon[i];
                }
                if (blp && !bln)
                {
                    if (multiply(lp, polygon[i], p) > 0)           // polygon[i] is below lp 
                        lp = polygon[i];
                }
            }
            return;
        }
        //// 如果多边形polygon的核存在，返回true，返回核上的一点p.顶点按逆时针方向输入  
        //bool core_exist(int vcount, Point2d[] polygon, Point2d p)
        //{
        //    int i, j, k;
        //    Line2d l;
        //    LINE lineset[MAXV];
        //    for (i = 0; i < vcount; i++)
        //    {
        //        lineset[i] = makeline(polygon[i], polygon[(i + 1) % vcount]);
        //    }
        //    for (i = 0; i < vcount; i++)
        //    {
        //        for (j = 0; j < vcount; j++)
        //        {
        //            if (i == j) continue;
        //            if (lineintersect(lineset[i], lineset[j], p))
        //            {
        //                for (k = 0; k < vcount; k++)
        //                {
        //                    l.s = polygon[k];
        //                    l.e = polygon[(k + 1) % vcount];
        //                    if (multiply(p, l.e, l.s) > 0)
        //                        //多边形顶点按逆时针方向排列，核肯定在每条边的左侧或边上 
        //                        break;
        //                }
        //                if (k == vcount)             //找到了一个核上的点 
        //                    break;
        //            }
        //        }
        //        if (j < vcount) break;
        //    }
        //    if (i < vcount)
        //        return true;
        //    else
        //        return false;
        //}
        /*************************\ 
        *       * 
        * 圆的基本运算           * 
        *          * 
        \*************************/
        /******************************************************************************
        返回值 ： 点p在圆内(包括边界)时，返回true 
        用途 ： 因为圆为凸集，所以判断点集，折线，多边形是否在圆内时，
         只需要逐一判断点是否在圆内即可。 
        *******************************************************************************/
        bool point_in_circle(Point2d o, double r, Point2d p)
        {
            double d2 = (p.X - o.X) * (p.X - o.X) + (p.Y - o.Y) * (p.Y - o.Y);
            double r2 = r * r;
            return d2 < r2 || Math.Abs(d2 - r2) < EP;
        }
        /******************************************************************************
        用 途 ：求不共线的三点确定一个圆 
        输 入 ：三个点p1,p2,p3 
        返回值 ：如果三点共线，返回false；反之，返回true。圆心由q返回，半径由r返回 
        *******************************************************************************/
        bool cocircle(Point2d p1, Point2d p2, Point2d p3, Point2d q, double r)
        {
            double x12 = p2.X - p1.X;
            double y12 = p2.Y - p1.Y;
            double x13 = p3.X - p1.X;
            double y13 = p3.Y - p1.Y;
            double z2 = x12 * (p1.X + p2.X) + y12 * (p1.Y + p2.Y);
            double z3 = x13 * (p1.X + p3.X) + y13 * (p1.Y + p3.Y);
            double d = 2.0 * (x12 * (p3.Y - p2.Y) - y12 * (p3.X - p2.X));
            if (Math.Abs(d) < EP) //共线，圆不存在 
                return false;
            q.X = (y13 * z2 - y12 * z3) / d;
            q.Y = (x12 * z3 - x13 * z2) / d;
            r = dist(p1, q);
            return true;
        }
        bool line_circle(LINE l, Point2d o, double r, Point2d p1, Point2d p2)
        {
            return true;
        }

        /**************************\ 
        *        * 
        * 矩形的基本运算          * 
        *                         * 
        \**************************/
        /* 
        说明：因为矩形的特殊性，常用算法可以化简： 
        1.判断矩形是否包含点 
        只要判断该点的横坐标和纵坐标是否夹在矩形的左右边和上下边之间。 
        2.判断线段、折线、多边形是否在矩形中 
        因为矩形是个凸集，所以只要判断所有端点是否都在矩形中就可以了。 
        3.判断圆是否在矩形中 
        圆在矩形中的充要条件是：圆心在矩形中且圆的半径小于等于圆心到矩形四边的距离的最小值。 
        */
        // 已知矩形的三个顶点(a,b,c)，计算第四个顶点d的坐标. 注意：已知的三个顶点可以是无序的 
        Point2d rect4th(Point2d a, Point2d b, Point2d c)
        {
            Point2d d = new Point2d();
            if (Math.Abs(dotmultiply(a, b, c)) < EP) // 说明c点是直角拐角处 
            {
                d.X = a.X + b.X - c.X;
                d.Y = a.Y + b.Y - c.Y;
            }
            if (Math.Abs(dotmultiply(a, c, b)) < EP) // 说明b点是直角拐角处 
            {
                d.X = a.X + c.X - b.X;
                d.Y = a.Y + c.Y - b.X;
            }
            if (Math.Abs(dotmultiply(c, b, a)) < EP) // 说明a点是直角拐角处 
            {
                d.X = c.X + b.X - a.X;
                d.Y = c.Y + b.Y - a.Y;
            }
            return d;
        }

        /*************************\ 
        *      * 
        * 常用算法的描述  * 
        *      * 
        \*************************/
        /* 
        尚未实现的算法： 
        1. 求包含点集的最小圆 
        2. 求多边形的交 
        3. 简单多边形的三角剖分 
        4. 寻找包含点集的最小矩形 
        5. 折线的化简 
        6. 判断矩形是否在矩形中 
        7. 判断矩形能否放在矩形中 
        8. 矩形并的面积与周长 
        9. 矩形并的轮廓 
        10.矩形并的闭包 
        11.矩形的交 
        12.点集中的最近点对 
        13.多边形的并 
        14.圆的交与并 
        15.直线与圆的关系 
        16.线段与圆的关系 
        17.求多边形的核监视摄象机 
        18.求点集中不相交点对 railwai 
        *//* 
        寻找包含点集的最小矩形 
        原理：该矩形至少一条边与点集的凸壳的某条边共线 
        First take the convex hull of the points. Let the resulting convex 
        polygon be P. It has been known for some time that the minimum 
        area rectangle enclosing P must have one rectangle side flush with 
        (i.e., collinear with and overlapping) one edge of P. This geometric 
        fact was used by Godfried Toussaint to develop the "rotating calipers" 
        algorithm in a hard-to-find 1983 paper, "Solving Geometric Problems 
        with the Rotating Calipers" (Proc. IEEE MELECON). The algorithm 
        rotates a surrounding rectangle from one flush edge to the next, 
        keeping track of the minimum area for each edge. It achieves O(n) 
        time (after hull computation). See the "Rotating Calipers Homepage" 
        http://www.cs.mcgill.ca/~orm/rotcal.frame.html for a description 
        and applet. 
        *//* 
        折线的化简 伪码如下： 
        Input: tol = the approximation tolerance 
        L = {V0,V1,http://www.cnblogs.com/Images/dot.gif,Vn-1} is any n-vertex polyline 

        Set start = 0; 
        Set k = 0; 
        Set W0 = V0; 
        for each vertex Vi (i=1,n-1) 
        { 
        if Vi is within tol from Vstart 
        then ignore it, and continue with the next vertex 

        Vi is further than tol away from Vstart 
        so add it as a new vertex of the reduced polyline 
        Increment k++; 
        Set Wk = Vi; 
        Set start = i; as the new initial vertex 
        } 

        Output: W = {W0,W1,http://www.cnblogs.com/Images/dot.gif,Wk-1} = the k-vertex simplified polyline 
        */
        /********************\ 
        *        * 
        * 补充    * 
        *     * 
        \********************/

        //两圆关系： 
        /* 两圆： 
        相离： return 1； 
        外切： return 2； 
        相交： return 3； 
        内切： return 4； 
        内含： return 5； 
        */
        int CircleRelation(Point2d p1, double r1, Point2d p2, double r2)
        {
            double d = Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));

            if (Math.Abs(d - r1 - r2) < EP) // 必须保证前两个if先被判定！ 
                return 2;
            if (Math.Abs(d - Math.Abs(r1 - r2)) < EP)
                return 4;
            if (d > r1 + r2)
                return 1;
            if (d < Math.Abs(r1 - r2))
                return 5;
            if (Math.Abs(r1 - r2) < d && d < r1 + r2)
                return 3;
            return 0; // indicate an error! 
        }
        //判断圆是否在矩形内：
        // 判定圆是否在矩形内，是就返回true（设矩形水平，且其四个顶点由左上开始按顺时针排列） 
        // 调用ptoldist函数，在第4页 
        bool CircleRecRelation(Point2d pc, double r, Point2d pr1, Point2d pr2, Point2d pr3, Point2d pr4)
        {
            if (pr1.X < pc.X && pc.X < pr2.X && pr3.Y < pc.Y && pc.Y < pr2.Y)
            {
                Line2d line1 = new Line2d(pr1, pr2);
                Line2d line2 = new Line2d(pr2, pr3);
                Line2d line3 = new Line2d(pr3, pr4);
                Line2d line4 = new Line2d(pr4, pr1);
                if (r < ptoldist(pc, line1) && r < ptoldist(pc, line2) && r < ptoldist(pc, line3) && r < ptoldist(pc, line4))
                    return true;
            }
            return false;
        }
        //点到平面的距离： 
        //点到平面的距离,平面用一般式表示ax+by+cz+d=0 
        double P2planeDist(double x, double y, double z, double a, double b, double c, double d)
        {
            return Math.Abs(a * x + b * y + c * z + d) / Math.Sqrt(a * a + b * b + c * c);
        }
        //点是否在直线同侧：
        //两个点是否在直线同侧，是则返回true 
        bool SameSide(Point2d p1, Point2d p2, LINE line)
        {
            return (line.a * p1.X + line.b * p1.Y + line.c) *
            (line.a * p2.X + line.b * p2.Y + line.c) > 0;
        }
        //镜面反射线：
        // 已知入射线、镜面，求反射线。 
        // a1,b1,c1为镜面直线方程(a1 x + b1 y + c1 = 0 ,下同)系数;  
        //a2,b2,c2为入射光直线方程系数;  
        //a,b,c为反射光直线方程系数. 
        // 光是有方向的，使用时注意：入射光向量:<-b2,a2>；反射光向量:<b,-a>. 
        // 不要忘记结果中可能会有"negative zeros" 
        void reflect(double a1, double b1, double c1, double a2, double b2, double c2, double a, double b, double c)
        {
            double n, m;
            double tpb, tpa;
            tpb = b1 * b2 + a1 * a2;
            tpa = a2 * b1 - a1 * b2;
            m = (tpb * b1 + tpa * a1) / (b1 * b1 + a1 * a1);
            n = (tpa * b1 - tpb * a1) / (b1 * b1 + a1 * a1);
            if (Math.Abs(a1 * b2 - a2 * b1) < 1e-20)
            {
                a = a2; b = b2; c = c2;
                return;
            }
            double xx, yy; //(xx,yy)是入射线与镜面的交点。 
            xx = (b1 * c2 - b2 * c1) / (a1 * b2 - a2 * b1);
            yy = (a2 * c1 - a1 * c2) / (a1 * b2 - a2 * b1);
            a = n;
            b = -m;
            c = m * yy - xx * n;
        }
        //矩形包含： 
        // 矩形2（C，D）是否在1（A，B）内
        bool r2inr1(double A, double B, double C, double D)
        {
            double X, Y, L, K, DMax;
            if (A < B)
            {
                double tmp = A;
                A = B;
                B = tmp;
            }
            if (C < D)
            {
                double tmp = C;
                C = D;
                D = tmp;
            }
            if (A > C && B > D)                 // trivial case  
                return true;
            else
             if (D >= B)
                return false;
            else
            {
                X = Math.Sqrt(A * A + B * B);         // outer rectangle's diagonal  
                Y = Math.Sqrt(C * C + D * D);         // inner rectangle's diagonal  
                if (Y < B) // check for marginal conditions 
                    return true; // the inner rectangle can freely rotate inside 
                else
                 if (Y > X)
                    return false;
                else
                {
                    L = (B - Math.Sqrt(Y * Y - A * A)) / 2;
                    K = (A - Math.Sqrt(Y * Y - B * B)) / 2;
                    DMax = Math.Sqrt(L * L + K * K);
                    if (D >= DMax)
                        return false;
                    else
                        return true;
                }
            }
        }
        //两圆交点： 
        // 两圆已经相交（相切） 
        void c2point(Point2d p1, double r1, Point2d p2, double r2, Point2d rp1, Point2d rp2)
        {
            double a, b, r;
            a = p2.X - p1.X;
            b = p2.Y - p1.Y;
            r = (a * a + b * b + r1 * r1 - r2 * r2) / 2;
            if (a == 0 && b != 0)
            {
                rp1.Y = rp2.Y = r / b;
                rp1.X = Math.Sqrt(r1 * r1 - rp1.Y * rp1.Y);
                rp2.X = -rp1.X;
            }
            else if (a != 0 && b == 0)
            {
                rp1.X = rp2.X = r / a;
                rp1.Y = Math.Sqrt(r1 * r1 - rp1.X * rp2.X);
                rp2.Y = -rp1.Y;
            }
            else if (a != 0 && b != 0)
            {
                double delta;
                delta = b * b * r * r - (a * a + b * b) * (r * r - r1 * r1 * a * a);
                rp1.Y = (b * r + Math.Sqrt(delta)) / (a * a + b * b);
                rp2.Y = (b * r - Math.Sqrt(delta)) / (a * a + b * b);
                rp1.X = (r - b * rp1.Y) / a;
                rp2.X = (r - b * rp2.Y) / a;
            }

            rp1.X += p1.X;
            rp1.Y += p1.Y;
            rp2.X += p1.X;
            rp2.Y += p1.Y;
        }
        //两圆公共面积：
        // 必须保证相交 
        //double c2area(Point2d p1, double r1, Point2d p2, double r2)
        //{
        //    Point2d rp1 = new Point2d(), rp2 = new Point2d();
        //    c2point(p1, r1, p2, r2, rp1, rp2);

        //    if (r1 > r2) //保证r2>r1 
        //    {
        //        swap(p1, p2);
        //        swap(r1, r2);
        //    }
        //    double a, b, rr;
        //    a = p1.x - p2.x;
        //    b = p1.y - p2.y;
        //    rr = Math.Sqrt(a * a + b * b);

        //    double dx1, dy1, dx2, dy2;
        //    double sita1, sita2;
        //    dx1 = rp1.x - p1.x;
        //    dy1 = rp1.y - p1.y;
        //    dx2 = rp2.x - p1.x;
        //    dy2 = rp2.y - p1.y;
        //    sita1 = Math.Acos((dx1 * dx2 + dy1 * dy2) / r1 / r1);

        //    dx1 = rp1.x - p2.x;
        //    dy1 = rp1.y - p2.y;
        //    dx2 = rp2.x - p2.x;
        //    dy2 = rp2.y - p2.y;
        //    sita2 = Math.Acos((dx1 * dx2 + dy1 * dy2) / r2 / r2);
        //    double s = 0;
        //    if (rr < r2)//相交弧为优弧 
        //        s = r1 * r1 * (PI - sita1 / 2 + Math.Sin(sita1) / 2) + r2 * r2 * (sita2 - Math.Sin(sita2)) / 2;
        //    else//相交弧为劣弧 
        //        s = (r1 * r1 * (sita1 - Math.Sin(sita1)) + r2 * r2 * (sita2 - Math.Sin(sita2))) / 2;

        //    return s;
        //}
        //圆和直线关系： 
        //0----相离 1----相切 2----相交 
        int clpoint(Point2d p, double r, double a, double b, double c, Point2d rp1, Point2d rp2)
        {
            int res = 0;

            c = c + a * p.X + b * p.Y;
            double tmp;
            if (a == 0 && b != 0)
            {
                tmp = -c / b;
                if (r * r < tmp * tmp)
                    res = 0;
                else if (r * r == tmp * tmp)
                {
                    res = 1;
                    rp1.Y = tmp;
                    rp1.X = 0;
                }
                else
                {
                    res = 2;
                    rp1.Y = rp2.Y = tmp;
                    rp1.X = Math.Sqrt(r * r - tmp * tmp);
                    rp2.X = -rp1.X;
                }
            }
            else if (a != 0 && b == 0)
            {
                tmp = -c / a;
                if (r * r < tmp * tmp)
                    res = 0;
                else if (r * r == tmp * tmp)
                {
                    res = 1;
                    rp1.X = tmp;
                    rp1.Y = 0;
                }
                else
                {
                    res = 2;
                    rp1.X = rp2.X = tmp;
                    rp1.Y = Math.Sqrt(r * r - tmp * tmp);
                    rp2.Y = -rp1.Y;
                }
            }
            else if (a != 0 && b != 0)
            {
                double delta;
                delta = b * b * c * c - (a * a + b * b) * (c * c - a * a * r * r);
                if (delta < 0)
                    res = 0;
                else if (delta == 0)
                {
                    res = 1;
                    rp1.Y = -b * c / (a * a + b * b);
                    rp1.X = (-c - b * rp1.Y) / a;
                }
                else
                {
                    res = 2;
                    rp1.Y = (-b * c + Math.Sqrt(delta)) / (a * a + b * b);
                    rp2.Y = (-b * c - Math.Sqrt(delta)) / (a * a + b * b);
                    rp1.X = (-c - b * rp1.Y) / a;
                    rp2.X = (-c - b * rp2.Y) / a;
                }
            }
            rp1.X += p.X;
            rp1.Y += p.Y;
            rp2.X += p.X;
            rp2.Y += p.Y;
            return res;
        }
        //内切圆： 
        void incircle(Point2d p1, Point2d p2, Point2d p3, Point2d rp, double r)
        {
            double dx31, dy31, dx21, dy21, d31, d21, a1, b1, c1;
            dx31 = p3.X - p1.X;
            dy31 = p3.Y - p1.Y;
            dx21 = p2.X - p1.X;
            dy21 = p2.Y - p1.Y;

            d31 = Math.Sqrt(dx31 * dx31 + dy31 * dy31);
            d21 = Math.Sqrt(dx21 * dx21 + dy21 * dy21);
            a1 = dx31 * d21 - dx21 * d31;
            b1 = dy31 * d21 - dy21 * d31;
            c1 = a1 * p1.X + b1 * p1.Y;

            double dx32, dy32, dx12, dy12, d32, d12, a2, b2, c2;
            dx32 = p3.X - p2.X;
            dy32 = p3.Y - p2.Y;
            dx12 = -dx21;
            dy12 = -dy21;

            d32 = Math.Sqrt(dx32 * dx32 + dy32 * dy32);
            d12 = d21;
            a2 = dx12 * d32 - dx32 * d12;
            b2 = dy12 * d32 - dy32 * d12;
            c2 = a2 * p2.X + b2 * p2.Y;

            rp.X = (c1 * b2 - c2 * b1) / (a1 * b2 - a2 * b1);
            rp.Y = (c2 * a1 - c1 * a2) / (a1 * b2 - a2 * b1);
            r = Math.Abs(dy21 * rp.X - dx21 * rp.Y + dx21 * p1.Y - dy21 * p1.X) / d21;
        }
        //求切点： 
        // p---圆心坐标， r---圆半径， sp---圆外一点， rp1,rp2---切点坐标 
        void cutpoint(Point2d p, double r, Point2d sp, Point2d rp1, Point2d rp2)
        {
            Point2d p2;
            p2.X = (p.X + sp.X) / 2;
            p2.Y = (p.Y + sp.Y) / 2;

            double dx2, dy2, r2;
            dx2 = p2.X - p.X;
            dy2 = p2.Y - p.Y;
            r2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);
            c2point(p, r, p2, r2, rp1, rp2);
        }
        //线段的左右旋： 
        /* l2在l1的左/右方向（l1为基准线） 
        返回 0 ： 重合； 
        返回 1 ： 右旋； 
        返回 –1 ： 左旋； 
        */
        int rotat(Line2d l1, Line2d l2)
        {
            double dx1, dx2, dy1, dy2;
            dx1 = l1.StartPoint.X - l1.EndPoint.X;
            dy1 = l1.StartPoint.Y - l1.EndPoint.Y;
            dx2 = l2.StartPoint.X - l2.EndPoint.X;
            dy2 = l2.StartPoint.Y - l2.EndPoint.Y;

            double d;
            d = dx1 * dy2 - dx2 * dy1;
            if (d == 0)
                return 0;
            else if (d > 0)
                return -1;
            else
                return 1;
        }
        /*
        公式： 

        球坐标公式： 
        直角坐标为 P(x, y, z) 时，对应的球坐标是(rsinφMath.Cosθ, rsinφMath.Sinθ, rcosφ),其中φ是向量OP与Z轴的夹角，范围[0，π]；是OP在XOY面上的投影到X轴的旋角，范围[0，2π]  

        直线的一般方程转化成向量方程： 
        ax+by+c=0 
        x-x0     y-y0 
           ------ = ------- // (x0,y0)为直线上一点，m,n为向量 
        m        n 
        转换关系： 
        a=n；b=-m；c=m·y0-n·x0； 
        m=-b; n=a; 

        三点平面方程： 
        三点为P1，P2，P3 
        设向量  M1=P2-P1; M2=P3-P1; 
        平面法向量：  M=M1 x M2 （） 
        平面方程：    M.i(x-P1.x)+M.j(y-P1.y)+M.k(z-P1.z)=0 
        */

        //计算几何C#实现和C#代码例子

        //public struct Line2d
        //{
        //    public Point2d s;
        //    public Point2d e;
        //    public Line2d(Point2d a, Point2d b) { s = a; e = b; }
        //};
        public const double EPS = 0.1;


        /* 求点p到线段l的最短距离,并返回线段上距该点最近的点np
        注意：np是线段l上到点p最近的点，不一定是垂足 */
        public static double ptolinesegdist(Point2d p, Line2d l, out Point2d np)
        {
            np = new Point2d(-1, -1);
            double r = relation(p, l);
            if (r < 0)
            {
                np = l.StartPoint;
                return DistanceTo(p, l.StartPoint);
            }
            if (r > 1)
            {
                np = l.EndPoint;
                return DistanceTo(p, l.EndPoint);
            }
            np = perpendicular(p, l);
            return DistanceTo(p, np);
        }

        /*
        r=dotmultiply(p1,p2,op),得到矢量(p1-op)和(p2-op)的点积，如果两个矢量都非零矢量
        r<0：两矢量夹角为钝角；
        r=0：两矢量夹角为直角；
        r>0：两矢量夹角为锐角
        *******************************************************************************/
        //public static double dotmultiply(Point2d p1, Point2d p2, Point2d p0)
        //{
        //    return ((p1.X - p0.X) * (p2.X - p0.X) + (p1.Y - p0.Y) * (p2.Y - p0.Y));
        //}

        //public static double relation(Point2d p, Line2d l)
        //{
        //    Line2d tl;
        //    tl.s = l.s;
        //    tl.e = p;
        //    return dotmultiply(tl.e, l.e, l.s) / (DistanceTo(l.s, l.e) * DistanceTo(l.s, l.e));
        //}
        //// 求点C到线段AB所在直线的垂足 P
        //public static Point2d perpendicular(Point2d p, Line2d l)
        //{
        //    double r = relation(p, l);
        //    Point2d tp = new Point2d(-1, -1);
        //    tp.X = (float)(l.s.X + r * (l.e.X - l.s.X));
        //    tp.Y = (float)(l.s.Y + r * (l.e.Y - l.s.Y));
        //    return tp;
        //}

        /// <summary>
        /// 求不共线的三点确定一个圆
        ///输 入 ：三个点p1,p2,p3
        /// 返回值 ：如果三点共线，返回false；反之，返回true。圆心由q返回，半径由r返回
        /// </summary>
        public static bool cocircle(Point2d p1, Point2d p2, Point2d p3, out Point2d q, out double r)
        {
            q = new Point2d(-1, -1);
            r = -1;
            double x12 = p2.X - p1.X;
            double y12 = p2.Y - p1.Y;
            double x13 = p3.X - p1.X;
            double y13 = p3.Y - p1.Y;
            double z2 = x12 * (p1.X + p2.X) + y12 * (p1.Y + p2.Y);
            double z3 = x13 * (p1.X + p3.X) + y13 * (p1.Y + p3.Y);
            double d = 2.0 * (x12 * (p3.Y - p2.Y) - y12 * (p3.X - p2.X));
            if (Math.Abs(d) < EPS) //共线，圆不存在
                return false;
            q.X = (float)((y13 * z2 - y12 * z3) / d);
            q.Y = (float)((x12 * z3 - x13 * z2) / d);
            r = DistanceTo(p1, q);
            return true;
        }
        /// <summary>
        /// 求不共线的三点确定一个内切圆
        ///输 入 ：三个点p1,p2,p3
        /// 返回值 ：如果三点共线，返回false；反之，返回true。圆心由q返回，半径由r返回
        /// </summary>
        public static void incircle(Point2d p1, Point2d p2, Point2d p3, out Point2d rp, out double r)
        {
            r = 0;
            rp = new Point2d(-1, -1);
            double dx31, dy31, dx21, dy21, d31, d21, a1, b1, c1;
            dx31 = p3.X - p1.X;
            dy31 = p3.Y - p1.Y;
            dx21 = p2.X - p1.X;
            dy21 = p2.Y - p1.Y;

            d31 = Math.Sqrt(dx31 * dx31 + dy31 * dy31);
            d21 = Math.Sqrt(dx21 * dx21 + dy21 * dy21);
            a1 = dx31 * d21 - dx21 * d31;
            b1 = dy31 * d21 - dy21 * d31;
            c1 = a1 * p1.X + b1 * p1.Y;

            double dx32, dy32, dx12, dy12, d32, d12, a2, b2, c2;
            dx32 = p3.X - p2.X;
            dy32 = p3.Y - p2.Y;
            dx12 = -dx21;
            dy12 = -dy21;

            d32 = Math.Sqrt(dx32 * dx32 + dy32 * dy32);
            d12 = d21;
            a2 = dx12 * d32 - dx32 * d12;
            b2 = dy12 * d32 - dy32 * d12;
            c2 = a2 * p2.X + b2 * p2.Y;

            rp.X = (float)((c1 * b2 - c2 * b1) / (a1 * b2 - a2 * b1));
            rp.Y = (float)((c2 * a1 - c1 * a2) / (a1 * b2 - a2 * b1));
            r = Math.Abs(dy21 * rp.X - dx21 * rp.Y + dx21 * p1.Y - dy21 * p1.X) / d21;
        }

        /*
        Calculate the intersection of a ray and a sphere
        The line segment is defined from p1 to p2
        The sphere is of radius半径 r and centered at sc
        There are potentially two points of intersection given by
        p = p1 + mu1 (p2 - p1)
        p = p1 + mu2 (p2 - p1)
        Return FALSE if the ray doesn't intersect the sphere.
        * 经验证点在圆形外面
        */
        public static bool RaySphere(Point2d p1, Point2d p2, Point2d sc, double r, out Point2d op1, out Point2d op2)
        {
            double a, b, c;
            double bb4ac;
            Point2d dp = new Point2d(0, 0);

            dp.X = p2.X - p1.X;
            dp.Y = p2.Y - p1.Y;
            a = dp.X * dp.X + dp.Y * dp.Y;
            b = 2 * (dp.X * (p1.X - sc.X) + dp.Y * (p1.Y - sc.Y));
            c = sc.X * sc.X + sc.Y * sc.Y;
            c += p1.X * p1.X + p1.Y * p1.Y;
            c -= 2 * (sc.X * p1.X + sc.Y * p1.Y);
            c -= r * r;
            bb4ac = b * b - 4 * a * c;
            if (Math.Abs(a) < EPS || bb4ac < 0)
            {
                op1 = new Point2d(0, 0);
                op2 = new Point2d(0, 0);
                return false;
            }

            double mu1 = (-b + Math.Sqrt(bb4ac)) / (2 * a);
            double mu2 = (-b - Math.Sqrt(bb4ac)) / (2 * a);
            op1 = new Point2d((float)(p1.X + mu1 * (p1.X - p2.X)), (float)(p1.Y + mu1 * (p1.Y - p2.Y)));
            op2 = new Point2d((float)(p1.X + mu2 * (p1.X - p2.X)), (float)(p1.Y + mu2 * (p1.Y - p2.Y)));
            return true;
        }

        /*
        原理：
        r = dotmultiply(s,e,o) / (dist(o,s)*dist(o,e))
        r'= multiply(s,e,o)

        r >= 1 angle = 0;
        r <= -1 angle = -PI
        -1<r<1 && r'>0 angle = arccos(r)
        -1<r<1 && r'<=0 angle = -arccos(r)
        */
        /// <summary>
        /// 返回顶角在o点，起始边为os，终止边为oe的夹角(单位：弧度)
        ///角度小于pi，返回正值
        /// 角度大于pi，返回负值
        /// 可以用于求线段之间的夹角
        /// 经圆心到矩形点上可以找到交点
        /// </summary>
        /// <param name="ptStart">线段起点</param>
        /// <param name="ptEnd">线段终点</param>
        /// <param name="ptCenter">圆心坐标</param>
        /// <param name="Radius2">圆半径平方</param>
        /// <param name="ptInter1">交点1(若不存在返回65536)</param>
        /// <param name="ptInter2">交点2(若不存在返回65536)</param>
        public static double Angle(Point2d o, Point2d s, Point2d e)
        {
            double cosfi, fi, norm;
            double dsx = s.X - o.X;
            double dsy = s.Y - o.Y;
            double dex = e.X - o.X;
            double dey = e.Y - o.Y;

            cosfi = dsx * dex + dsy * dey;
            norm = (dsx * dsx + dsy * dsy) * (dex * dex + dey * dey);
            cosfi /= Math.Sqrt(norm);

            if (cosfi >= 1.0) return 0;
            if (cosfi <= -1.0) return -3.1415926;

            fi = Math.Acos(cosfi);
            if (dsx * dey - dsy * dex > 0) return fi; // 说明矢量os 在矢量 oe的顺时针方向
            return -fi;
        }

        /// <summary>
        /// 线段与圆的交点
        /// 经圆心到矩形点上可以找到交点
        /// </summary>
        /// <param name="ptStart">线段起点</param>
        /// <param name="ptEnd">线段终点</param>
        /// <param name="ptCenter">圆心坐标</param>
        /// <param name="Radius2">圆半径平方</param>
        /// <param name="ptInter1">交点1(若不存在返回65536)</param>
        /// <param name="ptInter2">交点2(若不存在返回65536)</param>
        public static bool LineInterCircle(Point2d ptStart, Point2d ptEnd, Point2d ptCenter, double Radius2,
        ref Point2d ptInter1, ref Point2d ptInter2)
        {
            ptInter1.X = ptInter2.X = 65536.0f;
            ptInter2.Y = ptInter2.Y = 65536.0f;
            float fDis = (float)Math.Sqrt((ptEnd.X - ptStart.X) * (ptEnd.X - ptStart.X) + (ptEnd.Y - ptStart.Y) * (ptEnd.Y - ptStart.Y));
            Point2d d = new Point2d();
            d.X = (ptEnd.X - ptStart.X) / fDis;
            d.Y = (ptEnd.Y - ptStart.Y) / fDis;
            Point2d E = new Point2d();
            E.X = ptCenter.X - ptStart.X;
            E.Y = ptCenter.Y - ptStart.Y;
            float a = (float)(E.X * d.X + E.Y * d.Y);
            float a2 = a * a;
            float e2 = (float)(E.X * E.X + E.Y * E.Y);
            if ((Radius2 - e2 + a2) < 0)
            {
                return false;
            }
            else
            {
                float f = (float)Math.Sqrt(Radius2 - e2 + a2);
                float t = a - f;
                if (((t - 0.0) > -EPS) && (t - fDis) < EPS)
                {
                    ptInter1.X = ptStart.X + t * d.X;
                    ptInter1.Y = ptStart.Y + t * d.Y;
                }
                t = a + f;
                if (((t - 0.0) > -EPS) && (t - fDis) < EPS)
                {
                    ptInter2.X = ptStart.X + t * d.X;
                    ptInter2.Y = ptStart.Y + t * d.Y;
                }
                return true;
            }
        }
        /// <summary>
        /// 计算点P(x,y)与X轴正方向的夹角
        /// </summary>
        /// <param name="x">横坐标</param>
        /// <param name="y">纵坐标</param>
        /// <returns>夹角弧度</returns>
        private static double radPOX(double x, double y)
        {
            //P在(0,0)的情况
            if (x == 0 && y == 0) return 0;

            //P在四个坐标轴上的情况：x正、x负、y正、y负
            if (y == 0 && x > 0) return 0;
            if (y == 0 && x < 0) return Math.PI;
            if (x == 0 && y > 0) return Math.PI / 2;
            if (x == 0 && y < 0) return Math.PI / 2 * 3;

            //点在第一、二、三、四象限时的情况
            if (x > 0 && y > 0) return Math.Atan(y / x);
            if (x < 0 && y > 0) return Math.PI - Math.Atan(y / -x);
            if (x < 0 && y < 0) return Math.PI + Math.Atan(-y / -x);
            if (x > 0 && y < 0) return Math.PI * 2 - Math.Atan(-y / x);

            return 0;
        }
        public static double DistanceTo(Point2d pfrom, Point2d p)
        {
            return Math.Sqrt((p.X - pfrom.X) * (p.X - pfrom.X) + (p.Y - pfrom.Y) * (p.Y - pfrom.Y));
        }
        /// <summary>
        /// 返回点P围绕点A旋转弧度rad后的坐标
        /// </summary>
        /// <param name="P">待旋转点坐标</param>
        /// <param name="A">旋转中心坐标</param>
        /// <param name="rad">旋转弧度</param>
        /// <param name="isClockwise">true:顺时针/false:逆时针</param>
        /// <returns>旋转后坐标</returns>
        public static Point2d RotatePoint(Point2d P, Point2d A, double rad, bool isClockwise = true)
        {
            //点Temp1
            Point2d Temp1 = new Point2d(P.X - A.X, P.Y - A.Y);
            //点Temp1到原点的长度
            double lenO2Temp1 = DistanceTo(Temp1, new Point2d(0, 0));
            //∠T1OX弧度
            double angT1OX = radPOX(Temp1.X, Temp1.Y);
            //∠T2OX弧度（T2为T1以O为圆心旋转弧度rad）
            double angT2OX = angT1OX - (isClockwise ? 1 : -1) * rad;
            //点Temp2
            Point2d Temp2 = new Point2d(
            (int)(lenO2Temp1 * Math.Cos(angT2OX)),
            (int)(lenO2Temp1 * Math.Sin(angT2OX)));
            //点Q
            return new Point2d(Temp2.X + A.X, Temp2.Y + A.Y);
        }
    }

    #endregion
}
