# 01 - Basics

This is an example that explains in detail how the Config file works.

## Example

```json
{
  "$schema": "https://raw.githubusercontent.com/TarekLP/BONELAB-ItemReplacer/refs/heads/master/Utilities/schema.json",
  "name": "Simple",
  "color": "#FFEA00",
  "id": "simple",
  "enabled": true,
  "categories": [
    {
      "name": "AKM",
      "enabled": true,
      "entries": [
        {
          "original": "c1534c5a-a6b5-4177-beb8-04d947756e41",
          "replaceWith": "Remox.RemoxsWeaponPack.Spawnable.AKM",
          "isRegEx": false
        }
      ]
    }
  ]
}

```

<img width="340" height="80" alt="The Button that appears in the BoneMenu" src="https://github.com/user-attachments/assets/45222f17-1215-4d66-9649-62db5749effc" />


The first two properties - `name` and `color` are purely you could say decorational. They are used when creating the page & button in the Menu (as shown in the picture above).

The `id` is a property that MUST be unique, you cannot register two configs with the same ID.

The `$schema` property allows for a code/text editor (like [Visual Studio Code](https://code.visualstudio.com/)) to know how the config file is supposed to work, providing autocompletion, showing descriptions of properties and other useful things.

The replacer can be disabled or enabled using the `enabled` property.

The way replacements are handled is pretty simple:

- The config file has categories, which can be enabled or disabled by the player via the BoneMenu.

- These categories hold `entries`, which contain the actual replacements.

- For each replacement, you have to define the original (this is a barcode, you can use RegEx here, see the `02 - RegEx` example for more information) and a replacement (`replaceWith` property, also a barcode).

In this case, the replacer has only ONE category - **AKM**. This category contains one entry, which replaces `c1534c5a-a6b5-4177-beb8-04d947756e41` (base game AKM. For list of barcodes of the base game spawnables, go to the [Barcode List](https://github.com/TarekLP/BONELAB-ItemReplacer/blob/master/Utilities/BarcodeList.md)) with the [Remox's Weapon Pack AKM](https://mod.io/g/bonelab/m/remoxs-weapon-pack).

This is about it. This might change in the future.
