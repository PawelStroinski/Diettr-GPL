﻿using System;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Telerik.Windows.Controls;
using Dietphone.Views;
using Dietphone.BinarySerializers;
using Dietphone.Tools;
using MvvmCross.Platform;
using MvvmCross.Core.ViewModels;

namespace Dietphone
{
    public partial class App : Application
    {
        public RadPhoneApplicationFrame RootFrame { get; private set; }
        private bool phoneApplicationInitialized = false;

        public App()
        {
            UnhandledException += Application_UnhandledException;
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters
                //Application.Current.Host.Settings.EnableFrameRateCounter = true;
                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;
                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are being GPU accelerated with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;
            }
            // Standard Silverlight initialization
            InitializeComponent();
            // Phone-specific initialization
            InitializePhoneApplication();
            var setup = new Setup(RootFrame);
            setup.Initialize();
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            InitializeStreamProvider();
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            InitializeStreamProvider();
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            SaveFactories();
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            SaveFactories();
        }

        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
            else
            {
                var exception = e.ExceptionObject;
                SendExceptionQuestion(exception.ToString());
                e.Handled = true;
            }
        }
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;
            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new RadPhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;
            RootFrame.Navigating += RootFrame_Navigating;
            phoneApplicationInitialized = true;
        }

        private void InitializeStreamProvider()
        {
            MyApp.StreamProvider = new PhoneBinaryStreamProvider(new IsolatedFileFactory());
        }

        private static void SaveFactories()
        {
            var factories = MyApp.Factories;
            factories.Save();
        }

        private void SendExceptionQuestion(string exception)
        {
            if (MessageBox.Show(exception, Translations.BugInAppReportToTheAuthor,
                MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                SendException(exception);
            }
        }

        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
            else
            {
                var exception = e.Exception;
                SendExceptionQuestion(exception.ToString());
                e.Handled = true;
            }
        }

        private void RootFrame_Navigating(object sender, NavigatingCancelEventArgs args)
        {
            args.Cancel = true;
            RootFrame.Navigating -= RootFrame_Navigating;
            RootFrame.Dispatcher.BeginInvoke(() => { Mvx.Resolve<IMvxAppStart>().Start(); });
        }

        private void SendException(string exception)
        {
            EmailComposeTask task = new EmailComposeTask();
            task.To = "wp7@pabloware.com";
            task.Body = String.Format(Translations.IWouldLikeToReportTheFollowingBug, exception);
            task.Subject = Translations.Bug;
            task.Show();
        }
    }
}