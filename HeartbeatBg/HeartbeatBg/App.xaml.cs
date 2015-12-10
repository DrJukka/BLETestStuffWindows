using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
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


namespace HeartbeatBg
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
                Microsoft.ApplicationInsights.WindowsCollectors.Metadata |
                Microsoft.ApplicationInsights.WindowsCollectors.Session);
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }


        private async void App_BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                return;
            }

            if (rootFrame.SourcePageType.Equals(typeof(HeartBeatPage)))
            {
                if (BackgroundManager.IsBackgroundTaskRegistered())
                {
                    e.Handled = true;

                    //Todo, consider movcing this to be handled inside the UI, not in external popup
                    // so we are going back from HeartBeatPage and we have active background task
                    // so we need to ask whether we want to keep it or not
                    var dlg = new MessageDialog("Stop Background heartbeat monitoring ?");
                    dlg.Commands.Add(new UICommand("Yes", null, "YES"));
                    dlg.Commands.Add(new UICommand("No", null, "NO"));
                    var op = await dlg.ShowAsync();
                    if ((string)op.Id == "YES")
                    {
                        BackgroundManager.UnregisterBackgroundTask();

                        if (rootFrame.CanGoBack)
                        {   // if we started with Mainpage, we'll just  go back
                            rootFrame.GoBack();
                        }
                        else
                        {   //but if we started with Heartbeat page, we actually need to navigate to the mainpage
                            rootFrame.Navigate(typeof(MainPage));
                        }
                    }
                    else
                    {
                        // lets let the background task run, while UI goes out
                        Exit();
                    }

                    return;
                }
                else if (rootFrame.CanGoBack)
                {//this would be error situation, where we went to HeartBeatPage without active background task
                    rootFrame.GoBack();
                }
            }

            if (rootFrame.SourcePageType.Equals(typeof(MainPage)))
            {
                e.Handled = true;
                // from mainview we simply exit
                Exit();
                return;
            }

            // if we are here, we should just go back if it is possible
            if (rootFrame.CanGoBack && !e.Handled)
            {
                e.Handled = true;
                rootFrame.GoBack();
            }
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.Protocol)
            {
                ProtocolActivatedEventArgs eventArgs = args as ProtocolActivatedEventArgs;

                Frame rootFrame = new Frame();
                if (rootFrame.Content == null)
                {
                    NavigateToStarPage(rootFrame, eventArgs);
                }

                // Ensure the current window is active
                Window.Current.Activate();

                System.Diagnostics.Debug.WriteLine("OnActivated Uri: " + eventArgs.Uri);
            }
        }

        private void NavigateToStarPage(Frame rootFrame, System.Object parameter)
        {
            //if we have already active background task, we can go strait viewing values from it
            if (BackgroundManager.IsBackgroundTaskRegistered())
            {
                if (!rootFrame.Navigate(typeof(HeartBeatPage), parameter))
                {
                    throw new Exception("Failed to create initial page");
                }

                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            }
            else
            {
                if (!rootFrame.Navigate(typeof(MainPage), parameter))
                {
                    throw new Exception("Failed to create initial page");
                }

                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            }
        }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;
                rootFrame.Navigated += RootFrame_Navigated;
                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                NavigateToStarPage(rootFrame, e.Arguments);

                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
            }
            // Ensure the current window is active
            Window.Current.Activate();
            Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
        }

        private void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (((Frame)sender).SourcePageType.Equals(typeof(HeartBeatPage))){
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                return;
            }

            // Each time a navigation event occurs, update the Back button's visibility
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
            ((Frame)sender).CanGoBack ?
            AppViewBackButtonVisibility.Visible :
            AppViewBackButtonVisibility.Collapsed;
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("******************************OnSuspending");
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
