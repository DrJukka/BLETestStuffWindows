
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
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HeartbeatFg
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
            ReFreshDevicesList();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }
    
        private async void ReFreshDevicesList()
        {
            
            System.Diagnostics.Debug.WriteLine("FindAllAsync hearth : " + GattServiceUuids.HeartRate);
            System.Diagnostics.Debug.WriteLine("FindAllAsync hearth : " + GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.HeartRate));

            String findStuff = "System.DeviceInterface.Bluetooth.ServiceGuid:= \"{0000180D-0000-1000-8000-00805F9B34FB}\" AND System.Devices.InterfaceEnabled:= System.StructuredQueryType.Boolean#True";

            var devices = await DeviceInformation.FindAllAsync(findStuff);
            
            // this is the right way, stuff above is for debugging untill things works
          //  var devices = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.HeartRate));
            var items = new List<DeviceViewModel>();

            if (devices != null && devices.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine("FindAllAsync devices.Count : " + devices.Count);
                foreach (DeviceInformation device in devices)
                {
                    if (device != null && device.Kind == DeviceInformationKind.DeviceInterface)
                    {
                        System.Diagnostics.Debug.WriteLine("Found : " + device.Name + ", id: " + device.Id);
                        items.Add(new DeviceViewModel(device));
                    }
                }
            }
            DeviceSelectionListView.ItemsSource = items;

            noDevicesLabel.Visibility = items.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            DeviceSelectionListView.Visibility = items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void DeviceSelectionListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            DeviceViewModel selDevice = (DeviceViewModel)e.ClickedItem;

            AppSettings.SelectedDeviceName = selDevice.Name;
            AppSettings.SelectedDeviceId = selDevice.Id;

            System.Diagnostics.Debug.WriteLine("Device " + selDevice.Name + " selected, now navigating to HeartBeatPage");
            this.Frame.Navigate(typeof(HeartBeatPage)); //selDevice.Id
        }

        private void settingsButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            ReFreshDevicesList();
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
