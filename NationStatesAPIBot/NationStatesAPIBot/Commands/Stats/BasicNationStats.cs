﻿using Discord;
using Discord.Commands;
using NationStatesAPIBot.Types;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NationStatesAPIBot.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NationStatesAPIBot.Commands.Stats
{
    public class BasicNationStats : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<BasicNationStats> _logger;
        private readonly NationStatesApiService _apiDataService;
        private readonly DumpDataService _dumpDataService;
        private readonly Random _rnd = new Random();
        private readonly CultureInfo _locale;
        
        public BasicNationStats(ILogger<BasicNationStats> logger, NationStatesApiService apiService, DumpDataService dumpDataService, IOptions<AppSettings> config)
        {
            _logger = logger;
            _apiDataService = apiService;
            _dumpDataService = dumpDataService;
            _locale = config.Value.Locale;
        }

        [Command("nation", false), Alias("n"), Summary("Returns Basic Stats about a specific nation")]
        public async Task GetBasicStats(params string[] args)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetNationStats);
            try
            {
                string nationName = string.Join(" ", args);
                _logger.LogInformation(id, LogMessageBuilder.Build(id, $"BasicNationStats for {nationName} requested."));
                XmlDocument nationStats = await _apiDataService.GetNationStatsAsync(nationName, id);
                if (nationStats != null)
                {
                    var demonymplural = nationStats.GetElementsByTagName("DEMONYM2PLURAL")[0].InnerText;
                    var category = nationStats.GetElementsByTagName("CATEGORY")[0].InnerText;
                    var flagUrl = nationStats.GetElementsByTagName("FLAG")[0].InnerText;
                    var fullname = nationStats.GetElementsByTagName("FULLNAME")[0].InnerText;
                    var population = nationStats.GetElementsByTagName("POPULATION")[0].InnerText;
                    var region = nationStats.GetElementsByTagName("REGION")[0].InnerText;
                    var founded = nationStats.GetElementsByTagName("FOUNDED")[0].InnerText;
                    var lastActivity = nationStats.GetElementsByTagName("LASTACTIVITY")[0].InnerText;
                    var Influence = nationStats.GetElementsByTagName("INFLUENCE")[0].InnerText;
                    var wa = nationStats.GetElementsByTagName("UNSTATUS")[0].InnerText;

                    var freedom = nationStats.GetElementsByTagName("FREEDOM")[0].ChildNodes;
                    var civilStr = freedom[0].InnerText;
                    var economyStr = freedom[1].InnerText;
                    var politicalStr = freedom[2].InnerText;

                    var census = nationStats.GetElementsByTagName("CENSUS")[0].ChildNodes;
                    var civilRights = census[0].ChildNodes[0].InnerText;
                    var economy = census[1].ChildNodes[0].InnerText;
                    var politicalFreedom = census[2].ChildNodes[0].InnerText;
                    var influenceValue = census[3].ChildNodes[0].InnerText;
                    var endorsementCount = census[4].ChildNodes[0].InnerText;
                    var residency = census[5].ChildNodes[0].InnerText;
                    var residencyDbl = Convert.ToDouble(residency, _locale);
                    var residencyYears = (int)(residencyDbl / 365.242199);

                    var populationdbl = Convert.ToDouble(population);

                    var nationUrl = $"https://www.nationstates.net/nation={BaseApiService.ToID(nationName)}";
                    var regionUrl = $"https://www.nationstates.net/region={BaseApiService.ToID(region)}";

                    var builder = new EmbedBuilder();
                    builder.WithThumbnailUrl(flagUrl);
                    builder.WithTitle($"BasicStats for Nation");
                    builder.WithDescription($"**[{fullname}]({nationUrl})** {Environment.NewLine}" +
                        $"{(populationdbl / 1000.0 < 1 ? populationdbl : populationdbl / 1000.0).ToString(_locale)} {(populationdbl / 1000.0 < 1 ? "million" : "billion")} {demonymplural} | " +
                        $"Founded {founded} | " +
                        $"Last active {lastActivity}");
                    builder.AddField("Region",
                        $"[{region}]({regionUrl}) ");
                    int residencyDays = (int)(residencyDbl % 365.242199);
                    builder.AddField("Residency", $"Resident since " +
                        $"{(residencyYears < 1 ? "" : $"{residencyYears} year" + $"{(residencyYears > 1 ? "s" : "")}")} " +
                        $"{residencyDays} { (residencyDays > 1 ? $"days" : "day")}"
                        );
                    builder.AddField(category, $"C: {civilStr} ({civilRights}) | E: {economyStr} ({economy}) | P: {politicalStr} ({politicalFreedom})");
                    var waVoteString = "";
                    if (wa == "WA Member")
                    {
                        var gaVote = nationStats.GetElementsByTagName("GAVOTE")[0].InnerText;
                        var scVote = nationStats.GetElementsByTagName("SCVOTE")[0].InnerText;
                        if (!string.IsNullOrWhiteSpace(gaVote))
                        {
                            waVoteString += $"GA Vote: {gaVote} | ";
                        }
                        if (!string.IsNullOrWhiteSpace(scVote))
                        {
                            waVoteString += $"SC Vote: {scVote} | ";
                        }
                    }
                    builder.AddField(wa, $"{waVoteString} {endorsementCount} endorsements | {influenceValue} Influence ({Influence})");
                    builder.WithFooter($"NationStatesApiBot {AppSettings.VERSION} by drehtisch");
                    builder.WithColor(new Color(_rnd.Next(0, 256), _rnd.Next(0, 256), _rnd.Next(0, 256)));
                    await ReplyAsync(embed: builder.Build());
                }
                else
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle($"Something went wrong.");
                    builder.WithDescription("Probably no such nation.");
                    await ReplyAsync(embed: builder.Build());
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "A critical error occured."));
                await ReplyAsync("Something went wrong. Sorry :(");
            }
        }

        [Command("endorsed", false), Alias("e"), Summary("Returns all nations who endorsed a nation")]
        public async Task GetEndorsements(params string[] args)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetEndorsedBy);
            try
            {
                string nationName = string.Join(" ", args);
                XmlDocument nationStats = await _apiDataService.GetEndorsements(nationName, id);
                var endorsements = nationStats.GetElementsByTagName("ENDORSEMENTS")[0].InnerText;
                var builder = new EmbedBuilder();
                var nations = endorsements.Split(",").ToList(); ;
                builder.WithTitle($"{nationName} was endorsed by {nations.Count} nations:");
                if (!string.IsNullOrWhiteSpace(endorsements))
                {
                    StringBuilder sBuilder = new StringBuilder();
                    foreach (string name in nations)
                    {
                        sBuilder.Append(BaseApiService.FromID(name) + " ; ");
                    }
                    builder.WithDescription(sBuilder.ToString());
                }
                else
                {
                    builder.WithDescription("No one so far. Sorry :(");
                }
                builder.WithFooter($"NationStatesApiBot {AppSettings.VERSION} by drehtisch");
                builder.WithColor(new Color(_rnd.Next(0, 256), _rnd.Next(0, 256), _rnd.Next(0, 256)));
                //ToDo: Maybe move to embed sender ?
                var e = builder.Build();
                if (e.Length >= 2000)
                {
                    _logger.LogWarning(id, LogMessageBuilder.Build(id, $"Embeded has a length of {e.Length}"));
                }
                await ReplyAsync(embed: e);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "A critical error occured."));
                await ReplyAsync("Something went wrong. Sorry :(");
            }
        }

        [Command("nationsendorsedby", false), Alias("ne"), Summary("Returns all nations that where endorsed by a nation")]
        public async Task GetNationsendorsedby(params string[] args)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetEndorsedBy);
            try
            {
                string nationName = string.Join(" ", args);
                var builder = new EmbedBuilder();
                var endorsed = await _dumpDataService.GetNationsEndorsedBy(nationName);
                builder.WithTitle($"{nationName} has endorsed {endorsed.Count} nations:");
                if (endorsed.Count > 0)
                {
                    StringBuilder sBuilder = new StringBuilder();
                    foreach (var nation in endorsed)
                    {
                        sBuilder.Append(nation.NAME + " ; ");
                    }
                    builder.WithDescription(sBuilder.ToString());
                }
                else
                {
                    builder.WithDescription("No one so far.");
                }
                builder.WithFooter($"NationStatesApiBot {AppSettings.VERSION} by drehtisch");
                builder.WithColor(new Color(_rnd.Next(0, 256), _rnd.Next(0, 256), _rnd.Next(0, 256)));
                //ToDo: Maybe move to embed sender ?
                var e = builder.Build();
                if (e.Length >= 2000)
                {
                    _logger.LogWarning(id, LogMessageBuilder.Build(id, $"Embeded has a length of {e.Length}"));
                }
                await ReplyAsync(embed: e);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "A critical error occured."));
                await ReplyAsync("Something went wrong. Sorry :(");
            }
        }
    }
}

