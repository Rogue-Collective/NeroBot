using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.YouTube;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace NeroBot
{
    
    public class Youtubestuff
    {
        YouTubeService yts;
        YoutubeResource youtube;

        public Youtubestuff()
        {
            var bcs = new BaseClientService.Initializer { ApiKey = BotCred.GoogleAPIKey };
            //youtube = new YoutubeResource(bcs.)
            yts = new YouTubeService(bcs);
           
        }
    }
}
