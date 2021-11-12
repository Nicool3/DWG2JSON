using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

namespace JSON2RVT
{
    public static class ToolClass
    {
        public static double ConvertToFeet(this double num)
        {
            return UnitUtils.Convert(num, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
        }
    }
}
