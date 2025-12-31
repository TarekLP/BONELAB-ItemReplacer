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
		// Preferences
		public static MelonPreferences_Category MyModCategory;
		public static MelonPreferences_Entry<bool> IsReplacerEnabled;
		public static MelonPreferences_Entry<bool> DebugMode;

		// Category Preferences
		public static MelonPreferences_Entry<bool> ReplacePistols;
		public static MelonPreferences_Entry<bool> ReplaceSMGs;
		public static MelonPreferences_Entry<bool> ReplaceRifles;
		public static MelonPreferences_Entry<bool> ReplaceShotguns;

		public override void OnInitializeMelon()
		{
			MelonLogger.Msg(System.ConsoleColor.Magenta, "Rexmeck Replacer Initialized.");

			// 1. Setup Preferences
			MyModCategory = MelonPreferences.CreateCategory("RexmeckReplacer");
			IsReplacerEnabled = MyModCategory.CreateEntry("Enabled", true);
			DebugMode = MyModCategory.CreateEntry("DebugMode", false, "Debug Logging");

			ReplacePistols = MyModCategory.CreateEntry("ReplacePistols", true);
			ReplaceSMGs = MyModCategory.CreateEntry("ReplaceSMGs", true);
			ReplaceRifles = MyModCategory.CreateEntry("ReplaceRifles", true);
			ReplaceShotguns = MyModCategory.CreateEntry("ReplaceShotguns", true);

			// 2. Setup Menu
			CreateBoneMenu();
		}

		private void CreateBoneMenu()
		{
			Page rootPage = Page.Root;

			// Root Page (Purple)
			Page myPage = rootPage.CreatePage("Rexmeck Replacer", new Color(0.6f, 0.0f, 0.8f));

			// Master Toggle
			myPage.CreateBool("Enable Mod", Color.white, IsReplacerEnabled.Value, (bool value) =>
			{
				IsReplacerEnabled.Value = value;
				MyModCategory.SaveToFile();
			});

			// Categories Sub-Page
			Page filtersPage = myPage.CreatePage("Categories", Color.yellow);

			filtersPage.CreateBool("Pistols", Color.white, ReplacePistols.Value, (v) => { ReplacePistols.Value = v; MyModCategory.SaveToFile(); });
			filtersPage.CreateBool("SMGs", Color.white, ReplaceSMGs.Value, (v) => { ReplaceSMGs.Value = v; MyModCategory.SaveToFile(); });
			filtersPage.CreateBool("Rifles", Color.white, ReplaceRifles.Value, (v) => { ReplaceRifles.Value = v; MyModCategory.SaveToFile(); });
			filtersPage.CreateBool("Shotguns", Color.white, ReplaceShotguns.Value, (v) => { ReplaceShotguns.Value = v; MyModCategory.SaveToFile(); });

			// Debug Toggle
			myPage.CreateBool("Debug Logging", Color.red, DebugMode.Value, (bool value) =>
			{
				DebugMode.Value = value;
				MyModCategory.SaveToFile();
			});
		}
	}
}