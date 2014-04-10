using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QuantifiedDev.QuantifiedDevVisualStudioExtension
{
    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    public partial class MyControl : UserControl
    {
        public MyControl()
        {
            InitializeComponent();
            latitude.Text = Settings.Default.Latitude.ToString();
            longitude.Text = Settings.Default.Longitude.ToString();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "We are inside {0}.button1_Click()", this.ToString()),
                            "QuantifiedDevTool");

        }

        private void Save(object sender, RoutedEventArgs e)
        {
            Settings.Default.Latitude = double.Parse(latitude.Text);
            Settings.Default.Longitude = double.Parse(longitude.Text);
            Settings.Default.Save();
        }
    }
}