
using HeartbeatFg.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace HeartbeatFg
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HeartBeatPage : Page
    {
        private List<HeartbeatMeasurement> _data = null;

        public HeartBeatPage()
        {
            this.InitializeComponent();
            _data = new List<HeartbeatMeasurement>();
        }

        /*
        App to app launchn URI can be fetched with
        ProtocolActivatedEventArgs launchArgs = e.Parameter as ProtocolActivatedEventArgs;
        and we could do something about them if we would want to
        */
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            SetWaitVisibility(true);
            DeviceName.Text = AppSettings.SelectedDeviceName;
            _data.Clear();

            progressIndicator.Text = "Connecting...";

            System.Diagnostics.Debug.WriteLine("OnNavigatedTo");

            HeartBeatEngine.Instance.DeviceConnectionUpdated += Instance_DeviceConnectionUpdated;
            HeartBeatEngine.Instance.ValueChangeCompleted += Instance_ValueChangeCompleted;
            HeartBeatEngine.Instance.InitializeServiceAsync(AppSettings.SelectedDeviceId);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("OnNavigatedFrom");

            HeartBeatEngine.Instance.DeviceConnectionUpdated -= Instance_DeviceConnectionUpdated;
            HeartBeatEngine.Instance.ValueChangeCompleted -= Instance_ValueChangeCompleted;
            HeartBeatEngine.Instance.Deinitialize();

            base.OnNavigatedFrom(e);
        }

        private async void Instance_DeviceConnectionUpdated(bool isConnected, string error)
        {
            // Serialize UI update to the the main UI thread.
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (error != null)
                {
                    ShowErrorDialog(error, "Connect error.");
                }

                if (isConnected)
                {
                    progressIndicator.Text = "Waiting for data...";
                }
            });
        }

        private async void Instance_ValueChangeCompleted(HeartbeatMeasurement HeartbeatMeasurementValue)
        {
            // Serialize UI update to the the main UI thread.
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (progressGrid.Visibility == Visibility.Visible)
                {
                    SetWaitVisibility(false);
                }

                // lets put the value into the UI control
                HeartbeatValueBox.Text = "" + HeartbeatMeasurementValue.HeartbeatValue;

                //we need to store it in an array in order to visualize the values with graph
                _data.Add(HeartbeatMeasurementValue);

                if (_data.Count >= 2)
                {
                    // and we have our custom control to show the graph
                    // this does not draw well if there is only one value, thus using it only after we got at least two values
                    outputDataChart.PlotChart(_data.ToArray());
                }
            });
        }

        private void SetWaitVisibility(bool waitVisible)
        {
            progressGrid.Visibility = waitVisible ? Visibility.Visible : Visibility.Collapsed;
            valuesGrid.Visibility = waitVisible ? Visibility.Collapsed : Visibility.Visible;
        }


        private async void ShowErrorDialog(string message, string title)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                var dialog = new MessageDialog(message, title);
                await dialog.ShowAsync();
            });
        }
    }
}
