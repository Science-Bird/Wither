using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;


namespace Wither
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Wither : BaseUnityPlugin
    {
        public static Wither Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }

        public static AssetBundle NetworkAssets;

        public static ConfigEntry<bool> DyingApparatusOnly;
        public static ConfigEntry<float> QuotaFraction;
        public static ConfigEntry<int> MinValue;
        public static ConfigEntry<int> MaxValue;
        public static ConfigEntry<bool> ScaleWithApparatus;
        public static ConfigEntry<int> ScalingBase;
        public static ConfigEntry<bool> MysteriousScrap;
        public static ConfigEntry<int> MysteriousScrapRarity;
        public static ConfigEntry<int> MaxCatwalkTriggers;


        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            DyingApparatusOnly = base.Config.Bind("Event", "Dying Apparatus Only", false, "Only allow the secret dying apparatus to be inserted.");
            QuotaFraction = base.Config.Bind("Event", "Quota Fraction", 0.3f, new ConfigDescription("The fraction of the quota that the average total reward value should be (the average value of all 3 items combined). Ex. 0.3 means 30% of the quota, so if your quota is 1000 the total value will add up to 300 (which is 30% of 1000). This means each individual scrap would be worth 100.", new AcceptableValueRange<float>(0.05f, 3f)));
            MinValue = base.Config.Bind("Event", "Minimum Individual Value", 50, new ConfigDescription("The minimum value an individual scrap will be worth (no other variance or modifiers will bring it below this value). Ex. at 50 the minimum for all items combined is 150.", new AcceptableValueRange<int>(10, 1000)));
            MaxValue = base.Config.Bind("Event", "Maximum Individual Value", 600, new ConfigDescription("The aximum value an individual scrap will be worth (no other variance or modifiers will bring it above this value). Ex. at 300 the maximum for all items combined is 900", new AcceptableValueRange<int>(100, 5000)));
            ScaleWithApparatus = base.Config.Bind("Event", "Scale With Apparatus", true, "Scale the value of the reward items based on the apparatus inserted.");
            ScalingBase = base.Config.Bind("Event", "Scaling Base", 50, new ConfigDescription("If 'Scale With Apparatus' is enabled, this is the 'neutral' value. So, value calculations are unchanged if the apparatus is worth exactly this much (if it's worth more, value is adjusted by how many times bigger it is, vice versa for smaller).", new AcceptableValueRange<int>(1, 200)));
            MysteriousScrap = base.Config.Bind("Mysterious Scrap", "Spawn Mysterious Scrap", false, "A strange new object appears on Wither (silly joke item not intended for serious play, no need to enable unless you're curious).");
            MysteriousScrapRarity = base.Config.Bind("Mysterious Scrap", "Mysterious Scrap Rarity", 20, new ConfigDescription("Rarity for an average scrap item is ~30 (will only ever spawn on Wither under normal circumstances).", new AcceptableValueRange<int>(1, 250)));
            MaxCatwalkTriggers = base.Config.Bind("Other Map Features", "Max Catwalk Triggers", 4, "Maximum amount of times the fragile catwalk can potentially be walked on before it breaks (will vary randomly between 2 and this number)");


            Patch();

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");

            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            NetworkAssets = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "withernetworkasset"));
            if (NetworkAssets == null)
            {
                return;
            }

            NetcodePatcher(); // ONLY RUN ONCE
        }

        internal static void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            Logger.LogDebug("Patching...");

            Harmony.PatchAll();

            Logger.LogDebug("Finished patching!");
        }

        internal static void Unpatch()
        {
            Logger.LogDebug("Unpatching...");

            Harmony?.UnpatchSelf();

            Logger.LogDebug("Finished unpatching!");
        }

        private static void NetcodePatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }
}
