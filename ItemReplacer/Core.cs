using System.Text.RegularExpressions;

using BoneLib;

using Il2CppSLZ.Marrow.Warehouse;

using ItemReplacer.Managers;
using ItemReplacer.Patches;
using ItemReplacer.Utilities;

using MelonLoader;

namespace ItemReplacer
{
    public static class ModInfo
    {
        public const string Name = "ItemReplacer";

        public const string Author = "T&H Modding";
        public const string ThunderstoreAuthor = "TH_Modding";

        public const string Version = "1.1.0";
        public const string Description = "Item Replacer is a code mod that aims to allow players to easily create replacements for in-game items";
        public const string DownloadLink = $"https://thunderstore.io/c/bonelab/p/{ThunderstoreAuthor}/{Name}/";
    }

    public class Core : MelonMod
    {
        public static MelonLogger.Instance Logger { get; private set; }

        public static Thunderstore Thunderstore { get; private set; }

        private bool thunderstoreNotif;

        public static Core Instance { get; private set; }

        public override void OnInitializeMelon()
        {
            Instance = this;
            Logger = LoggerInstance;

            LoggerInstance.Msg("Loading dependencies");

            LoggerInstance.Msg("Setting up preferences");
            PreferencesManager.Setup();

            Fusion.Setup();

            LoggerInstance.Msg("Checking for updates");

            Thunderstore = new($"{ModInfo.Name} / {ModInfo.Version} A BONELAB Mod");
            Thunderstore.BL_FetchPackage(ModInfo.Name, ModInfo.ThunderstoreAuthor, ModInfo.Version, LoggerInstance);

            Hooking.OnLevelLoaded += OnLevelLoad;

            LoggerInstance.Msg("Setting up replacers");
            ReplacerManager.Setup();
            ReplacerManager.CreateFileWatcher();

            AssetWarehouse._onReady += (System.Action)(() =>
            {
                LoggerInstance.Msg("Setting up BoneMenu");
                MenuManager.Setup();
            });

            LoggerInstance.Msg("Initialized.");
        }

        public static string RemoveUnityRichText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            return Regex.Replace(text, "<(.*?)>", string.Empty);
        }

        public void OnLevelLoad(LevelInfo info)
        {
            if (PreferencesManager.IsDebug())
                LoggerInstance.Msg("Level Loaded!");

            CrateSpawnerPatches.LevelReplacements = 0;
            MenuManager.UpdateDebugCounts();
            if (!thunderstoreNotif)
            {
                thunderstoreNotif = true;
                Thunderstore.BL_SendNotification();
            }
        }
    }
}