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
    public static class ToolClass
    {
        /// <summary>
        /// 判断两值是否在允许误差内相等
        /// </summary>
        public static bool IsAlmostEqualTo(this double num1, double num2, double tolerance = 1.0e-06)
        {
            if (Math.Abs(num1 - num2) < tolerance) return true;
            else return false;
        }

        /// <summary>
        /// 获取两值的较小值
        /// </summary>
        public static double Min(double num1, double num2)
        {
            return num1 <= num2 ? num1 : num2;
        }

        /// <summary>
        /// 获取两值的较大值
        /// </summary>
        public static double Max(double num1, double num2)
        {
            return num1 >= num2 ? num1 : num2;
        }

        /// <summary>
        /// 判断两点是否在允许误差内相等
        /// </summary>
        public static bool IsAlmostEqualTo(this Point3d p1, Point3d p2, double tolerance = 1.0e-06)
        {
            if (Math.Abs(p1.X - p2.X) < tolerance && Math.Abs(p1.Y - p2.Y) < tolerance && Math.Abs(p1.Z - p2.Z) < tolerance) return true;
            else return false;
        }

        /// <summary>
        /// 求两点中点
        /// </summary>
        public static Point3d GetCenterPoint(this Point3d p1, Point3d p2)
        {
            return new Point3d((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, (p1.Z + p2.Z) / 2);
        }

        /// <summary>
        /// 输入数字
        /// </summary>
        public static double? GetDouble(this Document doc, string message)
        {
            PromptDoubleResult doubleRes = doc.Editor.GetDouble(new PromptDoubleOptions(message));
            if (doubleRes.Status == PromptStatus.OK) return doubleRes.Value;
            else return null;
        }

        /// <summary>
        /// 输入带默认值的数字
        /// </summary>
        public static double GetDouble(this Document doc, string message, double defaultDouble)
        {
            PromptDoubleOptions doubleOpt = new PromptDoubleOptions(message);
            doubleOpt.DefaultValue = defaultDouble;
            PromptDoubleResult doubleRes = doc.Editor.GetDouble(doubleOpt);
            if (doubleRes.Status == PromptStatus.OK) return doubleRes.Value;
            else return defaultDouble;
        }

        /// <summary>
        /// 输入字符串
        /// </summary>
        public static string GetString(this Document doc, string message)
        {
            PromptResult Res;
            PromptStringOptions Opts = new PromptStringOptions(message);
            Res = doc.Editor.GetString(Opts);
            if (Res.Status == PromptStatus.OK) return Res.StringResult;
            else return null;
        }

        /// <summary>
        /// 屏幕取点
        /// </summary>
        public static Point3d? GetPoint(this Document doc, string message)
        {
            PromptPointResult pointRes = doc.Editor.GetPoint(new PromptPointOptions(message));
            if (pointRes.Status == PromptStatus.OK) return pointRes.Value;
            else return null;
        }

        /// <summary>
        /// 获取选择集
        /// </summary>
        public static SelectionSet GetSelectionSet(this Document doc, string message, SelectionFilter selFtr = null)
        {
            PromptSelectionOptions opt = new PromptSelectionOptions();
            opt.MessageForAdding = message;
            PromptSelectionResult selectionRes = doc.Editor.GetSelection(opt, selFtr);
            if (selectionRes.Status == PromptStatus.OK) return selectionRes.Value;
            else return null;
        }

        /// <summary>
        /// 获取窗口内选择集
        /// </summary>
        public static SelectionSet GetWindowSelectionSet(this Document doc, Point3d p1, Point3d p2)
        {
            PromptSelectionResult selectionRes = doc.Editor.SelectWindow(p1, p2);
            if (selectionRes.Status == PromptStatus.OK) return selectionRes.Value;
            else return null;
        }

        /// <summary>
        /// 获取窗口相交选择集
        /// </summary>
        public static SelectionSet GetCrossSelectionSet(this Document doc, Point3d p1, Point3d p2, SelectionFilter selFtr = null)
        {
            PromptSelectionResult selectionRes = doc.Editor.SelectCrossingWindow(p1, p2, selFtr);
            if (selectionRes.Status == PromptStatus.OK) return selectionRes.Value;
            else return null;
        }

        /// <summary>
        /// 获取选择集角点
        /// </summary>
        public static Point3d[] GetSelectionSetPoints(this SelectionSet ss)
        {
            foreach (SelectedObject obj in ss)
            {
                switch (obj.SelectionMethod)
                {
                    case SelectionMethod.PickPoint:
                        PickPointSelectedObject ppSelObj = obj as PickPointSelectedObject;
                        Point3d pickedPoint = ppSelObj.PickPoint.PointOnLine;
                        return new Point3d[] { pickedPoint };

                    case SelectionMethod.Crossing:
                        CrossingOrWindowSelectedObject crossSelObj = obj as CrossingOrWindowSelectedObject;
                        PickPointDescriptor[] crossSelPickedPoints = crossSelObj.GetPickPoints();
                        return crossSelPickedPoints.Select(s => s.PointOnLine).ToArray();

                    case SelectionMethod.Window:
                        CrossingOrWindowSelectedObject windSelObj = obj as CrossingOrWindowSelectedObject;
                        PickPointDescriptor[] winSelPickedPoints = windSelObj.GetPickPoints();
                        return winSelPickedPoints.Select(s => s.PointOnLine).ToArray();
                }
            }
            return new Point3d[] { };
        }

        /// <summary>
        /// 获取选择集中的指定类型元素返回列表
        /// </summary>
        public static List<T> SelectType<T>(this SelectionSet ss) where T : Entity
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            List<T> resultList = new List<T>();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject obj in ss)
                {
                    if (obj != null)
                    {
                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;
                        if (ent.GetType() == typeof(T))
                        {
                            resultList.Add(ent as T);
                        }
                    }
                }
                trans.Commit();
            }
            return resultList;
        }

        /// <summary>
        /// 获取全部图形的指定类型元素返回列表
        /// </summary>
        public static List<T> SelectType<T>(this Database db) where T : Entity
        {
            List<T> resultList = new List<T>();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)trans.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
                foreach (ObjectId id in btr)
                {
                    if (id != null)
                    {
                        Entity ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                        if (ent.GetType() == typeof(T))
                        {
                            resultList.Add(ent as T);
                        }
                    }
                }
                trans.Commit();
            }
            return resultList;
        }

        /// <summary>
        /// 获取选择集中的元素返回元素集合
        /// </summary>
        public static DBObjectCollection GetCollection(this SelectionSet ss)
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            DBObjectCollection resultCollection = new DBObjectCollection();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject obj in ss)
                {
                    if (obj != null)
                    {
                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;
                        resultCollection.Add(ent);
                    }
                }
                trans.Commit();
            }
            return resultCollection;
        }

        /// <summary>
        /// 获取选择集中的全部元素返回列表
        /// </summary>
        public static List<Entity> GetSelectedEntity(this SelectionSet ss)
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            List<Entity> resultList = new List<Entity>();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject obj in ss)
                {
                    if (obj != null)
                    {
                        try
                        {
                            Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;
                            if (ent != null) resultList.Add(ent);
                        }
                        catch { }
                    }
                }
                trans.Commit();
            }
            return resultList;
        }

        /// <summary>
        /// 获取选择集实体包围盒点集
        /// </summary>
        public static Point3d[] GetGeometricExtents(this SelectionSet ss)
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            // 范围对象
            Extents3d extend = new Extents3d();
            Point3d[] result = new Point3d[2];
            if (ss != null)
            {
                foreach (SelectedObject obj in ss)
                {
                    if (obj != null)
                    {
                        using (Transaction trans = db.TransactionManager.StartTransaction())
                        {
                            Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;
                            extend.AddExtents(ent.GeometricExtents);
                            trans.Commit();
                        }
                    }
                }
                if (extend != null)
                {
                    result[0] = new Point3d(extend.MinPoint.X, extend.MinPoint.Y, extend.MinPoint.Z);  // 范围最小点
                    result[1] = new Point3d(extend.MaxPoint.X, extend.MaxPoint.Y, extend.MaxPoint.Z);  // 范围最大点
                }
            }
            return result;
        }

        /// <summary>
        /// 获取选择集实体包围盒
        /// </summary>
        public static Extents3d GetExtents(this SelectionSet ss)
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            // 范围对象
            Extents3d extend = new Extents3d();
            if (ss != null)
            {
                foreach (SelectedObject obj in ss)
                {
                    if (obj != null)
                    {
                        using (Transaction trans = db.TransactionManager.StartTransaction())
                        {
                            Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;
                            extend.AddExtents(ent.GeometricExtents);
                            trans.Commit();
                        }
                    }
                }
            }
            return extend;
        }

        /// <summary>
        /// 获取元素列表实体包围盒
        /// </summary>
        public static Extents3d GetExtents(this List<Entity> entList)
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            Extents3d extend = new Extents3d();
            foreach (Entity ent in entList)
            {
                if (ent != null) extend.AddExtents(ent.GeometricExtents);
            }
            return extend;
        }

        /// <summary>
        /// 获取实体Id
        /// </summary>
        public static ObjectId GetEntityId(this Document doc, string message)
        {
            ObjectId id = ObjectId.Null;
            PromptEntityOptions opt = new PromptEntityOptions(message);
            PromptEntityResult res = doc.Editor.GetEntity(opt);
            if (res.Status == PromptStatus.OK) id = res.ObjectId;
            return id;
        }

        /// <summary>
        /// 获取实体
        /// </summary>
        public static T GetEntity<T>(this ObjectId id) where T : Entity
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                if (id != null)
                {
                    Entity ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent.GetType() == typeof(T))
                    {
                        return ent as T;
                    }
                }
                trans.Commit();
            }
            return null;
        }

        /// <summary>
        /// 获取实体
        /// </summary>
        public static Entity GetEntity(this ObjectId id)
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            Entity ent = null;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                if (id != null)
                {
                    ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                }
                trans.Commit();
            }
            return ent;
        }

        /// <summary>
        /// 从BlockReference中获取块中全部实体，包含嵌套情况
        /// </summary>
        public static List<Entity> GetAllEntities(this BlockReference br)
        {
            List<Entity> allEntList = new List<Entity>();
            List<BlockReference> brList = new List<BlockReference>() { br };
            while (brList.Count > 0)
            {
                BlockReference brTemp = brList.First();
                List<Entity> brTempEntList = brTemp.GetEntities();
                foreach (Entity ent in brTempEntList)
                {
                    if (ent.GetType() == typeof(BlockReference))
                    {
                        if (!brList.Contains((BlockReference)ent))
                            brList.Add((BlockReference)ent);
                    }

                    else
                    {
                        if (!allEntList.Contains(ent))
                            allEntList.Add(ent);
                    }
                }
                brList.Remove(brTemp);
            }
            return allEntList;
        }

        /// <summary>
        /// 从BlockReference中获取块中实体
        /// </summary>
        public static List<Entity> GetEntities(this BlockReference br)
        {
            DBObjectCollection objCol = new DBObjectCollection();
            List<Entity> entList = new List<Entity>();
            if (br != null) br.Explode(objCol);
            foreach (Object obj in objCol)
            {
                Entity ent = obj as Entity;
                if (ent != null) entList.Add(ent);
            }
            return entList;
        }

        /// <summary>
        /// 生成单类型过滤器
        /// </summary>
        public static SelectionFilter ToTypeFilter(this string str)
        {
            TypedValue[] typelist = new TypedValue[1];
            typelist.SetValue(new TypedValue((int)DxfCode.Start, str), 0);
            SelectionFilter typefilter = new SelectionFilter(typelist);
            return typefilter;
        }

        /// <summary>
        /// 生成多类型过滤器
        /// </summary>
        public static SelectionFilter ToTypeFilter(this List<string> strlist)
        {
            int n = strlist.Count();
            string startopt = "<OR";
            string endopt = "OR>";
            TypedValue[] typelist = new TypedValue[n + 2];
            typelist.SetValue(new TypedValue((int)DxfCode.Operator, startopt), 0);
            for (int i = 1; i < n + 1; i++)
            {
                typelist.SetValue(new TypedValue((int)DxfCode.Start, strlist[i - 1]), i);
            }
            typelist.SetValue(new TypedValue((int)DxfCode.Operator, endopt), n + 1);

            SelectionFilter typefilter = new SelectionFilter(typelist);
            return typefilter;
        }

        /// <summary>
        /// 获取两点的2d距离
        /// </summary>
        public static double GetDistance2d(this Point3d point1, Point3d point2)
        {
            return (Math.Sqrt(Math.Pow((point1.X - point2.X), 2) + Math.Pow((point1.Y - point2.Y), 2)));
        }

        /// <summary>
        /// 获取点到直线的垂直距离
        /// </summary>
        public static double GetDistance2d(this Point3d point, Line line)
        {
            Point3d p = line.GetClosestPointTo(point, true);
            return p.GetDistance2d(point);
        }

        /// <summary>
        /// 判断点是否在直线段上
        /// </summary>
        public static bool IsInLine(this Point3d point, Line line)
        {
            if (point.IsAlmostEqualTo(line.StartPoint) || point.IsAlmostEqualTo(line.EndPoint)) return true;
            else
            {
                Vector2d v1 = new Vector2d((point.X - line.StartPoint.X), (point.Y - line.StartPoint.Y));
                Vector2d v2 = new Vector2d((point.X - line.EndPoint.X), (point.Y - line.EndPoint.Y));
                if (v1.GetAngleTo(v2).IsAlmostEqualTo(Math.PI)) return true;
                else return false;
            }
        }

        /// <summary>
        /// 判断double列表中的值是否全相等
        /// </summary>
        public static bool AllEquals(this List<double> numList)
        {
            for (int i = 0; i < numList.Count - 1; i++)
            {
                if (!numList[i].IsAlmostEqualTo(numList[i + 1])) return false;
            }
            return true;
        }

        /// <summary> 
        /// 根据图形边界显示视图 
        /// </summary> 
        public static void ZoomExtens(this Document doc)
        {
            Database db = doc.Database;
            Editor ed = doc.Editor;
            db.UpdateExt(true);
            if (db.Extmax.X < db.Extmin.X)
            {
                Plane plane = new Plane();
                Point3d pt1 = new Point3d(plane, db.Limmin);
                Point3d pt2 = new Point3d(plane, db.Limmax);
                doc.ZoomWindow(pt1, pt2);
            }
            else
            {
                doc.ZoomWindow(db.Extmin, db.Extmax);
            }
        }

        /// <summary> 
        /// 根据图形角点显示视图 
        /// </summary> 
        public static void ZoomWindow(this Document doc, Point3d pt1, Point3d pt2)
        {
            Database db = doc.Database;
            Editor ed = doc.Editor;
            using (Line line = new Line(pt1, pt2))
            {
                Extents3d extents = new Extents3d(line.GeometricExtents.MinPoint, line.GeometricExtents.MaxPoint);
                Point2d minPt = new Point2d(extents.MinPoint.X, extents.MinPoint.Y);
                Point2d maxPt = new Point2d(extents.MaxPoint.X, extents.MaxPoint.Y);
                ViewTableRecord view = ed.GetCurrentView();
                view.CenterPoint = minPt + (maxPt - minPt) / 2;
                view.Height = maxPt.Y - minPt.Y;
                view.Width = maxPt.X - minPt.X;
                ed.SetCurrentView(view);
            }
        }

        /// <summary> 
        /// 获取当前显示视图图形边界角点
        /// </summary> 
        public static Point3d[] GetCurrentWindow(this Document doc)
        {
            Database db = doc.Database;
            Editor ed = doc.Editor;
            ViewTableRecord view = ed.GetCurrentView();
            Point3d pointMin = new Point3d((view.CenterPoint.X - view.Width / 2), (view.CenterPoint.Y - view.Height / 2), 0);
            Point3d pointMax = new Point3d((view.CenterPoint.X + view.Width / 2), (view.CenterPoint.Y + view.Height / 2), 0);
            return new Point3d[] { pointMin, pointMax };
        }
    }
}
