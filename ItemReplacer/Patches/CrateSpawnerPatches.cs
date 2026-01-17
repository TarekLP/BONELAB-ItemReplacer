using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using HarmonyLib;

using Il2CppCysharp.Threading.Tasks;

using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;

using ItemReplacer.Managers;
using ItemReplacer.Utilities;

using UnityEngine;

namespace ItemReplacer.Patches
{
    [HarmonyPatch(typeof(CrateSpawner))]
    internal static class CrateSpawnerPatches
    {
        public static int TotalReplacements { get; internal set; } = 0;
        public static int LevelReplacements { get; internal set; } = 0;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CrateSpawner.Awake))]
        public static void Patch(CrateSpawner __instance)
        {
            try
            {
                // Is the mod enabled or disabled?
                if (PreferencesManager.Enabled?.Value != true) return;

                // if there is no barcode, there is no replacement.
                if (__instance?.spawnableCrateReference?.Barcode == null) return;

                string currentBarcode = __instance.spawnableCrateReference.Barcode.ID;
                string currentTitle = __instance.spawnableCrateReference?.Crate?.Title ?? "N/A";
                string targetBarcode = GetReplacement(currentBarcode);

                // While the barcode isnt null, there is a replacement.
                if (targetBarcode != null)
                {
                    var crateRef = new SpawnableCrateReference(targetBarcode);
                    if (crateRef?.TryGetCrate(out var crate) != true)
                    {
                        Core.Logger.Error($"Barcode does not exist in-game, the mod may not be installed. Not replacing item. (Barcode: {targetBarcode})");
                        return;
                    }

                    if (PreferencesManager.IsDebug())
                        Core.Logger.Msg($"Replacing with: {crate.Title.RemoveUnityRichText()} - {targetBarcode} (Original: {currentTitle.RemoveUnityRichText()} - {currentBarcode}) (Spawner: {__instance.name})");

                    __instance.spawnableCrateReference = crateRef;
                    __instance._spawnable = new Spawnable() { crateRef = crateRef, policyData = null };
                    __instance.SetSpawnable();

                    ReplacedSuccess();
                }
            }
            catch (Exception e)
            {
                Core.Logger.Error("An unexpected error has occurred in the CrateSpawner prefix", e);
            }
        }

        private static void ReplacedSuccess()
        {
            TotalReplacements++;
            LevelReplacements++;
            MenuManager.UpdateDebugCounts();
        }

        private static string GetReplacement(string barcode)
        {
            foreach (var config in ReplacerManager.Configs)
            {
                if (config?.Enabled != true)
                    continue;

                foreach (var category in config.Categories)
                {
                    if (category?.Enabled != true)
                        continue;

                    var replacement = category?.Entries?.FirstOrDefault(e => Match(barcode, e));
                    if (replacement != null)
                    {
                        if (string.IsNullOrWhiteSpace(replacement.Replacement))
                            Core.Logger.Warning($"Replacement in category '{category.Name}' of config '{config.ID}' has no replacement specified. This might be caused by an outdated config.");
                        return replacement.Replacement;
                    }
                }
            }
            return null;
        }

        private static bool Match(string barcode, ReplacerEntry entry)
        {
            if (entry.MatchType == MatchType.RegEx)
                return Regex.IsMatch(barcode, entry.Original);
            else if (entry.MatchType == MatchType.Scriban)
                return ScribanMatcher.Match(barcode, entry);
            else
                return barcode == entry.Original;
        }

        public static string RemoveUnityRichText(this string text)
            => Regex.Replace(text, "<.*?>", string.Empty);
    }
}