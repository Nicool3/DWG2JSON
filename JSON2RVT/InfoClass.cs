using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSON2RVT
{
    /// <summary>
    /// 轴网
    /// </summary>
    public class GridInfo
    {
        public string Name { get; set; }                // 轴网名称
        public double[] ColumnSpacing { get; set; }      // 轴网列间距数组
        public double[] RowSpacing { get; set; }        // 轴网行间距数组
    }
}
