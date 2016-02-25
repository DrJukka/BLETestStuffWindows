using BeaconScanner.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.Advertisement;
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
using System.Collections.ObjectModel;
using Windows.System.Threading;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BeaconScanner
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const UInt16 MAXLISTENINGCYCLES = 100;

        private ThreadPoolTimer _periodicTimer = null;
        private int _listeningCycleCounter = 0;

        public ObservableCollection<Beacon> BeaconCollection
        {
            get;
            set;
        }

        public ObservableCollection<Beacon> tmpCollection 
        {
            get;
            set;
        }

        public MainPage()
        {
            this.InitializeComponent();
            BeaconCollection = new ObservableCollection<Beacon>();
            BeaconListBox.ItemsSource = BeaconCollection;
            ProgressView.CancelTest += CancelTest;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            (Application.Current as App).canGobackAndExit += canGobackAndExit;

            BeaconEngine.Instance.Discovered += BeaconEngine_Discovered;
            BeaconEngine.Instance.Stopped += BeaconEngine_Stopped;
            BeaconEngine.Instance.Start(AppSettings.ManufacturerCode, AppSettings.BeaconCode);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            (Application.Current as App).canGobackAndExit -= canGobackAndExit;

            BeaconEngine.Instance.Stop();
            BeaconEngine.Instance.Discovered -= BeaconEngine_Discovered;
            BeaconEngine.Instance.Stopped -= BeaconEngine_Stopped;
        }

        public bool canGobackAndExit()
        {
            if (resultOverlay.Visibility == Visibility.Visible)
            {
                resultOverlay.Visibility = Visibility.Collapsed;
                return false;
            }

            if (ProgressView.Visibility == Visibility.Visible)
            {
                CancelTest();
                return false;
            }

            //nothing to handle here, so back can do default stuff
            return true;
        }
        public async void BeaconEngine_Stopped(BluetoothLEAdvertisementWatcherStatus status)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ShowMessageDialog("Beacon Engine Stopped : " + status);
            });
        }
        public async void BeaconEngine_Discovered(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            //System.Diagnostics.Debug.WriteLine("Found Beacon : " + args.BluetoothAddress);

            //the test is progressing, so we do minimun amount of work here
            //and simply mark the beacon we got as seen during this test cycle
            if(_periodicTimer != null)
            {
                foreach (Beacon beacon in tmpCollection)
                {
                    if(beacon.BluetoothAddress == args.BluetoothAddress){
                        if (_listeningCycleCounter < beacon.DiscoveryTestData.Length){
                            beacon.DiscoveryTestData[_listeningCycleCounter] = true;
                            return;
                        }
                    }
                }
                //if it was some beacon we do not have in our list, then we simply ignore it
                return;
            }

            //beaconlist is only updated while we are not doing test
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Beacon beacon = BeaconFactory.BeaconFromBluetoothLEAdvertisementReceivedEventArgs(args);
                if(beacon == null)
                {
                    return;
                }

                bool existingBeaconUpdated = false;

                foreach (Beacon existingBeacon in BeaconCollection)
                {
                    if (existingBeacon.Update(beacon))
                    {
                        existingBeaconUpdated = true;
                        beacon.Dispose();
                    }
                }

                if (!existingBeaconUpdated)
                {
                    BeaconCollection.Add(beacon);
                }
            });   
        }

        private void ReStart_AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            ReStart_beaconDiscovery();
        }

        private void ReStart_beaconDiscovery()
        {
            BeaconEngine.Instance.Stop();
            BeaconCollection = new ObservableCollection<Beacon>();
            BeaconListBox.ItemsSource = BeaconCollection;
            BeaconEngine.Instance.Start(AppSettings.ManufacturerCode, AppSettings.BeaconCode);
        }

        private void Setting_AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }

        private void Start_AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if(_periodicTimer != null)
            {
                _periodicTimer.Cancel();
                _periodicTimer = null;
            }

            // we use temporal copy of beaocnlist for testing purposes
            tmpCollection = new ObservableCollection<Beacon>();
            foreach (Beacon beacon in BeaconCollection)
            {
                // store the listening cycle, so we can show it in the results
                beacon.ListeningCycle = AppSettings.Lifecycle;
                tmpCollection.Add(BeaconFactory.DublicateBeacon(beacon));
            }

            //reset counter
            _listeningCycleCounter = 0;

            //and start the timer
            _periodicTimer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(PeriodicTimerCallback), 
                TimeSpan.FromMilliseconds(AppSettings.Lifecycle));

            //show the test overlay
            ProgressView.Visibility = Visibility.Visible;
        }

        private async void PeriodicTimerCallback(ThreadPoolTimer timer)
        {
            _listeningCycleCounter++;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                //update the progress into the test overlay
                ProgressView.CounterValue = "" + _listeningCycleCounter + "/" + MAXLISTENINGCYCLES;
            });

            //are we done with the test yet
            if (_listeningCycleCounter >= MAXLISTENINGCYCLES)
            {
                //done testing
                _periodicTimer.Cancel();
                _periodicTimer = null;

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {                    
                    //Set the test time temp array to the listview 
                    BeaconCollection = tmpCollection;
                    BeaconListBox.ItemsSource = BeaconCollection;
                    //hide the text overlay
                    ProgressView.Visibility = Visibility.Collapsed;
                });
            }
        }
        private void CancelTest()
        {
            if (_periodicTimer != null)
            {
                _periodicTimer.Cancel();
                _periodicTimer = null;
            }

            ProgressView.Visibility = Visibility.Collapsed;
        }

        private void BeaconListBox_ItemClick(object sender, ItemClickEventArgs e)
        {
            Beacon selectedBeacon = (Beacon)e.ClickedItem;
            if(selectedBeacon == null)
            {
                return;
            }

            resultOverlay.Visibility = Visibility.Visible;
            uidValueBox.Text = selectedBeacon.Id1;
            discoveryResultControl.Results = selectedBeacon.DiscoveryTestData;

            int successvalues = 0;
            int failedvalues = 0;

            bool lastWasSuccess = false;
            int currentFailCount = 0;
            int failPeriodCount = 0;
            int longestFail = 0;
                
            for (int i = 0; i < selectedBeacon.DiscoveryTestData.Count(); i++)
            {
                lastWasSuccess = selectedBeacon.DiscoveryTestData[i];
                if (lastWasSuccess)
                {
                    successvalues++;
                    if(currentFailCount > 0)
                    {
                        if(longestFail < currentFailCount)
                        {
                            longestFail = currentFailCount;
                        }
                        failPeriodCount++;

                        currentFailCount = 0;
                    }
                }
                else
                {
                    if (!lastWasSuccess)
                    {
                        currentFailCount++;
                    }

                    failedvalues++;
                }

            }
            listeningCycleBox.Text = selectedBeacon.ListeningCycle + " ms.";
            successFailsBox.Text = successvalues + " / " + failedvalues;
            longestFailBox.Text = longestFail.ToString();
            averageFailBox.Text = (failPeriodCount > 0) ? ((int)(failedvalues / failPeriodCount)).ToString() : "0";
        }

        private async void ShowMessageDialog(String message)
        {
            MessageDialog messageDialog = new MessageDialog(message);
            await messageDialog.ShowAsync();
        }

        private void closeResult_Click(object sender, RoutedEventArgs e)
        {
            resultOverlay.Visibility = Visibility.Collapsed;
        }
    }
}
