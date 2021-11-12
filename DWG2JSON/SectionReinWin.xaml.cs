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
using System.Xaml;

namespace DWG2JSON
{
    /// <summary>
    /// SectionReinWin.xaml 的交互逻辑
    /// </summary>
    public partial class SectionReinWin : Window
    {
        // 传出参数
        public string[] ReinValueArray;
        public bool ReinPointIn;
        public double Scale;
        public double PointsScale;
        public double OffsetDistance;
        public string ReinPointsValue;
        public enum Command { CreateReinLines, CreateReinPoints , CreateReinTexts };
        public Command CommandToExecute;

        public SectionReinWin()
        {
            InitializeComponent();
            this.Closing += Window_Closing;
            this.reinText1.Text = Properties.Settings.Default.reinText1Default;
            this.reinText2.Text = Properties.Settings.Default.reinText2Default;
            this.reinText3.Text = Properties.Settings.Default.reinText3Default;
            this.reinText4.Text = Properties.Settings.Default.reinText4Default;
            this.reinPointIn.IsChecked = Properties.Settings.Default.reinPointInDefault;
            this.reinPointOut.IsChecked = !Properties.Settings.Default.reinPointInDefault;
            this.scaleFactor.Text = Properties.Settings.Default.scaleFactorDefault;
            this.offsetDistance.Text = Properties.Settings.Default.offsetDistanceDefault;
            this.reinPointsText.Text = Properties.Settings.Default.reinPointsTextDefault;
            this.scalePointsFactor.Text = Properties.Settings.Default.scalePointsFactorDefault;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.reinText1Default = this.reinText1.Text;
            Properties.Settings.Default.reinText2Default = this.reinText2.Text;
            Properties.Settings.Default.reinText3Default = this.reinText3.Text;
            Properties.Settings.Default.reinText4Default = this.reinText4.Text;
            Properties.Settings.Default.reinPointInDefault = (bool)this.reinPointIn.IsChecked;
            Properties.Settings.Default.scaleFactorDefault = this.scaleFactor.Text;
            Properties.Settings.Default.offsetDistanceDefault = this.offsetDistance.Text;
            Properties.Settings.Default.reinPointsTextDefault = this.reinPointsText.Text;
            Properties.Settings.Default.scalePointsFactorDefault = this.scalePointsFactor.Text;
            Properties.Settings.Default.Save();
            e.Cancel = false;
        }

        private void confirmLinesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsNumber(this.offsetDistance.Text))
            {
                MessageBox.Show("请填写正确的偏移距离！");
                DialogResult = false;
            }
            OffsetDistance = Convert.ToDouble(this.offsetDistance.Text);
            CommandToExecute = Command.CreateReinLines;
            DialogResult = true;
        }

        private void cancelLinesButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void confirmPointsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsNumber(this.reinPointsText.Text)|| !IsNumber(this.scalePointsFactor.Text))
            {
                MessageBox.Show("请填写正确的钢筋直径及比例信息！");
                DialogResult = false;
            }
            PointsScale = 1 / Convert.ToDouble(this.scalePointsFactor.Text);
            ReinPointsValue = this.reinPointsText.Text;
            CommandToExecute = Command.CreateReinPoints;
            DialogResult = true;
        }

        private void cancelPointsButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void confirmButton_Click(object sender, RoutedEventArgs e)
        {
            ReinValueArray = new string[] { this.reinText1.Text, this.reinText2.Text, this.reinText3.Text, this.reinText4.Text }.Select(s=>"%%133"+s).ToArray();
            ReinPointIn = (bool)this.reinPointIn.IsChecked;
            if (!IsNumber(this.scaleFactor.Text))
            {
                MessageBox.Show("请填写正确的比例信息！");
                DialogResult = false;
            }
            Scale = 1 / Convert.ToDouble(this.scaleFactor.Text);
            if (ReinValueArray.Any(x => (x == null || x == "")))
            {
                MessageBox.Show("配筋信息不能为空！");
                DialogResult = false;
            }
            CommandToExecute = Command.CreateReinTexts;
            DialogResult = true;
        }

        private void reverseButton_Click(object sender, RoutedEventArgs e)
        {
            string[] tempArray = new string[] { this.reinText1.Text, this.reinText2.Text, this.reinText3.Text, this.reinText4.Text };
            this.reinText1.Text = tempArray[3];
            this.reinText2.Text = tempArray[2];
            this.reinText3.Text = tempArray[1];
            this.reinText4.Text = tempArray[0];
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private bool IsNumber(string str)
        {
            if (str == null || str == "") return false;
            System.Text.RegularExpressions.Regex rex = new System.Text.RegularExpressions.Regex(@"^-?\d*[.]?\d*$");
            return rex.IsMatch(str);
        }
    }
}
