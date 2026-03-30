using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using BoneLib;
using BoneLib.BoneMenu;
using BoneLib.BoneMenu.UI;
using BoneLib.Notifications;

using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.Warehouse;

using ItemReplacer.Helpers;
using ItemReplacer.Patches;
using ItemReplacer.Utilities;

using MelonLoader;

using UnityEngine;

namespace ItemReplacer.Managers
{
    public static class MenuManager
    {
        public static Page ModPage { get; private set; }

        public static Page ReplacersPage { get; private set; }

        public static Page DebugPage { get; private set; }

        public static Page ReplacerPage { get; private set; }

        public static Page CategoryPage { get; private set; }

        public static Page EntryPage { get; private set; }

        internal static FunctionElement TotalReplacedElement { get; set; }
        internal static FunctionElement LevelReplacedElement { get; set; }

        public static bool EditorMode { get; set; }

        private static string ReplacerName { get; set; }

        public static string OpenedReplacerID { get; private set; }

        public static void Setup()
        {
            ModPage ??= Page.Root.CreatePage(ModInfo.Name, new Color(0.6f, 0.0f, 0.8f));
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

            ReplacerPage ??= ReplacersPage.CreatePage("Example Replacer", Color.magenta);
            CategoryPage ??= ReplacerPage.CreatePage("Example Category", Color.magenta);
            EntryPage ??= CategoryPage.CreatePage("Example Entry", Color.magenta);
            ReplacersPage.RemoveAll();
            ReplacersPage.CreateBool("Editor Mode", Color.cyan, EditorMode, (v) =>
            {
                EditorMode = v;
                SetupReplacers();
            });
            ReplacersPage.CreateFunction(" ", Color.white, null).SetProperty(ElementProperties.NoBorder);
            if (EditorMode)
            {
                ReplacersPage.CreateString("Name", Color.white, ReplacerName, (v) => ReplacerName = v);
                ReplacersPage.CreateFunction("Create New Replacer", Color.green, CreateReplacer);
                ReplacersPage.CreateFunction(" ", Color.white, null).SetProperty(ElementProperties.NoBorder);
            }

            bool categoryUpdated = false;
            bool entryUpdated = false;

            foreach (var config in ReplacerManager.Configs)
            {
                if (config == null)
                {
                    Core.Logger.Error("Replacer is null, cannot generate element");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(config.ID))
                    Core.Logger.Error("ID is null or empty, cannot generate element");

                var link = ReplacersPage.CreateFunction(config.Name, config.GetColor(), () => CreateReplacerPage(config, true));
                if (!string.IsNullOrWhiteSpace(config.Description))
                    link.SetTooltip(config.Description);

                var (category, entry) = UpdatePages(config, false);
                if (category) categoryUpdated = true;
                if (entry) entryUpdated = true;
            }

            FixMenu(categoryUpdated, entryUpdated);
        }

        internal static (bool category, bool entry) UpdatePages(ReplacerConfig config, bool createElems)
        {
            bool categoryUpdated = false;
            bool entryUpdated = false;
            config.Categories.ForEach(x =>
            {
                if (createElems)
                    x.CreateCategory(config, ReplacerPage);
                if (CategoryName == x.Name)
                    categoryUpdated = true;

                x.Entries.ForEach(e =>
                {
                    if (e.GetTitle() == EntryPage.Name)
                        entryUpdated = true;
                });
            });

            return (categoryUpdated, entryUpdated);
        }

        internal static void FixMenu(bool category, bool entry)
        {
            if (Menu.CurrentPage == null)
                return;

            if (Menu.CurrentPage == CategoryPage && !category)
                Menu.OpenPage(ReplacersPage);
            else if (Menu.CurrentPage == EntryPage && !entry)
                Menu.OpenPage(category ? CategoryPage : ReplacersPage);

            if (Menu.CurrentPage == CategoryPage || Menu.CurrentPage == EntryPage)
                return;

            if (Menu.CurrentPage == ReplacerPage && !ReplacerManager.Configs.Any(x => x.ID == OpenedReplacerID))
                Menu.OpenParentPage();
        }

        internal static void CreateReplacer()
        {
            try
            {
                string id = ReplacerName.ToLower().Trim().Replace(' ', '_');
                var config = new ReplacerConfig()
                {
                    ID = id,
                    Name = ReplacerName,
                    Color = "#FFFFFF",
                    Enabled = true,
                    Categories = [new("Example")]
                };
                ReplacerManager.Register(config);
            }
            catch (Exception ex)
            {
                Core.Logger.Error("Failed to create replacer using the menu", ex);
                Notify("Error", "Failed to create replacer, check console for more information", 4.5f, NotificationType.Error);
            }
        }

        internal static void CreateReplacerPage(ReplacerConfig config, bool open = false)
        {
            ReplacerPage.RemoveAll();
            if (!string.IsNullOrWhiteSpace(config.FilePath) && File.Exists(config.FilePath))
                ReplacerPage.CreateFunction($"File: {Path.GetFileName(config.FilePath)}", Color.white, null).SetProperty(ElementProperties.NoBorder);

            if (EditorMode)
                ReplacerPage.CreateFunction("Delete Replacer", Color.red, () => ConfigDeleteDialog(config));

            var missing = config.Dependencies?.Where(x => !AssetWarehouse.Instance.HasPallet(new(x.Barcode)))?.ToList();
            if (missing?.Any() == true)
            {
                MissingDependencies(missing, config, ReplacerPage);
            }
            else
            {
                ReplacerPage.Name = config.Name;
                ReplacerPage.CreateBool("Enabled", Color.green, config.Enabled, (v) =>
                {
                    config.Enabled = v;
                    config.SaveToFile(false);
                });

                ReplacerPage.CreateFunction(" ", Color.white, null).SetProperty(ElementProperties.NoBorder);
                if (EditorMode)
                {
                    string category = string.Empty;
                    ReplacerPage.CreateString("Name", Color.white, category, (v) => category = v);
                    ReplacerPage.CreateFunction("Create New Category", Color.green, () =>
                    {
                        config.Categories.Add(new ReplacerCategory(category, string.Empty, []));
                        config.TrySaveToFile(false);
                        SetupReplacers();
                    });
                    ReplacerPage.CreateFunction(" ", Color.white, null).SetProperty(ElementProperties.NoBorder);
                }

                UpdatePages(config, true);
            }

            if (Menu.CurrentPage == ReplacerPage)
                CorrectPage(ReplacerPage);

            if (open)
            {
                Menu.OpenPage(ReplacerPage);
                OpenedReplacerID = config.ID;
            }
        }

        internal static void MissingDependencies(List<ReplacerDependency> missing, ReplacerConfig config, Page page)
        {
            page.Name = $"{config.Name} (!)";
            var title = CreateDefaultReplacerElems(page, config, missing.Count);
            missing.ForEach(x =>
            {
                FunctionElement elem = null;
                elem = page.CreateFunction($"{(!string.IsNullOrWhiteSpace(x.Title) ? x.Title : x.Barcode)}", Color.red, () =>
                {
                    InstallMissing(x, elem, () =>
                    {
                        page.Remove(elem);
                        missing.Remove(x);
                        title.ElementName = $"Missing Dependencies ({missing.Count})";
                        Notify("Success", "Successfully downloaded and installed missing dependency", 3.5f, NotificationType.Success);
                        if (!missing.Any())
                            CreateReplacerPage(config);
                    });
                });
            });
        }

        internal static string CategoryName { get; set; }

        internal static void Category(ReplacerConfig config, ReplacerCategory category, bool open = true)
        {
            CategoryPage ??= ReplacersPage.CreatePage("Example Category", Color.magenta);
            CategoryName = category.Name;
            CategoryPage.Color = category.Enabled ? Color.green : Color.red;
            CategoryPage.Name = category.Name;

            CategoryPage.RemoveAll();

            CategoryPage.CreateFunction("Delete Category", Color.red, () => CategoryDeleteDialog(category, config));
            CategoryPage.CreateBool("Enabled", Color.green, category.Enabled, (v) =>
            {
                category.Enabled = v;
                config.TrySaveToFile(false);
                SetupReplacers();
            });
            CategoryPage.CreateFunction(" ", Color.white, null).SetProperty(ElementProperties.NoBorder);

            CategoryPage.CreateString("Name", Color.white, category.Name, (v) =>
            {
                CategoryName = v;
                category.Name = v;
                config.TrySaveToFile(false);
                SetupReplacers();
            });
            CategoryPage.CreateString("Description", Color.cyan, category.Description, (v) =>
            {
                category.Description = v;
                config.TrySaveToFile(false);
                SetupReplacers();
            });

            CategoryPage.CreateFunction(" ", Color.white, null).SetProperty(ElementProperties.NoBorder);
            CategoryPage.CreateFunction("Replace Item In Left Hand", Color.green, () => CreateEntry(category, config, Handedness.LEFT));
            CategoryPage.CreateFunction("Replace Item In Right Hand", Color.cyan, () => CreateEntry(category, config, Handedness.RIGHT));

            CategoryPage.CreateFunction(" ", Color.white, null).SetProperty(ElementProperties.NoBorder);

            category.Entries.ForEach(x => CategoryPage.CreateFunction(x.GetTitle(), Color.white, () => Entry(config, category, x)));

            if (Menu.CurrentPage == CategoryPage)
                CorrectPage(CategoryPage);
            else if (open)
                Menu.OpenPage(CategoryPage);
        }

        internal static void CreateEntry(ReplacerCategory category, ReplacerConfig config, Handedness hand)
        {
            var crate = GetItem(hand);
            if (crate != null)
            {
                category.Entries.Add(new ReplacerEntry(crate.Barcode.ID, string.Empty));
                config.TrySaveToFile(false);
                SetupReplacers();
            }
        }

        internal static void Entry(ReplacerConfig config, ReplacerCategory category, ReplacerEntry entry, bool open = true)
        {
            EntryPage.Name = entry.GetTitle();
            EntryPage.RemoveAll();

            EntryPage.CreateFunction("Delete Entry", Color.red, () => EntryDeleteDialog(entry, category, config));
            EntryPage.CreateEnum("Match Type", Color.cyan, entry.MatchType, (v) =>
            {
                entry.MatchType = (MatchType)v;
                config.TrySaveToFile(false);
                SetupReplacers();
            });
            EntryPage.CreateFunction(" ", Color.white, null).SetProperty(ElementProperties.NoBorder);

            EntryPage.CreateString("Original", Color.white, entry.Original, (v) =>
            {
                entry.Original = v;
                config.TrySaveToFile(false);
                SetupReplacers();
            });
            EntryPage.CreateFunction("Set From Left Hand", Color.green, () =>
            {
                var item = GetItem(Handedness.LEFT);
                if (item == null)
                    return;

                entry.Original = item.Barcode.ID;
                config.TrySaveToFile(false);
                SetupReplacers();
            });
            EntryPage.CreateFunction("Set From From Right Hand", Color.cyan, () =>
            {
                var item = GetItem(Handedness.RIGHT);
                if (item == null)
                    return;

                entry.Original = item.Barcode.ID;
                config.TrySaveToFile(false);
                SetupReplacers();
            });
            EntryPage.CreateFunction(" ", Color.white, null).SetProperty(ElementProperties.NoBorder);

            EntryPage.CreateString("Replacement", Color.white, entry.Replacement, (v) =>
            {
                entry.Replacement = v;
                config.TrySaveToFile(false);
                SetupReplacers();
            });
            EntryPage.CreateFunction("Set From From Left Hand", Color.green, () =>
            {
                var item = GetItem(Handedness.LEFT);
                if (item == null)
                    return;

                entry.Replacement = item.Barcode.ID;
                config.TrySaveToFile(false);
                SetupReplacers();
            });
            EntryPage.CreateFunction("Set From From Right Hand", Color.cyan, () =>
            {
                var item = GetItem(Handedness.RIGHT);
                if (item == null)
                    return;

                entry.Replacement = item.Barcode.ID;
                config.TrySaveToFile(false);
                SetupReplacers();
            });
            EntryPage.CreateFunction(" ", Color.white, null).SetProperty(ElementProperties.NoBorder);

            if (Menu.CurrentPage == EntryPage)
                CorrectPage(EntryPage);
            else if (open)
                Menu.OpenPage(EntryPage);
        }

        internal static SpawnableCrate GetItem(Handedness hand)
        {
            var crate = GetSpawnableFromHand(hand);
            if (crate == null)
            {
                Notify("Error", $"No item is being held in {(hand == Handedness.LEFT ? "left" : "right")} hand!", 3.5f, NotificationType.Error);
                return null;
            }

            return crate;
        }

        internal static string GetTitle(this ReplacerEntry entry)
        {
            if (!AssetWarehouse.ready || AssetWarehouse.Instance == null)
                return entry.Original;

            if (!AssetWarehouse.Instance.TryGetCrate(new(entry.Original), out Crate crate))
                return entry.Original;

            return crate?.Title ?? entry.Original;
        }

        internal static void ConfigDeleteDialog(ReplacerConfig config)
        {
            Menu.DisplayDialog(new DialogData()
            {
                Title = "Are you sure?",
                Message = $"Are you sure you want to delete the replacer '{config.Name}'? This action cannot be undone.",
                Confirm = () => ReplacerManager.Unregister(config.ID),
                Deny = () => Core.Logger.Msg("Deletion cancelled")
            });
        }

        internal static void CategoryDeleteDialog(ReplacerCategory category, ReplacerConfig config)
        {
            Menu.DisplayDialog(new DialogData()
            {
                Title = "Are you sure?",
                Message = $"Are you sure you want to delete the category '{category.Name}'? This action cannot be undone.",
                Confirm = () =>
                {
                    config.Categories.Remove(category);
                    config.TrySaveToFile(false);
                    SetupReplacers();
                },
                Deny = () => Core.Logger.Msg("Deletion cancelled")
            });
        }

        internal static void EntryDeleteDialog(ReplacerEntry entry, ReplacerCategory category, ReplacerConfig config)
        {
            Menu.DisplayDialog(new DialogData()
            {
                Title = "Are you sure?",
                Message = "Are you sure you want to delete the entry? This action cannot be undone.",
                Confirm = () =>
                {
                    category.Entries.Remove(entry);
                    config.TrySaveToFile(false);
                    SetupReplacers();
                },
                Deny = () => Core.Logger.Msg("Deletion cancelled")
            });
        }

        internal static SpawnableCrate GetSpawnableFromHand(Handedness handness)
        {
            var hand = handness switch
            {
                Handedness.LEFT => Player.LeftHand,
                Handedness.RIGHT => Player.RightHand,
                _ => null
            };

            if (hand == null)
                return null;

            return hand?.AttachedReceiver?.Host?.GetGrip()?._marrowEntity?._poolee?.SpawnableCrate;
        }

        internal static FunctionElement CreateDefaultReplacerElems(Page page, ReplacerConfig config, int missing)
        {
            var title = page.CreateFunction($"Missing Dependencies ({missing})", Color.red, null);
            title.SetProperty(ElementProperties.NoBorder);
            if (Fusion.HasFusion) page.CreateFunction("Press to install missing dependency", Color.white, null).SetProperty(ElementProperties.NoBorder);
            page.CreateFunction("Refresh", Color.white, () => CreateReplacerPage(config));
            page.CreateFunction(" ", Color.white, null).SetProperty(ElementProperties.NoBorder);
            return title;
        }

        internal static void InstallMissing(ReplacerDependency dependency, FunctionElement element, Action success)
        {
            if (Fusion.HasFusion)
            {
                Core.Logger.Msg($"Requesting install of missing dependency '{dependency.Title}' (Mod ID: {dependency.ModID}) via LabFusion...");
                Notify("Info", "Beginning download and installation of missing dependency", 3.5f);
                Fusion.RequestInstall(dependency.ModID, (r) =>
                {
                    if (r == Fusion.ModResult.SUCCEEDED)
                        success?.Invoke();
                    else
                        Notify("Failure", "Failed to install missing dependency, check console for more information", 4.5f, NotificationType.Error);
                }, element);
            }
        }

        internal static void Notify(string title, string message, float length, NotificationType type = NotificationType.Information)
        {
            Notifier.Send(new()
            {
                Title = title,
                Message = message,
                PopupLength = length,
                Type = type,
                ShowTitleOnPopup = true
            });
        }

        internal static void CreateCategory(this ReplacerCategory category, ReplacerConfig config, Page page)
        {
            FunctionElement elem = null;
            elem = page.CreateFunction($"{category.Name} ({category.Entries.Count})", StateColor(category.Enabled), () =>
            {
                if (!EditorMode)
                {
                    category.Enabled = !category.Enabled;
                    elem.ElementName = $"{category.Name} ({category.Entries.Count})";
                    elem.ElementColor = StateColor(category.Enabled);
                    config.SaveToFile(false);
                }
                else
                {
                    Category(config, category);
                }
            });
            if (!string.IsNullOrWhiteSpace(category.Description))
                elem.SetTooltip(category.Description);
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