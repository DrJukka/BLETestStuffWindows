using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Background;
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

namespace BluetoothAppBG
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private BackgroundTaskProgressEventHandler _progressEventHandler = null;
        private BackgroundTaskCompletedEventHandler _taskCompletedHandler = null;

        public MainPage()
        {
            this.InitializeComponent();
            updateBGStatus();
         
           _progressEventHandler = new BackgroundTaskProgressEventHandler(OnProgress);
            _taskCompletedHandler = new BackgroundTaskCompletedEventHandler(OnCompleted);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            updateBGStatus();
            
            BackgroundManager.RegisterBackgroundTaskEventHandlers(_taskCompletedHandler, _progressEventHandler);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            BackgroundManager.UnRegisterBackgroundTaskEventHandlers(_taskCompletedHandler, _progressEventHandler);
        }


        private async void startStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (BackgroundManager.IsBackgroundTaskRegistered())
            {
                BackgroundManager.UnregisterBackgroundTask();
                BackgroundManager.UnRegisterBackgroundTaskEventHandlers(_taskCompletedHandler, _progressEventHandler);
            }
            else
            {
                await BackgroundManager.RegisterBackgroundTask(CommonData.GUID, CommonData.ServiceName, CommonData.ServiceDescriptor);
                BackgroundManager.RegisterBackgroundTaskEventHandlers(_taskCompletedHandler, _progressEventHandler);
            }

            updateBGStatus();
        }

        private void updateBGStatus()
        {
            if (BackgroundManager.IsBackgroundTaskRegistered())
            {
                statuxBox.Text = "Running";
            }
            else
            {
                statuxBox.Text = "Stopped";
            }
        }

        private async void OnProgress(BackgroundTaskRegistration sender, BackgroundTaskProgressEventArgs args)
        {
            // Serialize UI update to the the main UI thread.
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ShowErrorDialog(CommonData.LastMessage,"Got message");

            });
        }
        private async void OnCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            // Serialize UI update to the the main UI thread.
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ShowErrorDialog(CommonData.TaskExitReason, "BgTask exit");
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
