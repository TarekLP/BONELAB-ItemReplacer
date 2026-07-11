using HarmonyLib;

using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;

using ItemReplacer.Managers;
using ItemReplacer.Utilities;

using UnityEngine;

namespace ItemReplacer.Patches
{
    [HarmonyPatch(typeof(SpawnGun))]
    internal static class SpawnGunPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SpawnGun.OnFire))]
        public static bool OnFirePrefix(SpawnGun __instance, ref SpawnableCrate __state)
        {
            if (PreferencesManager.Enabled?.Value != true || PreferencesManager.ReplaceEverything?.Value != true)
                return true;

            __state = __instance._selectedCrate;
            __instance._selectedCrate = null;

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SpawnGun.OnFire))]
        public static void OnFirePostfix(SpawnGun __instance, ref SpawnableCrate __state)
        {
            if (PreferencesManager.Enabled?.Value != true || PreferencesManager.ReplaceEverything?.Value != true)
                return;

            __instance._selectedCrate = __state;

            if (__instance._selectedMode == UtilityModes.SPAWNER)
                OnFireSpawn(__instance);
        }

        private static void OnFireSpawn(SpawnGun spawnGun)
        {
            // Check for prevention
            if (!Fusion.CanUseSpawnGun)
                return;

            var crate = spawnGun._selectedCrate;

            if (crate == null)
                return;

            // Send a spawn request
            var spawnable = new Spawnable() { crateRef = new SpawnableCrateReference(crate.Barcode) };
            var transform = spawnGun.placerPreview.transform;

            if (!Fusion.IsConnected)
            {
                var scale = new Il2CppSystem.Nullable<Vector3>(Vector3.zero)
                {
                    hasValue = false,
                };

                var groupId = new Il2CppSystem.Nullable<int>(0)
                {
                    hasValue = false,
                };

                AssetSpawner.Register(spawnable);
                AssetSpawner.Spawn(spawnable, transform.position, transform.rotation, scale, null, false, groupId, null, null);
            }
            else
            {
                Fusion.SpawnGunSync(spawnable, transform);
            }
        }
    }
}