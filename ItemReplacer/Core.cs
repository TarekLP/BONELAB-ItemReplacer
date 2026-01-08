using MelonLoader;

using ItemReplacer.Managers;
using ItemReplacer.Utilities;
using BoneLib;
using ItemReplacer.Patches;

namespace ItemReplacer
{
    public static class ModInfo
    {
        public const string Name = "ItemReplacer";

        public const string Author = "T&H Modding";
        public const string ThunderstoreAuthor = "TH_Modding";

        public const string Version = "1.0.0";
        public const string Description = "Replaces Items in BONELAB with user specified replacements.";
        public const string DownloadLink = $"https://thunderstore.io/c/bonelab/p/{ThunderstoreAuthor}/{Name}/";
    }

    public class Core : MelonMod
    {

        public static MelonLogger.Instance Logger { get; private set; }

        public static Thunderstore Thunderstore { get; private set; }

        private bool ThunderstoreNotif = false;

        public override void OnInitializeMelon()
        {
            Logger = LoggerInstance;


            Thunderstore = new($"{ModInfo.Name} / {ModInfo.Version} A BONELAB Mod");
            Thunderstore.BL_FetchPackage(ModInfo.Name, ModInfo.ThunderstoreAuthor, ModInfo.Version, LoggerInstance);
            Hooking.OnLevelLoaded += OnLevelLoad;

            LoggerInstance.Msg("Setting up preferences");
            PreferencesManager.Setup();

            LoggerInstance.Msg("Setting up replacers");
            ReplacerManager.Setup();
            ReplacerManager.CreateFileWatcher();

            LoggerInstance.Msg("Setting up BoneMenu");
            MenuManager.Setup();

            LoggerInstance.Msg("Initialized.");
        }

        public void OnLevelLoad(LevelInfo info)
        {
            if (PreferencesManager.DebugMode?.Value == true) LoggerInstance.Msg("Level Loaded!");
            CrateSpawnerPatches.LevelReplacements = 0;
            MenuManager.UpdateDebugCounts();
            if (!ThunderstoreNotif)
            {
                ThunderstoreNotif = true;
                Thunderstore.BL_SendNotification();
            }
        }
    }
}