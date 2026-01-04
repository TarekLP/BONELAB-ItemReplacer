using System.Linq;
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

        // Gun replacing Logic
        [HarmonyPrefix]
        [HarmonyPriority(int.MaxValue)]
        [HarmonyPatch(nameof(CrateSpawner.SpawnSpawnableAsync))]
        public static bool Prefix(CrateSpawner __instance, ref UniTask<Poolee> __result)
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
                    return false;
                }
                else if (PreferencesManager.FusionSupport?.Value == true)
                {
                    bool _continue = Fusion.HandleFusionCrateSpawner(targetBarcode, __instance, out UniTask<Poolee> res);
                    __result = res ?? new UniTask<Poolee>(null);

                    return _continue;

                }
                return false;

            }
            return true;
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

                    var replacement = category?.Entries?.FirstOrDefault(e => e.IsRegEx ? Regex.IsMatch(barcode, e.Original) : barcode == e.Original);
                    if (replacement != null)
                        return replacement.ReplaceWith;
                }
            }
            return null;
        }

        private static void SpawnItem(string barcode, Vector3 position, Quaternion rotation, UniTaskCompletionSource<Poolee> source)
        {
            var scale = new Il2CppSystem.Nullable<Vector3>(Vector3.zero)
            {
                hasValue = false,
            };
            var groupId = new Il2CppSystem.Nullable<int>(0)
            {
                hasValue = false,
            };
            var spawnable = new Spawnable()
            {
                crateRef = new SpawnableCrateReference(barcode),
                policyData = null
            };
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
                catch (System.Exception e)
                {
                    Core.Logger.Error($"Error spawning replaced item", e);
                    source.TrySetResult(null);
                }
            });
        }

        public static string RemoveUnityRichText(this string text)
            => Regex.Replace(text, "<.*?>", string.Empty);

    }
}
