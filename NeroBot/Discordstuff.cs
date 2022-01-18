
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace NeroBot
{
    public class Discordstuff
    {
        DiscordSocketConfig dc;
        public DiscordSocketClient dsc;
        EmbedAuthorBuilder authorBuilder;
        //CommandService cs;

#if RELEASE
        public TextBox? tb;
#endif

        public List<string> streams;
        public List<string> Socials;
        public DateTime prev;
        char pref = ' ';
        public ulong LoggingChan = 0;
        public ulong WelcomeChan = 0;
        public ulong AnnounceChan = 0;
        public string DiscordInvite = "";
        public int crashcounter;
        public string dInvitetMessage = "";
        public string customJoinMsg = "";
        public string streamWhat = "";


        public Discordstuff()
        {
            dc = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All,
                AlwaysDownloadUsers = true

            };

            dsc = new DiscordSocketClient(dc);
            //cs = new CommandService();
            authorBuilder = new EmbedAuthorBuilder
            {
                Name = "NeroBot"
            };
            pref = '?';

            streams = new List<string>();
            Socials = new List<string>();

            streams.Add(BotCred.DefaultChannel);

            Task.Run(() => DiscordRun());



        }

        private async void DiscordRun()
        {
            await dsc.LoginAsync(TokenType.Bot, BotCred.DiscordBotToken);
            await dsc.StartAsync();

            dsc.MessageReceived += Dsc_MessageReceived;
            dsc.UserJoined += Dsc_OnUserJoin;
            dsc.UserBanned += Dsc_OnUserBan;
            dsc.ChannelCreated += Dsc_OnChanCreate;
            dsc.UserLeft += Dsc_OnUserLeft;
            dsc.GuildMemberUpdated += Dsc_OnMemberUpdate;
            dsc.ChannelDestroyed += Dsc_OnChanDestroy;
            dsc.ChannelUpdated += Dsc_OnChanUpdate;
            dsc.GuildUpdated += Dsc_OnGuildUpdate;


            await dsc.DownloadUsersAsync(dsc.Guilds);

            await Task.Delay(-1);
        }



        #region DiscordEvents

        private async Task Dsc_OnUserJoin(SocketGuildUser arg)
        {
            if (WelcomeChan != 0)
            {
                SocketTextChannel welc = GetSocketTextChannel(WelcomeChan);
                if (customJoinMsg != null)
                    await welc.SendMessageAsync(arg.Mention + customJoinMsg);
            }

        }

        private async Task Dsc_OnMemberUpdate(Cacheable<SocketGuildUser, ulong> arg1, SocketGuildUser arg2)
        {
            SocketTextChannel chan = GetSocketTextChannel(LoggingChan);
            var Guild = GetSocketGuild();
            var oldNick = await arg1.GetOrDownloadAsync();
            EmbedBuilder embed = new EmbedBuilder();
            embed.Author = authorBuilder;
            embed.AddField("Action", "User Updated");
            if (oldNick != null)
            {
                WriteTextSafe(oldNick.Nickname);
                embed.AddField("Previous Name", oldNick.Nickname, true);
            }
            embed.AddField("Updated Name", arg2.Nickname, true);
            embed.AddField("Username", arg2, true);
            embed.ImageUrl = arg2.GetAvatarUrl();
            embed.Color = Discord.Color.DarkOrange;
            await chan.SendMessageAsync(embed: embed.Build());
        }
        private async Task Dsc_OnUserLeft(SocketGuild arg1, SocketUser arg2)
        {
            SocketTextChannel chan = GetSocketTextChannel(LoggingChan);
            EmbedBuilder embed = new EmbedBuilder();
            embed.Author = authorBuilder;
            embed.AddField("Action", "Left Server", true);
            embed.AddField("User", arg2.Username + arg2.DiscriminatorValue);
            embed.ImageUrl = arg2.GetAvatarUrl();
            await chan.SendMessageAsync(embed: embed.Build());
        }

        private async Task Dsc_OnUserBan(SocketUser arg1, SocketGuild arg2)
        {
            if (LoggingChan != 0)
            {
                SocketTextChannel chan = GetSocketTextChannel(LoggingChan);
                var ban = await arg2.GetBanAsync(arg1, RequestOptions.Default);

                EmbedBuilder embed = new EmbedBuilder();

                embed.Author = authorBuilder;
                embed.AddField("Name", arg1.Username);
                embed.Description = "Banned";
                embed.Color = Discord.Color.Red;
                embed.AddField("Reason", ban.Reason);

                await chan.SendMessageAsync(embed: embed.Build());
            }

        }

        private async Task Dsc_OnChanCreate(SocketChannel arg)
        {
            SocketTextChannel chan = GetSocketTextChannel(LoggingChan);
            var chancreated = (SocketTextChannel)arg;
            var Guild = GetSocketGuild();
            var LogEntry = GetAuditLog(Guild, 1, ActionType.ChannelCreated);
            EmbedBuilder embed = new EmbedBuilder();
            embed.Author = authorBuilder;
            embed.AddField("Action", "Channel Created");
            embed.AddField("Channel Name", chancreated.Name, true);
            embed.Color = Discord.Color.Green;
            embed.WithTimestamp(arg.CreatedAt);
            embed.AddField("By", LogEntry.ElementAtAsync(0).Result.ElementAt(0).User.Username, true);
            await chan.SendMessageAsync(embed: embed.Build());
        }

        private async Task Dsc_OnChanUpdate(SocketChannel arg1, SocketChannel arg2)
        {
            SocketTextChannel chan = GetSocketTextChannel(LoggingChan);
            var orig = (SocketTextChannel)arg1;
            var updated = (SocketTextChannel)arg2;
            var Guild = GetSocketGuild();
            var LogEntry = GetAuditLog(Guild, 1, ActionType.ChannelUpdated);
            EmbedBuilder embed = new EmbedBuilder();
            embed.Author = authorBuilder;
            embed.AddField("Action", "Channel Updated");
            embed.AddField("Updated", LogEntry.ElementAtAsync(0).Result.ElementAt(0).Action);
            embed.AddField("By", LogEntry.ElementAtAsync(0).Result.ElementAt(0).User.Username);
            embed.Color = Discord.Color.LightOrange;
            embed.WithTimestamp(LogEntry.ElementAtAsync(0).Result.ElementAt(0).CreatedAt);

            await chan.SendMessageAsync(embed: embed.Build());
        }
        private async Task Dsc_OnChanDestroy(SocketChannel arg)
        {
            SocketTextChannel chan = GetSocketTextChannel(LoggingChan);
            var chanDestroyed = (SocketTextChannel)arg;
            var Guild = GetSocketGuild();
            var LogEntry = GetAuditLog(Guild, 1, ActionType.ChannelDeleted);
            EmbedBuilder embed = new EmbedBuilder();
            embed.Author = authorBuilder;
            embed.AddField("Action", "Channel Deleted");
            embed.AddField("Channel Name", chanDestroyed.Name, true);
            embed.AddField("By", LogEntry.ElementAtAsync(0).Result.ElementAt(0).User.Username, true);
            embed.Color = Discord.Color.Red;
            embed.WithTimestamp(LogEntry.ElementAtAsync(0).Result.ElementAt(0).CreatedAt);

            await chan.SendMessageAsync(embed: embed.Build());

        }

        private async Task Dsc_OnGuildUpdate(SocketGuild arg1, SocketGuild arg2)
        {
            try
            {
                SocketTextChannel chan = GetSocketTextChannel(LoggingChan);
                var Guild = GetSocketGuild();
                var LogEntry = GetAuditLog(Guild, 1, ActionType.GuildUpdated);
                EmbedBuilder embed = new EmbedBuilder();
                embed.Author = authorBuilder;
                embed.AddField("Action", "Guild Updated");
                embed.AddField("Data", LogEntry.ElementAtAsync(0).Result.ElementAt(0).Action);
            }
            catch (Exception e)
            {
#if RELEASE
                WriteTextSafe("Update: " + e.Message, tb);
#else
                Discordstuff.WriteTextSafe("Update: " + e.Message);
#endif          
                Logging.WriteToFile(e);

            }

        }



        private async Task Dsc_MessageReceived(SocketMessage arg)
        {
            try
            {
                var temp = new List<string>();
                temp = arg.Content.ToLower().Split(" ").ToList();
                var guild = arg.Author.MutualGuilds.First();
                var Guser = guild.GetUser(arg.Author.Id);

                if (Guser.GuildPermissions.BanMembers)
                {
                    if (temp[0].Contains(pref))
                    {
                        if (temp[0].Contains("cmdidentifier"))
                        {
                            var ch = temp[1].ToCharArray();
                            if (ch.Length <= 1)
                            {
                                pref = ch[0];
                            }

                            await arg.Channel.SendMessageAsync("Command Identifer set to " + pref);
                            return;
                        }
                        else if (temp[0].Contains("streamadd"))
                        {
                            AddStream(temp[1]);
                            await arg.Channel.SendMessageAsync("Stream added!");
                            return;
                        }
                        else if (temp[0].Contains("streamremove"))
                        {
                            RemoveStream(temp[1]);
                            return;
                        }
                        else if (temp[0].Contains("streams"))
                        {
                            for (int i = 0; i < streams.Count; i++)
                            {
                                await arg.Channel.SendMessageAsync(streams[i]);
                            }
                            return;
                        }
                        else if (temp[0].Contains("setlogging"))
                        {
                            LoggingChan = ulong.Parse(temp[1]);
                            return;
                        }
                        else if (temp[0].Contains("setwelcome"))
                        {
                            WelcomeChan = ulong.Parse(temp[1]);
                            return;
                        }
                        else if (temp[0].Contains("setannounce"))
                        {
                            AnnounceChan = ulong.Parse(temp[1]);
                        }
                        else if (temp[0].Contains("setcrash"))
                        {
                            crashcounter = int.Parse(temp[1]);
                            return;
                        }
                        else if (temp[0].Contains("addsocial"))
                        {
                            AddSocial(temp[1]);
                            return;
                        }
                        else if (temp[0].Contains("removesocial"))
                        {
                            RemoveSocial(temp[1]);
                            return;
                        }
                        else if (temp[0].Contains("setdiscordinvitemessage"))
                        {
                            for (int i = 1; i < temp.Count; i++)
                            {
                                dInvitetMessage += " " + temp[i];
                            }
                            return;
                        }
                        else if (temp[0].Contains("setinvite"))
                        {
                            DiscordInvite = temp[1];
                            return;
                        }
                        else if (temp[0].Contains("setjoinmsg"))
                        {
                            for (int i = 1; i < temp.Count; i++)
                            {
                                customJoinMsg += " " + temp[i];
                            }
                            return;
                        }
                        else if (temp[0].Contains("streamwhat"))
                        {
                            for (int i = 1; i < temp.Count; i++)
                            {
                                streamWhat += " " + temp[i];
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
            catch (Exception e)
            {
#if RELEASE
                WriteTextSafe("Update: " + e.Message, tb);
#else
                Discordstuff.WriteTextSafe("Update: " + e.Message);
#endif          
                Logging.WriteToFile(e);
            }
        }

        #endregion
        public async Task PostEmbed(string title, string Description, string thumbUrl, string User, string viewers)
        {
            if (AnnounceChan != 0)
            {
                try
                {
                    SocketTextChannel announce = GetSocketTextChannel(AnnounceChan);

                    var timeout = DateTime.Now.Subtract(TimeSpan.FromMinutes(30));

                    if (DateTime.Compare(prev, timeout) < 0)
                    {
                        var embed = new EmbedBuilder();

                        embed.Title = title;
                        embed.AddField("Name", User, true);
                        if (Description != "")
                        {
                            embed.AddField("Category/Game", Description, true);
                        }
                        embed.AddField("Viewers", viewers, true);
                        embed.Url = "https://twitch.tv/" + User;
                        embed.ThumbnailUrl = thumbUrl;
                        //WriteTextSafe(thumbUrl, tb);
                        embed.ImageUrl = "https://static-cdn.jtvnw.net/previews-ttv/live_user_" + User + "-1920x1080.jpg?" + GetJavascriptTimeStamp(DateTime.Now).ToString();
                        embed.Author = authorBuilder;
                        embed.WithCurrentTimestamp();

                        prev = DateTime.Now;

                        await announce.SendMessageAsync(text: "@everyone " + User + " Is streaming " + Description, embeds: new[] { embed.Build() });

                    }
                    else
                    {
                        return;
                    }
                }
                catch (Exception e)
                {
#if RELEASE
                    WriteTextSafe("Update: " + e.Message, tb);
#else
                Discordstuff.WriteTextSafe("Update: " + e.Message);
#endif
                    Logging.WriteToFile(e);
                }


            }
        }

        #region StreamFuncs
        public void AddStream(string stream)
        {
            streams.Add(stream);
        }

        public void RemoveStream(string stream)
        {
            try
            {
                for (int i = 0; i < streams.Count; i++)
                {
                    if (streams[i] == stream)
                    {
                        streams.RemoveAt(i);
                    }
                }
            }
            catch (Exception e)
            {
#if RELEASE
                WriteTextSafe("Update: " + e.Message, tb);
#else
                Discordstuff.WriteTextSafe("Update: " + e.Message);
#endif
                Logging.WriteToFile(e);

            }

        }
        #endregion

        #region Socials Funcs
        public void AddSocial(string social)
        {
            Socials.Add(social);
        }
        public void RemoveSocial(string social)
        {
            for (int i = 0; i < Socials.Count; i++)
            {
                if (Socials[i] == social)
                {
                    Socials.RemoveAt(i);
                }
            }
        }
        #endregion

        #region Other Funcs
        public static void WriteTextSafe(string text, TextBox? tb = null)
        {

            if (tb == null)
            {
                Console.WriteLine(text);
            }
            else
            {
                if (tb.InvokeRequired)
                {
                    Action safeWrite = delegate { WriteTextSafe(text, tb); };
                    tb.Invoke(safeWrite);
                }
                else
                {
                    tb.Text += text + Environment.NewLine;
                }
            }

        }

        public static Int64 GetJavascriptTimeStamp(DateTime dt)
        {
            var nineteenseventy = new DateTime(1970, 1, 1);
            var timeElapsed = dt.ToUniversalTime() - nineteenseventy;
            return (Int64)(timeElapsed.TotalMilliseconds + 0.5);

        }

        public SocketTextChannel GetSocketTextChannel(ulong chanID)
        {
            return (SocketTextChannel)dsc.GetChannelAsync(chanID, RequestOptions.Default).Result;
        }

        public SocketGuild GetSocketGuild()
        {
            return dsc.Guilds.ElementAt(0);
        }

        public IAsyncEnumerable<IReadOnlyCollection<RestAuditLogEntry>> GetAuditLog(SocketGuild g, int limit, ActionType at)
        {
            return g.GetAuditLogsAsync(limit, RequestOptions.Default, actionType: at);
        }
        #endregion
    }
}
