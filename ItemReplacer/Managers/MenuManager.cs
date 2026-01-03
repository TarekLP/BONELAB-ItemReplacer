using System.Collections.Generic;
using System.IO;
using System.Linq;

using BoneLib.BoneMenu;

using Il2CppSLZ.Marrow.Warehouse;

using ItemReplacer.Helpers;
using ItemReplacer.Patches;

using UnityEngine;

namespace ItemReplacer.Managers
{
    public static class MenuManager
    {
        public static Page AuthorPage { get; private set; }

        public static Page ModPage { get; private set; }

        public static Page ReplacersPage { get; private set; }

        private static Dictionary<string, Page> ReplacerPages { get; } = [];

        public static void Setup()
        {
            AuthorPage ??= Page.Root.CreatePage("T&H Modding Inc.", Color.white);
            ModPage ??= AuthorPage.CreatePage("Item Replacer", new Color(0.6f, 0.0f, 0.8f));
            ModPage.CreateBool("Enable Mod", new Color(0, 1, 0), PreferencesManager.Enabled.Value, (v) =>
            {
                PreferencesManager.Enabled.Value = v;
                PreferencesManager.Category.SaveToFile(false);
            });

            ModPage.CreateBool("Debug Logging", Color.cyan, PreferencesManager.DebugMode.Value, (v) =>
            {
                PreferencesManager.DebugMode.Value = v;
                PreferencesManager.Category.SaveToFile(false);
            });
            ModPage.CreateFunction("Dump all barcodes to TXT file", Color.red, () =>
            {
                Core.Logger.Msg("Dumping all barcodes...");
                List<string> barcodes = [];
                AssetWarehouse.Instance.gamePallets.ForEach(x =>
                {
                    if (AssetWarehouse.Instance.TryGetPallet(x, out Pallet pallet))
                    {
                        pallet.Crates.ForEach((System.Action<Crate>)(crate =>
                        {
                            if (crate.Barcode != null)
                                barcodes.Add($"{crate.Title.RemoveUnityRichText()} - {crate.Barcode.ID}");
                        }));
                    }
                });
                using var file = File.CreateText(Path.Combine(PreferencesManager.ConfigDir, "dump.txt"));
                barcodes.ForEach(x => file.WriteLine(x));
                file.Flush();
                file.Close();
                Core.Logger.Msg($"Dumped {barcodes.Count} barcodes to dump.txt");
            });
            ReplacersPage ??= ModPage.CreatePage("Replacers", Color.yellow);
            SetupReplacers();
        }

        internal static void SetupReplacers()
        {
            if (AuthorPage == null || ModPage == null || ReplacersPage == null)
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

                Page page = null;
                if (!ReplacerPages.ContainsKey(config.ID))
                {
                    if (!config.Color.TryFromHEX(out Color color))
                        Core.Logger.Error($"Color for '{config.ID}' is invalid");
                    page = ReplacersPage.CreatePage(config.Name, color, createLink: false);
                    ReplacerPages[config.ID] = page;
                }
                else
                {
                    page = ReplacerPages[config.ID];
                }

                ReplacersPage.CreatePageLink(page);

                page.RemoveAll();

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
                });
            }

            if (Menu.CurrentPage.Parent == ReplacersPage && !ReplacerPages.Any(x => x.Value == Menu.CurrentPage))
                Menu.OpenParentPage();
        }

        private static Color StateColor(bool state)
            => state ? Color.green : Color.red;
    }
}
