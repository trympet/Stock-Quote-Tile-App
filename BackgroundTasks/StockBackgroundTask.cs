using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

// Added during quickstart
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.Web.Syndication;
using Newtonsoft.Json.Linq;
using Microsoft.Toolkit.Uwp.Notifications; // Notifications library

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

            string apiEndpoint = "https://www.alphavantage.co/query?function=GLOBAL_QUOTE";
            string apiKey = "EDSGIWYZINYLVZ5U";

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiEndpoint + "&symbol=" + ticker + "&apikey=" + apiKey);
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
            updater.Clear();

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
                updater.Update(new TileNotification(tileContent.GetXml()));
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
        public double LastPrice { get; set; }
        public double Change { get; set; }
        public double Open { get; set; }
        public string ChangePercent { get; set; }

        public StockData(JObject jsonRes)
        {
            JObject data = jsonRes.Value<JObject>("Global Quote");
            LastPrice = data.Value<double>("05. price");
            Change = data.Value<double>("09. change");
            Open = data.Value<double>("02. open");
            ChangePercent = data.Value<string>("10. change percent");
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
                                    HintStyle = AdaptiveTextStyle.Subheader
                                },

                                new AdaptiveText()
                                {
                                    Text = "Last trade " + data.LastPrice,
                                    HintStyle = AdaptiveTextStyle.Subtitle
                                },

                                new AdaptiveText()
                                {
                                    Text = "Open " + data.Open,
                                    HintStyle = AdaptiveTextStyle.Subtitle
                                }
                            }
                        }
                    }
                }
            };
            return content;
        }
    }
}
