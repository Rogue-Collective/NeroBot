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

namespace NeroBot
{
    public class UpdatedArgs : EventArgs
    {
        public string Title;
        public string Uri;
    }

    
    public class Youtubestuff
    {
        List<string> urls;
        Atom10FeedFormatter atomform;

        public event EventHandler<UpdatedArgs> Updated;
        

        public Youtubestuff()
        {
            urls = new List<string>();
            var bcs = new BaseClientService.Initializer { ApiKey = BotCred.GoogleAPIKey };
            //yts = new YouTubeService(bcs);

            urls.Add("https://www.youtube.com/feeds/videos.xml?user=GameTrailers");

            var xmlcamel = XmlReader.Create(urls[0]);

            atomform = new Atom10FeedFormatter();

            atomform.ReadFrom(xmlcamel);

            

            Task.Run(() => DostuffInit());
            
            
        }

        public async Task DostuffInit()
        {
            var items = atomform.Feed.Items;

            foreach (var item in items)
            {
                Discordstuff.WriteTextSafe(item.Title.Text, LiveMonitor.textbox1);
            }

            await Task.Delay(Timeout.Infinite);
        }
    }
}
