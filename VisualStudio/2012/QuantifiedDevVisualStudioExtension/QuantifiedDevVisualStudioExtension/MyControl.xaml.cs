using System.Globalization;
using System.Windows;

namespace QuantifiedDev.QuantifiedDevVisualStudioExtension
{
    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    public partial class MyControl
    {
        public MyControl()
        {
            InitializeComponent();
            latitude.Text = Settings.Default.Latitude.ToString(CultureInfo.InvariantCulture);
            longitude.Text = Settings.Default.Longitude.ToString(CultureInfo.InvariantCulture);
            streamId.Text = Settings.Default.StreamId;
            readToken.Text = Settings.Default.ReadToken;
            info.Text = Settings.Default.InfoText;

        }

        private void Save(object sender, RoutedEventArgs e)
        {
            Settings.Default.Latitude = double.Parse(latitude.Text);
            Settings.Default.Longitude = double.Parse(longitude.Text);
            Settings.Default.Save();
        }
    }
}