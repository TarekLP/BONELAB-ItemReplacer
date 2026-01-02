using System.IO;

using MelonLoader;
using MelonLoader.Utils;

namespace RexmeckItemReplacer.Managers
{
    public static class PreferencesManager
    {
        // Preferences
        public static MelonPreferences_Category Category { get; private set; }
        public static MelonPreferences_Entry<bool> Enabled { get; private set; }
        public static MelonPreferences_Entry<bool> DebugMode { get; private set; }

        public static string ConfigDir => Path.Combine(MelonEnvironment.UserDataDirectory, ModInfo.Name);

        public static string ConfigFile => Path.Combine(ConfigDir, "Config.cfg");

        public static void Setup()
        {
            EnsureFolder();

            Category = MelonPreferences.CreateCategory(ModInfo.Name);
            Category.SetFilePath(ConfigFile);

            Enabled = Category.CreateEntry("Enabled", true);
            DebugMode = Category.CreateEntry("Debug", false, description: "When enabled, provides additional logging for debugging");

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
