using System;
using System.Collections.Generic;

using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

// Added during quickstart
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Newtonsoft.Json.Linq;
using Microsoft.Toolkit.Uwp.Notifications; // Notifications library
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace BackgroundTasks
{
    public sealed class StockBackgroundTask : IBackgroundTask
    {
        private string[] tickers = new string[] { "MSFT", "AAPL" };
        // private Dictionary<string, StockData> stocks = new Dictionary<string, StockData>();
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get a deferral, to prevent the task from closing prematurely
            // while asynchronous code is still running.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();


            var data = await UpdateStocks(this.tickers);

            // Update the live tile with the feed items.
            UpdateTile(data);

            // Inform the system that the task is finished.
            deferral.Complete();
        }

        private static async Task<Dictionary<string, StockData>> UpdateStocks(string[] tickers)
        {
            Dictionary<string, StockData> stocks = new Dictionary<string, StockData>();
            // Download the feed.
            foreach (var ticker in tickers)
            {
                var data = await GetStockFeed(ticker);
                using (var streamReader = new StreamReader(data.GetResponseStream()))
                {
                    // Read the bytes in responseStream and copy them to content.
                    var parsedResponse = JObject.Parse(streamReader.ReadToEnd());
                    var stockData = new StockData(parsedResponse);
                    stocks.Add(ticker, stockData);
                }
            }
            Debug.WriteLine(stocks.ToString());
            return stocks;
        }

        private static async Task<WebResponse> GetStockFeed(string ticker = "MSFT")
        {
            WebResponse feed = null;

            //string apiEndpoint = "https://www.alphavantage.co/query?function=GLOBAL_QUOTE";
            //string apiKey = "EDSGIWYZINYLVZ5U";
            //string url = apiEndpoint + "&symbol=" + ticker + "&apikey=" + apiKey;

            string url = "https://api.nasdaq.com/api/quote/" + ticker + "/info?assetclass=stocks";
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                feed = await request.GetResponseAsync();


                // Download the feed.
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return feed;
        }

        private static void UpdateTile(Dictionary<string, StockData> stocks)
        {
            // Create a tile update manager for the specified syndication feed.
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.EnableNotificationQueue(true);

            // Keep track of the number feed items that get tile notifications.
            int itemCount = 0;

            // Create a tile notification for each feed item.
            foreach (var stock in stocks)
            {
                XmlDocument tileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150Text03);

                var ticker = stock.Key;
                string titleText = ticker == null ? String.Empty : ticker;
                tileXml.GetElementsByTagName(textElementName)[0].InnerText = titleText;
                TileContent tileContent = Notification.CreateNotification(stock.Key, stock.Value);
                TileNotification notification = new TileNotification(tileContent.GetXml());
                notification.Tag = ticker;
                var coming = updater.GetScheduledTileNotifications();
                
                updater.Update(notification);
                // Create a new tile notification.
                // updater.Update(new TileNotification(tileXml));

                // Don't create more than 5 notifications.
                if (itemCount++ > 5) break;
            }
        }
        static string textElementName = "text";
    }

    class StockData
    {
        public string LastPrice { get; set; }
        public double Change { get; set; }
        public string Open { get; set; }
        public string ChangePercent { get; set; }
        public string Time { get; set; }

        public StockData(JObject jsonRes)
        {
            //JObject data = jsonRes.Value<JObject>("Global Quote");
            //LastPrice = data.Value<double>("05. price");
            //Change = data.Value<double>("09. change");
            //Open = data.Value<double>("02. open");
            //ChangePercent = data.Value<string>("10. change percent");
            JObject data = jsonRes.Value<JObject>("data");
            JObject primaryData = data.Value<JObject>("primaryData");
            JObject keyStats = data.Value<JObject>("keyStats");
            LastPrice = primaryData.Value<string>("lastSalePrice");
            Change = primaryData.Value<double>("netChange");
            Open = keyStats.Value<JObject>("OpenPrice").Value<string>("value");
            ChangePercent = data.Value<string>("percentageChange");
            Time = primaryData.Value<string>("lastTradeTimestamp");
            Debug.WriteLine(LastPrice);
            Debug.WriteLine(Change);
            Debug.WriteLine(Open);
            Debug.WriteLine(ChangePercent);
        }
    }

    static class Notification
    {
        public static TileContent CreateNotification(string ticker, StockData data)
        {
            // Construct the tile content
            TileContent content = new TileContent()
            {
                Visual = new TileVisual()
                {
                    TileMedium = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = "ur"
                                },

                                new AdaptiveText()
                                {
                                    Text = "mom",
                                    HintStyle = AdaptiveTextStyle.Subtitle
                                },

                                new AdaptiveText()
                                {
                                    Text = "gay",
                                    HintStyle = AdaptiveTextStyle.Subtitle
                                }
                            }
                        }
                    },

                    TileWide = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = ticker + " " + data.Change,
                                    HintStyle = AdaptiveTextStyle.Title
                                },

                                new AdaptiveText()
                                {
                                    Text = "Last trade " + data.LastPrice,
                                    HintStyle = AdaptiveTextStyle.Base
                                },

                                new AdaptiveText()
                                {
                                    Text = "Open " + data.Open,
                                    HintStyle = AdaptiveTextStyle.Base
                                },
                                new AdaptiveText()
                                {
                                    Text = data.Time,
                                    HintStyle = AdaptiveTextStyle.Base
                                }
                            }
                        }
                    }
                }
            };
            return content;
        }
    }

    public sealed class StockForegroundTask : IBackgroundTask
    {
        private BackgroundTaskDeferral backgroundTaskDeferral;
        private AppServiceConnection appServiceconnection;

        private IBackgroundTask task;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get a deferral so that the service isn't terminated.
            this.backgroundTaskDeferral = taskInstance.GetDeferral();

            // Associate a cancellation handler with the background task.
            taskInstance.Canceled += OnTaskCanceled;

            // Retrieve the app service connection and set up a listener for incoming app service requests.
            var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            appServiceconnection = details.AppServiceConnection;
            appServiceconnection.RequestReceived += OnRequestReceived;

            task = new StockBackgroundTask();
            task.Run(taskInstance);


        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            // This function is called when the app service receives a request.
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (this.backgroundTaskDeferral != null)
            {
                // Complete the service deferral.
                this.backgroundTaskDeferral.Complete();
            }
        }
    }
}
