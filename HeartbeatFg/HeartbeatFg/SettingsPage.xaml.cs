
using HeartbeatFg.Engine;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace HeartbeatFg
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();

            System.Diagnostics.Debug.WriteLine("MinHeartbeatValue : " + AppSettings.MinHeartbeatValue);
            System.Diagnostics.Debug.WriteLine("MaxHeartbeatValue : " + AppSettings.MaxHeartbeatValue);

            MinTextBox.Text = "" + AppSettings.MinHeartbeatValue;
            MaxTextBox.Text = "" + AppSettings.MaxHeartbeatValue;
        }

        private void MinMaxButton_Click(object sender, RoutedEventArgs e)
        {
            ushort changeValue = 1;

            if (sender == MinMinusButton && (AppSettings.MinHeartbeatValue > 1))
            {
                AppSettings.MinHeartbeatValue = (ushort)(AppSettings.MinHeartbeatValue - changeValue);
                MinTextBox.Text = "" + AppSettings.MinHeartbeatValue;
            }

            if (sender == MinPlusButton && (AppSettings.MinHeartbeatValue < (AppSettings.MaxHeartbeatValue - 1)))
            {
                AppSettings.MinHeartbeatValue = (ushort)(AppSettings.MinHeartbeatValue + changeValue);
                MinTextBox.Text = "" + AppSettings.MinHeartbeatValue;
            }

            if (sender == MaxMinusButton && (AppSettings.MaxHeartbeatValue > (AppSettings.MinHeartbeatValue + 1)))
            {
                AppSettings.MaxHeartbeatValue = (ushort)(AppSettings.MaxHeartbeatValue - changeValue);
                MaxTextBox.Text = "" + AppSettings.MaxHeartbeatValue;
            }

            if (sender == MaxPlusButton && (AppSettings.MaxHeartbeatValue < 250))
            {
                AppSettings.MaxHeartbeatValue = (ushort)(AppSettings.MaxHeartbeatValue + changeValue);
                MaxTextBox.Text = "" + AppSettings.MaxHeartbeatValue;
            }
        }
    }
}
