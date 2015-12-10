using Common;
using Windows.UI.Xaml;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Storage;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace HeartbeatBg
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HeartBeatPage : Page
    {
        private List<HeartbeatMeasurement> _data = null;
        private BackgroundTaskProgressEventHandler _progressEventHandler = null;

        public HeartBeatPage()
        {
            this.InitializeComponent();
            _data = new List<HeartbeatMeasurement>();
            _progressEventHandler = new BackgroundTaskProgressEventHandler(OnProgress);
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

            if (!BackgroundManager.RegisterBackgroundTaskEventHandlers(null, _progressEventHandler))
            {
                ShowErrorDialog("Plese go back and select device", "Device not selected");
                return;
            }
 
            // todo : we should have timer here, that brings error note if we don't get data from background task in x seconds
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("OnNavigatedFrom");

            //remove background task event handlers
            BackgroundManager.UnRegisterBackgroundTaskEventHandlers(null, _progressEventHandler);
            base.OnNavigatedFrom(e);
        }
        private void SetWaitVisibility(bool waitVisible)
        {
            progressGrid.Visibility = waitVisible ? Visibility.Visible : Visibility.Collapsed;
            valuesGrid.Visibility = waitVisible ? Visibility.Collapsed : Visibility.Visible;
        }

        // we are mis-using this function, as it is our only direct way on getting data back from our background task, 
        // we use the UInt32 Progress to forward the heartbeat value to the UI app.
        private async void OnProgress(BackgroundTaskRegistration sender, BackgroundTaskProgressEventArgs args)
        {
            // Serialize UI update to the the main UI thread.
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                SetWaitVisibility(false);

                // lets put the value into the UI control
                HeartbeatValueBox.Text = "" + args.Progress;

                //we need to store it in an array in order to visualize the values with graph
                _data.Add(new HeartbeatMeasurement
                {
                    HeartbeatValue = (ushort)args.Progress,
                    Timestamp = DateTimeOffset.Now
                });

                if (_data.Count >= 2)
                {
                    // and we have our custom control to show the graph
                    // this does not draw well if there is only one value, thus using it only after we got at least two values
                    outputDataChart.PlotChart(_data.ToArray());
                }
            });
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
