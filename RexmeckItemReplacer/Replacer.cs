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

namespace RexmeckItemReplacer
{

	[HarmonyPatch(typeof(CrateSpawner))]
	internal static class Replacer
	{
		/* private static readonly Dictionary<string, string> ReplacementMap = new Dictionary<string, string>
				{
					// PISTOLs
					{ CommonBarcodes.Guns.Eder22, "Rexmeck.WeaponPackLT.Spawnable.GLOCK17" }, // Glock 17
					{ CommonBarcodes.Guns.RedEder22, "Rexmeck.WeaponPackLT.Spawnable.GLOCK17" }, // Glock 17 (Red)
					{ CommonBarcodes.Guns.M1911, "Rexmeck.WeaponPackLT.Spawnable.1911CombatUnit" }, //1911
					{ CommonBarcodes.Guns.M9, "Rexmeck.WeaponPackLT.Spawnable.M9A3" }, //M9
					{ CommonBarcodes.Guns.eHGBlaster, "Rexmeck.WeaponPackLT.Spawnable.GLOCK17Switch" }, //EHG
					{ CommonBarcodes.Guns.P350, "Rexmeck.WeaponPackLT.Spawnable.P226AngelWrap" }, //P350
					{ CommonBarcodes.Guns.Gruber, "Rexmeck.WeaponPackLT.Spawnable.FiveSeven" }, //Gruber
					{ CommonBarcodes.Guns.PT8Alaris, "Rexmeck.WeaponPackLT.Spawnable.DesertEagle" }, //PT8-Alaris
					// SMGs
					{ CommonBarcodes.Guns.MP5, "Rexmeck.WeaponPackLT.Spawnable.MP5A2" }, // MP5
					{ CommonBarcodes.Guns.MP5KLaser, "Rexmeck.WeaponPackLT.Spawnable.MP5A2" }, //MP5K - Laser
					{ CommonBarcodes.Guns.MP5KFlashlight, "Rexmeck.WeaponPackLT.Spawnable.MP5A2" }, //MP5K - Flashlight
					{ CommonBarcodes.Guns.MP5KHolosight, "Rexmeck.WeaponPackLT.Spawnable.MP5A2" }, //MP5K - Holosight
					{ CommonBarcodes.Guns.MP5KSabrelake, "Rexmeck.WeaponPackLT.Spawnable.MP5SD" }, //MP5K - Sabrelake
					{ CommonBarcodes.Guns.MP5KIronsights, "Rexmeck.WeaponPackLT.Spawnable.MP5K" }, //MP5K - Ironsight
					{ CommonBarcodes.Guns.Vector, "Rexmeck.WeaponPackLT.Spawnable.KrissVector" }, // Kriss Vector
					{ CommonBarcodes.Guns.UZI, "Rexmeck.WeaponPackLT.Spawnable.MP5A2" }, // Uzi
					{ CommonBarcodes.Guns.UMP, "Rexmeck.WeaponPackLT.Spawnable.SMG45" }, // UMP
					// RIFLEs
					{ CommonBarcodes.Guns.Garand,"Rexmeck.WeaponPackLT.Spawnable.R700"}, //M1 Garand - Desert Eagle
					{ CommonBarcodes.Guns.M16ACOG, "Rexmeck.WeaponPackLT.Spawnable.AR15BCM" }, //M16 - ACOG
					{ CommonBarcodes.Guns.M16Holosight, "Rexmeck.WeaponPackLT.Spawnable.SG552AngelWrap" }, //M16 - Holo
					{ CommonBarcodes.Guns.M16IronSights, "Rexmeck.WeaponPackLT.Spawnable.M16A1" },//M16 - Ironsights
					{ CommonBarcodes.Guns.M16LaserForegrip, "Rexmeck.WeaponPackLT.Spawnable.M4A1" }, //M16 - Laser Foregrip
					{ CommonBarcodes.Guns.MK18HoloForegrip, "Rexmeck.WeaponPackLT.Spawnable.AKMCustom" }, //MK18 - Holo Foregrip
					{ CommonBarcodes.Guns.MK18Holosight, "Rexmeck.WeaponPackLT.Spawnable.M4A1" }, //MK18 -  Holosight
					{ CommonBarcodes.Guns.MK18IronSights, "Rexmeck.WeaponPackLT.Spawnable.AKM" }, //MK18 - Ironsight
					{ CommonBarcodes.Guns.MK18LaserForegrip, "Rexmeck.WeaponPackLT.Spawnable.M4A1" }, //MK18 - Laser Foregrip
					{ CommonBarcodes.Guns.MK18Sabrelake, "Rexmeck.WeaponPackLT.Spawnable.M4A1" }, //MK18 - Sabrelake
					{ CommonBarcodes.Guns.MK18Naked, "Rexmeck.WeaponPackLT.Spawnable.SG552" }, //MK18 - Naked
					{ CommonBarcodes.Guns.AKM, "Rexmeck.WeaponPackLT.Spawnable.AKMCustom" }, // AKM
					{ CommonBarcodes.Guns.PDRC, "Rexmeck.WeaponPackLT.Spawnable.P90" }, // PDRC
					// SHOTGUNs
					{ CommonBarcodes.Guns.M590A1, "Rexmeck.WeaponPackLT.Spawnable.SPAS12" }, // M590A1
					{ CommonBarcodes.Guns.M4, "Rexmeck.WeaponPackLT.Spawnable.M1014" }, //M1014
					{ CommonBarcodes.Guns.DuckSeasonShotgun, "Rexmeck.WeaponPackLT.Spawnable.Mossberg590Cruiser"}, //M870 - Duck Season Shotgun
					{ CommonBarcodes.Guns.FAB, "Rexmeck.WeaponPackLT.Spawnable.Mossberg590"  } //FAB
				};
		*/

		private static readonly Dictionary<string, string> Pistols = new Dictionary<string, string>
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

		private static readonly Dictionary<string, string> SMGs = new Dictionary<string, string>
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

		private static readonly Dictionary<string, string> Rifles = new Dictionary<string, string>
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

		private static readonly Dictionary<string, string> Shotguns = new Dictionary<string, string>
		{
			{ CommonBarcodes.Guns.M590A1, "Rexmeck.WeaponPackLT.Spawnable.SPAS12" },
			{ CommonBarcodes.Guns.M4, "Rexmeck.WeaponPackLT.Spawnable.M1014" },
			{ CommonBarcodes.Guns.DuckSeasonShotgun, "Rexmeck.WeaponPackLT.Spawnable.Mossberg590Cruiser"},
			{ CommonBarcodes.Guns.FAB, "Rexmeck.WeaponPackLT.Spawnable.Mossberg590"  }
		};
		//Menu Building Logic


		// Gun replacing Logic
		[HarmonyPrefix]
		[HarmonyPatch(nameof(CrateSpawner.SpawnSpawnableAsync))]
		[HarmonyPatch(nameof(CrateSpawner.SpawnSpawnable))]
		public static bool Prefix(CrateSpawner __instance)
		{
			// Is the mod enabled or disabled?
			if (!RexmeckReplacer.IsReplacerEnabled.Value) return true;
			if (__instance.spawnableCrateReference?.Barcode == null) return true;

			string currentBarcode = __instance.spawnableCrateReference.Barcode.ID;
			string targetBarcode = null;

			// Check Categories based on Settings
			if (RexmeckReplacer.ReplacePistols.Value && Pistols.ContainsKey(currentBarcode))
				targetBarcode = Pistols[currentBarcode];

			else if (RexmeckReplacer.ReplaceSMGs.Value && SMGs.ContainsKey(currentBarcode))
				targetBarcode = SMGs[currentBarcode];

			else if (RexmeckReplacer.ReplaceRifles.Value && Rifles.ContainsKey(currentBarcode))
				targetBarcode = Rifles[currentBarcode];

			else if (RexmeckReplacer.ReplaceShotguns.Value && Shotguns.ContainsKey(currentBarcode))
				targetBarcode = Shotguns[currentBarcode];


			// if there is no barcode, there is no replacement.
			if (__instance.spawnableCrateReference?.Barcode == null) return true;			
			
			//While the barcode isnt null, there is a replacement.
			if (targetBarcode != null)
			{
				MelonLogger.Msg("Replacing with: " + targetBarcode);
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
					crateRef = new Il2CppSLZ.Marrow.Warehouse.SpawnableCrateReference(targetBarcode),
					policyData = null
				};
				AssetSpawner.Register(spawnable);
				AssetSpawner.Spawn(spawnable, __instance.transform.position, __instance.transform.rotation, scale, null, false, groupId, null, null);
				return false;
				
			}
			return true;
		}
	}
}
 