using MelonLoader;
using BoneLib;
using BoneLib.BoneMenu;
using BoneLib.BoneMenu.UI;
using HarmonyLib;
using Il2CppSLZ.Marrow.Warehouse;
using UnityEngine;
using System.Collections.Generic;

[assembly: MelonInfo(typeof(RexmeckItemReplacer.RexmeckReplacer), RexmeckItemReplacer.BuildInfo.Name, RexmeckItemReplacer.BuildInfo.Version, RexmeckItemReplacer.BuildInfo.Author, RexmeckItemReplacer.BuildInfo.DownloadLink)]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace RexmeckItemReplacer
{
	public static class BuildInfo
	{
		public const string Name = "RexmeckGunReplacer";
		public const string Author = "Tarek";
		public const string Version = "1.0.0";
		public const string Company = "Tarek";
		public const string Description = "Replaces vanilla BONELAB guns with Rexmeck's Weapons that are far better (in my opinion).";
		public const string DownloadLink = null;
	}

	public class RexmeckReplacer : MelonMod
	{
		// ------Attributes------

		// Mapping Vanilla Barcodes -> Rexmeck LT Barcodes
		private static bool _debugMode = false;
		private static bool _menuCreated = false;
		private static readonly Dictionary<string, string> ReplacementMap = new Dictionary<string, string>
		{
			// PISTOLS
			{ "SLZ.BONELAB.Content.Spawnable.HandgunEder22training", "Rexmeck.WeaponPackLT.Spawnable.GLOCK17" }, // Glock 17
			{ "c1534c5a-fcfc-4f43-8fb0-d29531393131", "Rexmeck.WeaponPackLT.Spawnable.P226AngelWrap" }, //1911
			{ "c1534c5a-aade-4fa1-8f4b-d4c547756e4d", "Rexmeck.WeaponPackLT.Spawnable.M9A3" }, //M9
			{"SLZ.BONELAB.CORE.Spawnable.GunEHG", "Rexmeck.WeaponPackLT.Spawnable.P226" }, //EHG
			{ "c1534c5a-bcb7-4f02-a4f5-da9550333530", "Rexmeck.WeaponPackLT.Spawnable.P226AngelWrap" }, //P350
			{"c1534c5a-9f55-4c56-ae23-d33b47727562", "Rexmeck.WeaponPackLT.Spawnable.P226" }, //Gruber
			{"c1534c5a-50cf-4500-83d5-c0b447756e50", "Rexmeck.WeaponPackLT.Spawnable.P226" }, //PT8-Alaris
			// SMGs
			{ "c1534c5a-d00c-4aa8-adfd-3495534d474d", "Rexmeck.WeaponPackLT.Spawnable.MP5A2" }, // MP5
			{ "c1534c5a-ccfa-4d99-af97-5e95534d474d", "Rexmeck.WeaponPackLT.Spawnable.MP5A2" }, //MP5K - Laser
			{ "c1534c5a-3e35-4aeb-b1ec-4a95534d474d", "Rexmeck.WeaponPackLT.Spawnable.MP5A2" }, //MP5K - Flashlight
			{ "fa534c5a83ee4ec6bd641fec424c4142.Spawnable.MP5KRedDotSight", "Rexmeck.WeaponPackLT.Spawnable.MP5A2" }, //MP5K - Holosight
			{ "c1534c5a-6670-4ac2-a82a-a595534d474d", "Rexmeck.WeaponPackLT.Spawnable.MP5SD" }, //MP5K - Sabrelake
			{ "c1534c5a-9f54-4f32-b8b9-f295534d474d", "Rexmeck.WeaponPackLT.Spawnable.MP5K" }, //MP5K - Ironsight
			{ "c1534c5a-4c47-428d-b5a5-b05747756e56", "Rexmeck.WeaponPackLT.Spawnable.KrissVector" }, // Kriss Vector
			{ "c1534c5a-8d03-42de-93c7-f595534d4755", "Rexmeck.WeaponPackLT.Spawnable.MP5A2" }, // Uzi
			{ "c1534c5a-40e5-40e0-8139-194347756e55", "Rexmeck.WeaponPackLT.Spawnable.SMG45" }, // UMP
			// RIFLES
			{ "SLZ.BONELAB.Content.Spawnable.RifleM1Garand","Rexmeck.WeaponPackLT.Spawnable.DesertEagle"}, //M1 Garand - Desert Eagle
			{ "c1534c5a-ea97-495d-b0bf-ac955269666c", "Rexmeck.WeaponPackLT.Spawnable.M4A1" }, //M16 - ACOG
			{ "c1534c5a-cc53-4aac-b842-46955269666c", "Rexmeck.WeaponPackLT.Spawnable.SG552AngelWrap" }, //M16 - Holo
			{ "c1534c5a-9112-49e5-b022-9c955269666c", "Rexmeck.WeaponPackLT.Spawnable.M16A1" },//M16 - Ironsights
			{ "c1534c5a-4e5b-4fb7-be33-08955269666c", "Rexmeck.WeaponPackLT.Spawnable.M4A1" }, //M16 - Laser Foregrip
			{ "SLZ.BONELAB.Content.Spawnable.RifleMK18HoloForegrip", "Rexmeck.WeaponPackLT.Spawnable.M4A1" }, //MK18 - Holo Foregrip
			{ "c1534c5a-c061-4c5c-a5e2-3d955269666c", "Rexmeck.WeaponPackLT.Spawnable.M4A1" }, //MK18 -  Holosight
			{ "c1534c5a-f3b6-4161-a525-a8955269666c", "Rexmeck.WeaponPackLT.Spawnable.M4A1" }, //MK18 - Ironsight
			{ "c1534c5a-ec8e-418a-a545-cf955269666c", "Rexmeck.WeaponPackLT.Spawnable.M4A1" }, //MK18 - Laser Foregrip
			{ "c1534c5a-4b3e-4288-849c-ce955269666c", "Rexmeck.WeaponPackLT.Spawnable.M4A1" }, //MK18 - Sabrelake
			{ "c1534c5a-5c2b-4cb4-ae31-e7955269666c", "Rexmeck.WeaponPackLT.Spawnable.M4A1" }, //MK18 - Naked
			{ "c1534c5a-a6b5-4177-beb8-04d947756e41", "Rexmeck.WeaponPackLT.Spawnable.AKMCustom" }, // AKM
			{ "c1534c5a-04d7-41a0-b7b8-5a95534d4750", "Rexmeck.WeaponPackLT.Spawnable.P90" }, // PDRC
			// SHOTGUNS
			{ "c1534c5a-7f05-402f-9320-609647756e35", "Rexmeck.WeaponPackLT.Spawnable.SPAS12" },
			{ "c1534c5a-e0b5-4d4b-9df3-567147756e4d", "Rexmeck.WeaponPackLT.Spawnable.M1014" }, //M1014
			{ "c1534c5a-571f-43dc-8bc6-8e9553686f74", "Rexmeck.WeaponPackLT.Spawnable.Mossberg590Cruiser"}, //M870
			{ "c1534c5a-2774-48db-84fd-778447756e46", "Rexmeck.WeaponPackLT.Spawnable.M1014"  } //FAB
		};

		
		
		public override void OnInitializeMelon()
		{
			MelonLogger.Msg(System.ConsoleColor.Magenta, "Rexmeck Replacer Initialized.");

		}
		
	}
}