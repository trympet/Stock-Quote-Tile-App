using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Web.Syndication;
using Windows.UI.Popups;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.UI.Core.Preview;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            SystemNavigationManagerPreview mgr =
            SystemNavigationManagerPreview.GetForCurrentView();
            mgr.CloseRequested += SystemNavigationManager_CloseRequested;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.RegisterBackgroundTask();
            base.OnNavigatedTo(e);
        }


        private async void RegisterBackgroundTask()
        {
            var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            if (backgroundAccessStatus == BackgroundAccessStatus.AllowedSubjectToSystemPolicy ||
                backgroundAccessStatus == BackgroundAccessStatus.AlwaysAllowed)
            {
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == taskName)
                    {
                        task.Value.Unregister(true);
                    }
                }

                BackgroundTaskBuilder taskBuilder = new BackgroundTaskBuilder();
                taskBuilder.Name = taskName;
                taskBuilder.TaskEntryPoint = taskEntryPoint;
                taskBuilder.SetTrigger(new TimeTrigger(15, false));
                taskBuilder.Register();
            } else
            {
                FuckYouDiag();
            }
        }

        private async void RegisterAppTrigger()
        {
            var requestStatus = await Windows.ApplicationModel.Background.BackgroundExecutionManager.RequestAccessAsync();
            if (requestStatus != BackgroundAccessStatus.AlwaysAllowed)
            {
                // Depending on the value of requestStatus, provide an appropriate response
                // such as notifying the user which functionality won't work as expected
            }
        }
        
        private async void FuckYouDiag(IUICommand command = null)
        {
            var diag = new MessageDialog("What the fuck dude. I can't even run in the background. How am I supposed to fetch stonks???");
            // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
            diag.Commands.Add(new UICommand(
                "Fuck you"));
            await diag.ShowAsync();
        }

        private const string taskName = "StockBackgroundTask";
        private const string taskEntryPoint = "BackgroundTasks.StockBackgroundTask";

        private void TextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }

        private async void SystemNavigationManager_CloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            int closeNum = 1;
            Deferral deferral = e.GetDeferral();
            if (false)
            {
                // user cancelled the close operation
                e.Handled = true;
                deferral.Complete();
            }
            else
            {
                switch (closeNum)
                {
                    case 0:
                        e.Handled = false;
                        deferral.Complete();
                        break;

                    case 1:
                        if (ApiInformation.IsApiContractPresent(
                             "Windows.ApplicationModel.FullTrustAppContract", 1, 0))
                        {
                            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                        }
                        e.Handled = false;
                        deferral.Complete();
                        break;
                }
            }
        }
    }
}
