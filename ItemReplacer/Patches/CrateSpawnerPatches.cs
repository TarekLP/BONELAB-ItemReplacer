using System;
using System.Linq;
using System.Text.RegularExpressions;

using HarmonyLib;

using Il2CppCysharp.Threading.Tasks;

using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;

using ItemReplacer.Managers;
using ItemReplacer.Utilities;

using LabFusion.Marrow.Pool;

using UnityEngine;

namespace ItemReplacer.Patches
{
    [HarmonyPatch(typeof(CrateSpawner))]
    internal static class CrateSpawnerPatches
    {
        public static int TotalReplacements { get; internal set; } = 0;
        public static int LevelReplacements { get; internal set; } = 0;

        [HarmonyPrefix]
        [HarmonyPriority(int.MaxValue)]
        [HarmonyPatch(nameof(CrateSpawner.SpawnSpawnableAsync))]
        public static bool SpawnSpawnableAsyncPrefix(CrateSpawner __instance, bool isHidden, ref UniTask<Poolee> __result)
        {
            try
            {
                if (ReplaceItem(__instance, ref __result))
                    return LabFusion.Marrow.Patching.CrateSpawnerPatches.SpawnSpawnableAsyncPrefix(__instance, isHidden, ref __result);
                else
                    return false;
            }
            catch (Exception e)
            {
                Core.Logger.Error("An unexpected error has occurred in the CrateSpawner prefix", e);
                return true;
            }
        }

        private static bool ReplaceItem(CrateSpawner __instance, ref UniTask<Poolee> __result)
        {
            if (PreferencesManager.Enabled?.Value != true) return true;

            if (__instance?.spawnableCrateReference?.Barcode == null) return true;

            string currentBarcode = __instance.spawnableCrateReference.Barcode.ID;
            string currentTitle = __instance.spawnableCrateReference?.Crate?.Title ?? "N/A";
            string targetBarcode = GetReplacement(currentBarcode);

            if (string.IsNullOrWhiteSpace(currentBarcode)
                || !__instance.spawnableCrateReference.Barcode.IsValid()
                || !__instance.spawnableCrateReference.Barcode.IsValidSize()
                || currentBarcode == Barcode.EMPTY
                || currentBarcode == Barcode.EMPTY_OLD)
            {
                return true;
            }

            if (targetBarcode != null)
            {
                var crateRef = new SpawnableCrateReference(targetBarcode);
                if (crateRef?.TryGetCrate(out var crate) != true)
                {
                    Core.Logger.Error($"Barcode does not exist in-game, the mod may not be installed. Not replacing item. (Barcode: {targetBarcode})");
                    return true;
                }

                if (PreferencesManager.IsDebug())
                    Core.Logger.Msg($"Replacing with: {crate.Title.RemoveUnityRichText()} - {targetBarcode} (Original: {currentTitle.RemoveUnityRichText()} - {currentBarcode}) (Spawner: {__instance.name})");

                if (!Fusion.IsConnected)
                {
                    SpawnItem(targetBarcode, __instance.transform.position, __instance.transform.rotation, (p) => HandleSpawner(__instance, p), out __result);
                    ReplacedSuccess();
                }
                else if (PreferencesManager.FusionSupport?.Value == true)
                {
                    FusionSpawn(__instance, targetBarcode, out __result);
                }
                return false;
            }
            return true;
        }

        private static void FusionSpawn(CrateSpawner __instance, string targetBarcode, out UniTask<Poolee> __result)
        {
            if (Fusion.HandleFusionCrateSpawner(targetBarcode, __instance, out __result))
                SpawnItem(targetBarcode, __instance.transform.position, __instance.transform.rotation, (p) => HandleSpawner(__instance, p), out __result);

            ReplacedSuccess();
        }

        private static void SpawnItem(string barcode, Vector3 position, Quaternion rotation, Action<Poolee> callback, out UniTask<Poolee> source)
        {
            var _source = new UniTaskCompletionSource<Poolee>();
            source = new UniTask<Poolee>(_source.TryCast<IUniTaskSource<Poolee>>(), default);

            void continuation(Poolee poolee)
            {
                _source.TrySetResult(poolee);

                if (callback != null)
                    callback?.Invoke(poolee);
            }

            var spawnable = LocalAssetSpawner.CreateSpawnable(barcode);
            LocalAssetSpawner.Register(spawnable);
            LocalAssetSpawner.Spawn(spawnable, position, rotation, continuation);
        }

        private static void HandleSpawner(CrateSpawner spawner, Poolee poolee)
        {
            var go = poolee.gameObject;
            if (go == null)
                return;

            try
            {
                spawner.OnPooleeSpawn(go);
            }
            catch (Exception e)
            {
                Core.Logger.Error("An unexpected error has occurred while handling CrateSpawner OnPooleeSpawn", e);
            }

            poolee.OnDespawnDelegate += (Action<GameObject>)spawner.OnPooleeDespawn;

            try
            {
                // Some spawners might cause issues if you change the spawnable, for example from an NPC to a spawnable.
                // Hopefully there won't be many cases like this, but it's still an issue
                // Might resolve it later, though it will be difficult to patch it everywhere, if even possible
                spawner.onSpawnEvent?.Invoke(spawner, go);
            }
            catch (Exception e)
            {
                Core.Logger.Error("An unexpected error has occurred while invoking CrateSpawner onSpawnEvent", e);
            }
        }

        private static void ReplacedSuccess()
        {
            TotalReplacements++;
            LevelReplacements++;
            MenuManager.UpdateDebugCounts();
        }

        public static string GetReplacement(string barcode)
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
            if (string.IsNullOrWhiteSpace(entry?.Original) || string.IsNullOrWhiteSpace(entry?.Replacement))
                return false;

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