using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using MelonLoader;

using UnityEngine;

using BoneLib;
using BoneLib.BoneMenu;
using BoneLib.BoneMenu.UI;

using ItemReplacer.Helpers;
using ItemReplacer.Patches;

using Il2CppSLZ.Marrow.Warehouse;

namespace ItemReplacer.Managers
{
    public static class MenuManager
    {
        public static Page AuthorPage { get; private set; }

        public static Page ModPage { get; private set; }

        public static Page ReplacersPage { get; private set; }

        public static Page DebugPage { get; private set; }

        internal static FunctionElement TotalReplacedElement { get; set; }
        internal static FunctionElement LevelReplacedElement { get; set; }

        private static Dictionary<string, Page> ReplacerPages { get; } = [];

        public static void Setup()
        {
            AuthorPage ??= Page.Root.CreatePage(ModInfo.Author, Color.white);
            ModPage ??= AuthorPage.CreatePage(ModInfo.Name, new Color(0.6f, 0.0f, 0.8f));
            ModPage.CreateBoolPref("Enable Mod", new Color(0, 1, 0), ref PreferencesManager.Enabled);
            ModPage.CreateBoolPref("LabFusion Support", Color.cyan, ref PreferencesManager.FusionSupport);
            ReplacersPage ??= ModPage.CreatePage("Replacers", Color.yellow);
            SetupReplacers();
            DebugPage ??= ModPage.CreatePage("Debug", Color.cyan);
            SetupDebug();

            Core.Thunderstore.BL_CreateMenuLabel(ModPage, true);
        }

        internal static void SetupReplacers()
        {
            if (ReplacersPage == null)
                return;

            ReplacersPage.RemoveAll();

            foreach (var config in ReplacerManager.Configs)
            {
                if (config == null)
                {
                    Core.Logger.Error("Replacer is null, cannot generate element");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(config.ID))
                {
                    Core.Logger.Error("ID is null or empty, cannot generate element");
                }

                Page page = PageFromConfig(config);

                var link = ReplacersPage.CreatePageLink(page);
                if (!string.IsNullOrWhiteSpace(config.Description))
                    link.SetTooltip(config.Description);

                page.RemoveAll();

                if (!string.IsNullOrWhiteSpace(config.FilePath) && File.Exists(config.FilePath))
                    page.CreateFunction($"File: {Path.GetFileName(config.FilePath)}", Color.white, null).SetProperty(ElementProperties.NoBorder);

                page.CreateBool("Enabled", Color.green, config.Enabled, (v) =>
                {
                    config.Enabled = v;
                    config.SaveToFile(false);
                });

                page.CreateFunction(" ", Color.white, null).SetProperty(ElementProperties.NoBorder);

                config.Categories.ForEach(x =>
                {
                    FunctionElement elem = null;
                    elem = page.CreateFunction($"{x.Name} ({x.Entries.Count})", StateColor(x.Enabled), () =>
                    {
                        x.Enabled = !x.Enabled;
                        elem.ElementName = $"{x.Name} ({x.Entries.Count})";
                        elem.ElementColor = StateColor(x.Enabled);
                        config.SaveToFile(false);
                    });
                    if (!string.IsNullOrWhiteSpace(x.Description))
                        elem.SetTooltip(x.Description);
                });

                if (Menu.CurrentPage == page)
                    CorrectPage(page);
            }

            if (Menu.CurrentPage.Parent == ReplacersPage && !ReplacerPages.Any(x => x.Value == Menu.CurrentPage))
                Menu.OpenParentPage();
        }

        internal static void SetupDebug()
        {
            if (DebugPage == null)
                return;
            DebugPage.RemoveAll();

            TotalReplacedElement = DebugPage.CreateFunction($"Total Replaced: {CrateSpawnerPatches.TotalReplacements}", Color.white, null);
            LevelReplacedElement = DebugPage.CreateFunction($"Level Replaced: {CrateSpawnerPatches.LevelReplacements}", Color.white, null);
            TotalReplacedElement.SetProperty(ElementProperties.NoBorder);
            LevelReplacedElement.SetProperty(ElementProperties.NoBorder);

            DebugPage.CreateBoolPref("Debug Logging", Color.cyan, ref PreferencesManager.DebugMode);
            DebugPage.CreateFunction("Dump all barcodes to dump.txt", Color.red, DumpBarcodes);
        }

        internal static void UpdateDebugCounts()
        {
            if (TotalReplacedElement == null || LevelReplacedElement == null)
                return;

            TotalReplacedElement.ElementName = $"Total Replaced: {CrateSpawnerPatches.TotalReplacements}";
            LevelReplacedElement.ElementName = $"Level Replaced: {CrateSpawnerPatches.LevelReplacements}";
        }

        private static void DumpBarcodes()
        {
            Core.Logger.Msg("Dumping all barcodes...");
            List<string> spawnables = [];
            List<string> avatars = [];
            List<string> levels = [];
            List<string> unidentified = [];
            AssetWarehouse.Instance.gamePallets.ForEach(x =>
            {
                if (AssetWarehouse.Instance.TryGetPallet(x, out Pallet pallet))
                {
                    pallet.Crates.ForEach((System.Action<Crate>)(crate =>
                    {
                        if (crate.Barcode != null)
                        {
                            if (crate.GetIl2CppType().Name == nameof(SpawnableCrate))
                                spawnables.Add(FormatBarcode(crate, "Spawnable"));
                            else if (crate.GetIl2CppType().Name == nameof(AvatarCrate))
                                avatars.Add(FormatBarcode(crate, "Avatar"));
                            else if (crate.GetIl2CppType().Name == nameof(LevelCrate))
                                levels.Add(FormatBarcode(crate, "Level"));
                            else
                                unidentified.Add(FormatBarcode(crate, "Unidentified"));
                        }
                    }));
                }
            });
            using var file = File.CreateText(Path.Combine(PreferencesManager.ConfigDir, "dump.txt"));
            file.WriteLine("Title - Barcode - Crate Type");
            file.WriteLine($"=============================================={file.NewLine}");

            file.WriteList(avatars);
            file.WriteList(levels);
            file.WriteList(spawnables);
            unidentified.ForEach(file.WriteLine);

            file.Flush();
            file.Close();
            Core.Logger.Msg($"Dumped {spawnables.Count} spawnables, {avatars.Count} avatars, {levels.Count} levels and {unidentified.Count} unidentified crates to dump.txt");
        }

        const string dumpFormat = "{0} - {1} - {2}";

        private static void WriteList(this StreamWriter file, List<string> list)
        {
            list.ForEach(file.WriteLine);
            if (list.Count > 0)
                file.WriteLine($"{file.NewLine}=============================================={file.NewLine}");
        }

        private static string FormatBarcode(Crate crate, string typeName)
            => string.Format(dumpFormat, crate.Title?.RemoveUnityRichText() ?? "N/A", crate.Barcode?.ID ?? "N/A", typeName);

        private static Page PageFromConfig(ReplacerConfig config)
        {
            Page page;
            if (!ReplacerPages.ContainsKey(config.ID))
            {
                page = ReplacersPage.CreatePage(config.Name, config.GetColor(), createLink: false);
                ReplacerPages[config.ID] = page;
            }
            else
            {
                page = ReplacerPages[config.ID];
                page.Name = config.Name;
                page.Color = config.GetColor();
            }
            return page;
        }

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable S3011 // Make sure that this accessibility bypass is safe here

        private static void CorrectPage(Page page)
        {
            GUIMenu.Instance.GetType().GetMethod("DrawHeader",
                            bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(GUIMenu.Instance, [page]);
        }

#pragma warning restore S3011, IDE0079 // Remove unnecessary suppression

        private static Color GetColor(this ReplacerConfig config)
        {
            if (config.Color.TryFromHEX(out Color color))
            {
                return color;
            }
            else
            {
                Core.Logger.Error($"Color for '{config.ID}' is invalid");
                return Color.white;
            }
        }

        public static BoolElement CreateBoolPref(this Page page, string name, Color color, ref MelonPreferences_Entry<bool> pref, Action<bool> callback = null)
        {
            MelonPreferences_Entry<bool> localPref = pref;
            var elem = page.CreateBool(name, color, pref.Value, (v) =>
            {
                localPref.Value = v;
                PreferencesManager.Category.SaveToFile(false);
                callback?.InvokeActionSafe(v);
            });

            if (!string.IsNullOrWhiteSpace(pref.Description))
                elem.SetTooltip(pref.Description);

            return elem;
        }

        private static Color StateColor(bool state)
            => state ? Color.green : Color.red;
    }
}