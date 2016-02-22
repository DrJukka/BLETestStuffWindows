using BeaconScanner.Controls;
using BeaconScanner.Engine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace BeaconScanner
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ManufacturerBox.Text = AppSettings.ManufacturerCode.ToString();
            BeaconcodeBox.Text = AppSettings.BeaconCode.ToString();
            listeningCycleSlider.Value = AppSettings.Lifecycle;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            AppSettings.Lifecycle = listeningCycleSlider.Value;

            UInt16 manCode;
            if(UInt16.TryParse(ManufacturerBox.Text ,out manCode)){
                AppSettings.ManufacturerCode = manCode;
            }

            UInt16 beaconCode;
            if (UInt16.TryParse(BeaconcodeBox.Text, out beaconCode))
            {
                AppSettings.BeaconCode = beaconCode;
            }
        }

        private void listeningCycleSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        { 
            listeningCycleValue.Text = listeningCycleSlider.Value.ToString();
        }
    }
}
