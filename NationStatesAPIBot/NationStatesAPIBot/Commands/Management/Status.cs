using Discord;
using Discord.Commands;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NationStatesAPIBot.Services;

namespace NationStatesAPIBot.Commands.Management
{
    public class Status : ModuleBase<SocketCommandContext>
    {
        private readonly AppSettings _config;
        private Random random;
        public Status(IOptions<AppSettings> config)
        {
            _config = config.Value;
            random = new Random();
        }

        [Command("status"), Summary("Returns some status information.")]
        public async Task GetStatus()
        {
            var builder = new EmbedBuilder();
            builder.WithTitle("Bot Status");
            var configuration = "Production";
            var adminUser = Context.Client.GetUser(_config.DiscordBotAdminUser);
            var startTime = Program.StartTime;
            var uptime = DateTime.UtcNow.Subtract(startTime);
#if DEBUG
            configuration = "Development";
#endif
            builder.WithFields(new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder()
                {
                    Name = "Version:",
                    Value = AppSettings.VERSION
                },
                new EmbedFieldBuilder()
                {
                    Name = "Configuration:",
                    Value = configuration
                },
                new EmbedFieldBuilder()
                {
                    Name = $"{AppSettings.BOT_ADMIN_TERM} (Bot Admin/Developer)",
                    Value = $"{adminUser.Username}#{adminUser.Discriminator}"
                },
                new EmbedFieldBuilder()
                {
                    Name = "Number of Users on this Server:",
                    Value = Context.Guild != null ? Context.Guild.Users.Count : 0
                },
                new EmbedFieldBuilder()
                {
                    Name = "Uptime",
                    Value = $"{uptime.Days} Days {uptime.Hours} Hours {uptime.Minutes} Minutes"
                },
                new EmbedFieldBuilder()
                {
                    Name = "Dump Data",
                    Value = $"Available: {DumpDataService.DataAvailable}; Updating: {DumpDataService.IsUpdating}"
                },
                new EmbedFieldBuilder()
                {
                    Name = "Last Dump Data Update",
                    Value = $"{(DumpDataService.LastDumpUpdateTimeUtc == DateTime.UnixEpoch || (!DumpDataService.DataAvailable && DumpDataService.IsUpdating)?"Updating":DateTime.UtcNow.Subtract(DumpDataService.LastDumpUpdateTimeUtc).ToString("h'h 'm'm 's's'") + " ago")}"
                },
                new EmbedFieldBuilder()
                {
                    Name = "Recruitment",
                    Value = RecruitmentService.RecruitmentStatus
                },
                new EmbedFieldBuilder()
                {
                    Name = "Pool Status",
                    Value = RecruitmentService.PoolStatus
                }
            });
            await ReplyAsync(embed: builder.Build());
        }

        [Command("ping"), Summary("Does reply Pong on receiving Ping")]
        public async Task DoPing()
        {
            if (random.Next(0, 100) < 6)
            {
                await ReplyAsync("HA! Ponged!");
            }
            else
            {
                await ReplyAsync("Pong !");
            }
        }
    }
}
