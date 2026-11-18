using System;
using Rocket.API.Collections;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using ToolCupboard.Services;

namespace ToolCupboard
{
    public class ToolCupboardPlugin : RocketPlugin<ToolCupboardConfiguration>
    {
        public static ToolCupboardPlugin Instance { get; private set; }

        public ProtectionService ProtectionService { get; private set; }
        public DecayService DecayService { get; private set; }
        public HealingService HealingService { get; private set; }
        public DecayUIService DecayUIService { get; private set; }

        protected override void Load()
        {
            Instance = this;

            try
            {
                if (Configuration != null && Configuration.Instance == null)
                {
                    Configuration.Instance = new ToolCupboardConfiguration();
                    Configuration.Instance.LoadDefaults();
                }

                var config = Configuration?.Instance ?? new ToolCupboardConfiguration();
                if (config.DecayService == null) config.DecayService = new DecayServiceConfiguration();
                if (config.HealingService == null) config.HealingService = new HealingServiceConfiguration();
                if (config.DecayUIService == null) config.DecayUIService = new DecayUIServiceConfiguration();

                ProtectionService = new ProtectionService();

                DecayService = new DecayService(this, config.DecayService, ProtectionService);
                DecayService.Initialize();

                HealingService = new HealingService(this, config.HealingService, ProtectionService);
                HealingService.Initialize();

                DecayUIService = new DecayUIService(this, config.DecayUIService, config.DecayService, ProtectionService);
                DecayUIService.Initialize();

                Logger.Log($"[ToolCupboard] {Name} {Assembly.GetName().Version} loaded successfully.");
                Logger.Log("[ToolCupboard] Credits to Tespy for sponsoring percentage damage and custom items features.");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[ToolCupboard] Exception in Load(): {ex}");
            }
        }

        protected override void Unload()
        {
            try
            {
                DecayService?.Shutdown();
                HealingService?.Shutdown();
                DecayUIService?.Shutdown();

                Instance = null;
                Logger.Log($"[ToolCupboard] {Name} unloaded.");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[ToolCupboard] Exception in Unload(): {ex}");
            }
        }

        public override TranslationList DefaultTranslations => new TranslationList
        {
            { "UI_TEXT", "Your base is decaying!" }
        };
    }
}
