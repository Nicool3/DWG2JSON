using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System.Web.Script.Serialization;
using System.IO;

namespace JSON2RVT
{
    [Transaction(TransactionMode.Manual)]
    public class Json2Grid : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            JavaScriptSerializer js = new JavaScriptSerializer();
            GridInfo gridInfo = js.Deserialize<GridInfo>(ReadJsonFile(@"C:\Users\skq199101\Source\Repos\DWG2JSON\Grid.json"));

            double[] columnSpacing = gridInfo.ColumnSpacing;
            double[] rowSpacing = gridInfo.RowSpacing;
           
            double rowGridLength = columnSpacing.Sum() + 2000;
            double colGridLength = rowSpacing.Sum() + 2000;
            XYZ basePoint = new XYZ(0, 0, 0);

            using (Transaction trans = new Transaction(doc, "创建轴网"))
            {
                // GridType
                var gridTypeList = from element in new FilteredElementCollector(doc).OfClass(typeof(GridType))
                                   let type = element as GridType
                                   where type.Name.Contains("6.5mm 编号")
                                   select type;
                var gridType = gridTypeList.FirstOrDefault();
                if (gridType == null) throw new Exception("找不到 '6.5mm 编号' 轴网类型, 请载入");
                var gridTypeId = gridType.Id;

                trans.Start();
                gridType.get_Parameter(BuiltInParameter.GRID_BUBBLE_END_1).Set(1);
                gridType.get_Parameter(BuiltInParameter.GRID_BUBBLE_END_2).Set(1);

                // Column Grids
                List<Line> columnLines = new List<Line>();
                XYZ start = basePoint + new XYZ(0, -1000.0.ConvertToFeet(), 0);
                columnLines.Add(Line.CreateBound(start, start + new XYZ(0, colGridLength.ConvertToFeet(), 0)));
                for (int i = 0; i < columnSpacing.Count(); i++)
                {
                    start = start + new XYZ(columnSpacing[i].ConvertToFeet(), 0, 0);
                    columnLines.Add(Line.CreateBound(start, start + new XYZ(0, colGridLength.ConvertToFeet(), 0)));
                }

                for (int i = 0; i < columnLines.Count(); i++)
                {
                    // Grid
                    Grid lineGrid = Grid.Create(doc, columnLines[i]);
                    lineGrid.ChangeTypeId(gridTypeId);
                    try
                    {
                        lineGrid.Name = (i+1).ToString();
                    }
                    catch
                    {
                        doc.Delete(lineGrid.Id);
                        TaskDialog.Show("错误", $"图中已存在编号为{i}的轴线, 请删除后重试");
                        break;
                    }
                }

                // Row Grids
                List<Line> rowLines = new List<Line>();
                start = basePoint + new XYZ(-1000.0.ConvertToFeet(),0, 0);
                rowLines.Add(Line.CreateBound(start, start + new XYZ(rowGridLength.ConvertToFeet(), 0, 0)));
                for (int i = 0; i < rowSpacing.Count(); i++)
                {
                    start = start + new XYZ(0, rowSpacing[i].ConvertToFeet(), 0);
                    rowLines.Add(Line.CreateBound(start, start + new XYZ(rowGridLength.ConvertToFeet(), 0, 0)));
                }
                for (int i = 0; i < rowLines.Count(); i++)
                {
                    // Grid
                    Grid lineGrid = Grid.Create(doc, rowLines[i]);
                    lineGrid.ChangeTypeId(gridTypeId);
                    try
                    {
                        lineGrid.Name = Convert.ToChar('A' + i).ToString();
                    }
                    catch
                    {
                        doc.Delete(lineGrid.Id);
                        TaskDialog.Show("错误", $"图中已存在编号为{Convert.ToChar('A' + i).ToString()}的轴线, 请删除后重试");
                        break;
                    }
                }
                trans.Commit();
            }

            return Result.Succeeded;
        }

        public string ReadJsonFile(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, System.IO.FileAccess.Read, FileShare.Read))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    return sr.ReadLine();
                }
            }
        }
    }
}
