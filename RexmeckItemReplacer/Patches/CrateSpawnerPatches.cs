using BoneLib;

using HarmonyLib;

using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;

using Il2CppSystem;

using MelonLoader;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace RexmeckItemReplacer.Patches
{

    [HarmonyPatch(typeof(CrateSpawner))]
    internal static class CrateSpawnerPatches
    {

        private static readonly Dictionary<string, string> Pistols = new()
        {
            { CommonBarcodes.Guns.Eder22, "Rexmeck.WeaponPackLT.Spawnable.GLOCK17Switch" },
            { CommonBarcodes.Guns.RedEder22, "Rexmeck.WeaponPackLT.Spawnable.GLOCK17" },
            { CommonBarcodes.Guns.M1911, "Rexmeck.WeaponPackLT.Spawnable.1911CombatUnit" },
            { CommonBarcodes.Guns.M9, "Rexmeck.WeaponPackLT.Spawnable.M9A3" },
            { CommonBarcodes.Guns.eHGBlaster, "Rexmeck.WeaponPackLT.Spawnable.FiveSeven" },
            { CommonBarcodes.Guns.P350, "Rexmeck.WeaponPackLT.Spawnable.P226AngelWrap" },
            { CommonBarcodes.Guns.Gruber, "Rexmeck.WeaponPackLT.Spawnable.FiveSeven" },
            { CommonBarcodes.Guns.PT8Alaris, "Rexmeck.WeaponPackLT.Spawnable.DesertEagle" }
        };

        private static readonly Dictionary<string, string> SMGs = new()
        {
            { CommonBarcodes.Guns.MP5, "Rexmeck.WeaponPackLT.Spawnable.MP5A2" },
            { CommonBarcodes.Guns.MP5KLaser, "Rexmeck.WeaponPackLT.Spawnable.MP5K" },
            { CommonBarcodes.Guns.MP5KFlashlight, "Rexmeck.WeaponPackLT.Spawnable.MP5K" },
            { CommonBarcodes.Guns.MP5KHolosight, "Rexmeck.WeaponPackLT.Spawnable.MP5K" },
            { CommonBarcodes.Guns.MP5KSabrelake, "Rexmeck.WeaponPackLT.Spawnable.MP5SD" },
            { CommonBarcodes.Guns.MP5KIronsights, "Rexmeck.WeaponPackLT.Spawnable.MP5K" },
            { CommonBarcodes.Guns.Vector, "Rexmeck.WeaponPackLT.Spawnable.KrissVector" },
            { CommonBarcodes.Guns.UZI, "Rexmeck.WeaponPackLT.Spawnable.P90" },
            { CommonBarcodes.Guns.UMP, "Rexmeck.WeaponPackLT.Spawnable.SMG45" }
        };

        private static readonly Dictionary<string, string> Rifles = new()
        {
            { CommonBarcodes.Guns.Garand,"Rexmeck.WeaponPackLT.Spawnable.R700"},
            { CommonBarcodes.Guns.M16ACOG, "Rexmeck.WeaponPackLT.Spawnable.AR15BCM" },
            { CommonBarcodes.Guns.M16Holosight, "Rexmeck.WeaponPackLT.Spawnable.SG552AngelWrap" },
            { CommonBarcodes.Guns.M16IronSights, "Rexmeck.WeaponPackLT.Spawnable.M16A1" },
            { CommonBarcodes.Guns.M16LaserForegrip, "Rexmeck.WeaponPackLT.Spawnable.M4A1" },
            { CommonBarcodes.Guns.MK18HoloForegrip, "Rexmeck.WeaponPackLT.Spawnable.AKMCustom" },
            { CommonBarcodes.Guns.MK18Holosight, "Rexmeck.WeaponPackLT.Spawnable.M4A1" },
            { CommonBarcodes.Guns.MK18IronSights, "Rexmeck.WeaponPackLT.Spawnable.AKM" },
            { CommonBarcodes.Guns.MK18LaserForegrip, "Rexmeck.WeaponPackLT.Spawnable.M4A1" },
            { CommonBarcodes.Guns.MK18Sabrelake, "Rexmeck.WeaponPackLT.Spawnable.M4A1" },
            { CommonBarcodes.Guns.MK18Naked, "Rexmeck.WeaponPackLT.Spawnable.SG552" },
            { CommonBarcodes.Guns.AKM, "Rexmeck.WeaponPackLT.Spawnable.AKMCustom" },
            { CommonBarcodes.Guns.PDRC, "Rexmeck.WeaponPackLT.Spawnable.P90" }
        };

        private static readonly Dictionary<string, string> Shotguns = new()
        {
            { CommonBarcodes.Guns.M590A1, "Rexmeck.WeaponPackLT.Spawnable.SPAS12" },
            { CommonBarcodes.Guns.M4, "Rexmeck.WeaponPackLT.Spawnable.M1014" },
            { CommonBarcodes.Guns.DuckSeasonShotgun, "Rexmeck.WeaponPackLT.Spawnable.Mossberg590Cruiser"},
            { CommonBarcodes.Guns.FAB, "Rexmeck.WeaponPackLT.Spawnable.Mossberg590"  }
        };

        private static readonly Dictionary<string, string> NPCs = new()
        {
            { CommonBarcodes.NPCs.Nullbody, "RachelCorp.DestructibleDudes.Spawnable.NullbodyRegeneratable" },
	//		{ CommonBarcodes.NPCs.NullbodyAgent, "Rexmeck.WeaponPackLT.Spawnable.M1014" },  // - Nullbody Agent not in Gibbable Dudes
	//		{ CommonBarcodes.NPCs.NullbodyCorrupted, "Rexmeck.WeaponPackLT.Spawnable.M1014" }, // - Nullbody Corrupted not in Gibbable Dudes
			{ CommonBarcodes.NPCs.EarlyExitZombie, "RachelCorp.DestructibleDudes.Spawnable.EarlyExitRegeneratable"  },
            { CommonBarcodes.NPCs.Ford, "RachelCorp.DestructibleDudes.Spawnable.FordGibbable"  }
        };

        // Gun replacing Logic
        [HarmonyPrefix]
        [HarmonyPriority(-10)]
        [HarmonyPatch(nameof(CrateSpawner.SpawnSpawnableAsync))]
        [HarmonyPatch(nameof(CrateSpawner.SpawnSpawnable))]
        public static bool Prefix(CrateSpawner __instance)
        {
            // Is the mod enabled or disabled?
            if (!Core.IsReplacerEnabled.Value) return true;
            if (__instance.spawnableCrateReference?.Barcode == null) return true;

            string currentBarcode = __instance.spawnableCrateReference.Barcode.ID;
            string targetBarcode = null;

            // Check Categories based on Settings
            if (Core.ReplacePistols.Value && Pistols.ContainsKey(currentBarcode))
                targetBarcode = Pistols[currentBarcode];

            else if (Core.ReplaceSMGs.Value && SMGs.ContainsKey(currentBarcode))
                targetBarcode = SMGs[currentBarcode];

            else if (Core.ReplaceRifles.Value && Rifles.ContainsKey(currentBarcode))
                targetBarcode = Rifles[currentBarcode];

            else if (Core.ReplaceShotguns.Value && Shotguns.ContainsKey(currentBarcode))
                targetBarcode = Shotguns[currentBarcode];

            else if (Core.ReplaceNPCs.Value && NPCs.ContainsKey(currentBarcode))
                targetBarcode = NPCs[currentBarcode];


            // if there is no barcode, there is no replacement.
            if (__instance.spawnableCrateReference?.Barcode == null) return true;

            //While the barcode isnt null, there is a replacement.
            if (targetBarcode != null)
            {
                Core.Logger.Msg("Replacing with: " + targetBarcode);
                SpawnItem(targetBarcode, __instance.transform.position, __instance.transform.rotation);
                return false;

            }
            return true;
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
