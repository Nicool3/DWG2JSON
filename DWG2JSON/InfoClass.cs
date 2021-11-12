using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;

namespace DWG2JSON
{
    /// <summary>
    /// 轴网
    /// </summary>
    public class GridInfo
    {
        public string Name { get; set; }                // 轴网名称
        public double[] ColumnSpacing { get; set; }      // 轴网列间距数组
        public double[] RowSpacing { get; set; }        // 轴网行间距数组

        /// <summary>
        /// 根据轴间距生成轴网
        /// </summary>
        /// <param name="name">轴网名称</param>
        /// <param name="columnSpacing">列间距</param>
        /// <param name="rowSpacing">行间距</param>
        public GridInfo(string name, double[] columnSpacing, double[] rowSpacing)
        {
            Name = name;
            ColumnSpacing = columnSpacing;
            RowSpacing = rowSpacing;
        }

        /// <summary>
        /// 根据轴数量生成轴网
        /// </summary>
        /// <param name="name">轴网名称</param>
        /// <param name="columnCount">列数量</param>
        /// <param name="rowCount">行数量</param>
        public GridInfo(string name, int columnCount, int rowCount)
        {
            Name = name;
            ColumnSpacing = new double[columnCount - 1];
            RowSpacing = new double[rowCount - 1];
        }
    }

    /// <summary>
    /// 墙
    /// </summary>
    public class WallInfo
    {
        public Line CurveLine { get; set; }
        public double Width { get; set; }
        public Line Edge1
        {
            get
            {
                return CurveLine.GetOffsetCurves(Width / 2)[0] as Line;
            }
        }
        public Line Edge2
        {
            get
            {
                return CurveLine.GetOffsetCurves(-Width / 2)[0] as Line;
            }
        }
        public Region Region
        {
            get
            {
                return RegionInfo.CreateRegionFromPoints(new List<Point3d> { Edge1.StartPoint, Edge1.EndPoint, Edge2.EndPoint, Edge2.StartPoint });
            }
        }

        public WallInfo() { }
        public WallInfo(Line curveLine, double width)
        {
            CurveLine = curveLine;
            Width = width;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(WallInfo))
            {
                WallInfo wallInfo2 = obj as WallInfo;
                if (CurveLine.StartPoint.IsAlmostEqualTo(wallInfo2.CurveLine.StartPoint) &&
                    CurveLine.EndPoint.IsAlmostEqualTo(wallInfo2.CurveLine.EndPoint) &&
                    Width.IsAlmostEqualTo(wallInfo2.Width))
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public override int GetHashCode() { return 0; }
    }

    /// <summary>
    /// 矩形
    /// </summary>
    public class Rectangle
    {
        public Point3d Center { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }
        public Polyline Polyline { get; set; }

        public Rectangle(Point3d center, double length, double width)
        {
            Center = center;
            Length = length;
            Width = width;
            Polyline = new Polyline();
            Polyline.AddVertexAt(0, new Point2d(center.X - length / 2, center.Y - width / 2), 0, 0, 0);
            Polyline.AddVertexAt(1, new Point2d(center.X + length / 2, center.Y - width / 2), 0, 0, 0);
            Polyline.AddVertexAt(2, new Point2d(center.X + length / 2, center.Y + width / 2), 0, 0, 0);
            Polyline.AddVertexAt(3, new Point2d(center.X - length / 2, center.Y + width / 2), 0, 0, 0);
            Polyline.Closed = true;
        }

        public Rectangle(Point3d p1, Point3d p2)
        {
            Center = p1.GetCenterPoint(p2);
            Length = Math.Abs(p1.X-p2.X);
            Width = Math.Abs(p1.Y - p2.Y);
            Polyline = new Polyline();
            Polyline.AddVertexAt(0, new Point2d(p1.X, p1.Y), 0, 0, 0);
            Polyline.AddVertexAt(1, new Point2d(p2.X, p1.Y), 0, 0, 0);
            Polyline.AddVertexAt(2, new Point2d(p2.X, p2.Y), 0, 0, 0);
            Polyline.AddVertexAt(3, new Point2d(p1.X, p2.Y), 0, 0, 0);
            Polyline.Closed = true;
        }
    }

    /// <summary>
    /// 组合图元属性
    /// </summary>
    public class ComInfo
    {
        public string Name { get; set; }
        public double[] Center { get; set; }
        public double Length { get; set; }    // 实体包围盒长度-X方向
        public double Width { get; set; }     // 实体包围盒宽度-Y方向
        public List<LineInfo> LineInfoList { get; set; }
        public List<CircleInfo> CircleInfoList { get; set; }
        public List<TextInfo> TextInfoList { get; set; }
        private const double eps = 0.1;

        public struct LineInfo
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Length { get; set; }
            public double SinAngle { get; set; }
            public string LayerName { get; set; }

            public LineInfo(Line line, Point3d point)
            {
                Length = Math.Round(line.Length, 3);
                LayerName = line.Layer;
                if (line.Angle < Math.PI / 2 || line.Angle.IsAlmostEqualTo(Math.PI / 2) || line.Angle > 3 * Math.PI / 2 && !line.Angle.IsAlmostEqualTo(3 * Math.PI / 2))
                {
                    SinAngle = Math.Round(Math.Sin(line.Angle), 3);
                    X = Math.Round(line.StartPoint.X - point.X, 3);
                    Y = Math.Round(line.StartPoint.Y - point.Y, 3);
                }
                else
                {
                    SinAngle = Math.Round(Math.Sin(new Line(line.EndPoint, line.StartPoint).Angle), 3);
                    X = Math.Round(line.EndPoint.X - point.X, 3);
                    Y = Math.Round(line.EndPoint.Y - point.Y, 3);
                }
            }
            public LineInfo(double x, double y, double length, double sinAngle, string layerName)
            {
                X = x;
                Y = y;
                Length = length;
                SinAngle = sinAngle;
                LayerName = layerName;

            }
            public bool IsSimilar(LineInfo lineInfo2, double epsX, double epsY, double epsAngle)
            {
                if (Math.Abs(X - lineInfo2.X) < epsX &&
                    Math.Abs(Y - lineInfo2.Y) < epsY &&
                    Math.Abs(Length - lineInfo2.Length) < Math.Sqrt(epsX * epsX + epsY * epsY) &&
                    Math.Abs(Math.Asin(SinAngle) - Math.Asin(lineInfo2.SinAngle)) < epsAngle)
                    return true;
                else
                    return false;
            }
        }
        public struct CircleInfo
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Radius { get; set; }
            public string LayerName { get; set; }

            public CircleInfo(Circle circle, Point3d point)
            {
                Radius = Math.Round(circle.Radius, 3);
                X = Math.Round(circle.Center.X - point.X, 3);
                Y = Math.Round(circle.Center.Y - point.Y, 3);
                LayerName = circle.Layer;
            }
            public CircleInfo(double x, double y, double radius, string layerName)
            {
                X = x;
                Y = y;
                Radius = radius;
                LayerName = layerName;
            }
            public bool IsSimilar(CircleInfo circleInfo2, double epsX, double epsY)
            {
                if (Math.Abs(X - circleInfo2.X) < epsX &&
                    Math.Abs(Y - circleInfo2.Y) < epsY &&
                    Math.Abs(Radius - circleInfo2.Radius) < epsX)
                    return true;
                else
                    return false;
            }
        }
        public struct TextInfo
        {
            public double X { get; set; }
            public double Y { get; set; }
            public string TextString { get; set; }
            public string LayerName { get; set; }
            public TextInfo(DBText text, Point3d point)
            {
                TextString = text.TextString;
                X = Math.Round(text.Position.X - point.X, 3);
                Y = Math.Round(text.Position.Y - point.Y, 3);
                LayerName = text.Layer;
            }
            public TextInfo(double x, double y, string textString, string layerName)
            {
                X = x;
                Y = y;
                TextString = textString;
                LayerName = layerName;
            }
            public bool IsSimilar(TextInfo textInfo2, double epsX, double epsY)
            {
                if (Math.Abs(X - textInfo2.X) < epsX &&
                    Math.Abs(Y - textInfo2.Y) < epsY)
                    return true;
                else
                    return false;
            }
        }

        public ComInfo(SelectionSet ss, string name)
        {
            Point3d pointMin = ss.GetGeometricExtents()[0];  // 实体包围盒左下角点
            Point3d pointMax = ss.GetGeometricExtents()[1];  // 实体包围盒右上角点
            Point3d pointCenter = pointMin.GetCenterPoint(pointMax);
            Center = new double[] { Math.Round(pointCenter.X, 3), Math.Round(pointCenter.Y, 3) };
            Length = Math.Round(pointMax.X - pointMin.X, 3);
            Width = Math.Round(pointMax.Y - pointMin.Y, 3);

            List<Line> LineList = ss.SelectType<Line>();
            List<Circle> CircleList = ss.SelectType<Circle>();
            List<DBText> TextList = ss.SelectType<DBText>();
            List<Polyline> PolylineList = ss.SelectType<Polyline>();
            List<MText> MTextList = ss.SelectType<MText>();
            List<BlockReference> brList = ss.SelectType<BlockReference>();
            foreach (BlockReference br in brList)
            {
                List<Entity> blockEntList = br.GetAllEntities();
                LineList.AddRange(blockEntList.Where(s => s.GetType() == typeof(Line)).Select(s => (Line)s));
                CircleList.AddRange(blockEntList.Where(s => s.GetType() == typeof(Circle)).Select(s => (Circle)s));
                TextList.AddRange(blockEntList.Where(s=>s.GetType()==typeof(DBText)).Select(s=>(DBText)s));
                PolylineList.AddRange(blockEntList.Where(s => s.GetType() == typeof(Polyline)).Select(s => (Polyline)s));
                MTextList.AddRange(blockEntList.Where(s => s.GetType() == typeof(MText)).Select(s => (MText)s));
            }
            foreach (Polyline pline in PolylineList)
                LineList.AddRange(pline.ConvertToLines());
            foreach (MText mtext in MTextList)
                TextList.AddRange(mtext.ConvertToTexts());

            Name = name;
            LineInfoList = LineList.Select(s => new LineInfo(s, pointCenter)).ToList();
            CircleInfoList = CircleList.Select(s => new CircleInfo(s, pointCenter)).ToList();
            TextInfoList = TextList.Select(s => new TextInfo(s, pointCenter)).ToList();
        }

        public ComInfo(string name, double[] center, double length, double width,
            List<LineInfo> lineInfoList, List<CircleInfo> circleInfoList, List<TextInfo> textInfoList)
        {
            Name = name;
            Center = center;
            Length = length;
            Width = width;
            LineInfoList = lineInfoList;
            CircleInfoList = circleInfoList;
            TextInfoList = textInfoList;
        }

        public ComInfo()
        {
            Name = String.Empty;
            Center = new double[3];
            Length = 0;
            Width = 0;
            LineInfoList = new List<LineInfo>();
            CircleInfoList = new List<CircleInfo>();
            TextInfoList = new List<TextInfo>();
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(ComInfo))
            {
                ComInfo comInfo2 = obj as ComInfo;
                if (comInfo2.Name == Name && comInfo2.Center == Center && comInfo2.Length == Length && comInfo2.Width == Width &&
                    comInfo2.LineInfoList == LineInfoList && comInfo2.CircleInfoList == CircleInfoList && comInfo2.TextInfoList == TextInfoList)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public override int GetHashCode() { return 0; }

        public bool IsSimilar(ComInfo baseInfo, double eps = 0.1)
        {
            List<LineInfo> baseLineInfoList = baseInfo.LineInfoList;
            List<CircleInfo> baseCircleInfoList = baseInfo.CircleInfoList;
            List<TextInfo> baseTextInfoList = baseInfo.TextInfoList;

            List<LineInfo> thisLineInfoList = this.LineInfoList;
            List<CircleInfo> thisCircleInfoList = this.CircleInfoList;
            List<TextInfo> thisTextInfoList = this.TextInfoList;

            double epsX = eps * baseInfo.Length;
            double epsY = eps * baseInfo.Width;
            double epsAngle = 1;

            if (thisLineInfoList.Count >= baseLineInfoList.Count && thisCircleInfoList.Count >= baseCircleInfoList.Count && thisTextInfoList.Count >= baseTextInfoList.Count)
            {
                foreach (LineInfo lineInfo in baseLineInfoList)
                {
                    List<LineInfo> matchLineInfoList = thisLineInfoList.FindAll(s => lineInfo.IsSimilar(s, epsX, epsY, epsAngle));
                    if (matchLineInfoList.Count == 0) return false;
                }
                foreach (CircleInfo circleInfo in baseCircleInfoList)
                {
                    List<CircleInfo> matchCircleInfoList = thisCircleInfoList.FindAll(s => circleInfo.IsSimilar(s, epsX, epsY));
                    if (matchCircleInfoList.Count == 0) return false;
                }
                foreach (TextInfo textInfo in baseTextInfoList)
                {
                    List<TextInfo> matchTextInfoList = thisTextInfoList.FindAll(s => textInfo.IsSimilar(s, epsX, epsY));
                    if (matchTextInfoList.Count == 0) return false;
                }
                return true;
            }
            else return false;
        }

        public List<ComInfo> FindSimilar(Document doc, SelectionSet ss)
        {
            double boxLength = Length * 1.2;
            double boxWidth = Width * 1.2;
            List<ComInfo> resultInfoList = new List<ComInfo>();
            Point3d[] pointsCurrentWindow = doc.GetCurrentWindow();
            if (TextInfoList.Count() > 0)
            {
                TextInfo text0 = TextInfoList[0];
                List<DBText> textList = ss.SelectType<DBText>();
                foreach (DBText text in textList)
                {
                    Point3d center = new Point3d(text.Position.X - text0.X, text.Position.Y - text0.Y, 0);
                    Point3d pointMin = new Point3d(center.X - boxLength / 2, center.Y - boxWidth / 2, 0);
                    Point3d pointMax = new Point3d(center.X + boxLength / 2, center.Y + boxWidth / 2, 0);
                    doc.ZoomWindow(pointMin, pointMax);
                    SelectionSet ssTemp = doc.GetWindowSelectionSet(pointMin, pointMax);
                    if (ssTemp != null)
                    {
                        ComInfo infoTemp = new ComInfo(ssTemp, "Temp");
                        if (this.IsSimilar(infoTemp))
                        {
                            resultInfoList.Add(infoTemp);
                        }
                    }
                }
            }
            doc.ZoomWindow(pointsCurrentWindow[0], pointsCurrentWindow[1]);
            return resultInfoList;
        }

        public void Draw(double textHeight = 350)
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            foreach (LineInfo lineInfo in LineInfoList)
            {
                double CosAngle = Math.Sqrt(1 - lineInfo.SinAngle * lineInfo.SinAngle);
                Point3d p1 = new Point3d(lineInfo.X + Center[0], lineInfo.Y + Center[1], 0);
                Point3d p2 = new Point3d(lineInfo.X + Center[0] + lineInfo.Length * CosAngle, lineInfo.Y + Center[1] + lineInfo.Length * lineInfo.SinAngle, 0);
                db.AddEntity(new Line(p1, p2));
            }
            foreach (CircleInfo circleInfo in CircleInfoList)
            {
                Point3d p1 = new Point3d(circleInfo.X + Center[0], circleInfo.Y + Center[1], 0);
                db.AddEntity(new Circle(p1, new Vector3d(0, 0, 1), circleInfo.Radius));
            }
            foreach (TextInfo textInfo in TextInfoList)
            {
                DBText text = new DBText();
                text.Position = new Point3d(textInfo.X + Center[0], textInfo.Y + Center[1], 0);
                text.Height = textHeight;
                text.TextString = textInfo.TextString;
                db.AddEntity(text);
            }
        }
    }

    /// <summary>
    /// 直线及多段线
    /// </summary>
    public static class LineInfo
    {

        /// <summary>
        /// 获取直线方向，范围为 (-PI/2 - PI/2]
        /// </summary>
        public static double GetOrientation(this Line line)
        {
            double result = Math.Asin(Math.Sin(line.Angle));
            if (result.IsAlmostEqualTo(-Math.PI / 2)) result = Math.PI / 2;
            return result;
        }

        /// <summary>
        /// 获取直线方向向量
        /// </summary>
        public static Vector3d GetVector(this Line line)
        {
            if (line.Angle < Math.PI / 2 || line.Angle.IsAlmostEqualTo(Math.PI / 2) || line.Angle > 3 * Math.PI / 2 && !line.Angle.IsAlmostEqualTo(3 * Math.PI / 2))
                return (line.EndPoint - line.StartPoint).GetNormal();
            else
                return (line.StartPoint - line.EndPoint).GetNormal();
        }

        /// <summary>
        /// 获取直线端点
        /// </summary>
        public static Point3d[] GetPoints(this Line line)
        {
            Point3d[] points = new Point3d[] { line.StartPoint, line.EndPoint };
            return points;
        }

        /// <summary>
        /// 获取直线中点
        /// </summary>
        public static Point3d MiddlePoint(this Line line)
        {
            Point3d point = new Point3d((line.StartPoint.X + line.EndPoint.X) / 2,
                (line.StartPoint.Y + line.EndPoint.Y) / 2, (line.StartPoint.Z + line.EndPoint.Z) / 2);
            return point;
        }

        /// <summary>
        /// 获取两条直线的交点
        /// </summary>
        public static Point3dCollection IntersectWith(this Line line1, Line line2, Intersect intersectMode = Intersect.ExtendBoth)
        {
            Point3dCollection points = new Point3dCollection();
            line1.IntersectWith(line2, intersectMode, new Plane(), points, IntPtr.Zero, IntPtr.Zero);
            return points;
        }

        /// <summary>
        /// 获取直线和多段线的交点
        /// </summary>
        public static Point3dCollection IntersectWith(this Line line1, Polyline line2, Intersect intersectMode = Intersect.ExtendBoth)
        {
            Point3dCollection points = new Point3dCollection();
            line1.IntersectWith(line2, intersectMode, new Plane(), points, IntPtr.Zero, IntPtr.Zero);
            return points;
        }

        /// <summary>
        /// 判断两条直线是否完全重合
        /// </summary>
        public static bool EqualTo(this Line line, Line line2)
        {
            Point3d p1 = line.StartPoint;
            Point3d p2 = line.EndPoint;
            Point3d p3 = line2.StartPoint;
            Point3d p4 = line2.EndPoint;
            if (p1.IsAlmostEqualTo(p3) && p2.IsAlmostEqualTo(p4)) return true;
            else if (p1.IsAlmostEqualTo(p4) && p2.IsAlmostEqualTo(p3)) return true;
            else return false;
        }

        /// <summary>
        /// 判断两条直线是否平行，包括共线及重合的情况
        /// </summary>
        public static bool IsParallelTo(this Line line1, Line line2)
        {
            Point3dCollection points = line1.IntersectWith(line2);
            if (points != null && points.Count == 0) return true;
            else return false;
        }

        /// <summary>
        /// 判断两条直线是否共线
        /// </summary>
        public static bool IsInSameLine(this Line line1, Line line2)
        {
            double dis1 = line1.StartPoint.GetDistance2d(line2);
            double dis2 = line1.EndPoint.GetDistance2d(line2);
            if (dis1.IsAlmostEqualTo(0) && dis2.IsAlmostEqualTo(0)) return true;
            else return false;
        }

        /// <summary>
        /// 返回直线集中与目标直线平行但不共线的直线
        /// </summary>
        public static List<Line> GetParallelLines(this Line targetLine, List<Line> lineList)
        {
            List<Line> paraLines = new List<Line>();
            foreach (Line line in lineList)
            {
                if (!line.IsInSameLine(targetLine) && line.IsParallelTo(targetLine)) paraLines.Add(line);
            }
            return paraLines;
        }

        /// <summary>
        /// 返回与目标直线平行的直线间的距离
        /// </summary>
        public static double ParallelDistance(this Line line, Line targetLine)
        {
            double dis = 0;
            if (!line.IsInSameLine(targetLine) && line.IsParallelTo(targetLine))
                dis = line.StartPoint.GetDistance2d(targetLine);
            return dis;
        }

        /// <summary>
        /// 返回直线在指定点方向上的偏移直线
        /// </summary>
        public static Line GetOffsetLine(this Line line, double distance, Point3d p)
        {
            if (distance.IsAlmostEqualTo(0) || p.IsInLine(line)) return new Line();

            Line offsetLine = line.GetOffsetCurves(distance)[0] as Line;
            if (offsetLine.GetClosestPointTo(p, true).GetDistance2d(p) < line.GetClosestPointTo(p, true).GetDistance2d(p))
                return offsetLine;
            else if (offsetLine.GetClosestPointTo(p, true).GetDistance2d(p) > line.GetClosestPointTo(p, true).GetDistance2d(p))
                return line.GetOffsetCurves(-distance)[0] as Line;
            else
                return new Line();
        }

        /// <summary>
        /// 返回直线在指定点反方向上的偏移直线
        /// </summary>
        public static Line GetOppositeOffsetLine(this Line line, double distance, Point3d p)
        {
            if (distance.IsAlmostEqualTo(0) || p.IsInLine(line)) return new Line();

            Line offsetLine = line.GetOffsetCurves(distance)[0] as Line;
            if (offsetLine.GetClosestPointTo(p, true).GetDistance2d(p) > line.GetClosestPointTo(p, true).GetDistance2d(p))
                return offsetLine;
            else if (offsetLine.GetClosestPointTo(p, true).GetDistance2d(p) < line.GetClosestPointTo(p, true).GetDistance2d(p))
                return line.GetOffsetCurves(-distance)[0] as Line;
            else
                return new Line();
        }

        /// <summary>
        /// 将两条直线延伸至交点，如果平行或已有交点则不处理，返回交点至两条直线端点的距离
        /// </summary>
        public static double[] Extend(ref Line line1, ref Line line2)
        {
            double[] distance = new double[] { 0, 0 };
            if (line1.IsParallelTo(line2))
                return distance;
            else if (line1.IntersectWith(line2, Intersect.OnBothOperands).Count != 0)
                return distance;
            else
            {
                var points = line1.IntersectWith(line2, Intersect.ExtendBoth);
                if (points.Count > 0)
                {
                    Point3d p = points[0];
                    if (p.IsInLine(line1))
                    {
                        if (p.DistanceTo(line2.StartPoint) <= p.DistanceTo(line2.EndPoint)) { distance[1] = p.DistanceTo(line2.StartPoint); line2.StartPoint = p; }
                        else { distance[1] = p.DistanceTo(line2.EndPoint); line2.EndPoint = p; }
                    }
                    else if (p.IsInLine(line2))
                    {
                        if (p.DistanceTo(line1.StartPoint) <= p.DistanceTo(line1.EndPoint)) { distance[0] = p.DistanceTo(line1.StartPoint); line1.StartPoint = p; }
                        else { distance[0] = p.DistanceTo(line1.EndPoint); line1.EndPoint = p; }
                    }
                    else
                    {
                        if (p.DistanceTo(line1.StartPoint) <= p.DistanceTo(line1.EndPoint)) { distance[0] = p.DistanceTo(line1.StartPoint); line1.StartPoint = p; }
                        else { distance[0] = p.DistanceTo(line1.EndPoint); line1.EndPoint = p; }
                        if (p.DistanceTo(line2.StartPoint) <= p.DistanceTo(line2.EndPoint)) { distance[1] = p.DistanceTo(line2.StartPoint); line2.StartPoint = p; }
                        else { distance[1] = p.DistanceTo(line2.EndPoint); line2.EndPoint = p; }
                    }
                }
                return distance;
            }
        }

        /// <summary>
        /// 将两条共线直线合并
        /// </summary>
        public static Line Join(Line line1, Line line2)
        {
            Line newline = new Line();
            if (line1.IsInSameLine(line2))
            {
                Point3d[] points = new Point3d[] { line1.StartPoint, line1.EndPoint, line2.StartPoint, line2.EndPoint };
                points = points.OrderBy(s => s.X).ThenBy(s => s.Y).ToArray();
                newline = new Line(points.First(), points.Last());
            }
            return newline;
        }

        /// <summary>
        /// 将两条共线并重叠的直线合并成一条
        /// </summary>
        public static Line Overkill(this Line line1, Line line2)
        {
            Line newline = new Line();
            if (line1.IsInSameLine(line2) && (line1.StartPoint.IsInLine(line2) || line1.EndPoint.IsInLine(line2)))
            {
                Point3d[] points = new Point3d[] { line1.StartPoint, line1.EndPoint, line2.StartPoint, line2.EndPoint };
                points = points.OrderBy(s => s.X).ThenBy(s => s.Y).ToArray();
                newline = new Line(points.First(), points.Last());
            }
            return newline;
        }

        /// <summary>
        /// 返回两条共线直线距离最近或重叠部分的端点
        /// </summary>
        public static Point3d[] GetOverlapPoints(Line line1, Line line2)
        {
            Point3d[] result = new Point3d[2];
            if (line1.IsInSameLine(line2))
            {
                Point3d[] points = new Point3d[] { line1.StartPoint, line1.EndPoint, line2.StartPoint, line2.EndPoint };
                points = points.OrderBy(s => s.X).ThenBy(s => s.Y).ToArray();
                result[0] = points[1];
                result[1] = points[2];
            }
            return result;
        }

        /// <summary>
        /// 将多段线转换为直线
        /// </summary>
        public static List<Line> ConvertToLines(this Polyline pline)
        {
            List<Line> lines = new List<Line> { };
            if (pline != null)
            {
                int count = pline.NumberOfVertices;
                if (pline.Closed)
                {
                    for (int i = 0; i < count; i++)
                    {
                        Line newLine = new Line(pline.GetPoint3dAt(i), pline.GetPoint3dAt((i + 1) % count));
                        newLine.LayerId = pline.LayerId;
                        lines.Add(newLine);
                    }
                }
                else
                {
                    for (int i = 0; i < count - 1; i++)
                    {
                        Line newLine = new Line(pline.GetPoint3dAt(i), pline.GetPoint3dAt(i + 1));
                        newLine.LayerId = pline.LayerId;
                        lines.Add(newLine);
                    }
                }
            }
            return lines;
        }

        /// <summary>
        /// 求多条顺序连接的直线的交点
        /// </summary>
        public static List<Point3d> GetLineInters(this Line[] lineArray)
        {
            List<Point3d> interList = new List<Point3d> { };
            List<Line> toCheckList = lineArray.ToList();
            Line line = lineArray[0];
            while (toCheckList.Count() > 0)
            {
                foreach (Line subline in toCheckList)
                {
                    Point3dCollection points = line.IntersectWith(subline, Intersect.OnBothOperands);
                    if (points.Count > 0)
                    {
                        if (!interList.Contains(points[0]))
                        {
                            interList.Add(points[0]);
                            line = subline;
                            toCheckList.Remove(line);
                            break;
                        }
                    }
                }
            }
            return interList;
        }

        /// <summary>
        /// 通过点列生成多段线
        /// </summary>
        public static Polyline ToPline(this Point3d[] pointArray)
        {
            Polyline pline = new Polyline();
            for (int i = 0; i < pointArray.Count(); i++)
            {
                pline.AddVertexAt(i, new Point2d(pointArray[i].X, pointArray[i].Y), 0, 0, 0);
            }
            pline.Closed = true;
            return pline;
        }

        /// <summary>
        /// 利用平行线组形成短墙信息
        /// </summary>
        public static List<WallInfo> GetWallElements(List<Line> lineList, Region reg0)
        {
            List<WallInfo> wallInfoList = new List<WallInfo>();
            for (int i = 0; i < lineList.Count; i++)
            {
                Line line = lineList[i];
                List<Line> parallelLines = line.GetParallelLines(lineList);
                foreach (Line subline in parallelLines.OrderBy(s => s.ParallelDistance(line)))
                {
                    Line middleLine = new Line();
                    double width = 0;
                    Region subreg1 = line.GetRegionBetweenLines(subline, out middleLine, out width);
                    if (subreg1.Area > 0 && subreg1.IsInRegion(reg0))
                    {
                        WallInfo wallInfo = new WallInfo(middleLine, width);
                        if (!wallInfoList.Contains(wallInfo)) { wallInfoList.Add(wallInfo); break; }
                    }
                }
            }
            return wallInfoList;
        }

        // 判断墙体是否无可连接处
        public static bool IsAllJoined(this List<WallInfo> wallInfoList, double maxWidth)
        {
            for (int i = 0; i < wallInfoList.Count; i++)
            {
                WallInfo wallInfo = wallInfoList[i];
                for (int j = i + 1; j < wallInfoList.Count; j++)
                {
                    WallInfo subwallInfo = wallInfoList[j];
                    if (!wallInfo.CurveLine.IsParallelTo(subwallInfo.CurveLine))
                    {
                        var points = wallInfo.CurveLine.IntersectWith(subwallInfo.CurveLine, Intersect.OnBothOperands);
                        var pointsExtend = wallInfo.CurveLine.IntersectWith(subwallInfo.CurveLine, Intersect.ExtendBoth);
                        if (points.Count == 0 && pointsExtend.Count > 0)
                        {
                            Point3d interpoint = pointsExtend[0];
                            double dis1 = ToolClass.Min(interpoint.DistanceTo(wallInfo.CurveLine.StartPoint), interpoint.DistanceTo(wallInfo.CurveLine.EndPoint));
                            double dis2 = ToolClass.Min(interpoint.DistanceTo(subwallInfo.CurveLine.StartPoint), interpoint.DistanceTo(subwallInfo.CurveLine.EndPoint));
                            if ((dis1 > 0 && dis1 < maxWidth) || (dis2 > 0 && dis2 < maxWidth)) return false;
                        }
                    }
                }
            }
            return true;
        }

        // 在指定范围内连接墙体可连接处
        public static void JoinWalls(ref List<WallInfo> wallInfoList, double maxWidth, Region reg0)
        {
            for (int i = 0; i < wallInfoList.Count; i++)
            {
                WallInfo wallInfo = wallInfoList[i];
                double mindis = maxWidth;
                WallInfo nearestWallInfo = new WallInfo();
                for (int j = 0; j < wallInfoList.Count; j++)
                {
                    WallInfo subwallInfo = wallInfoList[j];
                    if (!wallInfo.CurveLine.IsParallelTo(subwallInfo.CurveLine))
                    {
                        var points = wallInfo.CurveLine.IntersectWith(subwallInfo.CurveLine, Intersect.OnBothOperands);
                        var pointsExtend = wallInfo.CurveLine.IntersectWith(subwallInfo.CurveLine, Intersect.ExtendBoth);
                        if (points.Count == 0 && pointsExtend.Count > 0)
                        {
                            Point3d interpoint = pointsExtend[0];
                            Line line1 = new Line(wallInfo.CurveLine.StartPoint, wallInfo.CurveLine.EndPoint);
                            Line line2 = new Line(subwallInfo.CurveLine.StartPoint, subwallInfo.CurveLine.EndPoint);
                            double[] dis = Extend(ref line1, ref line2);
                            if (dis[0] > 0 && dis[0] < mindis && dis[1] < maxWidth)
                            {
                                mindis = dis[0];
                                nearestWallInfo = subwallInfo;
                            }
                        }
                    }
                }
                if (mindis > 0 && mindis < maxWidth && nearestWallInfo != null)
                {
                    Line line1 = wallInfo.CurveLine.GetTransformedCopy(Matrix3d.Displacement(new Vector3d(0, 0, 0))) as Line;
                    Line line2 = nearestWallInfo.CurveLine.GetTransformedCopy(Matrix3d.Displacement(new Vector3d(0, 0, 0))) as Line;
                    Extend(ref line1, ref line2);
                    Region reg1 = new WallInfo(line1, wallInfo.Width).Region;
                    Region reg2 = new WallInfo(line2, wallInfo.Width).Region;
                    if (reg1.IsInRegion(reg0) && reg2.IsInRegion(reg0))
                    {
                        wallInfo.CurveLine = line1;
                    }
                }
            }
            return;
        }
    }

    /// <summary>
    /// 文字
    /// </summary>
    public static class TextInfo
    {
        public static DBText CreateText(this Database db, string content, Point3d position,
            double height = 3.5, double rotation = 0, double widthfactor = 0.7, string textstylename = "SMEDI",
            TextHorizontalMode thmode = TextHorizontalMode.TextCenter, TextVerticalMode tvmode = TextVerticalMode.TextVerticalMid)
        {
            DBText text = new DBText();
            text.TextString = content;
            text.Position = position;
            text.Height = height;
            text.Rotation = rotation;
            text.WidthFactor = widthfactor;
            text.TextStyleId = db.GetTextStyleId(textstylename);
            text.HorizontalMode = thmode;
            text.VerticalMode = tvmode;
            text.AlignmentPoint = text.Position;
            return text;
        }

        // 多行文字转为单行文字
        public static List<DBText> ConvertToTexts(this MText mtext)
        {
            DBObjectCollection objCol = new DBObjectCollection();
            List<DBText> textList = new List<DBText>();
            mtext.Explode(objCol);
            foreach (var obj in objCol)
            {
                if (obj.GetType() == typeof(DBText)) textList.Add((DBText)obj);
            }
            return textList;
        }
    }

    /// <summary>
    /// 面域
    /// </summary>
    public static class RegionInfo
    {
        /// <summary>
        /// 判断reg1是否在reg2内部
        /// </summary>
        public static bool IsInRegion(this Region reg1, Region reg2)
        {
            Region reg1Unite = reg1.GetTransformedCopy(Matrix3d.Displacement(new Vector3d(0, 0, 0))) as Region;
            Region reg2Unite = reg2.GetTransformedCopy(Matrix3d.Displacement(new Vector3d(0, 0, 0))) as Region;
            reg1Unite.BooleanOperation(BooleanOperationType.BoolUnite, reg2Unite);
            if (reg1Unite.Area.IsAlmostEqualTo(reg2.Area)) return true;
            else return false;
        }

        /// <summary>
        /// 判断两个面域是否相等
        /// </summary>
        public static bool IsSameRegion(this Region reg1, Region reg2)
        {
            Region reg1Unite = reg1.GetTransformedCopy(Matrix3d.Displacement(new Vector3d(0, 0, 0))) as Region;
            Region reg1Inter = reg1.GetTransformedCopy(Matrix3d.Displacement(new Vector3d(0, 0, 0))) as Region;
            Region reg2Unite = reg2.GetTransformedCopy(Matrix3d.Displacement(new Vector3d(0, 0, 0))) as Region;
            Region reg2Inter = reg2.GetTransformedCopy(Matrix3d.Displacement(new Vector3d(0, 0, 0))) as Region;
            reg1Unite.BooleanOperation(BooleanOperationType.BoolUnite, reg2Unite);
            reg1Inter.BooleanOperation(BooleanOperationType.BoolIntersect, reg2Inter);
            if (reg1Unite.Area.IsAlmostEqualTo(reg1.Area) && reg1Inter.Area.IsAlmostEqualTo(reg1.Area)) return true;
            else return false;
        }

        /// <summary>
        /// 将点顺序连接形成面域
        /// </summary>
        public static Region CreateRegionFromPoints(List<Point3d> points)
        {
            Region region = new Region();
            List<Line> lineList = new List<Line>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                if (!points[i].IsAlmostEqualTo(points[i + 1]))
                {
                    lineList.Add(new Line(points[i], points[i + 1]));
                }
            }
            lineList.Add(new Line(points[points.Count - 1], points[0]));

            if (lineList.Count > 2)
            {
                List<double> oriList = lineList.Select(s => s.GetOrientation()).ToList();
                if (!oriList.AllEquals())
                {
                    DBObjectCollection objCol = new DBObjectCollection();
                    foreach (Line line in lineList) objCol.Add(line);
                    try
                    {
                        DBObjectCollection regCol = Region.CreateFromCurves(objCol);
                        if (regCol?.Count > 0) return regCol[0] as Region;
                    }
                    catch { }
                }
            }
            return region;
        }

        /// <summary>
        /// 返回两条平行直线间围成的面域
        /// </summary>
        public static Region GetRegionBetweenLines(this Line line01, Line line02, out Line middleLine, out double width)
        {
            Region region = new Region();
            middleLine = new Line();
            width = 0;

            Line[] lineArray = new Line[] { line01, line02 };
            for (int i = 0; i < 2; i++)
            {
                Line line1 = lineArray[i];
                Line line2 = lineArray[1 - i];
                for (int j = 0; j < 2; j++)
                {
                    Point3d p1Start = line1.GetPoints()[j];
                    Point3d p1End = line1.GetPoints()[1 - j];
                    for (int k = 0; k < 2; k++)
                    {
                        Point3d p2Start = line2.GetPoints()[k];
                        Point3d p2End = line2.GetPoints()[1 - k];

                        Point3d p1StartRef = line2.GetClosestPointTo(p1Start, true);
                        Point3d p1EndRef = line2.GetClosestPointTo(p1End, true);
                        Point3d p2StartRef = line1.GetClosestPointTo(p2Start, true);
                        Point3d p2EndRef = line1.GetClosestPointTo(p2End, true);

                        if (p1StartRef.IsInLine(line2) && p1EndRef.IsInLine(line2))
                        {
                            middleLine = new Line(p1Start.GetCenterPoint(p1StartRef), p1End.GetCenterPoint(p1EndRef));
                            width = p1Start.GetDistance2d(line2);
                            return CreateRegionFromPoints(new List<Point3d> { p1Start, p1End, p1EndRef, p1StartRef });
                        }
                        else if (p1StartRef.IsInLine(line2) && p2StartRef.IsInLine(line1) && !p1StartRef.IsAlmostEqualTo(p2Start))
                        {
                            middleLine = new Line(p1Start.GetCenterPoint(p1StartRef), p2Start.GetCenterPoint(p2StartRef));
                            width = p1Start.GetDistance2d(line2);
                            return CreateRegionFromPoints(new List<Point3d> { p1Start, p1StartRef, p2Start, p2StartRef });
                        }
                    }
                }
            }
            return region;
        }
    }

    /// <summary>
    /// 样式
    /// </summary>
    public static class StyleInfo
    {
        /// <summary>
        /// 新建文字样式
        /// </summary>
        public static ObjectId AddTextStyle(this Database db, string TextStyleName, string FontFilename, string BigFontFilename, double WidthFactor)
        {
            ObjectId tsId = ObjectId.Null;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                TextStyleTable tst = (TextStyleTable)trans.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                if (!tst.Has(TextStyleName))
                {
                    TextStyleTableRecord tsr = new TextStyleTableRecord();
                    tsr.Name = TextStyleName;
                    tsr.FileName = FontFilename;
                    tsr.BigFontFileName = BigFontFilename;
                    tsr.XScale = WidthFactor;
                    tst.UpgradeOpen();
                    tst.Add(tsr);
                    trans.AddNewlyCreatedDBObject(tsr, true);
                    tst.DowngradeOpen();
                    tsId = tst[TextStyleName];
                }
                trans.Commit();
            }
            return tsId;
        }

        /// <summary>
        /// 将指定文字样式设为当前
        /// </summary>
        public static bool SetTextStyleCurrent(this Database db, string TextStyleName,
            string FontFilename = "smsim.shx", string BigFontFilename = "smfs.shx", double WidthFactor = 0.75)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                TextStyleTable tst = (TextStyleTable)trans.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                if (!tst.Has(TextStyleName))
                {
                    try { db.AddTextStyle(TextStyleName, FontFilename, BigFontFilename, WidthFactor); }
                    catch { return false; }
                }
                ObjectId tsId = tst[TextStyleName];
                db.Textstyle = tsId;
                trans.Commit();
            }
            return true;
        }

        /// <summary>
        /// 获取文字样式Id
        /// </summary>
        public static ObjectId GetTextStyleId(this Database db, string TextStyleName)
        {
            ObjectId TextStyleId = ObjectId.Null;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                TextStyleTable tst = (TextStyleTable)trans.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                if (tst.Has(TextStyleName)) TextStyleId = tst[TextStyleName];
                trans.Commit();
            }
            return TextStyleId;
        }

        /// <summary>
        /// 新建图层
        /// </summary>
        public static ObjectId AddLayer(this Database db, string LayerName, short ColorIndex)
        {
            ObjectId layerId = ObjectId.Null;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(LayerName))
                {
                    LayerTableRecord ltr = new LayerTableRecord();
                    ltr.Name = LayerName;
                    ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, ColorIndex);
                    lt.UpgradeOpen();
                    lt.Add(ltr);
                    trans.AddNewlyCreatedDBObject(ltr, true);
                    lt.DowngradeOpen();
                    layerId = lt[LayerName];
                }
                trans.Commit();
            }
            return layerId;
        }

        /// <summary>
        /// 将指定图层设为当前
        /// </summary>
        public static bool SetLayerCurrent(this Database db, string LayerName, short ColorIndex)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(LayerName))
                {
                    try { db.AddLayer(LayerName, ColorIndex); }
                    catch { return false; }
                }
                ObjectId layerId = lt[LayerName];
                db.Clayer = layerId;
                trans.Commit();
            }
            return true;
        }

        /// <summary>
        /// 加载指定线型
        /// </summary>
        public static void LoadLineType(this Database db, string LineTypeName)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LinetypeTable ltt = (LinetypeTable)trans.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                if (ltt.Has(LineTypeName) == false)
                {
                    try { db.LoadLineTypeFile(LineTypeName, "acad.lin"); }
                    catch { }
                }
                trans.Commit();
            }
        }
    }
}