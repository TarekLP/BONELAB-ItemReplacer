using BoneLib;

using HarmonyLib;

using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;

using Il2CppSystem;

using MelonLoader;

using RexmeckItemReplacer.Managers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using UnityEngine;

namespace RexmeckItemReplacer.Patches
{

    [HarmonyPatch(typeof(CrateSpawner))]
    internal static class CrateSpawnerPatches
    {

        // Gun replacing Logic
        [HarmonyPrefix]
        [HarmonyPriority(-10)]
        [HarmonyPatch(nameof(CrateSpawner.SpawnSpawnableAsync))]
        [HarmonyPatch(nameof(CrateSpawner.SpawnSpawnable))]
        public static bool Prefix(CrateSpawner __instance)
        {
            // Is the mod enabled or disabled?
            if (PreferencesManager.Enabled?.Value != true) return true;
            if (__instance.spawnableCrateReference?.Barcode == null) return true;

            string currentBarcode = __instance.spawnableCrateReference.Barcode.ID;
            string targetBarcode = GetReplacement(currentBarcode);


            // if there is no barcode, there is no replacement.
            if (__instance.spawnableCrateReference?.Barcode == null) return true;

            //While the barcode isnt null, there is a replacement.
            if (targetBarcode != null)
            {
                Core.Logger.Msg($"Replacing with: {targetBarcode} (Original: {currentBarcode})");
                SpawnItem(targetBarcode, __instance.transform.position, __instance.transform.rotation);
                return false;

            }
            return true;
        }

        private static string GetReplacement(string barcode)
        {
            foreach (var config in ReplacerManager.Configs)
            {
                if (!config.Enabled)
                    continue;

                foreach (var category in config.Categories)
                {
                    if (!category.Enabled)
                        continue;

                    var replacement = category.Entries.FirstOrDefault(e => Regex.IsMatch(barcode, e.Original));
                    if (replacement != null)
                        return replacement.ReplaceWith;
                }
            }
            return null;
        }

        private static void SpawnItem(string barcode, Vector3 position, Quaternion rotation)
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
            AssetSpawner.Spawn(spawnable, position, rotation, scale, null, false, groupId, null, null);
        }
    }
}
