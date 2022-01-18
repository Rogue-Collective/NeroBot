﻿using System.Diagnostics;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.FollowerService;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

#if RELEASE
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.Foundation.Collections;
#endif
using Newtonsoft.Json;

namespace NeroBot
{
    struct BotData
    {
        public int highscore;
        public ulong lgc;
        public ulong wc;
        public ulong ac;
        public string di;
        public string dim;
        public string cjm;
        public string what;
        public DateTime prev;
        public List<string> strms;
        public List<string> scls;

    }

    public class LiveMonitor
    {
        //Twitch stuff
        private LiveStreamMonitorService mon;
        private FollowerService fs;
        private TwitchAPI api;
        private static TwitchClient client;
        ConnectionCredentials creds = new(BotCred.BotName, BotCred.BotAuth);

#if RELEASE


        //Form stuff
        static NotifyIcon ni;
        private Form form1;
        private TextBox textbox1;

#endif //RELEASE

        //Other Stuff
        public Discordstuff ds;
        public string gam;
        public string title;
        public int crashHighScore = 0;
        //DateTime cooldown;

        public LiveMonitor()
        {
#if RELEASE
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ni = new NotifyIcon();
            ni.Visible = true;
            ni.Icon = new System.Drawing.Icon(@"NeroBot.ico");
            ni.Text = "NeroBot";

#endif //RELEASE

            ds = new Discordstuff();

            ReadVars();

            //Task.Delay(TimeSpan.FromSeconds(5));
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(creds, ds.streams[0]);

            api = new TwitchAPI();
            api.Settings.ClientId = BotCred.ClientID;
            api.Settings.AccessToken = BotCred.BotAccToken;

            fs = new FollowerService(api, 15);
            mon = new LiveStreamMonitorService(api, 15);

            Task.Run(() => ConfigLiveAsync());
#if RELEASE
            Task.Run(() => FormLoop());
#endif //RELEASE

        }

        private async Task ConfigLiveAsync()
        {
            if (ds.streams.Count > 0)
            {
                mon.SetChannelsByName(ds.streams);
                fs.SetChannelsByName(ds.streams);

#if RELEASE
                ni.Click += icn_click;
                ni.MouseDoubleClick += icn_dblclick;
                ToastNotificationManagerCompat.OnActivated += toastArgs;
#endif //RELEASE

                mon.OnStreamOnline += Mon_OnStreamOnline;
                mon.OnStreamOffline += Mon_OnStreamOffline;
                mon.OnStreamUpdate += Mon_OnStreamUpdate;
                fs.OnNewFollowersDetected += Fs_OnFollow;
                fs.OnServiceTick += Fs_OnTick;
                fs.OnChannelsSet += Fs_OnChannelsSet;

                client.OnLog += Client_OnLog;
                client.OnMessageReceived += Client_OnMessageRec;

            }
            client.Connect();
            fs.Start();
            mon.Start();

            await Task.Delay(-1);
        }

        #region OtherFuncs
        private void UpdateMonitors()
        {


            mon.Stop();
            fs.Stop();
            try
            {
                mon.SetChannelsByName(ds.streams);
                fs.SetChannelsByName(ds.streams);
                mon.Start();
                fs.Start();
            }
            catch (Exception e)
            {
#if RELEASE
                Discordstuff.WriteTextSafe("Update: " + e.Message, textbox1);
#else
                Discordstuff.WriteTextSafe("Update: " + e.Message);
#endif          
                Logging.WriteToFile(e);
            }
        }


        public string GetUserByID(string username)
        {
            try
            {
                var user = api.Helix.Users.GetUsersAsync(logins: new List<string> { username });
                return user.Result.Users[0].Id;
            }
            catch (Exception e)
            {
#if RELEASE
                Discordstuff.WriteTextSafe(e.Message, textbox1);
#else
                Discordstuff.WriteTextSafe(e.Message);
#endif //RELEASE
                Logging.WriteToFile(e);
                return e.ToString();
            }
            
        }

        private void ReadVars()
        {
            try
            {
                using (StreamReader sr = new StreamReader(@"bot.txt"))
                {
                    BotData bd = new BotData();
                    bd = JsonConvert.DeserializeObject<BotData>(sr.ReadToEnd());

                    crashHighScore = bd.highscore;
                    ds.LoggingChan = bd.lgc;
                    ds.WelcomeChan = bd.wc;
                    ds.DiscordInvite = bd.di;
                    ds.dInvitetMessage = bd.dim;
                    ds.customJoinMsg = bd.cjm;
                    ds.streams = bd.strms;
                    ds.Socials = bd.scls;
                    ds.streamWhat = bd.what;
                    ds.AnnounceChan = bd.ac;
                    ds.prev = bd.prev;

                }
            }
            catch (Exception e)
            {
#if RELEASE
                Discordstuff.WriteTextSafe(e.Message, textbox1);
#else
                Discordstuff.WriteTextSafe(e.Message);
#endif //RELEASE
                Logging.WriteToFile(e);
            }

        }

        private void SaveVars()
        {
            try
            {
                BotData bd = new BotData();
                bd.highscore = crashHighScore;
                bd.lgc = ds.LoggingChan;
                bd.wc = ds.WelcomeChan;
                bd.ac = ds.AnnounceChan;
                bd.di = ds.DiscordInvite;
                bd.dim = ds.dInvitetMessage;
                bd.cjm = ds.customJoinMsg;
                bd.strms = ds.streams;
                bd.scls = ds.Socials;
                bd.what = ds.streamWhat;
                bd.prev = ds.prev;

                using (StreamWriter sw = new StreamWriter(@"bot.txt"))
                {
                    var temp = JsonConvert.SerializeObject(bd, Formatting.Indented);
                    sw.WriteLine(temp);
                }
            }
            catch (Exception e)
            {
                Logging.WriteToFile(e);
            }

        }
        #endregion

#if RELEASE
        #region Windows stuff

        private async Task FormLoop()
        {
            form1 = new Form
            {
                Size = new Size(600, 400),
                Text = "NeroBot"
            };

            textbox1 = new TextBox();
            textbox1.Dock = DockStyle.Fill;
            textbox1.ReadOnly = true;
            textbox1.Multiline = true;
            textbox1.BackColor = Color.Black;
            textbox1.ForeColor = Color.White;
            textbox1.Parent = form1;
            textbox1.WordWrap = true;
            textbox1.Font = new Font(textbox1.Font.FontFamily, 16);
            textbox1.ScrollBars = ScrollBars.Vertical;
            ds.tb = textbox1;

            //Form Events
            form1.Click += Form_OnClick;
            form1.Activated += Form_OnActivate;
            form1.Load += Form_OnLoad;
            form1.FormClosed += Form_Closed;
            Application.Run(form1);

            await Task.Delay(Timeout.Infinite);
        }

        #region FormEvents

        private void Form_Closed(object? sender, FormClosedEventArgs e)
        {
            ni.Visible = false;
            ni.Dispose();

            SaveVars();

            Environment.Exit(Environment.ExitCode);
        }
        private void Form_OnLoad(object? sender, EventArgs e)
        {
            textbox1.Show();
        }

        private void Form_OnActivate(object? sender, EventArgs e)
        {

        }

        private void Form_OnClick(object? sender, EventArgs e)
        {

        }

        private void Form1_Move(object? sender, EventArgs e)
        {

        }
        #endregion


        private void toastArgs(ToastNotificationActivatedEventArgsCompat e)
        {
            try
            {
                ValueSet temp = e.UserInput;
                object? value = null;
                object? val = null;
                temp.TryGetValue("tbReply", out value);
                temp.TryGetValue("User", out val);

                if (e.Argument == "action=twitch")
                {
                    client.SendMessage(val.ToString(), value.ToString());
                    Process.Start(@"C:\Users\User\AppData\Roaming\Twitch\Bin\Twitch.exe");
                }
            }
            catch (Exception ex)
            {
#if RELEASE
                Discordstuff.WriteTextSafe("Update: " + ex.Message, textbox1);
#else
                Discordstuff.WriteTextSafe("Update: " + ex.Message);
#endif          
                Logging.WriteToFile(ex);

            } 
            
        }

        private void icn_dblclick(object? sender, MouseEventArgs e)
        {
            Process.Start(@"C:\Users\User\AppData\Roaming\Twitch\Bin\Twitch.exe");
        }

        private void icn_click(object? sender, EventArgs e)
        {
            if (form1.InvokeRequired)
            {
                form1.Invoke(delegate
                {
                    if (!form1.Visible)
                    {
                        form1.Show();
                        form1.WindowState = FormWindowState.Maximized;
                        form1.BringToFront();
                    }
                    else
                    {
                        form1.Hide();
                    }
                });
                return;
            }
            else
            {
                if (!form1.Visible)
                    form1.Show();
                else
                    form1.Hide();
            }
        }

        #endregion
#endif

        #region FollowerServiceFuncs

        private void Fs_OnChannelsSet(object? sender, OnChannelsSetArgs e)
        {
#if DEBUG
            Discordstuff.WriteTextSafe("FollowerService Channels set!");
#endif
        }

        private void Fs_OnTick(object? sender, OnServiceTickArgs e)
        {
            UpdateMonitors();
        }

        private void Fs_OnFollow(object sender, OnNewFollowersDetectedArgs e)
        {
#if RELEASE
            Discordstuff.WriteTextSafe("New Follower detected!", textbox1);
#else
            Discordstuff.WriteTextSafe("New Follower detected!");
#endif
            try
            {
                var timeout = DateTime.Now.Subtract(TimeSpan.FromMinutes(30));
                if (DateTime.Compare(e.NewFollowers[0].FollowedAt, timeout) < 0)
                {
#if RELEASE
                    Discordstuff.WriteTextSafe("Caught an old follower... somehow?", textbox1);
#else
                    Discordstuff.WriteTextSafe("Caught an old follower... somehow?");
#endif
                }
                else if (DateTime.Compare(e.NewFollowers[0].FollowedAt, timeout) > 0)
                {
                    client.SendMessage(e.NewFollowers[0].ToUserName, "Thanks for the follow! @" + e.NewFollowers[0].FromUserName);
                }
            }
            catch (Exception ex)
            {
#if RELEASE
                Discordstuff.WriteTextSafe(ex.Message, textbox1);
#else
                Discordstuff.WriteTextSafe(ex.Message);
#endif
            }


        }

        #endregion

        #region MonitorFuncs

        private void Mon_OnStreamUpdate(object sender, OnStreamUpdateArgs e)
        {
            throw new NotImplementedException();
        }

        private async void Mon_OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            if (ds.crashcounter > crashHighScore)
            {
                crashHighScore = ds.crashcounter;
            }
            ds.crashcounter = 0;
            ds.streamWhat = "";
#if RELEASE
            var user = await api.Helix.Users.GetUsersAsync(logins: ds.streams);
            new ToastContentBuilder()
                .AddArgument("action", "StreamOffline")
                .AddText(e.Stream.UserName)
                .AddText("Has Ended their stream!")
                .AddAppLogoOverride(new Uri(user.Users[0].ProfileImageUrl), ToastGenericAppLogoCrop.Circle)
                .Show();
#endif
        }

        private async void Mon_OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            try
            {
                var user = await api.Helix.Users.GetUsersAsync(logins: ds.streams);
                gam = e.Stream.GameName;
                title = e.Stream.Title;
                await ds.PostEmbed(e.Stream.Title, e.Stream.GameName, e.Stream.ThumbnailUrl, e.Stream.UserLogin, e.Stream.ViewerCount.ToString());
#if RELEASE
                new ToastContentBuilder()
                .AddArgument("action", "StreamOnline")
                .AddArgument("User", e.Stream.UserName)
                .AddText(e.Stream.UserLogin)
                .AddText(e.Stream.Title)
                .AddAppLogoOverride(new Uri(user.Users[0].ProfileImageUrl), ToastGenericAppLogoCrop.Circle)
                .Show();
#endif
            }
            catch (Exception ex)
            {
#if RELEASE
                Discordstuff.WriteTextSafe(ex.Message, textbox1);
#else
                Discordstuff.WriteTextSafe(ex.Message);
#endif
            }

        }
        #endregion

        #region ClientEvents
        private async void Client_OnMessageRec(object sender, OnMessageReceivedArgs e)
        {
            if (!e.ChatMessage.Message.StartsWith("!"))
            {
                return;
            }

            var temp = new List<string>();
            temp = e.ChatMessage.Message.ToLower().Split(" ").ToList();

            if (temp.ElementAt(0) == "!so")
            {
                client.SendMessage(e.ChatMessage.Channel, "Go and follow https://twitch.tv/" + temp.ElementAt(1));
                return;
            }

            if (temp.ElementAt(0) == "!socials" && ds.Socials.Count > 0)
            {
                for (int i = 0; i < ds.Socials.Count; i++)
                {
                    client.SendMessage(e.ChatMessage.Channel, ds.Socials[i]);
                }
                return;
            }

            if (temp.ElementAt(0) == "!discord" && ds.dInvitetMessage != null && ds.DiscordInvite != null)
            {
                client.SendMessage(e.ChatMessage.Channel, ds.dInvitetMessage + " " + ds.DiscordInvite);
                return;
            }

            if (temp.ElementAt(0) == "!game" && gam != null)
            {
                client.SendMessage(e.ChatMessage.Channel, gam);
                return;
            }


            if (temp.ElementAt(0) == "!title" && title != null)
            {
                client.SendMessage(e.ChatMessage.Channel, title);
                return;

            }

            if (!e.ChatMessage.IsBroadcaster && temp.ElementAt(0) == "!followage")
            {
                var f = await api.Helix.Users.GetUsersFollowsAsync(first: 50, fromId: e.ChatMessage.UserId, toId: GetUserByID(e.ChatMessage.Channel));
                var tem = DateTime.Now.Subtract(f.Follows[0].FollowedAt);
                var followed = ((int)tem.TotalDays);
                client.SendMessage(e.ChatMessage.Channel, "@" + e.ChatMessage.Username + " has been following for " + followed + " Days!");
                return;
            }

            if (temp.ElementAt(0) == "!crash" && gam == "Grand Theft Auto V")
            {
                ds.crashcounter++;
                if (ds.crashcounter > crashHighScore)
                {
                    crashHighScore = ds.crashcounter;
                }
                var message = e.ChatMessage.Channel + " has crashed " + ds.crashcounter + " times this stream!";
                client.SendMessage(e.ChatMessage.Channel, message);
                return;
            }


            if (temp.ElementAt(0) == "!crashcheck" && gam == "Grand Theft Auto V")
            {
                string message = e.ChatMessage.Channel + " has crashed " + ds.crashcounter + " times this stream!";
                client.SendMessage(e.ChatMessage.Channel, message);
                return;
            }

            if (temp.ElementAt(0) == "!topcrash" && gam == "Grand Theft Auto V")
            {
                client.SendMessage(e.ChatMessage.Channel, crashHighScore + " crashes is the most crashes per stream so far!");
                return;
            }

            if (temp.ElementAt(0) == "!uptime")
            {
                var tem = await api.Helix.Streams.GetStreamsAsync(userLogins: new List<string>() { e.ChatMessage.Channel });
                var timeElapsed = DateTime.Now.Subtract(tem.Streams.First().StartedAt);
                client.SendMessage(e.ChatMessage.Channel, timeElapsed.ToString(@"hh\:mm\:ss"));
                return;
            }

            if (temp.ElementAt(0) == "!what" && ds.streamWhat != null)
            {
                client.SendMessage(e.ChatMessage.Channel, "What am i doing? : " + ds.streamWhat);
            }

#if RELEASE
            if ((e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator) && temp.ElementAt(0) == "!summon")
            {
                new ToastContentBuilder()
                    .AddArgument("action", "twitch")
                    .AddText("URGENT!")
                    .AddText("NEEDED ON " + e.ChatMessage.Username + " STREAM")
                    .AddInputTextBox("tbReply", placeHolderContent: "Type a response")
                    .Show();
                return;
            }

            if ((e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator) && temp.ElementAt(0) == "!setwhat")
            {
                for (int i = 1; i < temp.Count; i++)
                {
                    ds.streamWhat += temp[i];
                }
            }
#endif
        }

        private void Client_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        {
#if DEBUG
            Discordstuff.WriteTextSafe"tClient : " + e.Data);
#endif
        }

        #endregion

    }
}