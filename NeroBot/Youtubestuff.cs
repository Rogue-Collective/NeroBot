using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.YouTube;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.ServiceModel.Syndication;
using System.Xml;
using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using CodeHollow.FeedReader.Parser;
using System.Xml.Linq;


namespace NeroBot
{
    public class UpdatedArgs : EventArgs
    {
        public string Author;
        public string Url;

        public UpdatedArgs(string author, string url)
        {
            this.Author = author;
            this.Url = url;
        }
    }

    
    public class Youtubestuff
    {
        public static List<string> urls;
        public Feed feed;
        //public XmlReader reader;
        

        public event EventHandler<UpdatedArgs> Updated;
        

        public Youtubestuff()
        {
            urls = new List<string>();

            urls.Add("https://www.youtube.com/feeds/videos.xml?user=GameTrailers");

            Task.Run(() => DostuffInit());
        }

        public async Task DostuffInit()
        { 
            while (true)
            {
                await CheckEvents();
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }

        public async Task CheckEvents()
        {
            try
            {
                for (int i = 0; i < urls.Count; i++)
                {
                    feed = await FeedReader.ReadAsync(urls[i]).ConfigureAwait(false);

                    var Atomfeed = feed.SpecificFeed;
                   
                    var item = feed.Items.FirstOrDefault();

                    string auth = "";
                    string url = "";

                    auth = item.Author;
                    url = item.Link;

                    //Discordstuff.WriteTextSafe("Author : " + auth, LiveMonitor.textbox1);
                    //Discordstuff.WriteTextSafe("Title : " + vTitle, LiveMonitor.textbox1);
                    //Discordstuff.WriteTextSafe("Author : " + auth + " LastUpdated : " + item.PublishingDate, LiveMonitor.textbox1);


                    if (item.PublishingDate > DateTime.Now.Subtract(TimeSpan.FromMinutes(20)))
                    {
                        Updated?.Invoke(this, new UpdatedArgs(auth, url));
                    }
                }
                
            }
            catch (Exception ex)
            {
                Discordstuff.WriteTextSafe(ex.Message, LiveMonitor.textbox1);
                Logging.WriteToFile(ex);
                
            }
            
        }

        public static void AddUrl(string url)
        {
            urls.Add(url);
        }

        public static void RemoveUrl(string url)
        {
            for (int i = 0; i < urls.Count;i++)
            {
                if (urls[i] == url)
                {
                    urls.RemoveAt(i);
                }
            }
        }
    }
}
