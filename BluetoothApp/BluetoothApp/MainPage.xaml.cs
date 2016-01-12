using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using BluetoothApp.Model;
using Windows.UI.Popups;
using BluetoothApp.Engine;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BluetoothApp
{
    
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("main -- OnNavigatedTo");
            base.OnNavigatedTo(e);
            ReFreshDevicesList();

            //start advertising our bt & start listening for incoming connections
            BTEngine.Instance.InitializeReceiver();
            BTEngine.Instance.ObexConnectionStatusChanged += Instance_ObexConnectionStatusChanged;
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            BTEngine.Instance.ObexConnectionStatusChanged -= Instance_ObexConnectionStatusChanged;
        }

        private async void Instance_ObexConnectionStatusChanged(bool connected, string error)
        {
            System.Diagnostics.Debug.WriteLine("ObexConnectionStatusChanged : " + connected + ", error: " + error);

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (error != null)
                {
                    ShowErrorDialog("Connection error : " + error, "Connection error");
                    return;
                }

                if (connected)
                {
                    this.Frame.Navigate(typeof(BtConnectionPage));
                }
            });
        }

        private async void ReFreshDevicesList()
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
            var items = new List<DeviceViewModel>();

                try
                {
                    System.Diagnostics.Debug.WriteLine("GetDeviceSelector");

                    string device1 = RfcommDeviceService.GetDeviceSelector(RfcommServiceId.FromUuid(BTEngine.Instance.BTGUID));
                    System.Diagnostics.Debug.WriteLine("FindAllAsync : " + device1);
                    // System.Devices.InterfaceClassGuid:="{B142FC3E-FA4E-460B-8ABC-072B628B3C70}" AND System.DeviceInterface.Bluetooth.ServiceGuid:="{00001101-0000-1000-8000-00805F9B34FB}" AND System.Devices.InterfaceEnabled:=System.StructuredQueryType.Boolean#True
                    //string device1 = "System.Devices.InterfaceClassGuid:=\"{B142FC3E-FA4E-460B-8ABC-072B628B3C70}\"";
                    var devices = await DeviceInformation.FindAllAsync(device1);

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
                }catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("GetDeviceSelector error : " + ex.Message);
                }
                DeviceSelectionListView.ItemsSource = items;

                noDevicesLabel.Visibility = items.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                DeviceSelectionListView.Visibility = items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private async void DeviceSelectionListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            DeviceViewModel selectedDevice = (DeviceViewModel)e.ClickedItem;

            if (selectedDevice == null)
            {
                ShowErrorDialog("No device selected", "Device selection error");
                return;
            }
            System.Diagnostics.Debug.WriteLine("Device " + selectedDevice.Name);

            RfcommDeviceService BtDevice = await RfcommDeviceService.FromIdAsync(selectedDevice.Id);
            if (BtDevice == null)
            {
                ShowErrorDialog("Didn't find the specified bluetooth device", "Device selection error");
                return;
            }

            string nameAttribute = await BTEngine.Instance.GetNameAttribute(BtDevice);
            if (nameAttribute.Length <= 0)
            {
                ShowErrorDialog("The specified bluetooth device is not having right version for our communications", "Device selection error");
            }

            System.Diagnostics.Debug.WriteLine("Bt attribute : " + nameAttribute);

            BTEngine.Instance.SelectedDevice = BtDevice;
            BTEngine.Instance.ConnectToDevice(BTEngine.Instance.SelectedDevice);
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            ReFreshDevicesList();
        }

        private async void openSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));
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
