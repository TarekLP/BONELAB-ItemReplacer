using HarmonyLib;

using Il2CppCysharp.Threading.Tasks;

using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;

namespace ItemReplacer.Patches
{
    [HarmonyPatch(typeof(LabFusion.Marrow.Patching.CrateSpawnerPatches))]
    public static class FusionPatches
    {
        [HarmonyPatch(nameof(LabFusion.Marrow.Patching.CrateSpawnerPatches.SpawnSpawnableAsyncPrefix))]
        [HarmonyPrefix]
        public static bool Prefix(CrateSpawner __instance, ref UniTask<Poolee> __result)
        {
            if (__instance?.spawnableCrateReference?.Barcode == null) return true;

            string currentBarcode = __instance.spawnableCrateReference.Barcode.ID;
            if (string.IsNullOrWhiteSpace(CrateSpawnerPatches.GetReplacement(currentBarcode)))
            {
                return true;
            }
            else
            {
                __result = new UniTask<Poolee>(null);
                return false;
            }
        }
    }
}