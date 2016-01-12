using BluetoothApp.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace BluetoothApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BtConnectionPage : Page
    {
        public BtConnectionPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (BTEngine.Instance.SelectedDevice != null)
            {
                DeviceName.Text = BTEngine.Instance.SelectedDevice.Device.Name;
            }

            if (BTEngine.Instance.Socket != null)
            {
                SocketName.Text = BTEngine.Instance.Socket.Information.RemoteHostName.DisplayName;
            }

            BTEngine.Instance.ObexConnectionStatusChanged += Instance_ObexConnectionStatusChanged;
            BTEngine.Instance.ObexErrorCallback += Instance_ObexErrorCallback;
            BTEngine.Instance.ObexMessage += Instance_ObexMessage;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            BTEngine.Instance.ObexConnectionStatusChanged -= Instance_ObexConnectionStatusChanged;
            BTEngine.Instance.ObexErrorCallback -= Instance_ObexErrorCallback;
            BTEngine.Instance.ObexMessage -= Instance_ObexMessage;
            BTEngine.Instance.DeInitialize();
        }

        private async void Instance_ObexConnectionStatusChanged(bool connected, string error)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                //We are disconnected
                if (!connected)
                {
                    if (this.Frame.CanGoBack)
                    {
                        this.Frame.GoBack();
                    }
                }
            });
        }

        private void Instance_ObexMessage(string message)
        {
            ShowErrorDialog(message, "Got message");
        }

        private void Instance_ObexErrorCallback(string error)
        {
            ShowErrorDialog(error,"Obex error");
        }

        private async void ShowErrorDialog(string message, string title)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                var dialog = new MessageDialog(message, title);
                await dialog.ShowAsync();
            });
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            BTEngine.Instance.SendData(MessageBox.Text);
        }
    }
}
