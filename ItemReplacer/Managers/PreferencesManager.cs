using System.IO;

using MelonLoader;
using MelonLoader.Utils;

namespace ItemReplacer.Managers
{
    internal static class PreferencesManager
    {
        // Preferences
        internal static MelonPreferences_Category Category { get; private set; }

        // The fuck you mean unnecessary suppresion
        // AND THE FUCK YOU MEAN I HAVE TO SILENCE THAT AS WELL.
        // Fuck Visual Studio, i hate this shit.
#pragma warning disable RCS1222 // Merge preprocessor directives
#pragma warning disable IDE0079
#pragma warning disable S2223
        internal static MelonPreferences_Entry<bool> Enabled;
        internal static MelonPreferences_Entry<bool> DebugMode;

        internal static MelonPreferences_Entry<bool> FusionSupport;
#pragma warning restore S2223, IDE0079, RCS1222

        internal static string ConfigDir => Path.Combine(MelonEnvironment.UserDataDirectory, ModInfo.Name);

        internal static string ConfigFile => Path.Combine(ConfigDir, "Config.cfg");

        internal static bool IsDebug()
        {
            if (DebugMode?.Value == true)
                return true;

            if (MelonDebug.IsEnabled())
                return true;

            return false;
        }

        internal static void Setup()
        {
            EnsureFolder();

            Category = MelonPreferences.CreateCategory(ModInfo.Name);

            Enabled = Category.CreateEntry("Enabled", true);
            DebugMode = Category.CreateEntry("Debug", false, description: "When enabled, provides additional logging for debugging");
            FusionSupport = Category.CreateEntry("FusionSupport", true, description: "When enabled, items will be replaced even when you are in a LabFusion lobby");

            Category.SetFilePath(ConfigFile);
            Category.SaveToFile(false);
        }

        internal static void EnsureFolder()
        {
            if (!Directory.Exists(ConfigDir))
            {
                Core.Logger.Msg("Creating missing folder in UserData");
                Directory.CreateDirectory(ConfigDir);
            }
        }
    }
}
