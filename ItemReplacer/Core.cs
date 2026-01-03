using MelonLoader;

using ItemReplacer.Managers;

namespace ItemReplacer
{
    public static class ModInfo
    {
        public const string Name = "ItemReplacer";
        public const string Author = "Tarek";
        public const string Version = "1.0.0";
        public const string Company = "T&H Modding";
        public const string Description = "Replaces Items in BONELAB with user specified replacements.";
        public const string DownloadLink = null;
    }

    public class Core : MelonMod
    {

        public static MelonLogger.Instance Logger { get; private set; }

        public override void OnInitializeMelon()
        {
            Logger = LoggerInstance;

            LoggerInstance.Msg("Setting up preferences");
            PreferencesManager.Setup();

            LoggerInstance.Msg("Setting up replacers");
            ReplacerManager.Setup();
            ReplacerManager.CreateFileWatcher();

            LoggerInstance.Msg("Setting up BoneMenu");
            MenuManager.Setup();

            LoggerInstance.Msg("Initialized.");
        }
    }
}