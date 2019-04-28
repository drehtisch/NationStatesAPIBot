﻿using Discord;
using Microsoft.EntityFrameworkCore;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Managers;
using NationStatesAPIBot.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace NationStatesAPIBot
{
    internal class NationStatesApiController
    {
        private const string Source = "NationStatesApiController";
        internal DateTime lastAPIRequest;
        internal DateTime lastTelegramSending;
        internal DateTime lastAutomaticNewNationsRequest;
        internal DateTime lastAutomaticRegionNationsRequest;
        internal bool IsRecruiting { get; private set; }
        internal DateTime RecruitmentStarttime { get; private set; }
        /// <summary>
        /// Creates an HttpWebRequest targeted to NationStatesAPI
        /// </summary>
        /// <param name="parameters">The api parameters to pass into the request</param>
        /// <returns>A prepared HttpWebRequest ready to be executed</returns>
        internal HttpWebRequest CreateApiRequest(string parameters)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"http://www.nationstates.net/cgi-bin/api.cgi?{parameters}");
            request.Method = "GET";
            request.UserAgent = ActionManager.NationStatesAPIUserAgent;
            return request;
        }
        /// <summary>
        /// Executes an provide HttpWebRequest targeted to NationStatesAPI
        /// </summary>
        /// <param name="webRequest">The HttpWebRequest to be executed </param>
        /// <param name="type">The Type of API Action to be executed</param>
        /// <param name="isScheduled">Flag that determines whether this request is automatically (e.g. by the recruiting process) executed or not</param>
        /// <returns>The response stream of the HttpWebRequest</returns>
        private async Task<Stream> ExecuteRequestAsync(HttpWebRequest webRequest, NationStatesApiRequestType type, bool isScheduled)
        {
            try
            {
                Log(LogSeverity.Verbose, $"Waiting to execute {type}-Request. Once ActionManager grants the permit the request will be executed.");
                while (!ActionManager.IsNationStatesApiActionReady(type, isScheduled))
                {
                    await Task.Delay((int)TimeSpan.FromTicks(ActionManager.API_REQUEST_INTERVAL).TotalMilliseconds);
                }
                switch (type)
                {
                    case NationStatesApiRequestType.SendTelegram:
                    case NationStatesApiRequestType.SendRecruitmentTelegram:
                        lastTelegramSending = DateTime.UtcNow;
                        break;
                    case NationStatesApiRequestType.GetNewNations:
                        lastAutomaticNewNationsRequest = DateTime.UtcNow;
                        break;
                    case NationStatesApiRequestType.GetNationsFromRegion:
                        lastAutomaticRegionNationsRequest = DateTime.UtcNow;
                        break;
                    default:
                        lastAPIRequest = DateTime.UtcNow;
                        break;
                }
                Log(LogSeverity.Debug, $"Executing an API Call to: {webRequest.RequestUri} now.");
                var response = await webRequest.GetResponseAsync();
                return response.GetResponseStream();
            }
            catch (Exception ex)
            {
                Log(LogSeverity.Error, ex.ToString());
                return null;
            }
        }
        /// <summary>
        /// Executes an provide HttpWebRequest targeted to NationStatesAPI
        /// </summary>
        /// <param name="webRequest">The HttpWebRequest to be executed </param>
        /// <param name="type">The Type of API Action to be executed</param>
        /// <returns>The response stream of the HttpWebRequest</returns>
        internal async Task<Stream> ExecuteRequestAsync(HttpWebRequest webRequest, NationStatesApiRequestType type)
        {
            return await ExecuteRequestAsync(webRequest, type, false);
        }
        /// <summary>
        /// Executes an provide HttpWebRequest targeted to NationStatesAPI
        /// </summary>
        /// <param name="webRequest">The HttpWebRequest to be executed </param>
        /// <param name="type">The Type of API Action to be executed</param>
        /// <param name="isScheduled">Flag that determines whether this request is automatically (e.g. by the recruiting process) executed or not</param>
        /// <returns>The text read from the response stream of the HttpWebRequest</returns>
        private async Task<string> ExecuteRequestWithTextResponseAsync(HttpWebRequest webRequest, NationStatesApiRequestType type, bool isScheduled)
        {
            using (var stream = await ExecuteRequestAsync(webRequest, type, isScheduled))
            {
                if (stream != null)
                {
                    StreamReader streamReader = new StreamReader(stream);
                    return await streamReader.ReadToEndAsync();
                }
                else
                {
                    Log(LogSeverity.Warning, "Tried executing request. Return stream were null. Check if an error occurred");
                    return string.Empty;
                }
            }
        }
        /// <summary>
        /// Requests newly created Nations from NationStatesAPI
        /// </summary>
        /// <param name="isScheduled">Flag that determines whether this request is automatically (e.g. by the recruiting process) executed or not</param>
        /// <returns>List of nation names</returns>
        internal async Task<List<string>> RequestNewNationsAsync(bool isScheduled)
        {
            var request = CreateApiRequest($"q=newnations&v={ActionManager.API_VERSION}");
            XmlDocument newNationsXML = new XmlDocument();
            using (var stream = await ExecuteRequestAsync(request, NationStatesApiRequestType.GetNewNations, isScheduled))
            {
                if (stream != null)
                {
                    newNationsXML.Load(stream);
                    XmlNodeList newNationsXMLNodes = newNationsXML.GetElementsByTagName("NEWNATIONS");

                    List<string> newNations = newNationsXMLNodes[0].InnerText.Split(',').ToList().Select(nation => ToID(nation)).ToList();
                    return newNations;
                }
                else
                {
                    Log(LogSeverity.Warning, "Finishing 'RequestNewNations' with empty list because got empty stream returned. Check if an error occurred.");
                    return new List<string>();
                }
            }
        }
        /// <summary>
        /// Requests all nations from specific region
        /// </summary>
        /// <param name="region">the region from which the member nations should be requested</param>
        /// <param name="isScheduled">Flag that determines whether this request is automatically (e.g. by the recruiting process) executed or not</param>
        /// <returns>List of nation names</returns>
        internal async Task<List<string>> RequestNationsFromRegionAsync(string region, bool isScheduled)
        {
            var id = ToID(region);
            var request = CreateApiRequest($"region={id}&q=nations&v={ActionManager.API_VERSION}");
            XmlDocument nationsXML = new XmlDocument();
            using (var stream = await ExecuteRequestAsync(request, NationStatesApiRequestType.GetNationsFromRegion, isScheduled))
            {
                if (stream != null)
                {
                    nationsXML.Load(stream);
                    XmlNodeList newNationsXMLNodes = nationsXML.GetElementsByTagName("NATIONS");

                    List<string> nations = newNationsXMLNodes[0].InnerText.Split(':').ToList().Select(nation => ToID(nation)).ToList();
                    return nations;
                }
                else
                {
                    Log(LogSeverity.Warning, "Finishing 'RequestNationsFromRegion' with empty list because got empty stream returned. Check if an error occurred.");
                    return new List<string>();
                }
            }
        }
        //TODO: Add Summary
        internal List<string> MatchNationsAgainstKnownNations(List<string> newNations, string StatusName)
        {
            return MatchNationsAgainstKnownNations(newNations, StatusName, null);
        }
        //TODO: Add Summary
        internal List<string> MatchNationsAgainstKnownNations(List<string> newNations, string StatusName, string StatusDescription)
        {
            using (var context = new BotDbContext())
            {
                List<string> current;
                if (string.IsNullOrWhiteSpace(ToID(StatusDescription)))
                {
                    current = context.Nations.Where(n => n.Status.Name == StatusName).Select(n => n.Name).ToList();
                }
                else
                {
                    current = context.Nations.Where(n => n.Status.Name == StatusName && n.Status.Description == ToID(StatusDescription)).Select(n => n.Name).ToList();
                }

                return newNations.Except(current).ToList();
            }
        }
        //TODO: Add Summary, ensure that nations that are already pending, send, skipped not added again
        internal async Task AddToPending(List<string> newNations)
        {
            int counter = 0;
            using (var context = new BotDbContext())
            {
                var status = await context.NationStatuses.FirstOrDefaultAsync(n => n.Name == "pending");
                if (status == null)
                {
                    status = new NationStatus() { Name = "pending" };
                    await context.NationStatuses.AddAsync(status);
                }
                List<Nation> notAddableNations = GetNationsByStatusName("send");
                notAddableNations.AddRange(GetNationsByStatusName("skipped"));
                notAddableNations.AddRange(GetNationsByStatusName("failed"));
                notAddableNations.AddRange(GetNationsByStatusName("reserved_manual"));
                notAddableNations.AddRange(GetNationsByStatusName("reserved_api"));
                foreach (string name in newNations)
                {
                    if (!notAddableNations.Exists(n => n.Name == name))
                    {
                        await context.Nations.AddAsync(new Nation() { Name = name, StatusTime = DateTime.UtcNow, Status = status, StatusId = status.Id });
                        counter++;
                    }
                }
                Log(LogSeverity.Verbose, $"{counter} nations added to pending");
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Compares new nations to stored nations, adds new ones and purges old ones that aren't members anymore
        /// </summary>
        /// <param name="newNations">List of new nation names</param>
        /// <param name="regionName">The region name to be used</param>
        /// <returns>An Tuple Item1 = joined Nations count, Item2 = left Nations count</returns>
        internal async Task<Tuple<int, int>> SyncRegionMembersWithDatabase(List<string> newNations, string regionName)
        {
            using (var context = new BotDbContext())
            {
                var joined = MatchNationsAgainstKnownNations(newNations, "member", regionName);
                var old = context.Nations.Where(n => n.Status.Name == "member" && n.Status.Description == ToID(regionName)).Select(n => n.Name).ToList();
                var currentWithOutJoined = newNations.Except(joined);
                var left = old.Except(currentWithOutJoined);
                var leftNations = context.Nations.Where(n => left.Contains(n.Name)).ToList();
                if (leftNations.Count > 0)
                {
                    context.RemoveRange(leftNations.ToArray());
                }
                if (joined.Count > 0)
                {
                    var status = await context.NationStatuses.FirstOrDefaultAsync(n => n.Name == "member" && n.Description == ToID(regionName));
                    if (status == null)
                    {
                        status = new NationStatus() { Name = "member", Description = ToID(regionName) };
                        await context.NationStatuses.AddAsync(status);
                    }
                    foreach (string name in joined)
                    {
                        await context.Nations.AddAsync(new Nation() { Name = name, StatusTime = DateTime.UtcNow, Status = status });
                    }
                }
                await context.SaveChangesAsync();
                return new Tuple<int, int>(joined.Count, leftNations.Count);
            }
        }
        //TODO: Fill out
        /// <summary>
        /// 
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="telegramId"></param>
        /// <param name="isRecruitment"></param>
        /// <param name="isScheduled"></param>
        /// <returns></returns>
        internal async Task<bool> SendTelegramAsync(string recipient, string telegramId, string secretKey, bool isRecruitment, bool isScheduled)
        {
            try
            {
                Log(LogSeverity.Verbose, $"Sending Telegram to {recipient} scheduled: {isScheduled} recruitment: {isRecruitment}");
                var request = CreateApiRequest($"a=sendTG" +
                    $"&client={HttpUtility.UrlEncode(ActionManager.NationStatesClientKey)}" +
                    $"&tgid={HttpUtility.UrlEncode(telegramId)}" +
                    $"&key={HttpUtility.UrlEncode(secretKey)}" +
                    $"&to={HttpUtility.UrlEncode(ToID(recipient))}");
                var responseText = await ExecuteRequestWithTextResponseAsync(request, isRecruitment ?
                    NationStatesApiRequestType.SendRecruitmentTelegram :
                    NationStatesApiRequestType.SendTelegram, isScheduled);
                if (!string.IsNullOrWhiteSpace(responseText) && responseText.Contains("queued"))
                {
                    Log(LogSeverity.Info, $"Telegram to {recipient} was queued successfully.");
                    return true;
                }
                else
                {
                    throw new Exception("NationStates reported an error: " + responseText);
                }

            }
            catch (Exception ex)
            {
                Log(LogSeverity.Error, ex.ToString());
                return false;
            }
        }
        /// <summary>
        /// Sends an Recruitmentelegram to an specified recipient
        /// </summary>
        /// <param name="recipient">The name of the nation which should receive the telegram</param>
        /// <returns>If the telegram could be queued successfully</returns>
        private async Task<bool> SendRecruitmentTelegramAsync(string recipient)
        {
            return await SendTelegramAsync(recipient, ActionManager.NationStatesRecruitmentTelegramID, ActionManager.NationStatesRecruitmentTGSecretKey, true, true);
        }

        internal static void Log(LogSeverity severity, string source, string message)
        {
            Task.Run(async () => await ActionManager.LoggerInstance.LogAsync(severity, $"{Source} - {source}", message));
        }

        internal static void Log(LogSeverity severity, string message)
        {
            Task.Run(async () => await ActionManager.LoggerInstance.LogAsync(severity, Source, message));
        }
        /// <summary>
        /// Converts nation/region name to format that can be used on api calls
        /// </summary>
        /// <param name="text">The text to ensure format on</param>
        /// <returns>Formated string</returns>
        internal static string ToID(string text)
        {
            return text?.Trim().ToLower().Replace(' ', '_');
        }
        /// <summary>
        /// An API Id back to nation/region name
        /// </summary>
        /// <param name="text"></param>
        /// <returns>Formated string convert back to name</returns>
        internal static string FromID(string text)
        {
            return text?.Trim().ToLower().Replace('_', ' ');
        }

        internal void StartRecruitingAsync()
        {
            Log(LogSeverity.Info, "Starting Recruitment process.");
            IsRecruiting = true;
            RecruitmentStarttime = DateTime.UtcNow;
            var lastSend = GetNationsByStatusName("send").Take(1).ToArray();
            if (lastSend.Length > 0)
            {
                lastTelegramSending = lastSend[0].StatusTime;
            }
            Task.Run(async () => await RecruitAsync());
        }
        internal void StopRecruitingAsync()
        {
            Log(LogSeverity.Info, "Stopping Recruitment process.");
            IsRecruiting = false;
        }

        internal static async Task<List<Nation>> GetRecruitableNations(int number)
        {
            List<Nation> returnNations = new List<Nation>();
            List<Nation> pendingNations = new List<Nation>();
            if (pendingNations.Count == 0)
            {
                pendingNations = ActionManager.NationStatesApiController.GetNationsByStatusName("pending");
            }
            while (returnNations.Count < number)
            {
                var picked = pendingNations.Take(1);
                var nation = picked.Count() > 0 ? picked.ToArray()[0] : null;
                if (nation != null)
                {
                    while (!await ActionManager.NationStatesApiController.CanReceiveRecruitmentTelegram(nation.Name))
                    {
                        pendingNations.Remove(nation);
                        await ActionManager.NationStatesApiController.SetNationStatusToAsync(nation, "skipped");
                        picked = pendingNations.Take(1);
                        nation = picked.Count() > 0 ? picked.ToArray()[0] : null;
                        Log(LogSeverity.Debug, "Recruitment", $"Nation: {nation.Name} would not receive this recruitment telegram and is therefore skipped.");
                    }
                    pendingNations.Remove(nation);
                    returnNations.Add(nation);
                }
            }

            return returnNations;
        }

        internal async Task SetNationStatusToAsync(Nation nation, string statusName)
        {
            await SetNationStatusToAsync(nation, statusName, "pending");
        }

        internal async Task SetNationStatusToAsync(Nation nation, string statusName, string currentStatus)
        {
            using (var dbContext = new BotDbContext())
            {
                var current = await dbContext.NationStatuses.FirstOrDefaultAsync(n => n.Name == currentStatus);
                if (nation.StatusId == current.Id)
                {
                    var status = await dbContext.NationStatuses.FirstOrDefaultAsync(n => n.Name == statusName);
                    if (status == null)
                    {
                        status = new NationStatus() { Name = statusName };
                        await dbContext.NationStatuses.AddAsync(status);
                        await dbContext.SaveChangesAsync();
                    }
                    nation.Status = status;
                    nation.StatusId = status.Id;
                    nation.StatusTime = DateTime.UtcNow;
                    dbContext.Nations.Update(nation);
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        private async Task RecruitAsync()
        {
            List<Nation> pendingNations = new List<Nation>();
            while (IsRecruiting)
            {
                try
                {
                    if (pendingNations.Count == 0)
                    {
                        pendingNations = GetNationsByStatusName("reserved_api");
                        if (pendingNations.Count < 10)
                        {
                            pendingNations = await GetRecruitableNations(10 - pendingNations.Count);
                            foreach (var pendingNation in pendingNations)
                            {
                                await SetNationStatusToAsync(pendingNation, "reserved_api");
                            }
                        }
                    }
                    var picked = pendingNations.Take(1);
                    var nation = picked.Count() > 0 ? picked.ToArray()[0] : null;
                    if (await CanReceiveRecruitmentTelegram(nation.Name))
                    {
                        if (ActionManager.IsNationStatesApiActionReady(NationStatesApiRequestType.SendRecruitmentTelegram, true))
                        {
                            if (nation != null)
                            {
                                if (await SendRecruitmentTelegramAsync(nation.Name))
                                {
                                    await SetNationStatusToAsync(nation, "send", "reserved_api");
                                }
                                else
                                {
                                    await SetNationStatusToAsync(nation, "failed", "reserved_api");
                                    Log(LogSeverity.Error, "Recruitment", $"Telegram to {nation.Name} could not be send.");
                                }
                                pendingNations.Remove(nation);

                            }
                            else
                            {
                                Log(LogSeverity.Warning, "Recruitment", "Pending Nations empty can not send telegram: No recipient."); //To-Do: Send alert to recruiters
                            }
                        }
                    }
                    else
                    {
                        await SetNationStatusToAsync(nation, "skipped");
                    }
                    if (ActionManager.IsNationStatesApiActionReady(NationStatesApiRequestType.GetNewNations, true))
                    {
                        var result = await ActionManager.NationStatesApiController.RequestNewNationsAsync(true);
                        var newnations = ActionManager.NationStatesApiController.MatchNationsAgainstKnownNations(result, "pending");
                        await ActionManager.NationStatesApiController.AddToPending(newnations);
                    }
                }
                catch (Exception ex)
                {
                    Log(LogSeverity.Error, ex.ToString());
                }
                await Task.Delay(1000);
            }
        }

        internal async Task<bool> CanReceiveRecruitmentTelegram(string nationName)
        {
            var request = CreateApiRequest($"nation={ToID(nationName)}&q=tgcanrecruit&from={ToID(ActionManager.RegionName)}&v={ActionManager.API_VERSION}");
            XmlDocument canRecruitXML = new XmlDocument();
            using (var stream = await ExecuteRequestAsync(request, NationStatesApiRequestType.WouldReceiveRecruitmentTelegram, true))
            {
                if (stream != null)
                {
                    canRecruitXML.Load(stream);
                    XmlNodeList canRecruitNodeList = canRecruitXML.GetElementsByTagName("TGCANRECRUIT");
                    return canRecruitNodeList[0].InnerText == "1";
                }
                else
                {
                    Log(LogSeverity.Error, $"Could not determine if the nation: {nationName} would receive an recruitment telegram. Returned stream were null. Check if an error occurred.");
                    return false;
                }
            }
        }

        internal List<Nation> GetNationsByStatusName(string name)
        {
            using (var dbContext = new BotDbContext())
            {
                return dbContext.Nations.Where(n => n.Status.Name == name).OrderByDescending(n => n.StatusTime).ToList();
            }
        }
    }
}
