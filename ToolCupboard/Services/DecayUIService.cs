#pragma warning disable CS0612, CS0618

using System;
using System.Collections.Generic;
using System.Threading;
using Rocket.Core.Utils;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using ToolCupboard.Models;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace ToolCupboard.Services
{
    public class DecayUIService
    {
        private readonly ToolCupboardPlugin plugin;
        private readonly DecayUIServiceConfiguration config;
        private readonly DecayServiceConfiguration decayConfig;
        private readonly ProtectionService protectionService;
        private Timer checkTimer;
        private readonly HashSet<ulong> activeUiPlayers = new HashSet<ulong>();
        private readonly object syncLock = new object();
        private bool isProcessing = false;

        public DecayUIService(ToolCupboardPlugin plugin, DecayUIServiceConfiguration config, DecayServiceConfiguration decayConfig, ProtectionService protectionService)
        {
            this.plugin = plugin;
            this.config = config ?? new DecayUIServiceConfiguration();
            this.decayConfig = decayConfig ?? new DecayServiceConfiguration();
            this.protectionService = protectionService;
        }

        public void Initialize()
        {
            if (!config.IsEnabled) return;

            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            checkTimer = new Timer(OnCheckTick, null, 3000, 3000);
        }

        public void Shutdown()
        {
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            checkTimer?.Dispose();
            checkTimer = null;

            ClearAllActiveUI();
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (player == null) return;
            ulong steamId = player.CSteamID.m_SteamID;

            lock (syncLock)
            {
                if (activeUiPlayers.Remove(steamId))
                {
                    try
                    {
                        EffectManager.askEffectClearByID(config.EffectId, player.CSteamID);
                    }
                    catch { }
                }
            }
        }

        private void ClearAllActiveUI()
        {
            lock (syncLock)
            {
                foreach (ulong steamId in activeUiPlayers)
                {
                    try
                    {
                        EffectManager.askEffectClearByID(config.EffectId, new CSteamID(steamId));
                    }
                    catch { }
                }
                activeUiPlayers.Clear();
            }
        }

        private void OnCheckTick(object state)
        {
            if (!config.IsEnabled || isProcessing) return;

            TaskDispatcher.QueueOnMainThread(() =>
            {
                if (isProcessing) return;
                isProcessing = true;

                try
                {
                    CheckPlayersInDecayingBases();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"[ToolCupboard] Exception in DecayUIService: {ex.Message}");
                }
                finally
                {
                    isProcessing = false;
                }
            });
        }

        private void CheckPlayersInDecayingBases()
        {
            if (Provider.clients == null || Provider.clients.Count == 0)
                return;

            var centers = protectionService.CollectProtectionCenters(
                decayConfig.UseClaimFlags,
                decayConfig.UseGenerators,
                decayConfig.ShouldGeneratorsRequireFuel,
                decayConfig.UseBeds,
                decayConfig.BedRadius,
                decayConfig.ShouldBedsBeClaimed,
                decayConfig.UseCustomItems,
                decayConfig.CustomItems
            );

            float checkRadiusSqr = 24f * 24f; // 24 meter player vicinity to check for base ownership

            for (int p = 0; p < Provider.clients.Count; p++)
            {
                var client = Provider.clients[p];
                if (client?.player == null || client.player.life.isDead || ReferenceEquals(client.playerID, null))
                    continue;

                ulong steamId = client.playerID.steamID.m_SteamID;
                ulong groupId = client.player.quests != null ? client.player.quests.groupID.m_SteamID : 0;
                Vector3 playerPos = client.player.transform.position;

                bool inDecayingBase = IsPlayerNearDecayingOwnedBuilding(playerPos, steamId, groupId, centers, checkRadiusSqr);

                lock (syncLock)
                {
                    bool currentlyShowing = activeUiPlayers.Contains(steamId);

                    if (inDecayingBase && !currentlyShowing)
                    {
                        try
                        {
                            string message = plugin.Translate("UI_TEXT");
                            EffectManager.sendUIEffect(config.EffectId, (short)config.EffectId, client.playerID.steamID, true, message);

                            if (!string.IsNullOrEmpty(config.EffectIconUrl))
                            {
                                EffectManager.sendUIEffectImageURL((short)config.EffectId, client.playerID.steamID, true, "Icon", config.EffectIconUrl);
                            }

                            activeUiPlayers.Add(steamId);
                        }
                        catch { }
                    }
                    else if (!inDecayingBase && currentlyShowing)
                    {
                        try
                        {
                            EffectManager.askEffectClearByID(config.EffectId, client.playerID.steamID);
                        }
                        catch { }
                        activeUiPlayers.Remove(steamId);
                    }
                }
            }
        }

        private bool IsPlayerNearDecayingOwnedBuilding(Vector3 playerPos, ulong steamId, ulong groupId, List<ProtectionCenter> centers, float checkRadiusSqr)
        {
            // Check Barricades near player
            if (BarricadeManager.regions != null && Regions.tryGetCoordinate(playerPos, out byte rx, out byte ry))
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = rx + dx;
                    if (nx < 0 || nx >= Regions.WORLD_SIZE) continue;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int ny = ry + dy;
                        if (ny < 0 || ny >= Regions.WORLD_SIZE) continue;

                        var region = BarricadeManager.regions[nx, ny];
                        if (region?.drops == null) continue;

                        for (int i = 0; i < region.drops.Count; i++)
                        {
                            var drop = region.drops[i];
                            if (drop == null || drop.model == null) continue;

                            var data = drop.GetServersideData();
                            if (data == null) continue;

                            bool isOwned = (data.owner != 0 && data.owner == steamId) || (groupId != 0 && data.group == groupId);
                            if (!isOwned) continue;

                            if ((drop.model.position - playerPos).sqrMagnitude <= checkRadiusSqr)
                            {
                                if (!protectionService.IsProtected(drop.model.position, data.owner, data.group, centers))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            // Check Structures near player
            if (StructureManager.regions != null && Regions.tryGetCoordinate(playerPos, out byte sx, out byte sy))
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = sx + dx;
                    if (nx < 0 || nx >= Regions.WORLD_SIZE) continue;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int ny = sy + dy;
                        if (ny < 0 || ny >= Regions.WORLD_SIZE) continue;

                        var region = StructureManager.regions[nx, ny];
                        if (region?.drops == null) continue;

                        for (int i = 0; i < region.drops.Count; i++)
                        {
                            var drop = region.drops[i];
                            if (drop == null || drop.model == null) continue;

                            var data = drop.GetServersideData();
                            if (data == null) continue;

                            bool isOwned = (data.owner != 0 && data.owner == steamId) || (groupId != 0 && data.group == groupId);
                            if (!isOwned) continue;

                            if ((drop.model.position - playerPos).sqrMagnitude <= checkRadiusSqr)
                            {
                                if (!protectionService.IsProtected(drop.model.position, data.owner, data.group, centers))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
