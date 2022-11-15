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

namespace DWG2JSON
{
    public static class DrawClass
    {
        /// <summary>
        /// 将图形对象添加到图形文件中
        /// </summary>
        public static ObjectId AddEntity(this Database db, Entity ent)
        {
            ObjectId entId = ObjectId.Null;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)trans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                entId = btr.AppendEntity(ent);
                trans.AddNewlyCreatedDBObject(ent, true);
                trans.Commit();
            }
            return entId;
        }
    }

    /// <summary>
    /// 剖面钢筋
    /// </summary>
    public class SectionReinDraw
    {
        // 获取当前文档和数据库
        Document doc = Application.DocumentManager.MdiActiveDocument;
        Database db = Application.DocumentManager.MdiActiveDocument.Database;
        Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

        public Circle[] CircleArray { get; set; }
        public Line[] LineArray { get; set; }
        public DBText[] TextArray { get; set; }
        public double Scale { get; set; }

        public SectionReinDraw(double scale, string[] reinValueArray, Line[] lineBase, Point3d pointBase, bool reinPointIn)
        {
            double reinPointRadius = scale * 20;
            double reinPointSpace = scale * 200;
            double reinDistance = scale * 40;
            double lineLength1 = scale * 350;
            double lineLength2 = scale * 800;
            double lineLength3 = scale * 1250;
            double lineLength4 = lineLength3 + 1.5 * reinPointSpace;
            double textHeight = scale * 350;
            double space = scale * 40;
            TextHorizontalMode textH = TextHorizontalMode.TextLeft;
            TextVerticalMode textV = TextVerticalMode.TextBottom;

            CircleArray = new Circle[6];
            LineArray = new Line[12];
            TextArray = new DBText[4];

            // 确定2条直线的上下关系，lineBase0在上
            Line lineBase0 = lineBase[0];
            Line lineBase1 = lineBase[1];
            Line lineBetweenBase = new Line(lineBase1.StartPoint, lineBase0.GetClosestPointTo(lineBase1.StartPoint, true));
            if ((lineBetweenBase.Angle - Math.PI) > 10e-6 || lineBetweenBase.Angle.IsAlmostEqualTo(0))
            {
                lineBase0 = lineBase[1];
                lineBase1 = lineBase[0];
            }

            // 点筋及点筋引线
            Line lineRein0 = new Line();
            Line lineRein1 = new Line();
            Vector3d v0 = new Vector3d();
            Vector3d v1 = new Vector3d();
            if (reinPointIn)
            {
                lineRein0 = lineBase0.GetOffsetLine(reinDistance, lineBase1.StartPoint);
                lineRein1 = lineBase1.GetOffsetLine(reinDistance, lineBase0.StartPoint);
                v0 = (lineBase0.StartPoint - lineRein0.StartPoint).GetNormal();  // 与定位线0垂直的引线方向
                v1 = (lineBase1.StartPoint - lineRein1.StartPoint).GetNormal();  // 与定位线1垂直的引线方向
            }
            else
            {
                lineRein0 = lineBase0.GetOppositeOffsetLine(reinDistance, lineBase1.StartPoint);
                lineRein1 = lineBase1.GetOppositeOffsetLine(reinDistance, lineBase0.StartPoint);
                v0 = (lineRein0.StartPoint - lineBase0.StartPoint).GetNormal();  // 与定位线0垂直的引线方向
                v1 = (lineRein1.StartPoint - lineBase1.StartPoint).GetNormal();  // 与定位线1垂直的引线方向
                lineLength2 = scale * 800 + 2 * reinDistance;
            }
            Point3d p0 = lineRein0.GetClosestPointTo(pointBase, true);
            Point3d p1 = lineRein1.GetClosestPointTo(pointBase, true);

            for (int i = 0; i < 3; i++)
            {
                Point3d center0 = p0 + i * reinPointSpace * lineBase0.GetVector();
                CircleArray[i] = new Circle(center0, new Vector3d(0, 0, 1), reinPointRadius);
                LineArray[i] = new Line(center0, center0 + v0 * lineLength1);
            }
            for (int i = 3; i < 6; i++)
            {
                Point3d center1 = p1 + (i - 3) * reinPointSpace * lineBase1.GetVector();
                CircleArray[i] = new Circle(center1, new Vector3d(0, 0, 1), reinPointRadius);
                LineArray[i] = new Line(center1, center1 + v1 * (lineLength1 + textHeight));
            }

            // 纵筋引线
            Point3d startp0 = lineBase0.GetClosestPointTo(pointBase, true) + reinPointSpace * 1.5 * lineBase0.GetVector();
            LineArray[6] = new Line(startp0, startp0 + v0 * lineLength2);
            Point3d startp1 = lineBase1.GetClosestPointTo(pointBase, true) + reinPointSpace * 1.5 * lineBase1.GetVector();
            LineArray[7] = new Line(startp1, startp1 + v1 * (lineLength2 + textHeight));

            // 文字及文字引线
            if (reinValueArray.Count() == 4)
            {
                LineArray[8] = new Line(LineArray[6].EndPoint, LineArray[6].EndPoint + lineBase0.GetVector() * lineLength3);
                LineArray[9] = new Line(LineArray[7].EndPoint, LineArray[7].EndPoint + lineBase1.GetVector() * lineLength3);
                LineArray[10] = new Line(LineArray[0].EndPoint, LineArray[0].EndPoint + lineBase0.GetVector() * lineLength4);
                LineArray[11] = new Line(LineArray[3].EndPoint, LineArray[3].EndPoint + lineBase1.GetVector() * lineLength4);
                Vector3d vh = (LineArray[8].EndPoint - LineArray[8].StartPoint).GetNormal();
                Vector3d vv = (LineArray[6].EndPoint - LineArray[6].StartPoint).GetNormal();

                TextArray[0] = db.CreateText(reinValueArray[0], LineArray[6].EndPoint + vh * (space + 0.5 * reinPointSpace) + vv * space,
                    textHeight, LineArray[8].Angle, 0.7, "SMEDI", textH, textV);
                TextArray[1] = db.CreateText(reinValueArray[1], LineArray[0].EndPoint + vh * (space + 2 * reinPointSpace) + vv * space,
                    textHeight, LineArray[9].Angle, 0.7, "SMEDI", textH, textV);
                TextArray[2] = db.CreateText(reinValueArray[2], LineArray[3].EndPoint + vh * (space + 2 * reinPointSpace) + vv * space,
                    textHeight, LineArray[10].Angle, 0.7, "SMEDI", textH, textV);
                TextArray[3] = db.CreateText(reinValueArray[3], LineArray[7].EndPoint + vh * (space + 0.5 * reinPointSpace) + vv * space,
                    textHeight, LineArray[11].Angle, 0.7, "SMEDI", textH, textV);
            }
        }

        public void Draw()
        {
            db.SetLayerCurrent("结-钢筋", 1);
            foreach (Circle circle in CircleArray)
            {
                if (circle != null)
                    db.AddEntity(circle);
            }
            db.SetLayerCurrent("结-细线", 7);
            foreach (Line line in LineArray)
            {
                if (line != null)
                    db.AddEntity(line);
            }
            db.SetLayerCurrent("结-文字", 7);
            foreach (DBText text in TextArray)
            {
                if (text != null)
                    db.AddEntity(text);
            }
        }
    }

    /// <summary>
    /// 转角点筋
    /// </summary>
    public class CornerReinDraw
    {
        // 获取当前文档和数据库
        Document doc = Application.DocumentManager.MdiActiveDocument;
        Database db = Application.DocumentManager.MdiActiveDocument.Database;
        Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

        public Circle[] CircleArray { get; set; }
        public Line[] LineArray { get; set; }
        public DBText ReinText { get; set; }
        public double Scale { get; set; }

        public CornerReinDraw(double scale, string reinValue, Line[] lineBase, Point3d pointBase, Boolean reinOnly = false)
        {
            double reinPointRadius = scale * 20;
            double reinDistance = scale * 40;
            double lineLength1 = scale * 850;
            double textHeight = scale * 350;
            double space = scale * 40;
            TextVerticalMode textV = TextVerticalMode.TextBottom;

            CircleArray = new Circle[4];
            LineArray = new Line[4];

            List<Point3d> interPoints = lineBase.GetLineInters();
            if (interPoints.Count() == 4)
            {
                Polyline pline = interPoints.ToArray().ToPline();
                Polyline plineOffset = pline.GetOffsetCurves(reinDistance)[0] as Polyline;
                if (plineOffset.Area > pline.Area) plineOffset = pline.GetOffsetCurves(-reinDistance)[0] as Polyline;
                if (plineOffset.NumberOfVertices == 4)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Point3d p = plineOffset.GetPoint3dAt(i);
                        CircleArray[i] = new Circle(p, new Vector3d(0, 0, 1), reinPointRadius);
                    }
                    LineArray[0] = new Line(plineOffset.GetPoint3dAt(0), plineOffset.GetPoint3dAt(2));
                    LineArray[1] = new Line(plineOffset.GetPoint3dAt(1), plineOffset.GetPoint3dAt(3));
                }

                Point3d midPoint = interPoints[0].GetCenterPoint(interPoints[2]);
                Line lineV = new Line(midPoint, midPoint + new Vector3d(0, 1, 0));
                Line lineH = new Line(pointBase, pointBase + new Vector3d(1, 0, 0));
                var inters = lineV.IntersectWith(lineH);
                if (inters.Count > 0)
                {
                    LineArray[2] = new Line(midPoint, inters[0]);
                    LineArray[3] = new Line(inters[0], inters[0] + lineLength1 * (pointBase - inters[0]).GetNormal());
                    Vector3d vh = (LineArray[3].EndPoint - LineArray[3].StartPoint).GetNormal();
                    Vector3d vv = new Vector3d(0, 1, 0);
                    if (!reinOnly) { 
                    if (vh.X.IsAlmostEqualTo(1))
                        ReinText = db.CreateText("4%%133" + reinValue, inters[0] + (space*6) * vh + space * vv,
                        textHeight, 0, 0.7, "SMEDI", TextHorizontalMode.TextLeft, textV);
                    else
                        ReinText = db.CreateText("4%%133" + reinValue, inters[0] + (space * 6) * vh + space * vv,
                        textHeight, 0, 0.7, "SMEDI", TextHorizontalMode.TextRight, textV);
                    }
                }
            }
        }

        public void Draw()
        {
            db.SetLayerCurrent("结-钢筋", 1);
            foreach (Circle circle in CircleArray)
            {
                if (circle != null)
                    db.AddEntity(circle);
            }
            db.SetLayerCurrent("结-细线", 7);
            foreach (Line line in LineArray)
            {
                if (line != null)
                    db.AddEntity(line);
            }
            db.SetLayerCurrent("结-文字", 7);
            if (ReinText != null)
                db.AddEntity(ReinText);
        }
    }
}