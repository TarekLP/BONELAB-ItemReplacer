using System.Linq;

using BoneLib;

using MelonLoader;

using RexmeckItemReplacer.Managers;

namespace RexmeckItemReplacer
{
    public static class ModInfo
    {
        public const string Name = "RexmeckGunReplacer";
        public const string Author = "Tarek";
        public const string Version = "1.0.0";
        public const string Company = "Tarek";
        public const string Description = "Replaces vanilla BONELAB guns with Rexmeck's Weapons that are far better (in my opinion).";
        public const string DownloadLink = null;
    }

    public class Core : MelonMod
    {

        public static MelonLogger.Instance Logger { get; private set; }

        private const string RexmeckMP5K = "Rexmeck.WeaponPackLT.Spawnable.MP5K";
        private const string RexmeckM4A1 = "Rexmeck.WeaponPackLT.Spawnable.M4A1";
        private static readonly ReplacerConfig RexmeckConfig = new()
        {
            ID = "rexmeck-default",
            Name = "Rexmeck",
            Color = "#FF00FF",
            Enabled = true,
            Categories = []
        };

        public override void OnInitializeMelon()
        {
            Logger = LoggerInstance;

            LoggerInstance.Msg("Setting up preferences");
            PreferencesManager.Setup();

            LoggerInstance.Msg("Setting up replacers");
            ReplacerManager.Setup();
            ReplacerManager.CreateFileWatcher();
            LoggerInstance.Msg("Adding default ones");
            if (ReplacerManager.Configs.Any(x => x.ID == RexmeckConfig.ID))
                ReplacerManager.Unregister(RexmeckConfig.ID, false);

            CreateRexmeckCategories();
            ReplacerManager.Register(RexmeckConfig, true);

            LoggerInstance.Msg("Setting up BoneMenu");
            MenuManager.Setup();

            LoggerInstance.Msg("Initialized.");
        }

        private static void CreateRexmeckCategories()
        {
            RexmeckConfig.Categories.Add(new("Pistols", [
                    new(CommonBarcodes.Guns.Eder22, "Rexmeck.WeaponPackLT.Spawnable.GLOCK17Switch"),
                    new(CommonBarcodes.Guns.RedEder22, "Rexmeck.WeaponPackLT.Spawnable.GLOCK17"),
                    new(CommonBarcodes.Guns.M1911, "Rexmeck.WeaponPackLT.Spawnable.1911CombatUnit"),
                    new(CommonBarcodes.Guns.M9, "Rexmeck.WeaponPackLT.Spawnable.M9A3"),
                    new(CommonBarcodes.Guns.eHGBlaster, "Rexmeck.WeaponPackLT.Spawnable.FiveSeven"),
                    new(CommonBarcodes.Guns.P350, "Rexmeck.WeaponPackLT.Spawnable.P226AngelWrap"),
                    new(CommonBarcodes.Guns.Gruber, "Rexmeck.WeaponPackLT.Spawnable.FiveSeven"),
                    new(CommonBarcodes.Guns.PT8Alaris, "Rexmeck.WeaponPackLT.Spawnable.DesertEagle")
                ]));
            RexmeckConfig.Categories.Add(new("SMGs", [
                new(CommonBarcodes.Guns.MP5, "Rexmeck.WeaponPackLT.Spawnable.MP5A2"),
                    new(CommonBarcodes.Guns.MP5KLaser, RexmeckMP5K),
                    new(CommonBarcodes.Guns.MP5KFlashlight, RexmeckMP5K),
                    new(CommonBarcodes.Guns.MP5KHolosight, RexmeckMP5K),
                    new(CommonBarcodes.Guns.MP5KSabrelake, "Rexmeck.WeaponPackLT.Spawnable.MP5SD"),
                    new(CommonBarcodes.Guns.MP5KIronsights, RexmeckMP5K),
                    new(CommonBarcodes.Guns.Vector, "Rexmeck.WeaponPackLT.Spawnable.KrissVector"),
                    new(CommonBarcodes.Guns.UZI, "Rexmeck.WeaponPackLT.Spawnable.P90"),
                    new(CommonBarcodes.Guns.UMP, "Rexmeck.WeaponPackLT.Spawnable.SMG45")
                ]));
            RexmeckConfig.Categories.Add(new("Rifles", [
                new(CommonBarcodes.Guns.Garand,"Rexmeck.WeaponPackLT.Spawnable.R700"),
                    new(CommonBarcodes.Guns.M16ACOG, "Rexmeck.WeaponPackLT.Spawnable.AR15BCM"),
                    new(CommonBarcodes.Guns.M16Holosight, "Rexmeck.WeaponPackLT.Spawnable.SG552AngelWrap"),
                    new(CommonBarcodes.Guns.M16IronSights, "Rexmeck.WeaponPackLT.Spawnable.M16A1"),
                    new(CommonBarcodes.Guns.M16LaserForegrip, RexmeckM4A1),
                    new(CommonBarcodes.Guns.MK18HoloForegrip, "Rexmeck.WeaponPackLT.Spawnable.AKMCustom"),
                    new(CommonBarcodes.Guns.MK18Holosight, RexmeckM4A1),
                    new(CommonBarcodes.Guns.MK18IronSights, "Rexmeck.WeaponPackLT.Spawnable.AKM"),
                    new(CommonBarcodes.Guns.MK18LaserForegrip, RexmeckM4A1),
                    new(CommonBarcodes.Guns.MK18Sabrelake, RexmeckM4A1),
                    new(CommonBarcodes.Guns.MK18Naked, "Rexmeck.WeaponPackLT.Spawnable.SG552"),
                    new(CommonBarcodes.Guns.AKM, "Rexmeck.WeaponPackLT.Spawnable.AKMCustom"),
                    new(CommonBarcodes.Guns.PDRC, "Rexmeck.WeaponPackLT.Spawnable.P90")
                ]));
            RexmeckConfig.Categories.Add(new("Shotguns", [
                new(CommonBarcodes.Guns.M590A1, "Rexmeck.WeaponPackLT.Spawnable.SPAS12"),
                    new(CommonBarcodes.Guns.M4, "Rexmeck.WeaponPackLT.Spawnable.M1014"),
                    new(CommonBarcodes.Guns.DuckSeasonShotgun, "Rexmeck.WeaponPackLT.Spawnable.Mossberg590Cruiser"),
                    new(CommonBarcodes.Guns.FAB, "Rexmeck.WeaponPackLT.Spawnable.Mossberg590" )
                ]));
        }
    }
}