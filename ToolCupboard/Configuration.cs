using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Rocket.API;

namespace ToolCupboard
{
    public class ToolCupboardConfiguration : IRocketPluginConfiguration
    {
        public DecayUIServiceConfiguration DecayUIService { get; set; } = new DecayUIServiceConfiguration();
        public DecayServiceConfiguration DecayService { get; set; } = new DecayServiceConfiguration();
        public HealingServiceConfiguration HealingService { get; set; } = new HealingServiceConfiguration();

        public ToolCupboardConfiguration()
        {
            LoadDefaults();
        }

        public void LoadDefaults()
        {
            DecayUIService = new DecayUIServiceConfiguration
            {
                IsEnabled = false,
                EffectId = 8301,
                EffectIconUrl = "https://i.imgur.com/RKCMPg4.png"
            };

            DecayService = new DecayServiceConfiguration
            {
                IsEnabled = true,
                UseClaimFlags = true,
                UseGenerators = false,
                ShouldGeneratorsRequireFuel = true,
                UseBeds = false,
                BedRadius = 32f,
                ShouldBedsBeClaimed = true,
                UseCustomItems = false,
                CustomItems = new List<CustomItem>
                {
                    new CustomItem { Name = "Lockers", Radius = 50f, ItemIds = "328 1281" }
                },
                DamageInterval = 300,
                DamagePerInterval = 1f,
                UsePercentageDamage = false,
                ShouldBypassBed = true,
                ShouldBypassOilPump = true,
                ShouldBypassFarm = true,
                ShouldBypassTank = true,
                ShouldBypassCharge = true,
                ShouldBypassTrap = true,
                ShouldBypassBeacon = true,
                ShouldBypassStorage = true,
                BypassItems = new List<ushort> { 0 }
            };

            HealingService = new HealingServiceConfiguration
            {
                IsEnabled = true,
                UseClaimFlags = true,
                UseGenerators = false,
                ShouldGeneratorsRequireFuel = true,
                UseBeds = false,
                BedRadius = 32f,
                ShouldBedsBeClaimed = true,
                UseCustomItems = false,
                CustomItems = new List<CustomItem>
                {
                    new CustomItem { Name = "Lockers", Radius = 50f, ItemIds = "328 1281" }
                },
                HealingInterval = 600,
                HealingPerInterval = 3f,
                UsePercentageHealing = false,
                BypassItems = new List<ushort> { 0 }
            };
        }
    }

    public class DecayUIServiceConfiguration
    {
        public bool IsEnabled { get; set; } = false;
        public ushort EffectId { get; set; } = 8301;
        public string EffectIconUrl { get; set; } = "https://i.imgur.com/RKCMPg4.png";
    }

    public class DecayServiceConfiguration
    {
        public bool IsEnabled { get; set; } = true;
        public bool UseClaimFlags { get; set; } = true;
        public bool UseGenerators { get; set; } = false;
        public bool ShouldGeneratorsRequireFuel { get; set; } = true;
        public bool UseBeds { get; set; } = false;
        public float BedRadius { get; set; } = 32f;
        public bool ShouldBedsBeClaimed { get; set; } = true;
        public bool UseCustomItems { get; set; } = false;

        [XmlArray("CustomItems")]
        [XmlArrayItem("CustomItem")]
        public List<CustomItem> CustomItems { get; set; } = new List<CustomItem>();

        public uint DamageInterval { get; set; } = 300;
        public float DamagePerInterval { get; set; } = 1f;
        public bool UsePercentageDamage { get; set; } = false;

        public bool ShouldBypassBed { get; set; } = true;
        public bool ShouldBypassOilPump { get; set; } = true;
        public bool ShouldBypassFarm { get; set; } = true;
        public bool ShouldBypassTank { get; set; } = true;
        public bool ShouldBypassCharge { get; set; } = true;
        public bool ShouldBypassTrap { get; set; } = true;
        public bool ShouldBypassBeacon { get; set; } = true;
        public bool ShouldBypassStorage { get; set; } = true;

        [XmlArray("BypassItems")]
        [XmlArrayItem("Item")]
        public List<ushort> BypassItems { get; set; } = new List<ushort>();
    }

    public class HealingServiceConfiguration
    {
        public bool IsEnabled { get; set; } = true;
        public bool UseClaimFlags { get; set; } = true;
        public bool UseGenerators { get; set; } = false;
        public bool ShouldGeneratorsRequireFuel { get; set; } = true;
        public bool UseBeds { get; set; } = false;
        public float BedRadius { get; set; } = 32f;
        public bool ShouldBedsBeClaimed { get; set; } = true;
        public bool UseCustomItems { get; set; } = false;

        [XmlArray("CustomItems")]
        [XmlArrayItem("CustomItem")]
        public List<CustomItem> CustomItems { get; set; } = new List<CustomItem>();

        public uint HealingInterval { get; set; } = 600;
        public float HealingPerInterval { get; set; } = 3f;
        public bool UsePercentageHealing { get; set; } = false;

        [XmlArray("BypassItems")]
        [XmlArrayItem("Item")]
        public List<ushort> BypassItems { get; set; } = new List<ushort>();
    }

    public class CustomItem
    {
        [XmlAttribute]
        public string Name { get; set; } = "";

        [XmlAttribute]
        public float Radius { get; set; } = 50f;

        [XmlAttribute]
        public string ItemIds { get; set; } = "";

        private HashSet<ushort> parsedIds;

        public HashSet<ushort> GetParsedItemIds()
        {
            if (parsedIds != null) return parsedIds;

            parsedIds = new HashSet<ushort>();
            if (string.IsNullOrWhiteSpace(ItemIds)) return parsedIds;

            string[] tokens = ItemIds.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (ushort.TryParse(token.Trim(), out ushort id))
                {
                    parsedIds.Add(id);
                }
            }
            return parsedIds;
        }
    }
}
