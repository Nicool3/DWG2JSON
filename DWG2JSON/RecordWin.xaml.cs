using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DWG2JSON
{
    /// <summary>
    /// RecordWin.xaml 的交互逻辑
    /// </summary>
    public partial class RecordWin : Window
    {
        public double Scale;

        public RecordWin(List<ComInfo> comInfoList)
        {
            InitializeComponent();
            this.Closing += Window_Closing;
            this.scaleFactor.Text = Properties.Settings.Default.scaleFactorDefault;
            List<ComInfo> uniqueNameList = comInfoList.GroupBy(x => x.Name).Select(y => y.First()).ToList();
            comboBox1.ItemsSource = uniqueNameList;
            comboBox1.DisplayMemberPath = "Name";
            comboBox1.SelectedValuePath = "Name";
            comboBox1.SelectedIndex = 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.scaleFactorDefault = this.scaleFactor.Text;
            Properties.Settings.Default.Save();
            e.Cancel = false;
        }

        private void confirmButton_Click(object sender, RoutedEventArgs e)
        {
            Scale = 1 / Convert.ToDouble(this.scaleFactor.Text);
            DialogResult = true;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
