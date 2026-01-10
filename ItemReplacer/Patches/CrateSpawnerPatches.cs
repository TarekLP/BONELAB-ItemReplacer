using System;
using System.Linq;
using System.Text.RegularExpressions;

using HarmonyLib;

using Il2CppCysharp.Threading.Tasks;

using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;

using ItemReplacer.Helpers;
using ItemReplacer.Managers;
using ItemReplacer.Utilities;

using Scriban;
using Scriban.Runtime;

using UnityEngine;

namespace ItemReplacer.Patches
{
    [HarmonyPatch(typeof(CrateSpawner))]
    internal static class CrateSpawnerPatches
    {
        public static int TotalReplacements { get; internal set; } = 0;
        public static int LevelReplacements { get; internal set; } = 0;

        // Item Replacement Logic
        [HarmonyPrefix]
        [HarmonyPriority(int.MaxValue)]
        [HarmonyPatch(nameof(CrateSpawner.SpawnSpawnableAsync))]
        public static bool SpawnSpawnableAsyncPrefix(CrateSpawner __instance, ref UniTask<Poolee> __result)
        {
            try
            {
                // Is the mod enabled or disabled?
                if (PreferencesManager.Enabled?.Value != true) return true;

                // if there is no barcode, there is no replacement.
                if (__instance?.spawnableCrateReference?.Barcode == null) return true;

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
                        return true;
                    }

                    if (PreferencesManager.IsDebug())
                        Core.Logger.Msg($"Replacing with: {crate.Title.RemoveUnityRichText()} - {targetBarcode} (Original: {currentTitle.RemoveUnityRichText()} - {currentBarcode})");

                    if (!Fusion.IsConnected)
                    {
                        var source = new UniTaskCompletionSource<Poolee>();
                        __result = new UniTask<Poolee>(source.TryCast<IUniTaskSource<Poolee>>(), default);
                        SpawnItem(targetBarcode, __instance.transform.position, __instance.transform.rotation, source);
                        ReplacedSuccess();
                        return false;
                    }
                    else if (PreferencesManager.FusionSupport?.Value == true)
                    {
                        Fusion.HandleFusionCrateSpawner(targetBarcode, __instance, out UniTask<Poolee> res);
                        __result = res ?? new UniTask<Poolee>(null);
                        ReplacedSuccess();
                        return false;
                    }
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Core.Logger.Error("An unexpected error has occurred in the CrateSpawner prefix", e);
                return true;
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
                        return replacement.Replacement;
                }
            }
            return null;
        }

        private static bool Match(string barcode, ReplacerEntry entry)
        {
            if (entry.MatchType == MatchType.RegEx)
                return Regex.IsMatch(barcode, entry.Original);
            else if (entry.MatchType == MatchType.Scriban)
                return Scriban(barcode, entry);
            else
                return barcode == entry.Original;
        }

        private static bool Scriban(string barcode, ReplacerEntry entry)
        {
            if (entry.Template?.HasErrors != false)
                return false;

            if (!AssetWarehouse.Instance.TryGetCrate(new Barcode(barcode), out var crate))
                return false;

            var scrate = new ScribanCrate(crate);

            var scriptObject = new ScriptObject(StringComparer.OrdinalIgnoreCase);
            scriptObject.Import(scrate);
            scriptObject.Import(typeof(ScribanHelper));

            var templateContext = new TemplateContext();
            templateContext.PushGlobal(scriptObject);

            var result = entry.Template.Render(templateContext);
            return result.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase);
        }

        private static void SpawnItem(string barcode, Vector3 position, Quaternion rotation, UniTaskCompletionSource<Poolee> source)
        {
            var scale = CreateNull(Vector3.zero);
            var groupId = CreateNull(0);
            var spawnable = new Spawnable()
            {
                crateRef = new SpawnableCrateReference(barcode),
                policyData = null
            };

            // Note to future self: you have to register it, it doesnt work otherwise.
            AssetSpawner.Register(spawnable);

            var task = AssetSpawner.SpawnAsync(spawnable, position, rotation, scale, null, false, groupId, null, null);
            var awaiter = task.GetAwaiter();
            awaiter.OnCompleted(() =>
            {
                try
                {
                    var poolee = awaiter.GetResult();
                    if (poolee == null)
                    {
                        source.TrySetResult(null);
                        return;
                    }

                    source.TrySetResult(poolee);
                }
                catch (Exception e)
                {
                    Core.Logger.Error("Error spawning replaced item", e);
                    source.TrySetResult(null);
                }
            });
        }

        private static Il2CppSystem.Nullable<T> CreateNull<T>(T value) where T : new()
            => new(value) { hasValue = false };

        public static string RemoveUnityRichText(this string text)
            => Regex.Replace(text, "<.*?>", string.Empty);
    }
}