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

        }

        private void Save(object sender, RoutedEventArgs e)
        {
            Settings.Default.Latitude = double.Parse(latitude.Text);
            Settings.Default.Longitude = double.Parse(longitude.Text);
            Settings.Default.Save();
        }

        private void Register(object sender, RoutedEventArgs e)
        {
            string uri = string.Format("https://app.quantifieddev.org/dashboard?streamId={0}&readToken={1}", Settings.Default.StreamId, Settings.Default.ReadToken);
            System.Diagnostics.Process.Start("iexplore.exe", uri);
            
        }
    }
}