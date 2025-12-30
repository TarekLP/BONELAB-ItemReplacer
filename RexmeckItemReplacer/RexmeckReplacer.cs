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
		// Preferences to save the state
		public static MelonPreferences_Category MyModCategory;
		public static MelonPreferences_Entry<bool> IsReplacerEnabled;

		public override void OnInitializeMelon()
		{
			MelonLogger.Msg(System.ConsoleColor.Magenta, "Rexmeck Replacer Initialized.");

			// 1. Setup Preferences
			MyModCategory = MelonPreferences.CreateCategory("RexmeckReplacer");
			IsReplacerEnabled = MyModCategory.CreateEntry("Enabled", true);

			// 2. Setup Menu
			CreateBoneMenu();
		}

		private void CreateBoneMenu()
		{
			Page rootPage = Page.Root;

			// Create a sub-page with your purple color
			Page myPage = rootPage.CreatePage("Rexmeck Replacer", new Color(0.6f, 0.0f, 0.8f));

			// Create the toggle
			myPage.CreateBool("Enable Replacer", Color.white, IsReplacerEnabled.Value, (bool value) =>
			{
				IsReplacerEnabled.Value = value;
				MyModCategory.SaveToFile();
				MelonLogger.Msg($"Replacer toggled: {value}");
			});
		}

	}
}