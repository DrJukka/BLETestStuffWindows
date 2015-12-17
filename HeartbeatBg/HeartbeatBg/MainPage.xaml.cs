using Common;
using Common.Model;
using System;
using System.Collections.Generic;
using Windows.Data.Xml.Dom;
using Windows.ApplicationModel.Activation;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HeartbeatBg
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            ReFreshDevicesList();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            BLEDeviceEngine.Instance.DeviceConnectionUpdated += Instance_DeviceConnectionUpdated;
            ReFreshDevicesList();
            SetWaitVisibility(false);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            BLEDeviceEngine.Instance.DeviceConnectionUpdated -= Instance_DeviceConnectionUpdated;
            BLEDeviceEngine.Instance.Deinitialize();
        }
        private void SetWaitVisibility(bool waitVisible)
        {
            progressGrid.Visibility = waitVisible ? Visibility.Visible : Visibility.Collapsed;
            RootGrid.Visibility = waitVisible ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void Instance_DeviceConnectionUpdated(bool isConnected, string error)
        {
            System.Diagnostics.Debug.WriteLine("Instance_DeviceConnectionUpdated : " + isConnected);
            if (isConnected)
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    progressRing.Text = "Registering Background service";
                    System.Diagnostics.Debug.WriteLine("Device is now conneted, registering background task");

                    string retValue = BackgroundManager.RegisterBackgroundTask(BLEDeviceEngine.Instance.Characteristic);
                    SetWaitVisibility(false);

                    if (retValue != null)
                    {
                        ShowErrorDialog(retValue, "BackgroundTask error");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine("Navigating to HeartBeatPage");
                    this.Frame.Navigate(typeof(HeartBeatPage));
                });
            }
            else
            {
                SetWaitVisibility(false);
                ShowErrorDialog("Failed to connect to the device, reason : " + error,"Device Connection");
            }
        }


        private async void ReFreshDevicesList()
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                var devices = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.HeartRate));
                var items = new List<DeviceViewModel>();

                if (devices != null && devices.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("FindAllAsync devices.Count : " + devices.Count);
                    foreach (DeviceInformation device in devices)
                    {
                        if (device != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Found : " + device.Name + ", id: " + device.Id);
                            items.Add(new DeviceViewModel(device));
                        }
                    }
                }
                DeviceSelectionListView.ItemsSource = items;

                noDevicesLabel.Visibility = items.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                DeviceSelectionListView.Visibility = items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private void DeviceSelectionListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            SetWaitVisibility(true);

            DeviceViewModel selDevice = (DeviceViewModel)e.ClickedItem;
            System.Diagnostics.Debug.WriteLine("Device " + selDevice.Name + " selected, now initializing");

            //lets save the name here, so we can show it later on
            AppSettings.SelectedDeviceName = selDevice.Name;

            BLEDeviceEngine.Instance.InitializeServiceAsync(selDevice.Id, GattCharacteristicUuids.HeartRateMeasurement);
            // todo, we should have wait dialog here to show that we are connecting
        }

        private void settingsButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            ReFreshDevicesList();
        }
        private void setTile_Click(object sender, RoutedEventArgs e)
        {
            LiveTile.CreateSecoondaryTile();
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
