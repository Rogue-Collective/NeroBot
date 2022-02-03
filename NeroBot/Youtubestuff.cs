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
        SyndicationItem item;

        public UpdatedArgs(SyndicationItem item)
        {
            this.item = item;
        }
    }

    
    public class Youtubestuff
    {
        List<string> urls;
        Atom10FeedFormatter atomform;
        XmlReader xmlr;

        public event EventHandler<UpdatedArgs> Updated;
        

        public Youtubestuff()
        {
            urls = new List<string>();
            var bcs = new BaseClientService.Initializer { ApiKey = BotCred.GoogleAPIKey };
            //yts = new YouTubeService(bcs);

            urls.Add("https://www.youtube.com/feeds/videos.xml?user=GameTrailers");

            xmlr = XmlReader.Create(urls[0]);

            atomform = new Atom10FeedFormatter();

            atomform.ReadFrom(xmlr);

            

            Task.Run(() => DostuffInit());
            
            
        }

        public async Task DostuffInit()
        {


            await CheckEvents();

            await Task.Delay(Timeout.Infinite);
        }

        public async Task CheckEvents()
        {
            atomform.ReadFrom(xmlr);
            var item = atomform.Feed.Items.FirstOrDefault();

            if (item.LastUpdatedTime > DateTime.Now.Subtract(TimeSpan.FromMinutes(5)))
            {
                Updated?.Invoke(this, new UpdatedArgs(item)) ; 
            }
        }
    }
}
