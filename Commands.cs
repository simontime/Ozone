using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.VoiceNext;
using ImageProcessor;
using ImageProcessor.Imaging.Filters.Photo;
using LibHac;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static DSharpPlus.Permissions;
using static Ozone.Utils;

namespace Ozone
{
    public class TopLevelCommands
    {
        public const long DeviceID = 0; // <REDACTED>

        public const string ImgurAPIKey = "<REDACTED>";
        public const string MashapeAPIKey = "<REDACTED>";
        public const string AmdorenAPIKey = "<REDACTED>";
        public const string TimeZoneDBAPIKey = "<REDACTED>";

        public const string ETicketRSAKey = "<REDACTED>";
        public const string SSLRSAKey = "<REDACTED>";

        public static Keyset Keys = ExternalKeys.ReadKeyFile("keys.txt");

        [Command(";")]
        public async Task SM18(CommandContext Context, int Length)
        {
            var random = new Random();
            string RandomString(int length)
            {
                const string chars = "abcdefghijklmnopqrstuvwxyz";
                return new string(Enumerable.Repeat(chars, length)
                  .Select(s => s[random.Next(s.Length)]).ToArray());
            }
            await Context.RespondAsync(RandomString(Length));
        }

        [Command("reboot"), Aliases("reset"), RequireOwner]
        public async Task Reboot(CommandContext Context)
        {
            await Context.RespondAsync("Rebooting bot...");
            await Context.Client.ReconnectAsync(true);
        }

        [Command("purge")]
        [Description("Purge a bunch of messages.")]
        [RequireUserPermissions(ManageMessages)]
        public async Task Purge(CommandContext Context, [Description("Number of messages to purge")] int Num)
        {
            var Msgs = await Context.Channel.GetMessagesAsync(Num + 1);
            await Context.Channel.DeleteMessagesAsync(Msgs.ToArray());
            var Response = await Context.RespondAsync("Deleted those messages :ok_hand:");
            await Task.Delay(5000);
            await Context.Channel.DeleteMessageAsync(Response);
        }

        [Command("invite")]
        public async Task Invite(CommandContext ctx)
        {
            var Embed = new DiscordEmbedBuilder();

            Embed.WithAuthor
                (
                "Click here to invite me to your server!",
                "https://discordapp.com/oauth2/authorize?client_id=468660375819124737&permissions=8&scope=bot",
                ctx.Client.CurrentUser.AvatarUrl
                );

            Embed.WithColor(DiscordColor.Blurple);

            await ctx.RespondAsync(null, false, Embed);
        }

        [Command("avatar")]
        [Description("Gets the avatar image of a specified user.")]
        [Aliases("pfp")]
        public async Task Avatar(CommandContext Context, [Description("User to get the avatar image for."), RemainingText]DiscordMember User)
        {
            if (User != null)
            {
                var Embed = new DiscordEmbedBuilder();

                Embed.WithColor(DiscordColor.Blurple);

                Embed.WithAuthor($"{User.DisplayName}'s avatar image:");

                Embed.WithImageUrl(User.AvatarUrl);

                await Context.RespondAsync(null, false, Embed);
            }
            else
            {
                var Embed = new DiscordEmbedBuilder();

                Embed.WithColor(DiscordColor.Blurple);

                Embed.WithAuthor($"{Context.Member.Nickname}'s avatar image:");

                Embed.WithImageUrl(Context.Member.AvatarUrl);

                await Context.RespondAsync(null, false, Embed);
            }
        }

        [Command("flip")]
        [Description("Flip the endianness of a hex string.")]
        public async Task Flip(CommandContext Context, [Description("Input hex string.")] string InBytes)
        {
            var Bytes = HexStrToB(InBytes).Reverse().ToArray();

            var Flipped = BToHexStr(Bytes).ToLower();

            await Context.RespondAsync(Flipped);
        }


        [Command("ping"), Description("Bog-standard ping command..."), Aliases("pong")]
        public async Task Ping(CommandContext Ctx)
        {
            await Ctx.RespondAsync($":ping_pong: Pong! Ping: {Ctx.Client.Ping}ms");
        }

        [Command("titleinfo"), Description("Force title info retrieval."), Aliases("t")]
        public async Task NCA(CommandContext Ctx, string TitleID)
        {
            TitleID             = TitleID.Substring(0, 13) + "000";

            using (var File     = WebUtils.MakeReqToAtum(TitleID, DeviceID, Ctx))
            {
                var NCA         = new Nca(Keys, File, false);
                var Rom         = new Romfs(NCA.OpenSection(0, false));
                var Open        = Rom.OpenFile(Rom.Files[0]);
                var Listing1    = new BinaryReader(Open);
                var Listing2    = new BinaryReader(Open);

                bool InclBCAT   = false;

                string Title    = null;
                string Dev      = null;
                string Version  = null;
                string BCATPass = null;             

                Nacp NACP_BCAT  = new Nacp(Listing1);

                Version         = NACP_BCAT.DisplayVersion;

                if (NACP_BCAT.BcatPassphrase.Length > 1)
                {
                    InclBCAT = true;
                    BCATPass = NACP_BCAT.BcatPassphrase;
                }

                Open.Position = 0;

                NacpLang NACP   = new NacpLang(Listing2);

                foreach (var Pos in Enumerable.Range(0, 9))
                {
                    var T = NACP.Title;
                    var D = NACP.Developer;

                    if (T.Length < 1)
                    {
                        NACP                          = new NacpLang(Listing2);
                        Listing2.BaseStream.Position += Pos * 0x300;
                        continue;
                    }
                    else
                    {
                        Title = T;
                        Dev   = D;
                        break;
                    }
                }

                string Response = 
                    $"Title: {Title ?? "Unable to get title name."}" +
                    $"\nDeveloper: {Dev ?? "Unable to get developer name."}" +
                    $"\nVersion: {Version ?? "Unable to get version."}";

                if (InclBCAT)
                {
                    Response += $"\nBCAT passphrase: {BCATPass}";
                }

                await Ctx.RespondAsync(Response);
                await Ctx.RespondWithFileAsync(Rom.OpenFile(Rom.Files[1]), "icon.jpg");
            }
        }

        [Command("memlist"), Description("Returns a text file populated with all the members in this guild.")]
        public async Task MemList(CommandContext Ctx)
        {
            var Strm = new MemoryStream();
            var Wrt = new StreamWriter(Strm);
            var Members = await Ctx.Guild.GetAllMembersAsync();
            foreach (var Member in Members)
            {
                string Init;
                if ($"{Member.Id}".Length <= 17)               
                    Init = $"{Member.Id}  | ";               
                else
                    Init = $"{Member.Id} | ";
                Wrt.WriteLine($"{Init}{Member.Username}#{Member.Discriminator}");
            }
            Strm.Position = 0;
            await Ctx.RespondWithFileAsync(Strm, "members.txt");
        }

        [Command("pin"), Description("Pins a message to the current channel.")]
        public async Task Pin(CommandContext ctx, [Description("ID of the message to be pinned")]DiscordMessage ID)
        {
            await ID.PinAsync();
        }

        [Command("ver"), Description("Converts a Switch version into its equivalent number and vice-versa.")]
        public async Task VerEnc(CommandContext ctx, string Type, string Text)
        {
            string make(uint major, uint minor, uint micro, uint bugfix)
            {
                var m = (major - 0xFC000000) << 26;
                var n = (minor - 0x03F00000) << 20;
                var c = (micro - 0x000F0000) << 16;
                return $"{m + n + c + bugfix}";
            }

            string parse(uint version)
            {
                var major   = (version & 0xFC000000) >> 26;
                var minor   = (version & 0x03F00000) >> 20;
                var micro   = (version & 0x000F0000) >> 16;
                var bugfix  = version & 0x0000FFFF;
                return $"{major}.{minor}.{micro}.{bugfix}";
            }

            if (Type == "encode")
            {
                await ctx.RespondAsync(make(
                    Convert.ToUInt32(Text.Substring(0, 1)),
                    Convert.ToUInt32(Text.Substring(2, 1)),
                    Convert.ToUInt32(Text.Substring(4, 1)),
                    Convert.ToUInt32(Text.Substring(6))
                    ));
            }
            else if (Type == "decode")
            {
                await ctx.RespondAsync(parse(Convert.ToUInt32(Text)));
            }
            else
            {
                await ctx.RespondAsync("Error: type must be either encode or decode!");
            }
        }

        [Command("join"), Description("Joins a voice channel.")]
        public async Task Join(CommandContext ctx, DiscordChannel chn = null)
        {
            var vnext = ctx.Client.GetVoiceNextClient();
            if (vnext == null)
            {
                await ctx.RespondAsync("VNext is not enabled or configured.");
                return;
            }
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc != null)
            {
                await ctx.RespondAsync($"I'm already connected to {vnc.Channel.Name}!");
                return;
            }
            var vstat = ctx.Member?.VoiceState;
            if (vstat?.Channel == null && chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel first!");
                return;
            }
            if (chn == null)
                chn = vstat.Channel;
            vnc = await vnext.ConnectAsync(chn);
            await ctx.RespondAsync($"Connected to `{chn.Name}`");
        }

        [Command("leave"), Description("Leaves a voice channel.")]
        public async Task Leave(CommandContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNextClient();
            if (vnext == null)
            {
                await ctx.RespondAsync("VNext is not enabled or configured.");
                return;
            }
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
            {
                await ctx.RespondAsync("Not connected in this guild.");
                return;
            }
            vnc.Disconnect();
            await ctx.RespondAsync("Disconnected");
        }

        public static Queue AudioQueue = new Queue();
        public static string CurrentlyPlaying = null;

        [Command("play"), Description("Plays an audio file.")]
        public async Task Play(CommandContext ctx, [RemainingText, Description("Full path to the file to play.")] string filename)
        {
            filename = filename.Replace("@", "");
            AudioQueue.Enqueue(filename);
            var vnext = ctx.Client.GetVoiceNextClient();
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
            {
                await ctx.RespondAsync("I'm not in a voice channel ;(");
                return;
            }
            while (vnc.IsPlaying)
            {
                await ctx.RespondAsync($"Added {filename} to queue.");
                await vnc.WaitForPlaybackFinishAsync();
            }
            Exception exc = null;
            await ctx.Message.RespondAsync($"Now playing: `{filename}`");
            CurrentlyPlaying = filename;
            await vnc.SendSpeakingAsync(true);
            try
            {
                var ffmpeg_inf = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/C youtube-dl -f 251 -o - {AudioQueue.Dequeue()} | ffmpeg -i pipe:0 -f s16le -ar 48000 -ac 2 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                var ffmpeg = Process.Start(ffmpeg_inf);
                var ffout = ffmpeg.StandardOutput.BaseStream;
                using (var ms = new MemoryStream())
                {
                    await ffout.CopyToAsync(ms);
                    ms.Position = 0;

                    var buff = new byte[3840];
                    var br = 0;
                    while ((br = ms.Read(buff, 0, buff.Length)) > 0)
                    {
                        if (br < buff.Length)
                            for (var i = br; i < buff.Length; i++)
                                buff[i] = 0;

                        await vnc.SendAsync(buff, 20);
                    }
                }
            }
            catch (Exception ex) { exc = ex; }
            finally
            {
                CurrentlyPlaying = null;
                await vnc.SendSpeakingAsync(false);
            }
        }
        

        [Command("queue"), Description("Displays all items in the music queue.")]
        public async Task QueueReader(CommandContext ctx)
        {
            string Queue = null;
            int ctr = 0;
            foreach(var item in AudioQueue)
            {
                Queue += $"{++ctr}: <{item}>\n";
            }
            await ctx.RespondAsync(Queue ?? "There is nothing currently queued.");
        }

        [Command("nowplaying"), Description("Displays the item currently being played in the audio queue.")]
        public async Task NowPlaying(CommandContext ctx)
        {
            await ctx.RespondAsync(CurrentlyPlaying ?? "There is nothing currently being played.");
        }

        [Command("news"), Description("Decrypts and displays a BCAT news file.")]
        public async Task News(CommandContext ctc)
        {
            HttpWebRequest Request   = (HttpWebRequest)WebRequest.Create(ctc.Message.Attachments[0].Url);
            HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
            var Strm                 = new BinaryReader(Response.GetResponseStream());

            File.WriteAllBytes($"temp_{ctc.User.Id.ToString()}.bcat", Strm.ReadBytes((int)Response.ContentLength));

            Strm.Dispose();

            var Str2  = new FileStream($"temp_{ctc.User.Id.ToString()}.bcat", FileMode.Open);
            var Ticks = DateTime.UtcNow.Ticks.ToString();

            File.WriteAllBytes($"bcat_{ctc.User.Id.ToString()}_{Ticks}",
                BCATUtils.DecryptBCAT
                (
                "0100000000001000",
                "acda358b4d32d17fd4037c1b5e0235427a8563f93b0fdb42a4a536ee95bbf80f",
                Str2)
                );

            Str2.Dispose();

            ProcessStartInfo JSON = new ProcessStartInfo("msgpack-cli", $"decode bcat_{ctc.User.Id.ToString()}_{Ticks} --out=bcat_{ctc.User.Id.ToString()}_{Ticks}.json")
            {
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(JSON);

            await Task.Delay(200);

            var FileStrm = File.ReadAllText($"bcat_{ctc.User.Id.ToString()}_{Ticks}.json");
            var Author   = JObject.Parse(FileStrm)["subject"]["text"].ToString();
            var Thumb    = JObject.Parse(FileStrm)["topic_image"].ToString();
            var Image    = JObject.Parse(FileStrm)["body"]["main_image"].ToString();
            var Text     = JObject.Parse(FileStrm)["body"]["text"].ToString().Replace("<strong>", "**").Replace("</strong>", "**");

            var client          = new WebClient();
            var value2s         = new NameValueCollection();
            client.Headers.Add("Authorization", $"Client-ID {ImgurAPIKey}");
            value2s["image"]    = Thumb;
            value2s["type"]     = "base64";
            value2s["name"]     = "temp";
            var response        = client.UploadValues("https://api.imgur.com/3/image", value2s);
            var responseString  = JObject.Parse(Encoding.Default.GetString(response))["data"]["link"].ToString();

            var client2         = new WebClient();
            var values          = new NameValueCollection();
            client2.Headers.Add("Authorization", $"Client-ID {ImgurAPIKey}");
            values["image"]     = Image;
            values["type"]      = "base64";
            values["name"]      = "temp";
            var response2       = client2.UploadValues("https://api.imgur.com/3/image", values);
            var responseString2 = JObject.Parse(Encoding.Default.GetString(response2))["data"]["link"].ToString();

            DiscordEmbedBuilder Embed = new DiscordEmbedBuilder();
            Embed.WithColor(DiscordColor.CornflowerBlue);
            Embed.WithThumbnailUrl(responseString);
            Embed.WithAuthor(Author);
            Embed.WithImageUrl(responseString2);
            Embed.AddField(JObject.Parse(FileStrm)["topic_name"].ToString(), Text);
            Embed.WithFooter(JObject.Parse(FileStrm)["footer"]["text"].ToString());

            await ctc.RespondAsync(null, false, Embed);
        }

        [Command("arm")]
        [Description("Converts a hex opcode to its AArch64 assembly counterpart.")]
        [Aliases("o", "opcode")]
        public async Task ARM(CommandContext ctx, [Description("Opcode as hex string, use 0x to specify big endian.")]string OpCode)
        {
            using (var client = new WebClient())
            {
                if (OpCode.Substring(0, 2) == "0x")
                {
                    var values = new NameValueCollection
                    {
                        ["xjxfun"]     = "ajxRunCommand",
                        ["xjxr"]       = (DateTime.UtcNow.Ticks / 10000).ToString(),
                        ["xjxargs[]"]  = $"<xjxobj><e><k>txtInput</k><v>S<![CDATA[" +
                        $"{OpCode.Substring(8, 2).ToUpper()} " +
                        $"{OpCode.Substring(6, 2).ToUpper()} " +
                        $"{OpCode.Substring(4, 2).ToUpper()} " +
                        $"{OpCode.Substring(2, 2).ToUpper()}" +
                        $"]]></v></e><e><k>txtInput2</k><v>S</v></e><e><k>opt_arch</k><v>S2</v></e></xjxobj>"
                    };
                    var response       = client.UploadValues("http://armconverter.com/hextoarm/", values);
                    var responseString = Encoding.Default.GetString(response);
                    await ctx.RespondAsync($"`{responseString.Substring(87).Split(Convert.ToChar("<"))[0]}`");
                }
                else
                {
                    var values = new NameValueCollection
                    {
                        ["xjxfun"]     = "ajxRunCommand",
                        ["xjxr"]       = (DateTime.UtcNow.Ticks / 10000).ToString(),
                        ["xjxargs[]"]  = $"<xjxobj><e><k>txtInput</k><v>S<![CDATA[" +
                        $"{OpCode.Substring(0, 2).ToUpper()} " +
                        $"{OpCode.Substring(2, 2).ToUpper()} " +
                        $"{OpCode.Substring(4, 2).ToUpper()} " +
                        $"{OpCode.Substring(6, 2).ToUpper()}" +
                        $"]]></v></e><e><k>txtInput2</k><v>S</v></e><e><k>opt_arch</k><v>S2</v></e></xjxobj>"
                    };
                    var response       = client.UploadValues("http://armconverter.com/hextoarm/", values);
                    var responseString = Encoding.Default.GetString(response);
                    await ctx.RespondAsync($"`{responseString.Substring(87).Split(Convert.ToChar("<"))[0]}`");
                }
            }
        }

        [Command("cc"), Description("Currency conversion"), Aliases("currency")]
        public async Task CC(CommandContext Context, string Source, string Destination, string Value)
        {
            string InitReq = WebUtils.MakeReqToShogun(Context, $"https://www.amdoren.com/api/currency.php?api_key={AmdorenAPIKey}&from={Source}&to={Destination}&amount={Value}");
            string Out     = JObject.Parse(InitReq)["amount"].ToString();
            await Context.RespondAsync($"{Value} {Source} = {Out} {Destination}");
        }

        [Command("time"), Description("Get the time at a specified timezone.")]
        public async Task Time(CommandContext Context, string Timezone)
        {
            string InitReq  = WebUtils.MakeReq($"http://api.timezonedb.com/v2/get-time-zone?key={TimeZoneDBAPIKey}&format=json&by=zone&zone={Timezone}");
            string Zonename = JObject.Parse(InitReq)["zoneName"].ToString();
            string Time     = JObject.Parse(InitReq)["formatted"].ToString();
            await Context.RespondAsync(null, false, new DiscordEmbedBuilder().WithAuthor($"Time in: {Zonename}").WithTitle($"{Time}"));
        }

        [Command("toptitles"), Description("Get the top 15 eShop titles for a specified region."), Aliases("top", "top15")]
        public async Task Top(CommandContext Context, [Description("A two-letter ISO country code.")]string Region)
        {
            Region                    = Region.ToUpper();
            string FinalReq           = WebUtils.MakeReqToShogun(Context, $"https://bugyo.hac.lp1.eshop.nintendo.net/shogun/v1/titles?shop_id=3&lang=en&country={Region}&sort=popular&limit=15&sales_status=onsale%2Cpre_order&offset=0&price_min=0.01");
            DiscordEmbedBuilder Embed = new DiscordEmbedBuilder();
            
            Embed.WithColor(DiscordColor.CornflowerBlue);

            foreach (int Num in Enumerable.Range(0, 15))
            {
                var TitleName   = JObject.Parse(FinalReq)["contents"][Num]["formal_name"].ToString();
                var ReleaseData = JObject.Parse(FinalReq)["contents"][Num]["release_date_on_eshop"].ToString();

                Embed.AddField($"{Num + 1}: {TitleName}", $"Release date: {ReleaseData}");
            }

            await Context.RespondAsync(null, false, Embed);
        }

        [Command("nsuid"), Description("Get the name of a game from its nsUid."), Aliases("ns", "uid")]
        public async Task NSUID(CommandContext Context, string Region, string ID, [RemainingText]string P)
        {
            string FinalReq = WebUtils.MakeReqToShogun(Context, $"https://bugyo.hac.lp1.eshop.nintendo.net/shogun/v1/titles/{ID}?shop_id=3&lang=en&country={Region}");
            if (P == "send")
            {
                File.WriteAllText(ID + ".json", FinalReq);
                await Context.RespondWithFileAsync(ID + ".json");
            }
            string PriceReq = WebUtils.MakeReqToShogun(Context, $"https://bugyo.hac.lp1.eshop.nintendo.net/shogun/v1/prices?shop_id=3&lang=en&ids={ID}&country={Region}").Trim(Convert.ToChar("[")).Trim(Convert.ToChar("]"));
            DiscordEmbedBuilder Embed = new DiscordEmbedBuilder();
            Embed.WithColor(DiscordColor.CornflowerBlue);
            try { Embed.WithThumbnailUrl(JObject.Parse(FinalReq)["applications"][0]["image_url"].ToString()); } catch (Exception) { }
            try { Embed.WithAuthor(JObject.Parse(FinalReq)["formal_name"].ToString()); } catch (Exception) { }
            try { Embed.WithImageUrl(JObject.Parse(FinalReq)["hero_banner_url"].ToString()); } catch (Exception) { }
            try { Embed.WithTitle(JObject.Parse(FinalReq)["publisher"]["name"].ToString()); } catch (Exception) { }
            try { Embed.WithDescription(JObject.Parse(FinalReq)["catch_copy"].ToString()); } catch (Exception) { }
            try { Embed.AddField($"Price: {JObject.Parse(PriceReq)["price"]["regular_price"]["formatted_value"].ToString()} {JObject.Parse(PriceReq)["price"]["regular_price"]["currency"].ToString()}", $"You can earn {JObject.Parse(PriceReq)["price"]["gold_point"]["gift_gp"].ToString()} gold points for this game."); } catch (Exception) { }
            try { Embed.WithFooter($"Size: {(Convert.ToDouble(JObject.Parse(FinalReq)["total_rom_size"].ToString()) / 1024768):0,00} MB / Release date: {JObject.Parse(FinalReq)["release_date_on_eshop"].ToString()}"); } catch (Exception) { }
            await Context.RespondAsync(null, false, Embed);
        }

        [Command("tid"), Description("Get the name of a game from its title ID."), Aliases("id")]
        public async Task TID(CommandContext Context, [Description("Region (As ISO two-letter country code)")] string Region, [Description("Input TID hex string.")] string TID)
        {
            if (TID.Substring(0, 4) == "0100" && TID.Length == 16 && TID.Substring(13, 3) == "000")
            {
                await Context.TriggerTypingAsync();
                string InitReq  = WebUtils.MakeReqToShogun(Context, $"https://bugyo.hac.lp1.eshop.nintendo.net/shogun/v1/contents/ids?shop_id=3&lang=en&title_ids={TID}&country={Region}&type=title");
                string ID       = JObject.Parse(InitReq)["id_pairs"][0]["id"].ToString();
                string FinalReq = WebUtils.MakeReqToShogun(Context, $"https://bugyo.hac.lp1.eshop.nintendo.net/shogun/v1/titles/{ID}?shop_id=3&lang=en&country={Region}");
                string PriceReq = WebUtils.MakeReqToShogun(Context, $"https://bugyo.hac.lp1.eshop.nintendo.net/shogun/v1/prices?shop_id=3&lang=en&ids={ID}&country={Region}").Trim(Convert.ToChar("[")).Trim(Convert.ToChar("]"));
                DiscordEmbedBuilder Embed = new DiscordEmbedBuilder();
                Embed.WithColor(DiscordColor.CornflowerBlue);
                try { Embed.WithThumbnailUrl(JObject.Parse(FinalReq)["applications"][0]["image_url"].ToString()); } catch (Exception) { }
                try { Embed.WithAuthor(JObject.Parse(FinalReq)["formal_name"].ToString()); } catch (Exception) { }
                try { Embed.WithImageUrl(JObject.Parse(FinalReq)["hero_banner_url"].ToString()); } catch (Exception) { }
                try { Embed.WithTitle(JObject.Parse(FinalReq)["publisher"]["name"].ToString()); } catch (Exception) { }
                try { Embed.WithDescription(JObject.Parse(FinalReq)["catch_copy"].ToString()); } catch (Exception) { }
                try { Embed.AddField($"Price: {JObject.Parse(PriceReq)["price"]["regular_price"]["formatted_value"].ToString()} {JObject.Parse(PriceReq)["price"]["regular_price"]["currency"].ToString()}", $"You can earn {JObject.Parse(PriceReq)["price"]["gold_point"]["gift_gp"].ToString()} gold points for this game."); } catch (Exception) { }
                try { Embed.WithFooter($"Size: {(Convert.ToDouble(JObject.Parse(FinalReq)["total_rom_size"].ToString()) / 1024768):0,00} MB / Release date: {JObject.Parse(FinalReq)["release_date_on_eshop"].ToString()}"); } catch (Exception) { }
                await Context.RespondAsync(null, false, Embed);
            }
            else if (TID.Substring(0, 4) == "0100" && TID.Length == 16 && TID.Substring(13, 3) != "000" && TID.Substring(13, 3) != "800")
            {
                await Context.TriggerTypingAsync();
                string InitReq  = WebUtils.MakeReqToShogun(Context, $"https://bugyo.hac.lp1.eshop.nintendo.net/shogun/v1/contents/ids?shop_id=3&lang=en&title_ids={TID}&country={Region}&type=aoc");
                string ID       = JObject.Parse(InitReq)["id_pairs"][0]["id"].ToString();
                string FinalReq = WebUtils.MakeReqToShogun(Context, $"https://bugyo.hac.lp1.eshop.nintendo.net/shogun/v1/aocs/{ID}?shop_id=3&lang=en&country={Region}");
                string PriceReq = WebUtils.MakeReqToShogun(Context, $"https://bugyo.hac.lp1.eshop.nintendo.net/shogun/v1/prices?shop_id=3&lang=en&ids={ID}&country={Region}").Trim(Convert.ToChar("[")).Trim(Convert.ToChar("]"));
                DiscordEmbedBuilder Embed = new DiscordEmbedBuilder();
                Embed.WithColor(DiscordColor.CornflowerBlue);
                try { Embed.WithThumbnailUrl(JObject.Parse(FinalReq)["applications"][0]["image_url"].ToString()); } catch (Exception) { }
                try { Embed.WithAuthor(JObject.Parse(FinalReq)["formal_name"].ToString()); } catch (Exception) { }
                try { Embed.WithImageUrl(JObject.Parse(FinalReq)["hero_banner_url"].ToString()); } catch (Exception) { }
                try { Embed.WithTitle(JObject.Parse(FinalReq)["publisher"]["name"].ToString()); } catch (Exception) { }
                try { Embed.WithDescription(JObject.Parse(FinalReq)["catch_copy"].ToString()); } catch (Exception) { }
                try { Embed.AddField($"Price: {JObject.Parse(PriceReq)["price"]["regular_price"]["formatted_value"].ToString()} {JObject.Parse(PriceReq)["price"]["regular_price"]["currency"].ToString()}", $"You can earn {JObject.Parse(PriceReq)["price"]["gold_point"]["gift_gp"].ToString()} gold points for this game."); } catch (Exception) { }
                try { Embed.WithFooter($"Size: {(Convert.ToDouble(JObject.Parse(FinalReq)["total_rom_size"].ToString()) / 1024768):0,00} MB / Release date: {JObject.Parse(FinalReq)["release_date_on_eshop"].ToString()}"); } catch (Exception) { }
                await Context.RespondAsync(null, false, Embed);
            }
            else if (TID.Substring(0, 4) == "0100" && TID.Length == 16 && TID.Substring(13, 3) == "800")
            {
                await Context.TriggerTypingAsync();
                TID = TID.Substring(0, 13) + "000";
                string InitReq  = WebUtils.MakeReqToShogun(Context, $"https://bugyo.hac.lp1.eshop.nintendo.net/shogun/v1/contents/ids?shop_id=3&lang=en&title_ids={TID}&country={Region}&type=title");
                string ID       = JObject.Parse(InitReq)["id_pairs"][0]["id"].ToString();
                string FinalReq = WebUtils.MakeReqToShogun(Context, $"https://bugyo.hac.lp1.eshop.nintendo.net/shogun/v1/titles/{ID}?shop_id=3&lang=en&country={Region}");
                string PriceReq = WebUtils.MakeReqToShogun(Context, $"https://bugyo.hac.lp1.eshop.nintendo.net/shogun/v1/prices?shop_id=3&lang=en&ids={ID}&country={Region}").Trim(Convert.ToChar("[")).Trim(Convert.ToChar("]"));
                DiscordEmbedBuilder Embed = new DiscordEmbedBuilder();
                Embed.WithColor(DiscordColor.CornflowerBlue);
                try { Embed.WithThumbnailUrl(JObject.Parse(FinalReq)["applications"][0]["image_url"].ToString()); } catch (Exception) { }
                try { Embed.WithAuthor(JObject.Parse(FinalReq)["formal_name"].ToString()); } catch (Exception) { }
                try { Embed.WithImageUrl(JObject.Parse(FinalReq)["hero_banner_url"].ToString()); } catch (Exception) { }
                try { Embed.WithTitle(JObject.Parse(FinalReq)["publisher"]["name"].ToString()); } catch (Exception) { }
                try { Embed.WithDescription(JObject.Parse(FinalReq)["catch_copy"].ToString()); } catch (Exception) { }
                try { Embed.AddField($"Price: {JObject.Parse(PriceReq)["price"]["regular_price"]["formatted_value"].ToString()} {JObject.Parse(PriceReq)["price"]["regular_price"]["currency"].ToString()}", $"You can earn {JObject.Parse(PriceReq)["price"]["gold_point"]["gift_gp"].ToString()} gold points for this game."); } catch (Exception) { }
                try { Embed.WithFooter($"Size: {(Convert.ToDouble(JObject.Parse(FinalReq)["total_rom_size"].ToString()) / 1024768):0,00} MB / Release date: {JObject.Parse(FinalReq)["release_date_on_eshop"].ToString()}"); } catch (Exception) { }
                await Context.RespondAsync(null, false, Embed);
            }
        }

        [Command("eshop"), Description("Search for an eShop listing."), Aliases("shop", "search", "s")]
        public async Task EShop(CommandContext Context, [RemainingText]string Query)
        {
            int Num = 0;

            string InitReq = WebUtils.MakeReqToShogun(Context, $"https://bugyo.hac.lp1.eshop.nintendo.net/shogun/v1/titles?shop_id=3&lang=en&country=AU&sort=popular&limit=9&offset=0&freeword={Query}");

            var Wait = Context.Client.GetInteractivityModule();

            DiscordEmbedBuilder Choice = new DiscordEmbedBuilder();
            Choice.WithAuthor("Make a selection:");
            Choice.WithColor(DiscordColor.Yellow);

            try
            {
                foreach (var option in Enumerable.Range(0, JObject.Parse(InitReq)["contents"].Count()))
                {
                    Choice.AddField($"{option + 1}: {JObject.Parse(InitReq)["contents"][option]["formal_name"].ToString()}", $"Released: {JObject.Parse(InitReq)["contents"][option]["release_date_on_eshop"].ToString()}");
                    Num += option;
                }

                await Context.RespondAsync(null, false, Choice);

                var msg = await Wait.WaitForMessageAsync(xm => xm.Author == Context.Member);

                if (msg != null)
                {
                    try
                    {
                        string ID = null;
                        ID = JObject.Parse(InitReq)["contents"][Convert.ToInt32(msg.Message.Content) - 1]["id"].ToString();
                        string Type = null;
                        if (ID.Substring(0, 4) == "7001")
                        {
                            Type = "titles";
                        }
                        else if (ID.Substring(0, 4) == "7005")
                        {
                            Type = "aocs";
                        }
                        else if (ID.Substring(0, 4) == "7007")
                        {
                            Type = "bundles";
                        }
                        string FinalReq = WebUtils.MakeReqToShogun(Context, $"https://bugyo.hac.lp1.eshop.nintendo.net/shogun/v1/{Type}/{ID}?shop_id=3&lang=en&country=AU");
                        string Shoplink = "Not available for purchase via website.";

                        DiscordEmbedBuilder Embed = new DiscordEmbedBuilder();

                        try { Embed.WithThumbnailUrl(JObject.Parse(FinalReq)["applications"][0]["image_url"].ToString()); } catch (Exception) { }
                        try { Embed.WithColor(new DiscordColor(JObject.Parse(FinalReq)["dominant_colors"][0].ToString())); } catch (Exception) { }
                        try { Embed.WithAuthor(JObject.Parse(FinalReq)["formal_name"].ToString()); } catch (Exception) { }
                        try { Embed.WithImageUrl(JObject.Parse(FinalReq)["hero_banner_url"].ToString()); } catch (Exception) { }
                        try { Embed.WithTitle(JObject.Parse(FinalReq)["publisher"]["name"].ToString()); } catch (Exception) { }
                        try { Embed.WithDescription(JObject.Parse(FinalReq)["catch_copy"].ToString()); } catch (Exception) { }
                        try { Embed.AddField("Title ID:", JObject.Parse(FinalReq)["applications"][0]["id"].ToString()); } catch (Exception) { }
                        try { Shoplink = $"[Buy from eShop](https://ec.nintendo.com/AU/en/{Type}/{ID})"; } catch (Exception) { }
                        try { Embed.WithFooter($"Size: {(Convert.ToDouble(JObject.Parse(FinalReq)["total_rom_size"].ToString()) / 1024768):0,0} MB / Release date: {JObject.Parse(FinalReq)["release_date_on_eshop"].ToString()}"); } catch (Exception) { }

                        var message = await Context.RespondAsync(null, false, Embed);
                        string PriceReq = WebUtils.MakeReqToShogun(Context, $"https://bugyo.hac.lp1.eshop.nintendo.net/shogun/v1/prices?shop_id=3&lang=en&ids={ID}&country=AU").Trim(Convert.ToChar("[")).Trim(Convert.ToChar("]"));
                        try { Embed.AddField($"Price: {JObject.Parse(PriceReq)["price"]["regular_price"]["formatted_value"].ToString()} {JObject.Parse(PriceReq)["price"]["regular_price"]["currency"].ToString()}", Shoplink); } catch (Exception) { }
                        await message.ModifyAsync(null, Embed);
                    }
                    catch (Exception)
                    {
                        await Context.RespondAsync($"Selection cancelled {Context.Member.Mention}.");
                    }
                }
            }
            catch (Exception)
            {
                await Context.RespondAsync($"Sorry {Context.Member.Mention}, there are no results for your query.");
            }
        }

        [Command("error"), Description("Returns details of a Switch error."), Aliases("err", "serr", "aaaaa")]
        public async Task Error(CommandContext Context, string Code)
        {
            if (Code.Substring(0, 2) == "0x")
            {
                int Err                   = Convert.ToInt32(Code.Substring(2), 16);
                string ErrorCode          = $"{(Err & 0x1FF):2000}-{((Err >> 9) & 0x3FFF):0000}";
                string[] Details          = SwitchErrorsInitialiser.Lookup(ErrorCode);

                DiscordEmbedBuilder Embed = new DiscordEmbedBuilder();

                Embed.WithColor(DiscordColor.Red);

                Embed.AddField("Error code:", ErrorCode);
                Embed.AddField("Module:", $"{Details[2]} ({(Err & 0x1FF).ToString()})");
                Embed.AddField("Details:", Details[0]);
                Embed.AddField("Reason:", Details[1]);

                await Context.RespondAsync(null, false, Embed);
            }
            else
            {
                string[] Details          = SwitchErrorsInitialiser.Lookup(Code);
                DiscordEmbedBuilder Embed = new DiscordEmbedBuilder();

                Embed.WithColor(DiscordColor.Red);

                Embed.AddField("Error code:", Code);
                Embed.AddField("Module:", $"{Details[2]} ({(Convert.ToDecimal(Code.Substring(0, 4)) - 2000).ToString()})");
                Embed.AddField("Details:", Details[0]);
                Embed.AddField("Reason:", Details[1]);

                await Context.RespondAsync(null, false, Embed);
            }
        }

        [Command("ban"), RequireUserPermissions(BanMembers)]
        public async Task Fix(CommandContext Context, DiscordMember Code)
        {
            await Context.Guild.BanMemberAsync(Code);
            await Context.RespondAsync($"Banned {Code.Username} :ok_hand:");
        }

        [Command("kick"), RequireUserPermissions(KickMembers)]
        public async Task Boot(CommandContext Context, DiscordMember Code)
        {
            await Context.Guild.RemoveMemberAsync(Code);
            await Context.RespondAsync($"Kicked {Code.Username} :ok_hand:");
        }
    
        [Command("love"), Description("Finds the love compatibility rating of two people!"), Aliases("❤")]
        public async Task Love(CommandContext Context, [Description("Name of the first person")]string FirstName, [Description("Name of the second person")]string SecondName)
        {
            var response    = (HttpWebRequest)WebRequest.Create($"https://love-calculator.p.mashape.com/getPercentage?fname={FirstName}&sname={SecondName}");

            response.Headers.Add("X-Mashape-Key", MashapeAPIKey);
            response.Accept = "application/json";

            var Rd          = new StreamReader(response.GetResponse().GetResponseStream()).ReadToEnd();
            var Percentage  = JObject.Parse(Rd)["percentage"].ToString();
            var Message     = JObject.Parse(Rd)["result"].ToString();
            var embed       = new DiscordEmbedBuilder();
            var emoji       = DiscordEmoji.FromName(Context.Client, ":heart:");

            embed.WithColor(DiscordColor.Red);
            embed.WithAuthor($"{FirstName} {emoji} {SecondName}:");
            embed.AddField($"{Percentage}% compatible with each other", Message);

            await Context.RespondAsync(null, false, embed);
        }

        [Command("junk")]
        [Description("Produces a file with random junk data under the specified filename.")]
        public async Task Junk(CommandContext Context, [RemainingText, Description("Output filename.")] string FileName)
        {
            var Buf    = new byte[262144];
            var rnd    = new RNGCryptoServiceProvider();
            var Strm   = new MemoryStream();
            var Writer = new BinaryWriter(Strm);

            rnd.GetBytes(Buf);

            Writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            Writer.Write(262144 + 0x24);
            Writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            Writer.Write(Encoding.ASCII.GetBytes("fmt "));
            Writer.Write(16);
            Writer.Write((short)1);
            Writer.Write((short)1);
            Writer.Write(32000);
            Writer.Write(64000);
            Writer.Write((short)2);
            Writer.Write((short)16);
            Writer.Write(Encoding.ASCII.GetBytes("data"));
            Writer.Write(262144);
            Writer.Write(Buf);

            Strm.Position = 0;
            await Context.RespondWithFileAsync(Strm, FileName);

            Writer.Dispose();
            Strm.Dispose();
        }

        public static string UrbanRepl(string Word)
        {
            return Word.Replace("[", "").Replace("]", "");
        }

        [Command("urban"), Description("Get a word definition from Urban Dictionary."), Aliases("ud")]
        public async Task Urban(CommandContext Context, [RemainingText]string Word)
        {
            string FinalReq           = WebUtils.MakeReq($"http://api.urbandictionary.com/v0/define?term={Word}");

            DiscordEmbedBuilder Embed = new DiscordEmbedBuilder();

            Embed.WithColor(DiscordColor.CornflowerBlue);

            Embed.WithAuthor(JObject.Parse(FinalReq)["list"][0]["word"].ToString());
            Embed.WithTitle($"Submitted by: {UrbanRepl(JObject.Parse(FinalReq)["list"][0]["author"].ToString())}");
            Embed.WithDescription($"Added on: {UrbanRepl(JObject.Parse(FinalReq)["list"][0]["written_on"].ToString())}");
            Embed.AddField("Definition:", UrbanRepl(JObject.Parse(FinalReq)["list"][0]["definition"].ToString()));
            Embed.AddField("Example:", UrbanRepl(JObject.Parse(FinalReq)["list"][0]["example"].ToString()));
            Embed.WithFooter($"Upvotes: {UrbanRepl(JObject.Parse(FinalReq)["list"][0]["thumbs_up"].ToString())} / Downvotes: {UrbanRepl(JObject.Parse(FinalReq)["list"][0]["thumbs_down"].ToString())}");

            await Context.RespondAsync(null, false, Embed);
        }

        [Command("cat"), Description("Get a random image of a cat."), Aliases("meow", "pussy")]
        public async Task Cat(CommandContext Context)
        {
            var Emoji                 = DiscordEmoji.FromName(Context.Client, ":smile:");
            string InitReq            = WebUtils.MakeReqToShogun(Context, "http://aws.random.cat/meow");
            string ID                 = JObject.Parse(InitReq)["file"].ToString();

            DiscordEmbedBuilder Embed = new DiscordEmbedBuilder();

            Embed.WithColor(DiscordColor.CornflowerBlue);
            Embed.WithAuthor($"Here's a cute cat for ya {Emoji}");
            Embed.WithImageUrl(ID);

            await Context.RespondAsync(null, false, Embed);
        }

        [Command("random")]
        public async Task Random(CommandContext ctx, string min, string max)
        {
            await ctx.RespondAsync($"🎲 Your random number is: {RandomBigInteger.NextBigInteger(BigInteger.Parse(min), BigInteger.Parse(max))}");
        }

        [Command("enc"), Description("Encode text using a 𝓶𝔂𝓼𝓽𝓮𝓻𝔂 𝓬𝓲𝓹𝓱𝓮𝓻.")]
        public async Task Enc(CommandContext Context, [RemainingText, Description("Input string.")] string Input)
        {
            byte[] GenerateRandomKey(int Length)
            {
                byte[] RandomKey = new byte[Length];
                var RNG = new RNGCryptoServiceProvider();
                RNG.GetBytes(RandomKey);

                return RandomKey;
            }

            byte[] RandK     = GenerateRandomKey(16);

            byte[] RandI     = GenerateRandomKey(16);

            byte[] Key       = AESUtils.EncryptCBC
                (
                RandK,
                HexStrToB("f3d246ab1dc49abe412a35a6cced7e0a"),
                HexStrToB("50ae431a44734f1b87db938e8e95ec8b"),
                PaddingMode.Zeros
                );

            byte[] IV        = AESUtils.EncryptCBC
                (
                RandI,
                HexStrToB("0ba58f1e74fb8ecafaf038f3ece58b48"),
                HexStrToB("37a33a96f23e53e8d7933899f733faa9"),
                PaddingMode.Zeros
                );

            byte[] FirstCrypt = AESUtils.EncryptCBC
                (
                StrToB(Input),
                Key,
                IV,
                PaddingMode.Zeros
                );

            byte[] FinalCrypt = AESUtils.EncryptECB
                (
                FirstCrypt.Concat(RandK).Concat(RandI).ToArray(),
                HexStrToB("190258605c7074194f801d0172d235eee62f0673c6fb207f21cfc9ccb514c295"),
                PaddingMode.Zeros
                );

            await Context.RespondAsync(BToHexStr(FinalCrypt));
        }

        [Command("dec"), Description("Decode text using a 𝓶𝔂𝓼𝓽𝓮𝓻𝔂 𝓬𝓲𝓹𝓱𝓮𝓻.")]
        public async Task Dec(CommandContext Context, [RemainingText, Description("Input string.")] string Input)
        {
            byte[] FinalCrypt = AESUtils.DecryptECB
                (
                HexStrToB(Input),
                HexStrToB("190258605c7074194f801d0172d235eee62f0673c6fb207f21cfc9ccb514c295"),
                PaddingMode.Zeros
                );

            byte[] RandK      = HexStrToB
                (
                BToHexStr(FinalCrypt).Substring(BToHexStr(FinalCrypt)
                .Length - 64)
                .Substring(0, 32)
                );

            byte[] RandI      = HexStrToB
                (
                BToHexStr(FinalCrypt).Substring(BToHexStr(FinalCrypt)
                .Length - 32)
                );

            byte[] Key        = AESUtils.EncryptCBC
                (
                RandK,
                HexStrToB("f3d246ab1dc49abe412a35a6cced7e0a"),
                HexStrToB("50ae431a44734f1b87db938e8e95ec8b"),
                PaddingMode.Zeros
                );

            byte[] IV         = AESUtils.EncryptCBC
                (
                RandI,
                HexStrToB("0ba58f1e74fb8ecafaf038f3ece58b48"),
                HexStrToB("37a33a96f23e53e8d7933899f733faa9"),
                PaddingMode.Zeros
                );

            await Context.RespondAsync
                (
                Encoding.UTF8.GetString(
                    (
                    AESUtils.DecryptCBC(
                        HexStrToB(BToHexStr(FinalCrypt).Substring(0, BToHexStr(FinalCrypt).Length - 64)),
                        Key,
                        IV,
                        PaddingMode.Zeros)))
                        .TrimEnd(Convert.ToChar(0x00)).Replace("@", "")
                        );
        }

        [Command("wikihow"), Aliases("wh"), Description("Returns a random out-of-context wikihow image.")]
        public async Task Wikihow(CommandContext ctx)
        {
            HttpWebRequest Request   = (HttpWebRequest)WebRequest.Create("https://hargrimm-wikihow-v1.p.mashape.com/images");
            Request.Headers.Add("X-Mashape-Key", MashapeAPIKey);
            Request.Accept           = "application/json";

            HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
            var Reader   = new StreamReader(Response.GetResponseStream());
            var Read     = Reader.ReadToEnd();

            var ImgUrl   = "https://www.wikihow.com/images/thumb/" + JObject.Parse(Read)["1"].ToString().Substring(38);

            var Embed    = new DiscordEmbedBuilder()
            {
                Color    = DiscordColor.Azure,
                ImageUrl = ImgUrl
            };

            await ctx.RespondAsync(embed: Embed);

            Reader.Dispose();
            Response.Dispose();
        }

        public static IMatrixFilter Okami
        {
            get
            {
                return new OkamiFilter();
            }
        }

        [Command("imgf"), Description("Image filtering"), Aliases("filter")]
        public async Task ImgF(CommandContext Context, string Command, [RemainingText]int Param)
        {
            HttpWebRequest Request   = (HttpWebRequest)WebRequest.Create(Context.Message.Attachments[0].Url);
            HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
            var Strm = Response.GetResponseStream();

            await Context.TriggerTypingAsync();

            IMatrixFilter Filter;

            switch (Command)
            {
                case "comic":
                    Filter = MatrixFilters.Comic;
                    break;

                case "okami":
                    Filter = Okami;
                    break;

                case "bw":
                    Filter = MatrixFilters.BlackWhite;
                    break;

                case "sepia":
                    Filter = MatrixFilters.Sepia;
                    break;

                case "invert":
                    Filter = MatrixFilters.Invert;
                    break;

                case "grey":
                    Filter = MatrixFilters.GreyScale;
                    break;

                case "gotham":
                    Filter = MatrixFilters.Gotham;
                    break;

                case "polaroid":
                    Filter = MatrixFilters.Polaroid;
                    break;

                default:
                    Filter = MatrixFilters.BlackWhite;
                    break;
            }

            var Format              = new ImageProcessor.Imaging.Formats.PngFormat();

            Stream ImgStrm          = new MemoryStream();

            using (var imageFactory = new ImageFactory())
            {
                imageFactory.Load(Strm)
                            .Format(Format)
                            .Filter(Filter)
                            .Save(ImgStrm);
            }

            await Context.RespondWithFileAsync(ImgStrm, "image.png");

            Strm.Dispose();
            ImgStrm.Dispose();
        }

        [Command("pixelate"), Description("Pixelate an image"), Aliases("pixel", "pix")]
        public async Task Pixelate(CommandContext Context, int Param)
        {
            HttpWebRequest Request   = (HttpWebRequest)WebRequest.Create(Context.Message.Attachments[0].Url);
            HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
            var Strm = Response.GetResponseStream();

            await Context.TriggerTypingAsync();

            var Format     = new ImageProcessor.Imaging.Formats.PngFormat();

            Stream ImgStrm = new MemoryStream();

            using (var imageFactory = new ImageFactory())
            {
                imageFactory.Load(Strm)
                            .Format(Format)
                            .Pixelate(Param)
                            .Save(ImgStrm);
            }

            await Context.RespondWithFileAsync(ImgStrm, "image.png");

            Strm.Dispose();

            ImgStrm.Dispose();
        }

        [Command("spin"), Description("Make a spinning gif animation of an uploaded image."), Aliases("animate", "anim")]
        public async Task Bounce(CommandContext Context)
        {
            HttpWebRequest Request   = (HttpWebRequest)WebRequest.Create(Context.Message.Attachments[0].Url);
            HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();

            var Strm = Response.GetResponseStream();

            await Context.TriggerTypingAsync();

            await Context.RespondWithFileAsync(BouncyBallAnimator.Animate(Image.FromStream(Strm)), "output.gif");
        }

        [Command("blur"), Description("Blur an image"), Aliases("gaussian", "imgb")]
        public async Task Blur(CommandContext Context, int Param)
        {
            HttpWebRequest Request   = (HttpWebRequest)WebRequest.Create(Context.Message.Attachments[0].Url);
            HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
            var Strm = Response.GetResponseStream();

            await Context.TriggerTypingAsync();

            var Format     = new ImageProcessor.Imaging.Formats.PngFormat();

            Stream ImgStrm = new MemoryStream();

            using (var imageFactory = new ImageFactory())
            {
                imageFactory.Load(Strm)
                            .Format(Format)
                            .GaussianBlur(Param)
                            .Save(ImgStrm);
            }

            await Context.RespondWithFileAsync(ImgStrm, "image.png");

            Strm.Dispose();

            ImgStrm.Dispose();
        }

        [Command("sharpen"), Description("Sharpen an image"), Aliases("sharp", "imgs")]
        public async Task Sharpen(CommandContext Context, int Param)
        {
            HttpWebRequest Request   = (HttpWebRequest)WebRequest.Create(Context.Message.Attachments[0].Url);
            HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
            var Strm = Response.GetResponseStream();

            await Context.TriggerTypingAsync();

            var Format     = new ImageProcessor.Imaging.Formats.PngFormat();

            Stream ImgStrm = new MemoryStream();

            using (var imageFactory = new ImageFactory())
            {
                imageFactory.Load(Strm)
                            .Format(Format)
                            .GaussianSharpen(Param)
                            .Save(ImgStrm);
            }

            await Context.RespondWithFileAsync(ImgStrm, "image.png");

            Strm.Dispose();

            ImgStrm.Dispose();
        }

        [Command("round"), Description("Round an image's corners"), Aliases("corners", "imgr")]
        public async Task Round(CommandContext Context, int Param)
        {
            HttpWebRequest Request   = (HttpWebRequest)WebRequest.Create(Context.Message.Attachments[0].Url);
            HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
            var Strm = Response.GetResponseStream();

            await Context.TriggerTypingAsync();

            var Format     = new ImageProcessor.Imaging.Formats.PngFormat();

            Stream ImgStrm = new MemoryStream();

            using (var imageFactory = new ImageFactory())
            {
                imageFactory.Load(Strm)
                            .Format(Format)
                            .RoundedCorners(Param)
                            .Save(ImgStrm);
            }

            await Context.RespondWithFileAsync(ImgStrm, "image.png");

            Strm.Dispose();

            ImgStrm.Dispose();
        }

        [Command("replace"), Description("Round an image's colours"), Aliases("colour", "recolour")]
        public async Task Replace(CommandContext Context, string Original, string Replacement, int Threshold)
        {
            HttpWebRequest Request   = (HttpWebRequest)WebRequest.Create(Context.Message.Attachments[0].Url);
            HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
            var Strm = Response.GetResponseStream();

            await Context.TriggerTypingAsync();

            var Format     = new ImageProcessor.Imaging.Formats.PngFormat();

            Stream ImgStrm = new MemoryStream();

            using (var imageFactory = new ImageFactory())
            {
                imageFactory.Load(Strm)
                            .Format(Format)
                            .ReplaceColor(Color.FromName(Original), Color.FromName(Replacement), Threshold)
                            .Save(ImgStrm);
            }

            await Context.RespondWithFileAsync(ImgStrm, "image.png");

            Strm.Dispose();

            ImgStrm.Dispose();
        }

        [Command("rotate"), Description("Rotate an image"), Aliases("rot", "imgrt")]
        public async Task Rotate(CommandContext Context, [Description("Number of degrees to rotate the image by.")]decimal Param)
        {
            HttpWebRequest Request   = (HttpWebRequest)WebRequest.Create(Context.Message.Attachments[0].Url);
            HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
            var Strm = Response.GetResponseStream();

            await Context.TriggerTypingAsync();

            var Format     = new ImageProcessor.Imaging.Formats.PngFormat();

            Stream ImgStrm = new MemoryStream();

            using (var imageFactory = new ImageFactory())
            {
                imageFactory.Load(Strm)
                            .Format(Format)
                            .Rotate((float)Param)
                            .Save(ImgStrm);
            }

            await Context.RespondWithFileAsync(ImgStrm, "image.png");

            Strm.Dispose();

            ImgStrm.Dispose();
        }

        [Command("8ball")]
        public async Task _8Ball(CommandContext Context)
        {
            await Context.RespondAsync(EightBall.GetRandomMessage());
        }

        [Command("thom"), Aliases("thomleg", "thomleg50")]
        public async Task Thom(CommandContext Context)
        {
            await Context.RespondAsync(ThomTools.GeneratePhrase());
        }

        [Command("touch")]
        [Description("Produces a null file with the specified filename.")]
        public async Task Touch(CommandContext Context, [RemainingText, Description("Output filename.")] string FileName)
        {
            await Context.RespondWithFileAsync(new MemoryStream(), FileName);
        }

        [Command("oof")]
        public async Task Oof(CommandContext Context)
        {
            await Context.RespondWithFileAsync(Ozone.Oof.OofAudio(), "oof.ogg");
        }

        [Command("user")]
        public async Task Userinfo(CommandContext ctx, [RemainingText]DiscordMember User)
        {
            var Embed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.CornflowerBlue
            };

            string CurrentGame = null;
            try { Embed.WithAuthor($"Info for {User.Username}:"); } catch (Exception) { }
            try { Embed.AddField($"ID:", $"{User.Id}"); } catch (Exception) { }
            try { CurrentGame = User.Presence.Game.Name; } catch (Exception) { }
            try { CurrentGame += $"\n{User.Presence.Game.Details}"; } catch (Exception) { }
            try { CurrentGame += $"\n{User.Presence.Game.State}"; } catch (Exception) { }
            try { Embed.AddField($"Currently playing:", CurrentGame ?? "Nothing."); } catch (Exception) { }
            try { Embed.AddField($"Joined Discord at:", $"{User.CreationTimestamp.UtcDateTime.ToLongDateString()} / {User.CreationTimestamp.UtcDateTime.ToLongTimeString()}"); } catch (Exception) { }
            try { Embed.AddField($"Joined this server at:", $"{User.JoinedAt.UtcDateTime.ToLongDateString()} / {User.JoinedAt.UtcDateTime.ToLongTimeString()}"); } catch (Exception) { }
            try { Embed.AddField($"Server owner:", $"{User.IsOwner}", true); } catch (Exception) { }
            try { Embed.AddField($"Is a bot:", $"{User.IsBot}", true); } catch (Exception) { }
            try { Embed.AddField($"Voice state:", $"Currently in #{User.VoiceState.Channel.Name}", true); } catch (Exception) { }

            try { Embed.WithThumbnailUrl(User.AvatarUrl); } catch (Exception) { }
            await ctx.RespondAsync(embed: Embed);
        }

        [Command("channel"), Description("Gets info for a specified channel.")]
        public async Task ChannelInfo(CommandContext ctx, DiscordChannel ID)
        {
            if (ID.Type == DSharpPlus.ChannelType.Voice)
            {
                var Embed = new DiscordEmbedBuilder();
                try { Embed.WithAuthor($"Info for {ID.Name}:"); } catch (Exception) { }
                try { Embed.AddField($"Created at:", $"{ID.CreationTimestamp.ToUniversalTime()}"); } catch (Exception) { }
                try { Embed.AddField($"Bitrate:", $"{ID.Bitrate / 1000} kbps"); } catch (Exception) { }
                try { Embed.AddField($"User limit:", $"{ID.UserLimit}"); } catch (Exception) { }
                await ctx.RespondAsync(embed: Embed);
            }
            else if (ID.Type == DSharpPlus.ChannelType.Text)
            {
                var Embed = new DiscordEmbedBuilder();
                try { Embed.WithAuthor($"Info for {ID.Name}:"); } catch (Exception) { }
                try { Embed.AddField($"Created at:", $"{ID.CreationTimestamp.ToUniversalTime()}"); } catch (Exception) { }
                try { Embed.AddField($"Topic:", $"{ID.Topic}"); } catch (Exception) { }
                try { Embed.AddField($"Category:", $"{ID.Parent.Name}"); } catch (Exception) { }
                try { Embed.AddField($"Position:", $"{ID.Position}"); } catch (Exception) { }
                try { Embed.AddField($"Is NSFW:", $"{ID.IsNSFW}"); } catch (Exception) { }
                await ctx.RespondAsync(embed: Embed);
            }
        }

        [Command("server"), Description("Gets info for this guild."), Aliases("srv", "count")]
        public async Task ServerInfo(CommandContext ctx)
        {
            var Embed = new DiscordEmbedBuilder();

            Embed.WithColor(DiscordColor.CornflowerBlue);

            try { Embed.WithAuthor($"Info for {ctx.Guild.Name}:"); } catch (Exception) { }
            try { Embed.AddField($"Created at:", $"{ctx.Guild.CreationTimestamp.ToUniversalTime()}"); } catch (Exception) { }
            try { Embed.AddField($"Owner:", $"{ctx.Guild.Owner.Mention}", true); } catch (Exception) { }
            try { Embed.AddField($"Number of members:", $"{ctx.Guild.MemberCount}", true); } catch (Exception) { }
            try { Embed.AddField($"Number of channels:", $"{ctx.Guild.Channels.Count}", true); } catch (Exception) { }
            try { Embed.AddField($"Number of roles:", $"{ctx.Guild.Roles.Count}", true); } catch (Exception) { }
            try { Embed.AddField($"Number of emojis:", $"{ctx.Guild.Emojis.Count}", true); } catch (Exception) { }
            try { Embed.AddField($"Is this guild large:", $"{ctx.Guild.IsLarge}", true); } catch (Exception) { }
            try { Embed.AddField($"Verification level:", $"{ctx.Guild.VerificationLevel}", true); } catch (Exception) { }
            try { Embed.AddField($"Region:", $"{ctx.Guild.RegionId}", true); } catch (Exception) { }
            try { Embed.WithThumbnailUrl(ctx.Guild.IconUrl); } catch (Exception) { }

            await ctx.RespondAsync(embed: Embed);
        }

        [Command("rta")]
        [Aliases("audit", "logs")]
        [Description("Get the last 10 audit log entries for this guild.")]
        [RequireUserPermissions(ViewAuditLog)]
        public async Task Audit(CommandContext Ctx)
        {
            var Logs   = await Ctx.Guild.GetAuditLogsAsync(10);

            var Output = "Last ten audit log entries:\n\n";

            foreach (int Num in Enumerable.Range(0, 9))
            {
                var Username = Logs[Num].UserResponsible.Username;
                var Discrim  = Logs[Num].UserResponsible.Discriminator;
                var Action   = Logs[Num].ActionType;
                var Time     = Logs[Num].CreationTimestamp.ToLocalTime();

                Output += $"`{Username}#{Discrim} performed {Action} at {Time}`\n";
            }

            await Ctx.RespondAsync(Output);
        }

        [Command("members"), Description("Gets the member count of each guild the bot is in.")]
        public async Task Memcount(CommandContext C)
        {
            var Str = "Number of members in each guild I'm in:";

            foreach (var Srv in C.Client.Guilds.Values)
            {
                Str += $"\n`{Srv.Name}` owned by `{Srv.Owner.Username}#{Srv.Owner.Discriminator}` has {Srv.MemberCount} members.";
            }

            await C.RespondAsync(Str);
        }

        [Command("decaesebc"), Description("Decrypt a hex string given a key."), Aliases("decebc", "de")]
        public async Task DecAESEBC(CommandContext Context, [Description("Key (as hex)")] string Key, [Description("Input data (as hex)")] string Data)
        {
            byte[] Final = AESUtils.DecryptECB(HexStrToB(Data), HexStrToB(Key), PaddingMode.None);
            await Context.RespondAsync(BToHexStr(Final).ToLower());
        }

        [Command("decaescbc"), Description("Decrypt a hex string given a key and IV."), Aliases("deccbc", "dc")]
        public async Task DecAESCBC(CommandContext Context, [Description("Key (as hex)")] string Key, [Description("IV (as hex)")] string IV, [Description("Input data (as hex)")] string Data)
        {
            byte[] Final = AESUtils.DecryptCBC(HexStrToB(Data), HexStrToB(Key), HexStrToB(IV), PaddingMode.None);
            await Context.RespondAsync(BToHexStr(Final).ToLower());
        }

        [Command("decaesctr"), Description("Decrypt a hex string given a key and nonce."), Aliases("decctr", "dr")]
        public async Task DecAESCTR(CommandContext Context, [Description("Key (as hex)")] string Key, [Description("Counter nonce (as hex)")] string CTR, [Description("Input data (as hex)")] string Data)
        {
            byte[] Final = AESUtils.DecryptCTR(HexStrToB(Key), HexStrToB(CTR), HexStrToB(Data));
            await Context.RespondAsync(BToHexStr(Final).ToLower());
        }

        [Command("encaesebc"), Description("Encrypt a hex string given a key."), Aliases("encebc", "ee")]
        public async Task EncAESEBC(CommandContext Context, [Description("Key (as hex)")] string Key, [Description("Input data (as hex)")] string Data)
        {
            byte[] Final = AESUtils.EncryptECB(HexStrToB(Data), HexStrToB(Key), PaddingMode.None);
            await Context.RespondAsync(BToHexStr(Final).ToLower());
        }

        [Command("encaescbc"), Description("Encrypt a hex string given a key and IV."), Aliases("enccbc", "ec")]
        public async Task EncAESCBC(CommandContext Context, [Description("Key (as hex)")] string Key, [Description("IV (as hex)")] string IV, [Description("Input data (as hex)")] string Data)
        {
            byte[] Final = AESUtils.EncryptCBC(HexStrToB(Data), HexStrToB(Key), HexStrToB(IV), PaddingMode.None);
            await Context.RespondAsync(BToHexStr(Final).ToLower());
        }

        [Command("encaesctr"), Description("Encrypt a hex string given a key and counter."), Aliases("encctr", "er")]
        public async Task EncAESCTR(CommandContext Context, [Description("Key (as hex)")] string Key, [Description("Counter nonce (as hex)")] string CTR, [Description("Input data (as hex)")] string Data)
        {
            byte[] Final = AESUtils.EncryptCTR(HexStrToB(Key), HexStrToB(CTR), HexStrToB(Data));
            await Context.RespondAsync(BToHexStr(Final).ToLower());
        }

        [Command("aescmac"), Description("Generate an AES-CMAC over input data."), Aliases("cmac", "mac")]
        public async Task AESCMAC(CommandContext Context, [Description("Key (as hex)")] string Key, [Description("Input data (as string)")] string Data)
        {
            byte[] Final = CMACUtils.AESCMAC(HexStrToB(Key), Encoding.UTF8.GetBytes(Data));
            await Context.RespondAsync(Convert.ToBase64String(Final));
        }

        [Command("register"), Description("Register your PRODINFO with the bot for titlekey decryption.")]
        public async Task Register(CommandContext Context)
        {
            await Context.TriggerTypingAsync();

            string Dir                = Directory.GetCurrentDirectory();
            string UserID             = Context.User.Id.ToString();

            HttpWebRequest Request    = (HttpWebRequest)WebRequest.Create(Context.Message.Attachments[0].Url);
            HttpWebResponse Response  = (HttpWebResponse)Request.GetResponse();
            BinaryReader ReadResponse = new BinaryReader(Response.GetResponseStream());

            ReadResponse.ReadBytes(0x3890);

            byte[] CTR                = ReadResponse.ReadBytes(0x10);

            byte[] Result             = ReadResponse.ReadBytes(0x230);

            string Key                = ETicketRSAKey;

            string Final              = BToHexStr(AESUtils.DecryptCTR(HexStrToB(Key), CTR, Result));

            var R                     = new RSACryptoServiceProvider(2048);

            R.ImportParameters(RSAUtils.RecoverRSAParameters
                (RSAUtils.GetBigInteger(HexStrToB(Final.Substring(512, 512))),
                 RSAUtils.GetBigInteger(HexStrToB(Final.Substring(1024, 8))),
                 RSAUtils.GetBigInteger(HexStrToB(Final.Substring(0, 512)))));

            Directory.CreateDirectory($"{Dir}/RegisteredKeys");

            TextWriter PEM            = new StreamWriter($"{Dir}/RegisteredKeys/{UserID}.pem");

            RSAUtils.ExportPrivateKey(R, PEM);

            PEM.Close();

            await Context.RespondAsync("Successfully registered your PRODINFO!");
        }

        [Command("rsa"), Description("Generate a .pem private key from E,N,D factors.")]
        public async Task RSA(CommandContext Context, string E, string N, string D)
        {
            string Dir      = Directory.GetCurrentDirectory();
            string UserID   = Context.User.Id.ToString();
            var R           = new RSACryptoServiceProvider(2048);

            R.ImportParameters(RSAUtils.RecoverRSAParameters
                (RSAUtils.GetBigInteger(HexStrToB(N)),
                 RSAUtils.GetBigInteger(HexStrToB(E)),
                 RSAUtils.GetBigInteger(HexStrToB(D))));

            var Strm        = new MemoryStream();
            TextWriter PEM  = new StreamWriter(Strm);

            RSAUtils.ExportPrivateKey(R, PEM);

            PEM.Close();

            await Context.RespondWithFileAsync(Strm, "key.pem");

            Strm.Close();
        }

        [Command("cert"), Description("Extract your cert from your PRODINFO.")]
        public async Task Cert(CommandContext Context)
        {
            await Context.TriggerTypingAsync();

            string Dir                  = Directory.GetCurrentDirectory();
            string UserID               = Context.User.Id.ToString();

            HttpWebRequest Request      = (HttpWebRequest)WebRequest.Create(Context.Message.Attachments[0].Url);
            HttpWebResponse Response    = (HttpWebResponse)Request.GetResponse();
            BinaryReader ReadResponse   = new BinaryReader(Response.GetResponseStream());

            ReadResponse.ReadBytes(0xAE0);

            byte[] Cert                 = ReadResponse.ReadBytes(0x800);

            X509Certificate certificate = new X509Certificate();
            certificate.Import(Cert);

            byte[] key                  = certificate.GetPublicKey();

            byte[] Modulus              = HexStrToB(BToHexStr(key).Substring(18, 512));

            ReadResponse.ReadBytes(0x2800);

            byte[] CTR                  = ReadResponse.ReadBytes(0x10);

            byte[] Result               = ReadResponse.ReadBytes(0x120);

            string Key                  = SSLRSAKey;

            byte[] PrivateExponent      = HexStrToB(BToHexStr(AESUtils.DecryptCTR(HexStrToB(Key), CTR, Result)).Substring(0, 512));

            var R                       = new RSACryptoServiceProvider(2048);

            R.ImportParameters(RSAUtils.RecoverRSAParameters(RSAUtils.GetBigInteger(Modulus.ToArray()), 65537, RSAUtils.GetBigInteger(PrivateExponent.ToArray())));

            Directory.CreateDirectory($"{Dir}/RegisteredCerts/{UserID}");

            TextWriter PEM              = new StreamWriter($"{Dir}/RegisteredCerts/{UserID}/key.pem");

            File.WriteAllBytes($"{Dir}/RegisteredCerts/{UserID}/cert.der", Cert);

            RSAUtils.ExportPrivateKey(R, PEM);

            PEM.Close();

            ProcessStartInfo SSL1       = new ProcessStartInfo("cert", $"x509 -inform DER -in \"{Dir}/RegisteredCerts/{UserID}/cert.der\" -outform PEM -out \"{Dir}/RegisteredCerts/{UserID}/cert.pem\"")
            {
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(SSL1);

            await Task.Delay(500);

            File.WriteAllText($"{Dir}/RegisteredCerts/{UserID}/nx_tls_client_cert.pem", File.ReadAllText($"{Dir}/RegisteredCerts/{UserID}/key.pem") + File.ReadAllText($"{Dir}/RegisteredCerts/{UserID}/cert.pem"));

            await Task.Delay(500);

            ProcessStartInfo SSL2       = new ProcessStartInfo("cert", $"pkcs12 -export -in \"{Dir}/RegisteredCerts/{UserID}/nx_tls_client_cert.pem\" -out \"{Dir}/RegisteredCerts/{UserID}/nx_tls_client_cert.pfx\" -passout pass:switch")
            {
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(SSL2);

            await Task.Delay(500);

            FileStream CertFile         = new FileStream($"{Dir}/RegisteredCerts/{UserID}/nx_tls_client_cert.pfx", FileMode.Open);
            FileStream CertPem          = new FileStream($"{Dir}/RegisteredCerts/{UserID}/nx_tls_client_cert.pem", FileMode.Open);

            await Context.RespondWithFileAsync(CertFile);
            await Context.RespondWithFileAsync(CertPem);

            CertFile.Close();

            Directory.Delete($"{Dir}/RegisteredCerts/{UserID}", true);
        }

        [Command("titlekey"), Description("Decrypt an RSA-OAEP wrapped titlekey using your registered PRODINFO."), Aliases("tk")]
        public async Task Titlekey(CommandContext Context, [Description("Encrypted titlekey (as hex)")] string MSG)
        {
            string Dir              = Directory.GetCurrentDirectory();
            string UserID           = Context.User.Id.ToString();

            if (File.Exists($"{Dir}/RegisteredKeys/{UserID}.pem"))
            {
                await Context.TriggerTypingAsync();

                File.WriteAllBytes($"temp_{UserID}.bin", HexStrToB(MSG));

                await Task.Delay(200);

                ProcessStartInfo SSL = new ProcessStartInfo("openssl.exe", $"pkeyutl -in temp_{UserID}.bin -decrypt -inkey \"{Dir}/RegisteredKeys/{UserID}.pem\" -pkeyopt rsa_padding_mode:oaep -pkeyopt rsa_oaep_md:sha256 -pkeyopt rsa_mgf1_md:sha256 -out temp_data_{UserID}.bin")
                {
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(SSL);

                await Task.Delay(200);

                await Context.RespondAsync($"Titlekey: {BToHexStr(File.ReadAllBytes($"temp_data_{UserID}.bin")).ToLower()}");
            }
            else
            {
                await Context.TriggerTypingAsync();

                await Context.RespondAsync("You aren't registered with me!\nSend me a DM with your PRODINFO and the comment \".register\"");
            }
        }

        [Command("keyblob"), Description("Decrypts your console-unique keyblobs.")]
        public async Task Keyblob(CommandContext ctx, [Description("Your TSEC key.")]string TSEC, [Description("Your SBK.")]string SBK, [Description("The keyblob key for your target firmware.")]string KeyblobKey, [Description("Your keyblob.")]string Keyblob)
        {
            var TSecBytes                   = HexStrToB(TSEC);
            var SBKBytes                    = HexStrToB(SBK);
            var KKBytes                     = HexStrToB(KeyblobKey);
            var CMACFactor                  = HexStrToB(Keyblob.Substring(0, 0x20));
            var CMACData                    = HexStrToB(Keyblob.Substring(0x20));
            var UnwrapFirst                 = AESUtils.DecryptECB(KKBytes, TSecBytes, PaddingMode.None);
            var UniqueKeyBlobKey            = AESUtils.DecryptECB(UnwrapFirst, SBKBytes, PaddingMode.None);
            var CMACKey                     = AESUtils.DecryptECB(Keys.keyblob_mac_key_source, UniqueKeyBlobKey, PaddingMode.None);
            var CMAC                        = CMACUtils.AESCMAC(CMACKey, CMACData);
            var CTR                         = HexStrToB(Keyblob.Substring(0x20, 0x20));
            var Data                        = HexStrToB(Keyblob.Substring(0x40));

            if (BToHexStr(CMAC).ToUpper() != BToHexStr(CMACFactor))
            {
                await ctx.RespondAsync($"Error: CMAC verification failed! Check something...\n{BToHexStr(CMAC).ToUpper()} != {BToHexStr(CMACFactor).ToUpper()}");
            }
            else
            {
                var DecryptedKeyBlob = AESUtils.DecryptCTR(UniqueKeyBlobKey, CTR, Data);
                var MasterKeyKek     = DecryptedKeyBlob.Take(0x10).ToArray();
                var Package1Key      = DecryptedKeyBlob.Skip(0x80).Take(0x10).ToArray();
                var MasterKey        = AESUtils.DecryptECB(Keys.master_key_source, MasterKeyKek, PaddingMode.None);

                await ctx.RespondAsync($"CMAC verification successful!\n\nKeyblob: ```\n{BToHexStr(DecryptedKeyBlob)}```Master key: {BToHexStr(MasterKey)}\nPackage1 key: {BToHexStr(Package1Key)}");
            }
        }

        [Command("db64"), Description("Decode a base64 string to ASCII")]
        public async Task DB64(CommandContext Context, string Base64)
        {
            await Context.RespondAsync($"{Encoding.UTF8.GetString(Convert.FromBase64String(Base64))}");
        }

        [Command("eb64"), Description("Encode an ASCII string to base64")]
        public async Task EB64(CommandContext Context, string Base64)
        {
            await Context.RespondAsync($"{(Convert.ToBase64String(Encoding.UTF8.GetBytes(Base64)))}");
        }
    }
}