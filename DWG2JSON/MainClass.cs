using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.IO;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using System.Web.Script.Serialization;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace DWG2JSON
{
    public class MainClass
    {
        // 获取当前文档和数据库
        Document doc = Application.DocumentManager.MdiActiveDocument;
        Database db = Application.DocumentManager.MdiActiveDocument.Database;
        Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

        /// <summary>
        /// 将轴网数据写入JSON文件
        /// </summary>
        [CommandMethod("GRID2JSON")]
        public void Grid2Json()
        {
            double scale = doc.GetDouble("请输入实际尺寸放大系数: ", 1);
            SelectionSet ss = doc.GetSelectionSet("请选择轴网直线", "LINE".ToTypeFilter());
            if (ss == null) return;
            List<Line> columnGrid = new List<Line>();
            List<Line> rowGrid = new List<Line>();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject obj in ss)
                {
                    Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;
                    Line line = ent as Line;
                    if (line == null) continue;
                    if (line.Angle.IsAlmostEqualTo(Math.PI / 2) || line.Angle.IsAlmostEqualTo(Math.PI * 3 / 2)) columnGrid.Add(line);
                    if (line.Angle.IsAlmostEqualTo(0) || line.Angle.IsAlmostEqualTo(Math.PI)) rowGrid.Add(line);
                }
                List<Line> columnGridOrdered = columnGrid.OrderBy(s => s.StartPoint.X).ToList();
                List<Line> rowGridOrdered = rowGrid.OrderBy(s => s.StartPoint.Y).ToList();
                double[] columnSpacing = new double[columnGridOrdered.Count - 1];
                double[] rowSpacing = new double[rowGridOrdered.Count - 1];
                for (int i = 0; i < columnGridOrdered.Count - 1; i++)
                {
                    columnSpacing[i] = scale * (columnGridOrdered[i].StartPoint.GetDistance2d(columnGridOrdered[i + 1]));
                }
                for (int i = 0; i < rowGridOrdered.Count - 1; i++)
                {
                    rowSpacing[i] = scale * (rowGridOrdered[i].StartPoint.GetDistance2d(rowGridOrdered[i + 1]));
                }

                GridInfo gridInfo = new GridInfo("Grid1", columnSpacing, rowSpacing);

                JavaScriptSerializer js = new JavaScriptSerializer();
                string gridJson = js.Serialize(gridInfo);//序列化
                WriteToJsonFile(@"C:\Users\skq199101\Source\Repos\DWG2JSON\Grid.json", gridJson);
            }
        }

        /// <summary>
        /// 测试生成墙体信息
        /// </summary>
        [CommandMethod("WALL")]
        public void CreateWallInfo()
        {
            SelectionSet ss1 = doc.GetSelectionSet("请选择直线", "LINE".ToTypeFilter());
            if (ss1 == null) return;
            List<Line> lineList = ss1.SelectType<Line>();
            DBObjectCollection lineCol = ss1.GetCollection();

            // 通过所选线生成墙面面域范围
            DBObjectCollection regionCol = Region.CreateFromCurves(lineCol);
            List<Region> regionList = new List<Region>();
            foreach (DBObject obj in regionCol)
            {
                if (obj != null && obj.GetType() == typeof(Region))
                {
                    Region reg = obj as Region;
                    if (reg.Area > 0) regionList.Add(reg);
                }
            }
            ed.WriteMessage("\n共形成" + regionList.Count + "个封闭区域");
            Region reg0 = regionList.OrderByDescending(s => s.Area).FirstOrDefault();
            foreach (Region reg in regionList)
            {
                if (reg == reg0) continue;
                else
                {
                    reg0.BooleanOperation(BooleanOperationType.BoolSubtract, reg);
                }
            }

            // 利用平行线组形成短墙信息
            List<WallInfo> wallInfoListRaw = LineInfo.GetWallElements(lineList, reg0);

            // 处理轴线相交的墙体
            List<WallInfo> wallInfoListAdjusted = new List<WallInfo>(wallInfoListRaw);
            for (int i = 0; i < wallInfoListRaw.Count; i++)
            {
                WallInfo wallInfo = wallInfoListRaw[i];
                if (wallInfoListAdjusted.Contains(wallInfo))
                {
                    for (int j = i + 1; j < wallInfoListRaw.Count; j++)
                    {
                        WallInfo subwallInfo = wallInfoListRaw[j];
                        if (!wallInfo.CurveLine.IsParallelTo(subwallInfo.CurveLine))
                        {
                            var points = wallInfo.CurveLine.IntersectWith(subwallInfo.CurveLine, Intersect.OnBothOperands);
                            if (points.Count > 0)
                            {
                                if (wallInfo.CurveLine.Length >= subwallInfo.CurveLine.Length) wallInfoListAdjusted.Remove(subwallInfo);
                                else wallInfoListAdjusted.Remove(wallInfo);
                            }
                        }
                    }
                }
            }

            // 最大墙厚
            double maxWidth = wallInfoListAdjusted.Max(s => s.Width);
            ed.WriteMessage("\n最大墙厚：" + maxWidth);

            // 处理共线可合并的墙体
            List<WallInfo> wallInfoListJoined = new List<WallInfo>(wallInfoListAdjusted);
            for (int i = 0; i < wallInfoListAdjusted.Count; i++)
            {
                WallInfo wallInfo = wallInfoListAdjusted[i];
                if (wallInfoListJoined.Contains(wallInfo))
                {
                    List<Line> tempLineList = new List<Line>();
                    for (int j = i + 1; j < wallInfoListAdjusted.Count; j++)
                    {
                        WallInfo subwallInfo = wallInfoListAdjusted[j];
                        if (wallInfo.CurveLine.IsInSameLine(subwallInfo.CurveLine) && wallInfo.Width.IsAlmostEqualTo(subwallInfo.Width))
                        {
                            Point3d[] tempPoints = LineInfo.GetOverlapPoints(wallInfo.CurveLine, subwallInfo.CurveLine);
                            if (tempPoints.Count() == 2)
                            {
                                Line tempLine = new Line(tempPoints[0], tempPoints[1]);
                                if (tempLine.Length < maxWidth * 1.01)
                                {
                                    WallInfo tempWallInfo = new WallInfo(tempLine, wallInfo.Width);
                                    //if (tempWallInfo.Region.IsInRegion(reg0))
                                    //{
                                    tempLineList.Add(subwallInfo.CurveLine);
                                    wallInfoListJoined.Remove(subwallInfo);
                                    //}
                                }
                            }

                        }
                    }
                    if (tempLineList.Count > 0)
                    {
                        Line templine = wallInfo.CurveLine;
                        for (int k = 0; k < tempLineList.Count; k++)
                        {
                            templine = LineInfo.Join(tempLineList[k], templine);
                        }
                        wallInfoListJoined.Add(new WallInfo(templine, wallInfo.Width));
                        wallInfoListJoined.Remove(wallInfo);
                    }
                }
            }

            foreach (WallInfo info in wallInfoListJoined)
            {
                Line line = info.CurveLine;
                db.AddEntity(line);
            }
            /*
            // 将相交处墙体相连
            int iter = 0;
            while (!wallInfoList.IsAllJoined(maxWidth / 2 * 1.01) && iter < 100)
            {
                LineInfo.JoinWalls(ref wallInfoList, maxWidth / 2 * 1.01, reg0);
                iter++;
            }

            foreach (WallInfo info in wallInfoList)
            {
                Line line = info.CurveLine;
                db.AddEntity(line);
            }
            */
        }

        /// <summary>
        /// 创建图素记录
        /// </summary>
        [CommandMethod("CR")]
        public void CreateRecord()
        {
            // 读取现有记录
            List<ComInfo> infoList = new List<ComInfo>();
            string path = @"C:\Users\skq199101\Source\Repos\DWG2JSON\DetailComInfo.json";
            string jsonString = ReadJsonFile(path);
            JavaScriptSerializer js = new JavaScriptSerializer();
            js.MaxJsonLength = (int)1e+8;
            if (!string.IsNullOrEmpty(jsonString)) infoList = js.Deserialize<List<ComInfo>>(jsonString);
            // 创建窗体
            RecordWin win = new RecordWin(infoList);
            var result = win.ShowDialog();
            if (result.HasValue && result.Value)
            {
                string name = win.comboBox1.Text;
                double scale = win.Scale;
                if (!string.IsNullOrEmpty(name))
                {
                    List<ComInfo> newInfoList = new List<ComInfo>();
                    SelectionSet ss = doc.GetSelectionSet("Select something to create a record");
                    while (ss != null)
                    {
                        ComInfo info = new ComInfo(ss, name, scale);
                        if (info != null) newInfoList.Add(info);
                        ss = doc.GetSelectionSet("Continue to select something");
                    }
                    int count = 0;
                    foreach (ComInfo info in newInfoList)
                    {
                        infoList.Add(info);
                        count++;
                    }

                    string info1Json = js.Serialize(infoList);//序列化    
                    WriteToJsonFile(path, info1Json);
                    ed.WriteMessage("已将" + count + "条记录写入 " + path + "\n");
                }
                else
                {
                    MessageBox.Show("请选择或输入正确的名称！");
                }
            }
        }

        /// <summary>
        /// 创建临时单条图素记录
        /// </summary>
        [CommandMethod("CTR")]
        public void CreateTempRecord()
        {
            string path = @"C:\Users\skq199101\Source\Repos\DWG2JSON\DetailComInfoTemp.json";
            JavaScriptSerializer js = new JavaScriptSerializer();
            js.MaxJsonLength = (int)1e+8;

            SelectionSet ss = doc.GetSelectionSet("Select something to recognize");
            if(ss != null)
            {
                ComInfo info = new ComInfo(ss, "", 1);
                string info1Json = js.Serialize(info);//序列化    
                OverWriteToJsonFile(path, info1Json);
            }
        }

        /// <summary>
        /// 绘制全部图素记录
        /// </summary>
        [CommandMethod("DR")]
        public void DrawRecord()
        {
            // 读取现有记录
            List<ComInfo> infoList = new List<ComInfo>();
            string path = @"C:\Users\skq199101\Source\Repos\DWG2JSON\ComInfo.json";
            string jsonString = ReadJsonFile(path);
            JavaScriptSerializer js = new JavaScriptSerializer();
            if (!string.IsNullOrEmpty(jsonString))
            {
                infoList = js.Deserialize<List<ComInfo>>(jsonString);
                foreach (ComInfo comInfo in infoList)
                {
                    comInfo.Draw();
                }
            }
        }

        /// <summary>
        /// 查找相似元素
        /// </summary>
        [CommandMethod("SS")]
        public void SuperSimilar()
        {

            SelectionSet ss = doc.GetSelectionSet("Select");
            SelectionSet ssRange = doc.Editor.SelectAll().Value;
            ComInfo info0 = new ComInfo(ss, "info0");

            List<ComInfo> comInfoList = info0.FindSimilar(doc, ssRange);

            ed.WriteMessage("\nFound " + comInfoList.Count().ToString() + " Elements.");
            foreach (ComInfo comInfo in comInfoList)
            {
                db.SetLayerCurrent("注释", 3);
                Point3d center = new Point3d(comInfo.Center[0], comInfo.Center[1], comInfo.Center[2]);
                db.AddEntity(new Rectangle(center, 1.2 * comInfo.Length, 1.2 * comInfo.Width).Polyline);
            }
        }

        /// <summary>
        /// 剖面配筋图绘制工具
        /// </summary>
        [CommandMethod("JJ")]
        public void CreateReinSectionText()
        {
            var win = new SectionReinWin();
            var result = win.ShowDialog();
            if (result.HasValue && result.Value)
            {
                switch (win.CommandToExecute)
                {
                    case SectionReinWin.Command.CreateReinLines:
                        CreateReinLines(win.OffsetDistance);
                        break;

                    case SectionReinWin.Command.CreateReinPoints:
                        CreateReinPoints(win.PointsScale, win.ReinPointsValue);
                        break;

                    case SectionReinWin.Command.CreateReinTexts:
                        CreateReinTexts(win.Scale, win.ReinValueArray, win.ReinPointIn);
                        break;
                }
            }
        }

        /// <summary>
        /// 选择壁板边界绘制剖面配筋图
        /// </summary>
        public void CreateReinLines(double offsetDistance)
        {
            SelectionSet ss = doc.GetSelectionSet("请选择需要绘制配筋图的多段线", "LWPOLYLINE".ToTypeFilter());
            if (ss != null)
            {
                List<Polyline> plineList = ss.SelectType<Polyline>();
                List<Line> lineList = new List<Line>();
                // 获取的多段线按面积排序，取最大的为最外圈边线
                plineList = plineList.OrderByDescending(s => s.Area).ToList();
                Polyline plineBoundary = plineList[0];
                // 偏移外圈多段线
                Polyline plineBoundaryOffset = plineBoundary.GetOffsetCurves((double)offsetDistance)[0] as Polyline;
                if (plineBoundaryOffset.Area > plineBoundary.Area) plineBoundaryOffset = plineBoundary.GetOffsetCurves(-(double)offsetDistance)[0] as Polyline;
                lineList.AddRange(plineBoundaryOffset.ConvertToLines());
                // 偏移内圈多段线
                foreach (Polyline pline in plineList)
                {
                    if (pline != plineBoundary)
                    {
                        Polyline plineOffset = pline.GetOffsetCurves(-(double)offsetDistance)[0] as Polyline;
                        if (plineOffset.Area < pline.Area) plineOffset = pline.GetOffsetCurves((double)offsetDistance)[0] as Polyline;
                        lineList.AddRange(plineOffset.ConvertToLines());
                    }
                }

                // 对每根直线两端查找可延伸处
                List<Line> extendedLineList = new List<Line>();
                foreach (Line line in lineList)
                {
                    Line newLine = line;
                    for (int i = 0; i < 2; i++)
                    {
                        Point3d point0 = newLine.GetPoints()[i];
                        Point3d point1 = newLine.GetPoints()[1 - i];
                        foreach (Line subline in lineList)
                        {
                            if (subline != line && !subline.IsParallelTo(line))
                            {
                                var interPoints = line.IntersectWith(subline, Intersect.ExtendThis);
                                if (interPoints.Count > 0)
                                {
                                    Point3d inter = interPoints[0];
                                    if (!inter.IsInLine(line) && inter.DistanceTo(point0) < inter.DistanceTo(point1))
                                    {
                                        Line tempLine = new Line(point0, inter);
                                        bool noCross = true;
                                        foreach (Polyline pline in plineList)
                                        {
                                            if (tempLine.IntersectWith(pline, Intersect.OnBothOperands).Count > 0) { noCross = false; break; }
                                        }
                                        if (noCross && (new Line(inter, point1)).Length > newLine.Length)
                                        {
                                            if (i == 0) newLine = new Line(inter, point1);
                                            else newLine = new Line(point1, inter);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    extendedLineList.Add(newLine);
                }

                // 生成的直线去重复
                List<Line> overkilledLineList = new List<Line>();
                List<Line> todeleteLineList = new List<Line>();
                foreach (Line line in extendedLineList)
                {
                    if (!todeleteLineList.Contains(line))
                    {
                        Line newLine = line;
                        foreach (Line subline in extendedLineList)
                        {
                            if (subline != newLine && newLine.IsInSameLine(subline) && (newLine.StartPoint.IsInLine(subline) || newLine.EndPoint.IsInLine(subline)))
                            {
                                newLine = newLine.Overkill(subline);
                                todeleteLineList.Add(subline);
                            }
                        }
                        overkilledLineList.Add(newLine);
                    }
                }
                
                db.SetLayerCurrent("结-钢筋", 1);
                foreach (Line line in overkilledLineList)
                {
                    line.Layer = "结-钢筋";
                    db.AddEntity(line);
                }
            }
        }

        /// <summary>
        /// 选择纵筋绘制转角点筋
        /// </summary>
        public void CreateReinPoints(double scale, string reinValue, Boolean reinOnly=false)
        {
            SelectionSet ss = doc.GetSelectionSet("请选择4条相交的钢筋直线", "LINE".ToTypeFilter());
            while (ss != null)
            {
                Line[] lineArray = ss.SelectType<Line>().Where(s => s.Layer.Contains("结-钢筋")).ToArray();
                if (lineArray.Count() == 4)
                {
                    Point3d? pointBase = doc.GetPoint("点击生成位置");
                    if (pointBase != null)
                    {
                        CornerReinDraw reinPoints = new CornerReinDraw(scale, reinValue, lineArray, (Point3d)pointBase);
                        reinPoints.Draw();
                    }
                }
                ss = doc.GetSelectionSet("请选择4条相交的钢筋直线", "LINE".ToTypeFilter());
            }
        }

        /// <summary>
        /// 选择纵筋绘制钢筋标注
        /// </summary>
        public void CreateReinTexts(double scale, string[] reinValueArray, bool reinPointIn)
        {
            SelectionSet ss = doc.GetSelectionSet("请选择直线", "LINE".ToTypeFilter());
            while (ss != null)
            {
                Point3d[] sspoints = ss.GetSelectionSetPoints();
                if (sspoints.Count() == 4)
                {
                    Point3d pointBase = sspoints[0].GetCenterPoint(sspoints[2]);
                    Line[] lineArray = ss.SelectType<Line>().Where(s => s.Layer.Contains("结-钢筋")).ToArray();
                    if (lineArray.Count() == 2)
                    {
                        SectionReinDraw reinGroup1 = new SectionReinDraw(scale, reinValueArray, lineArray, pointBase, reinPointIn);
                        reinGroup1.Draw();
                    }
                }
                ss = doc.GetSelectionSet("请选择直线", "LINE".ToTypeFilter());
            }
        }

        /// <summary>
        /// 查找图形区域边界
        /// </summary>
        [CommandMethod("FB")]
        public void FindBoundaries()
        {
            double offsetDistance = 4;
            SelectionSet ss0 = doc.GetSelectionSet("Select Finding Range");

            if (ss0 != null)
            {
                List<Entity> entList = ss0.GetSelectedEntity().Where(s => !s.IsDrawingBorder()).ToList();      // 所选的除图框外全部实体对象
                List<Entity> entListCollected = new List<Entity>();  // 已进行分区的实体对象
                List<Rectangle> recList = new List<Rectangle>();     // 分区矩形列表
                foreach (Entity ent in entList)
                {
                    if (!entListCollected.Contains(ent))
                    {
                        List<Entity> entListTemp = new List<Entity>() { ent };
                        SelectionSet ssTemp = null;
                        List<Entity> ssTempEntList = new List<Entity>();
                        Extents3d extend = new Extents3d();
                        extend.AddExtents(ent.GeometricExtents);
                        if (extend != null)
                        {
                            int iterCount = 0;
                            do
                            {
                                entListTemp.AddRange(ssTempEntList.Where(s => !entListTemp.Contains(s)));
                                Point3d pointMin = extend.MinPoint - offsetDistance * new Vector3d(1, 1, 0);
                                Point3d pointMax = extend.MaxPoint + offsetDistance * new Vector3d(1, 1, 0);
                                ssTemp = doc.GetCrossSelectionSet(pointMin, pointMax);
                                if (ssTemp != null)
                                {
                                    ssTempEntList = ssTemp.GetSelectedEntity().Where(s => !s.IsDrawingBorder()).ToList();
                                    extend = ssTempEntList.GetExtents();
                                }
                                iterCount++;
                            }
                            while (ssTempEntList.Count > entListTemp.Count && iterCount < 100);
                            entListCollected.AddRange(entListTemp.Where(s => !entListCollected.Contains(s)));     
                            recList.Add(new Rectangle(extend.MinPoint - 0.5 * offsetDistance * new Vector3d(1, 1, 0), extend.MaxPoint + 0.5 * offsetDistance * new Vector3d(1, 1, 0)));
                        }
                    }
                }
                // 图形中添加识别框
                db.SetLayerCurrent("识别框", 11);
                foreach (Rectangle rec in recList)
                {
                    db.AddEntity(rec.Polyline);
                }
            }
        }

        /// <summary>
        /// 测试
        /// </summary>
        [CommandMethod("CS")]
        public void Test()
        {
            /*
            SelectionSet ss = doc.GetSelectionSet("Select");
            List<BlockReference> EntList = ss.SelectType<BlockReference>();
            ed.WriteMessage(EntList.Count.ToString());
            */
            ObjectId id = doc.GetEntityId("Select");
            Entity ent = id.GetEntity();

            ed.WriteMessage(ent.IsDrawingBorder().ToString());
        }

        public void WriteToJsonFile(string path, string jsonContents)
        {
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    sw.WriteLine(jsonContents);
                }
            }
        }

        public void OverWriteToJsonFile(string path, string jsonContents)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    sw.WriteLine(jsonContents);
                }
            }
        }

        public string ReadJsonFile(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    return sr.ReadLine();
                }
            }
        }
    }
}