using System.IO;

using MelonLoader;
using MelonLoader.Utils;

namespace ItemReplacer.Managers
{
    public static class PreferencesManager
    {
        // Preferences
        public static MelonPreferences_Category Category { get; private set; }
        public static MelonPreferences_Entry<bool> Enabled { get; private set; }
        public static MelonPreferences_Entry<bool> DebugMode { get; private set; }

        public static MelonPreferences_Entry<bool> FusionSupport { get; private set; }

        public static string ConfigDir => Path.Combine(MelonEnvironment.UserDataDirectory, ModInfo.Name);

        public static string ConfigFile => Path.Combine(ConfigDir, "Config.cfg");

        public static bool IsDebug()
        {
            if (DebugMode?.Value == true)
                return true;

            if (MelonDebug.IsEnabled())
                return true;

            return false;
        }

        public static void Setup()
        {
            EnsureFolder();

            Category = MelonPreferences.CreateCategory(ModInfo.Name);
            Category.SetFilePath(ConfigFile);

            Enabled = Category.CreateEntry("Enabled", true);
            DebugMode = Category.CreateEntry("Debug", false, description: "When enabled, provides additional logging for debugging");
            FusionSupport = Category.CreateEntry("FusionSupport", true, description: "When enabled, items will be replaced even when you are in a LabFusion lobby");

            Category.SaveToFile(false);
        }

        public static void EnsureFolder()
        {
            if (!Directory.Exists(ConfigDir))
            {
                Core.Logger.Msg("Creating missing folder in UserData");
                Directory.CreateDirectory(ConfigDir);
            }
        }
    }
}
