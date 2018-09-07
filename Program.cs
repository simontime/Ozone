using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.VoiceNext;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ozone
{
    public class Program
    {
        public DiscordClient Client { get; set; }
        public VoiceNextClient VoiceCli { get; set; }
        public InteractivityModule Interactivity { get; set; }
        public CommandsNextModule Commands { get; set; }
        public static int NumOfUsers = 0;

        public async Task RunBotAsync()
        {

            var json = "";
            using (var fs = File.OpenRead("config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();

            var cfgjson = JsonConvert.DeserializeObject<ConfigJson>(json);
            var cfg = new DiscordConfiguration
            {
                Token = cfgjson.Token,
                TokenType = TokenType.Bot,

                AutoReconnect = true,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            };

            this.Client = new DiscordClient(cfg);

            this.Client.Ready += this.Client_Ready;
            this.Client.GuildAvailable += this.Client_GuildAvailable;
            this.Client.ClientErrored += this.Client_ClientError;

            var ccfg = new CommandsNextConfiguration
            {
                StringPrefix = cfgjson.CommandPrefix,

                CaseSensitive = false,

                EnableDms = true,

                EnableMentionPrefix = true
            };

            this.Client.UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehaviour = TimeoutBehaviour.Ignore,

                PaginationTimeout = TimeSpan.FromDays(7),

                Timeout = TimeSpan.FromDays(7)
            });

            var Voice = new VoiceNextConfiguration
            {
                VoiceApplication = DSharpPlus.VoiceNext.Codec.VoiceApplication.Music
            };

            this.Commands = this.Client.UseCommandsNext(ccfg);
            this.Client.UseVoiceNext(Voice);
            this.Commands.CommandExecuted += this.Commands_CommandExecuted;
            this.Commands.CommandErrored += this.Commands_CommandErrored;

            this.Commands.RegisterCommands<TopLevelCommands>();

            await this.Client.ConnectAsync();

            await Task.Delay(-1);
        }

        public static void Main(string[] args)
        {
            var prog = new Program();
            prog.RunBotAsync().GetAwaiter().GetResult();
        }

        private Task Client_Ready(ReadyEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "Ozone", "Client is ready to process events.", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "Ozone", $"Guild available: {e.Guild.Name}", DateTime.Now);
            NumOfUsers += e.Guild.MemberCount;
            return Task.CompletedTask;
        }

        private Task Client_ClientError(ClientErrorEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Error, "Ozone", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task Commands_CommandExecuted(CommandExecutionEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "Ozone", $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);
            Client.UpdateStatusAsync(new DiscordGame($"with {NumOfUsers} users! | .help"));
            return Task.CompletedTask;
        }

        private async Task Commands_CommandErrored(CommandErrorEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "Ozone", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

            if (e.Exception is ChecksFailedException ex)
            {
                var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

                var embed = new DiscordEmbedBuilder
                {
                    Title = "Access denied",
                    Description = $"{emoji} You do not have the permissions required to execute this command.",
                    Color = new DiscordColor(0xFF0000)
                };
                await e.Context.RespondAsync("", embed: embed);
            }
            else
            {
                if (
                    e.Context.Message.Content.Substring(0, 3) == ".yk" ||
                    e.Context.Message.Content.Substring(0, 3) == ".p " ||
                    e.Context.Message.Content.Substring(0, 2) == ".-" ||
                    e.Context.Message.Content.Substring(0, 2) == "._" ||
                    e.Context.Message.Content.Substring(0, 2) == ".."
                   )
                {
                   
                }
                else
                {
                    var emoji = DiscordEmoji.FromName(e.Context.Client, ":x:");

                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"Error executing the \"{e.Context.Message.Content.Substring(1).Split(' ')[0]}\" command.",
                        Description = $"{emoji} An error occurred:\n`{e.Exception.Message}`",
                        Color = new DiscordColor(0xFF0000)
                    };
                    await e.Context.RespondAsync("", embed: embed);
                }
            }
        }
    }

    public struct ConfigJson
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("prefix")]
        public string CommandPrefix { get; private set; }
    }
}