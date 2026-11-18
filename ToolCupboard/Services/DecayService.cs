using System;
using System.Collections.Generic;
using System.Threading;
using Rocket.Core.Utils;
using SDG.Unturned;
using Steamworks;
using ToolCupboard.Models;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace ToolCupboard.Services
{
    public class DecayService
    {
        private readonly ToolCupboardPlugin plugin;
        private readonly DecayServiceConfiguration config;
        private readonly ProtectionService protectionService;
        private Timer decayTimer;
        private bool isProcessing = false;

        public DecayService(ToolCupboardPlugin plugin, DecayServiceConfiguration config, ProtectionService protectionService)
        {
            this.plugin = plugin;
            this.config = config ?? new DecayServiceConfiguration();
            this.protectionService = protectionService;
        }

        public void Initialize()
        {
            if (!config.IsEnabled) return;

            int intervalMs = (int)Math.Max(5, config.DamageInterval) * 1000;
            decayTimer = new Timer(OnDecayTick, null, intervalMs, intervalMs);
        }

        public void Shutdown()
        {
            decayTimer?.Dispose();
            decayTimer = null;
        }

        private void OnDecayTick(object state)
        {
            if (!config.IsEnabled || isProcessing) return;

            TaskDispatcher.QueueOnMainThread(() =>
            {
                if (isProcessing) return;
                isProcessing = true;

                try
                {
                    PerformDecayCycle();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"[ToolCupboard] Exception in DecayService: {ex.Message}");
                }
                finally
                {
                    isProcessing = false;
                }
            });
        }

        public void PerformDecayCycle()
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

            // 1. Scan Barricades
            if (BarricadeManager.regions != null)
            {
                for (byte x = 0; x < Regions.WORLD_SIZE; x++)
                {
                    for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                    {
                        var region = BarricadeManager.regions[x, y];
                        if (region?.drops == null) continue;

                        for (int i = region.drops.Count - 1; i >= 0; i--)
                        {
                            if (i >= region.drops.Count) continue;
                            var drop = region.drops[i];
                            if (drop == null || drop.model == null) continue;

                            var data = drop.GetServersideData();
                            if (data == null || data.barricade == null) continue;

                            ushort itemId = drop.asset != null ? drop.asset.id : (ushort)0;
                            if (IsBypassed(itemId, drop.asset, drop.interactable))
                                continue;

                            Vector3 pos = drop.model.position;
                            if (protectionService.IsProtected(pos, data.owner, data.group, centers))
                                continue;

                            ushort maxHp = drop.asset != null ? drop.asset.health : (ushort)100;
                            float dmgAmount = config.UsePercentageDamage
                                ? Mathf.Max(1f, maxHp * (config.DamagePerInterval / 100.0f))
                                : config.DamagePerInterval;

                            BarricadeManager.damage(drop.model, dmgAmount, 1.0f, false, CSteamID.Nil, EDamageOrigin.Unknown);
                        }
                    }
                }
            }

            // 2. Scan Structures
            if (StructureManager.regions != null)
            {
                for (byte x = 0; x < Regions.WORLD_SIZE; x++)
                {
                    for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                    {
                        var region = StructureManager.regions[x, y];
                        if (region?.drops == null) continue;

                        for (int i = region.drops.Count - 1; i >= 0; i--)
                        {
                            if (i >= region.drops.Count) continue;
                            var drop = region.drops[i];
                            if (drop == null || drop.model == null) continue;

                            var data = drop.GetServersideData();
                            if (data == null || data.structure == null) continue;

                            ushort itemId = drop.asset != null ? drop.asset.id : (ushort)0;
                            if (config.BypassItems != null && config.BypassItems.Contains(itemId))
                                continue;

                            Vector3 pos = drop.model.position;
                            if (protectionService.IsProtected(pos, data.owner, data.group, centers))
                                continue;

                            ushort maxHp = drop.asset != null ? drop.asset.health : (ushort)100;
                            float dmgAmount = config.UsePercentageDamage
                                ? Mathf.Max(1f, maxHp * (config.DamagePerInterval / 100.0f))
                                : config.DamagePerInterval;

                            StructureManager.damage(drop.model, Vector3.up, dmgAmount, 1.0f, false, CSteamID.Nil, EDamageOrigin.Unknown);
                        }
                    }
                }
            }
        }

        private bool IsBypassed(ushort itemId, ItemBarricadeAsset asset, Interactable interactable)
        {
            if (config.BypassItems != null && config.BypassItems.Contains(itemId))
                return true;

            if (config.ShouldBypassBed && (interactable is InteractableBed || (asset != null && asset.build == EBuild.BED)))
                return true;

            if (config.ShouldBypassOilPump && (interactable is InteractableOil || (asset != null && asset.build == EBuild.OIL)))
                return true;

            if (config.ShouldBypassFarm && (interactable is InteractableFarm || (asset != null && asset.build == EBuild.FARM)))
                return true;

            if (config.ShouldBypassTank && (interactable is InteractableTank || (asset != null && (asset.build == EBuild.TANK || asset.build == EBuild.BARREL_RAIN))))
                return true;

            if (config.ShouldBypassCharge && (interactable is InteractableCharge || (asset != null && asset.build == EBuild.CHARGE)))
                return true;

            if (config.ShouldBypassTrap && (interactable is InteractableTrap || (asset != null && (asset.build == EBuild.SPIKE || asset.build == EBuild.WIRE))))
                return true;

            if (config.ShouldBypassBeacon && (asset != null && asset.build == EBuild.BEACON))
                return true;

            if (config.ShouldBypassStorage && (interactable is InteractableStorage || (asset != null && (asset.build == EBuild.STORAGE || asset.build == EBuild.STORAGE_WALL))))
                return true;

            return false;
        }
    }
}
