﻿using DiscordPlayerCountBot.Providers.Base;
using DiscordPlayerCountBot.Services;
using PlayerCountBot;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordPlayerCountBot.Providers
{
    public class SteamProvider : ServerInformationProvider
    {
        public async override Task<GenericServerInformation?> GetServerInformation(BotInformation information, Dictionary<string, string> applicationVariables)
        {
            var service = new SteamService();

            try
            {
                var addressAndPort = information.GetAddressAndPort();
                var response = await service.GetSteamApiResponse(addressAndPort.Item1, addressAndPort.Item2, applicationVariables["SteamAPIKey"]);

                if (response == null)
                    throw new ApplicationException($"Server Address: {information.Address} was not found in Steam's directory.");

                HandleLastException(information);

                return new()
                {
                    Address = addressAndPort.Item1,
                    Port = addressAndPort.Item2,
                    CurrentPlayers = response.players,
                    MaxPlayers = response.max_players,
                    PlayersInQueue = response.GetQueueCount()
                };
            }
            catch (Exception e)
            {
                if (e.Message == LastException?.Message)
                    return null;

                WasLastExecutionAFailure = true;
                LastException = e;

                if (e is KeyNotFoundException keyNotFoundException)
                {
                    Logger.Error($"[SteamProvider] - SteamAPIKey is missing from Application variable configuration.");
                    return null;
                }

                if (e is HttpRequestException requestException)
                {
                    Logger.Error($"[SteamProvider] - The Steam has failed to respond. {requestException.StatusCode}");
                    return null;
                }

                if (e is WebException webException)
                {
                    if (webException.Status == WebExceptionStatus.Timeout)
                    {
                        Logger.Error($"[SteamProvider] - Speaking with Steam has timed out.");
                        return null;
                    }
                    else if (webException.Status == WebExceptionStatus.ConnectFailure)
                    {
                        Logger.Error($"[SteamProvider] - Could not connect to Steam.");
                        return null;
                    }
                    else
                    {
                        Logger.Error($"[SteamProvider] - There was an error speaking with your CFX server.", e);
                        return null;
                    }
                }

                if (e is ApplicationException applicationException)
                {
                    Logger.Error($"[SteamProvider] - {applicationException.Message}");
                    return null;
                }

                Logger.Error($"[SteamProvider] - There was an error speaking with Steam.", e);
                throw;
            }
        }
    }
}