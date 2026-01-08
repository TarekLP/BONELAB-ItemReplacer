# 02 - Regex

This example showcases how powerful the option to use RegEx actually is.

It has been added to be able to avoid having to make multiple entries for the same spawnable but different variants.

Though it allows for more things.

## Example

```json
{
  "$schema": "https://raw.githubusercontent.com/TarekLP/BONELAB-ItemReplacer/refs/heads/master/Utilities/schema.json",
  "name": "Miku",
  "color": "#00FFFF",
  "id": "miku-all",
  "enabled": true,
  "categories": [
    {
      "name": "Miku",
      "enabled": true,
      "entries": [
        {
          "original": "(.*?)",
          "replaceWith": "BaBaCorp.BaBasToybox.Spawnable.MikuPlush",
          "isRegEx": true
        }
      ]
    }
  ]
}
```

To use regex, in the entry you have to set the `isRegEx` property to `true`.

Now what `(.*?)` does is match EVERYTHING. That means it's gonna replace EVERYTHING with a [Miku Plush](https://mod.io/g/bonelab/m/babacorps-toybox). I doubt any genuine serious replacer would need to do that, but you can.

Does it actually have any real use case thats not replacing everything? I have no clue, but you can try to do something with it.
