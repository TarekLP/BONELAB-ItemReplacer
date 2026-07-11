using System;
using System.Collections.Generic;

using Il2CppSLZ.Marrow.Warehouse;

using ItemReplacer.Helpers;
using ItemReplacer.Managers;

using Scriban;
using Scriban.Runtime;

namespace ItemReplacer.Utilities
{
    public class ScribanCrate
    {
        public CrateType Type { get; }

        public string Barcode { get; }

        public string Title { get; }

        public string Description { get; }

        public bool Redacted { get; }

        public bool Unlockable { get; }

        public ScriptArray<string> Tags { get; }

        public ScriptArray<string> BoneTags { get; }

        public string Pallet { get; }

        public ScribanCrate(Crate crate)
        {
            Title = crate.Title;
            Description = crate.Description;
            Redacted = crate.Redacted;
            Barcode = crate.Barcode.ID;
            Unlockable = crate.Unlockable;
            if (crate.Tags == null)
                Tags = [];
            else
                Tags = [.. crate.Tags];

            Pallet = crate.Pallet.Barcode.ID;

            BoneTags = [.. crate.BoneTags?.Tags.GetBarcodes<BoneTagReference, DataCard>()];

            Type = crate.GetIl2CppType().Name switch
            {
                nameof(SpawnableCrate) => CrateType.Spawnable,
                nameof(AvatarCrate) => CrateType.Avatar,
                nameof(LevelCrate) => CrateType.Level,
                nameof(VFXCrate) => CrateType.VFX,
                _ => throw new ArgumentOutOfRangeException($"Crate type {crate.GetIl2CppType().Name} is not supported."),
            };
        }

        public enum CrateType
        {
            Spawnable,
            Avatar,
            Level,
            VFX
        }
    }

    public class ScribanPallet
    {
        public string Title { get; }
        public string Description { get; }
        public string Author { get; }
        public string Barcode { get; }

        public string[] Tags { get; }

        public bool Redacted { get; }

        public bool Unlockable { get; }

        public string Version { get; }

        public string SDKVersion { get; }

        public ScriptArray<string> Crates { get; }

        public ScriptArray<ScribanChangeLog> ChangeLogs { get; }

        public ScriptArray<string> DataCards { get; }

        public string[] Dependencies { get; }

        public ScribanPallet(Pallet pallet)
        {
            Barcode = pallet.Barcode.ID;
            Unlockable = pallet.Unlockable;
            Redacted = pallet.Redacted;
            Title = pallet.Title;
            if (pallet.Tags == null)
                Tags = [];
            else
                Tags = pallet.Tags.ToArray();
            Version = pallet.Version;
            Author = pallet.Author;
            Description = pallet.Description;
            SDKVersion = pallet.SDKVersion;

            if (pallet.ChangeLogs == null)
            {
                ChangeLogs = [];
            }
            else
            {
                List<ScribanChangeLog> scribanChangeLogs = [];
                foreach (var c in pallet.ChangeLogs)
                    scribanChangeLogs.Add(new ScribanChangeLog(c));
                ChangeLogs = [.. scribanChangeLogs];
            }

            Crates = [.. pallet.Crates.GetBarcodes()];
            DataCards = [.. pallet.DataCards.GetBarcodes()];
            Dependencies = [.. pallet.PalletDependencies.GetBarcodes<PalletReference, Pallet>()];
        }
    }

    public class ScribanChangeLog(Pallet.ChangeLog changelog)
    {
        public string Title { get; } = changelog.title;

        public string Version { get; } = changelog.version;

        public string Text { get; } = changelog.text;
    }

    public class ScribanDataCard(DataCard dataCard)
    {
        public string Title { get; } = dataCard.Title;
        public string Description { get; } = dataCard.Description;

        public string Barcode { get; } = dataCard.Barcode.ID;

        public bool Redacted { get; } = dataCard.Redacted;

        public bool Unlockable { get; } = dataCard.Unlockable;

        public string Pallet { get; } = dataCard.Pallet.Barcode.ID;
    }

    public static class ScribanHelper
    {
        public static ScribanPallet GetPallet(string barcode)
        {
            if (AssetWarehouse.Instance.TryGetPallet(new Barcode(barcode), out var pallet))
                return new ScribanPallet(pallet);

            return null;
        }

        public static ScribanCrate GetCrate(string barcode)
        {
            if (AssetWarehouse.Instance.TryGetCrate(new Barcode(barcode), out var crate))
                return new ScribanCrate(crate);

            return null;
        }

        public static ScribanDataCard GetDataCard(string barcode)
        {
            if (AssetWarehouse.Instance.TryGetDataCard(new Barcode(barcode), out var dataCard))
                return new ScribanDataCard(dataCard);

            return null;
        }

        public static string CleanString(string str)
            => Core.RemoveUnityRichText(str);
    }

    public static class ScribanMatcher
    {
        public static bool Match(string barcode, ReplacerEntry entry)
        {
            if (entry.Template?.HasErrors != false)
                return false;

            if (!AssetWarehouse.Instance.TryGetCrate(new Barcode(barcode), out var crate))
                return false;

            var scrate = new ScribanCrate(crate);

            var scriptObject = new ScriptObject(StringComparer.OrdinalIgnoreCase);
            scriptObject.Import(scrate);
            scriptObject.Import(typeof(ScribanHelper));

            var templateContext = new TemplateContext();
            templateContext.PushGlobal(scriptObject);

            var result = entry.Template.Render(templateContext);
            return result.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase);
        }
    }
}