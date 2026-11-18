using System;
using System.Collections.Generic;
using System.Reflection;
using SDG.Unturned;
using ToolCupboard.Models;
using UnityEngine;

namespace ToolCupboard.Services
{
    public class ProtectionService
    {
        private static readonly FieldInfo BubblesField = typeof(ClaimManager).GetField("bubbles", BindingFlags.Static | BindingFlags.NonPublic);

        public List<ProtectionCenter> CollectProtectionCenters(
            bool useClaimFlags,
            bool useGenerators,
            bool requireFuel,
            bool useBeds,
            float bedRadius,
            bool bedsMustBeClaimed,
            bool useCustomItems,
            List<CustomItem> customItems)
        {
            var centers = new List<ProtectionCenter>();

            // 1. Claim Flags
            if (useClaimFlags)
            {
                try
                {
                    var bubbles = BubblesField?.GetValue(null) as List<ClaimBubble>;
                    if (bubbles != null)
                    {
                        for (int i = 0; i < bubbles.Count; i++)
                        {
                            var b = bubbles[i];
                            if (b != null)
                            {
                                centers.Add(new ProtectionCenter
                                {
                                    Position = b.origin,
                                    SqrRadius = b.sqrRadius,
                                    Radius = Mathf.Sqrt(b.sqrRadius),
                                    Owner = b.owner,
                                    Group = b.group,
                                    SourceType = "ClaimFlag"
                                });
                            }
                        }
                    }
                }
                catch { }
            }

            // Build fast lookup for custom items
            Dictionary<ushort, float> customItemRadiusMap = null;
            if (useCustomItems && customItems != null && customItems.Count > 0)
            {
                customItemRadiusMap = new Dictionary<ushort, float>();
                foreach (var ci in customItems)
                {
                    if (ci == null) continue;
                    foreach (var id in ci.GetParsedItemIds())
                    {
                        customItemRadiusMap[id] = ci.Radius;
                    }
                }
            }

            // If generators, beds, or custom items are enabled, scan active interactable barricades
            bool needsBarricadeScan = useGenerators || useBeds || (customItemRadiusMap != null && customItemRadiusMap.Count > 0);

            if (needsBarricadeScan && BarricadeManager.regions != null)
            {
                float bedSqrRadius = bedRadius * bedRadius;

                for (byte x = 0; x < Regions.WORLD_SIZE; x++)
                {
                    for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                    {
                        var region = BarricadeManager.regions[x, y];
                        if (region?.drops == null) continue;

                        for (int i = 0; i < region.drops.Count; i++)
                        {
                            var drop = region.drops[i];
                            if (drop == null) continue;

                            var data = drop.GetServersideData();
                            if (data == null) continue;

                            Vector3 pos = drop.model != null ? drop.model.position : data.point;
                            ulong owner = data.owner;
                            ulong group = data.group;
                            ushort itemId = drop.asset != null ? drop.asset.id : (ushort)0;

                            // Fallback for Claim Flags if reflection bubbles was empty
                            if (useClaimFlags && drop.interactable is InteractableClaim claim && centers.Count == 0)
                            {
                                centers.Add(new ProtectionCenter
                                {
                                    Position = pos,
                                    Radius = 32f,
                                    SqrRadius = 1024f,
                                    Owner = claim.owner != 0 ? claim.owner : owner,
                                    Group = claim.group != 0 ? claim.group : group,
                                    SourceType = "ClaimFlag"
                                });
                            }

                            // 2. Generators
                            if (useGenerators && drop.interactable is InteractableGenerator gen)
                            {
                                bool isPowered = gen.isPowered;
                                bool hasFuel = !requireFuel || gen.fuel > 0;

                                if (isPowered && hasFuel)
                                {
                                    float range = gen.wirerange > 0 ? gen.wirerange : 64f;
                                    centers.Add(new ProtectionCenter
                                    {
                                        Position = pos,
                                        Radius = range,
                                        SqrRadius = range * range,
                                        Owner = owner,
                                        Group = group,
                                        SourceType = "Generator"
                                    });
                                }
                            }

                            // 3. Beds
                            if (useBeds && drop.interactable is InteractableBed bed)
                            {
                                bool isClaimed = bed.isClaimed;
                                if (!bedsMustBeClaimed || isClaimed)
                                {
                                    ulong bedOwner = isClaimed ? bed.owner.m_SteamID : owner;
                                    centers.Add(new ProtectionCenter
                                    {
                                        Position = pos,
                                        Radius = bedRadius,
                                        SqrRadius = bedSqrRadius,
                                        Owner = bedOwner,
                                        Group = group,
                                        SourceType = "Bed"
                                    });
                                }
                            }

                            // 4. Custom Items
                            if (customItemRadiusMap != null && itemId != 0 && customItemRadiusMap.TryGetValue(itemId, out float customRadius))
                            {
                                centers.Add(new ProtectionCenter
                                {
                                    Position = pos,
                                    Radius = customRadius,
                                    SqrRadius = customRadius * customRadius,
                                    Owner = owner,
                                    Group = group,
                                    SourceType = "CustomItem"
                                });
                            }
                        }
                    }
                }
            }

            return centers;
        }

        public bool IsProtected(Vector3 point, ulong owner, ulong group, List<ProtectionCenter> centers)
        {
            if (centers == null || centers.Count == 0)
                return false;

            for (int i = 0; i < centers.Count; i++)
            {
                if (centers[i].Protects(point, owner, group))
                    return true;
            }

            return false;
        }
    }
}
