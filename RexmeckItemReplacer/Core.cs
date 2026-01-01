using MelonLoader;
using BoneLib.BoneMenu;
using UnityEngine;

namespace RexmeckItemReplacer
{
    public static class ModInfo
    {
        public const string Name = "Item Replacer";
        public const string Author = "Tarek";
        public const string Version = "1.0.0";
        public const string Company = "TH_Modding";
        public const string Description = "Replaces Items in BONELAB with user specified replacements.";
        public const string DownloadLink = null;
    }

    public class Core : MelonMod
    {

        // Preferences
        public static MelonPreferences_Category MyModCategory { get; private set; }
        public static MelonPreferences_Entry<bool> IsReplacerEnabled { get; private set; }
        public static MelonPreferences_Entry<bool> DebugMode { get; private set; }

        // Category Preferences
        public static MelonPreferences_Entry<bool> ReplacePistols { get; private set; }
        public static MelonPreferences_Entry<bool> ReplaceSMGs { get; private set; }
        public static MelonPreferences_Entry<bool> ReplaceRifles { get; private set; }
        public static MelonPreferences_Entry<bool> ReplaceShotguns { get; private set; }
        public static MelonPreferences_Entry<bool> ReplaceNPCs { get; private set; }

        public static MelonLogger.Instance Logger { get; private set; }

        public override void OnInitializeMelon()
        {
            Logger = LoggerInstance;

            // 1. Setup Preferences
            MyModCategory = MelonPreferences.CreateCategory("RexmeckReplacer");
            IsReplacerEnabled = MyModCategory.CreateEntry("Enabled", true);
            DebugMode = MyModCategory.CreateEntry("DebugMode", false, "Debug Logging");

            ReplacePistols = MyModCategory.CreateEntry("ReplacePistols", true);
            ReplaceSMGs = MyModCategory.CreateEntry("ReplaceSMGs", true);
            ReplaceRifles = MyModCategory.CreateEntry("ReplaceRifles", true);
            ReplaceShotguns = MyModCategory.CreateEntry("ReplaceShotguns", true);
            ReplaceNPCs = MyModCategory.CreateEntry("ReplaceNPCs", true);

            // 2. Setup Menu
            CreateBoneMenu();

            LoggerInstance.Msg(System.ConsoleColor.Magenta, "Item Replacer.");
        }

        private static void CreateBoneMenu()
        {
            Page rootPage = Page.Root;

            // Root Page (Purple)
            Page myPage = rootPage.CreatePage("Item Replacer",Color.magenta);

            // Master Toggle
            myPage.CreateBool("Enable Mod", Color.white, IsReplacerEnabled.Value, (v) =>
            {
                IsReplacerEnabled.Value = v;
                MyModCategory.SaveToFile();
            });

            // Categories Sub-Page
            Page filtersPage = myPage.CreatePage("Categories", Color.green);

            filtersPage.CreateBool("Replace Pistols", Color.green, ReplacePistols.Value, (v) => { ReplacePistols.Value = v; MyModCategory.SaveToFile(); });
            filtersPage.CreateBool("Replace SMGs", Color.green, ReplaceSMGs.Value, (v) => { ReplaceSMGs.Value = v; MyModCategory.SaveToFile(); });
            filtersPage.CreateBool("Replace ARs", Color.green, ReplaceRifles.Value, (v) => { ReplaceRifles.Value = v; MyModCategory.SaveToFile(); });
            filtersPage.CreateBool("Replace Shotguns", Color.green, ReplaceShotguns.Value, (v) => { ReplaceShotguns.Value = v; MyModCategory.SaveToFile(); });
            filtersPage.CreateBool("Replace NPCs", Color.green, ReplaceNPCs.Value, (v) => { ReplaceNPCs.Value = v; MyModCategory.SaveToFile(); });

            // Debug Toggle
            myPage.CreateBool("Debug Logging", Color.red, DebugMode.Value, (v) =>
            {
                DebugMode.Value = v;
                MyModCategory.SaveToFile();
            });

        }
    }
}