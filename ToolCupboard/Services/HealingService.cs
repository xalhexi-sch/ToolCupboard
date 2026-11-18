using System;
using System.Collections.Generic;
using System.Threading;
using Rocket.Core.Utils;
using SDG.Unturned;
using ToolCupboard.Models;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace ToolCupboard.Services
{
    public class HealingService
    {
        private readonly ToolCupboardPlugin plugin;
        private readonly HealingServiceConfiguration config;
        private readonly ProtectionService protectionService;
        private Timer healingTimer;
        private bool isProcessing = false;

        public HealingService(ToolCupboardPlugin plugin, HealingServiceConfiguration config, ProtectionService protectionService)
        {
            this.plugin = plugin;
            this.config = config ?? new HealingServiceConfiguration();
            this.protectionService = protectionService;
        }

        public void Initialize()
        {
            if (!config.IsEnabled) return;

            int intervalMs = (int)Math.Max(5, config.HealingInterval) * 1000;
            healingTimer = new Timer(OnHealingTick, null, intervalMs, intervalMs);
        }

        public void Shutdown()
        {
            healingTimer?.Dispose();
            healingTimer = null;
        }

        private void OnHealingTick(object state)
        {
            if (!config.IsEnabled || isProcessing) return;

            TaskDispatcher.QueueOnMainThread(() =>
            {
                if (isProcessing) return;
                isProcessing = true;

                try
                {
                    PerformHealingCycle();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"[ToolCupboard] Exception in HealingService: {ex.Message}");
                }
                finally
                {
                    isProcessing = false;
                }
            });
        }

        public void PerformHealingCycle()
        {
            var centers = protectionService.CollectProtectionCenters(
                config.UseClaimFlags,
                config.UseGenerators,
                config.ShouldGeneratorsRequireFuel,
                config.UseBeds,
                config.BedRadius,
                config.ShouldBedsBeClaimed,
                config.UseCustomItems,
                config.CustomItems
            );

            if (centers == null || centers.Count == 0)
                return;

            // 1. Repair Barricades
            if (BarricadeManager.regions != null)
            {
                for (byte x = 0; x < Regions.WORLD_SIZE; x++)
                {
                    for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                    {
                        var region = BarricadeManager.regions[x, y];
                        if (region?.drops == null) continue;

                        for (int i = 0; i < region.drops.Count; i++)
                        {
                            var drop = region.drops[i];
                            if (drop == null || drop.model == null) continue;

                            var data = drop.GetServersideData();
                            if (data == null || data.barricade == null) continue;

                            ushort itemId = drop.asset != null ? drop.asset.id : (ushort)0;
                            if (config.BypassItems != null && config.BypassItems.Contains(itemId))
                                continue;

                            ushort maxHp = drop.asset != null ? drop.asset.health : (ushort)100;
                            if (data.barricade.health >= maxHp)
                                continue;

                            Vector3 pos = drop.model.position;
                            if (!protectionService.IsProtected(pos, data.owner, data.group, centers))
                                continue;

                            float healAmount = config.UsePercentageHealing
                                ? Mathf.Max(1f, maxHp * (config.HealingPerInterval / 100.0f))
                                : config.HealingPerInterval;

                            BarricadeManager.repair(drop.model, healAmount, 1.0f);
                        }
                    }
                }
            }

            // 2. Repair Structures
            if (StructureManager.regions != null)
            {
                for (byte x = 0; x < Regions.WORLD_SIZE; x++)
                {
                    for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                    {
                        var region = StructureManager.regions[x, y];
                        if (region?.drops == null) continue;

                        for (int i = 0; i < region.drops.Count; i++)
                        {
                            var drop = region.drops[i];
                            if (drop == null || drop.model == null) continue;

                            var data = drop.GetServersideData();
                            if (data == null || data.structure == null) continue;

                            ushort itemId = drop.asset != null ? drop.asset.id : (ushort)0;
                            if (config.BypassItems != null && config.BypassItems.Contains(itemId))
                                continue;

                            ushort maxHp = drop.asset != null ? drop.asset.health : (ushort)100;
                            if (data.structure.health >= maxHp)
                                continue;

                            Vector3 pos = drop.model.position;
                            if (!protectionService.IsProtected(pos, data.owner, data.group, centers))
                                continue;

                            float healAmount = config.UsePercentageHealing
                                ? Mathf.Max(1f, maxHp * (config.HealingPerInterval / 100.0f))
                                : config.HealingPerInterval;

                            StructureManager.repair(drop.model, healAmount, 1.0f);
                        }
                    }
                }
            }
        }
    }
}
