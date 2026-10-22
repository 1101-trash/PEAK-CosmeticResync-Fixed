# CosmeticResync FIXED for PEAK

Fixes the issue of MultiCosmetic selections, such as multiple hats and accessories, not being restored when starting a game\. Now all your cosmetics will be properly saved and displayed\. Based on BlueAmulet's [CosmeticResync](https://old.thunderstore.io/c/peak/p/BlueAmulet/CosmeticResync/), which preserves your cosmetic choices preventing getting your cosmetics changed when cosmetic mods are added or removed\.

This fix works over the [MultiCosmetic Mod](https://old.thunderstore.io/c/peak/p/Grimm_Industries/MultiCosmetic_Mod/) functionality overall \(see note\), however it still sync your outfits, eyes, face and other skin customization choices \(without MultiCosmetic\) of your character just as the original mod\.

### [Download CosmeticResync Fixed from Thunderstore](https://thunderstore.io/c/peak/p/1101trash/CosmeticResync_Fixed/)

---

## What this fork fixes

Compared with the original mod, this fork waits for custom cosmetic options to finish loading before restoring saved MultiCosmetic selections\. This helps prevent hats and accessories from being skipped when their mods register them late\. It also validates MultiCosmetic data and avoids overwriting previously saved selections with incomplete data\.

Meaning that if you have many cosmetic mods installed they will be all saved and loaded correctly on your character no matter how many of them are added or active\.

Only  MultiCosmeticPatch was edited using the same logic, adding features you can see in the [changelog](./thunderstore/CHANGELOG.md)\. The mod without MultiCosmetic Mod should work the same *\(not tested\)*\.

> **Note\:** This fix is specifically for use with MultiCosmetic\. If you don't use MultiCosmetic, you can use the original CosmeticResync instead\.
> 
> MultiCosmetic lets you combine multiple head cosmetics, such as hair, hats, ears and others accessories that you can find in cosmetic mods\.

This is my first attempt of a mod in C\# and I'm still learning the language\. If you encounter any bug or have a suggestion, feel free to create an [issue](https://github.com/1101-trash/PEAK-CosmeticResync-Fixed/issues), as well as if you think you could upgrade the code fork it and pull a request\. I appreciate any feedback that can help improve this project\.

---

## IMPORTANT\! \- Make sure to remove original CosmeticResync mod if you already had it\!

---

### Installation \(mod manager\)

Simply click Install with App/Install with Mod Manager to automatically download and install the mod with the Thunderstore App or a compatible mod manager \(I recommend [Gale](https://github.com/Kesomannen/gale)\)

### Installation \(manual\)

If you are installing this manually, do the following\:

1. Install BepInEx if you don't have it from [here](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/)
2. Extract the archive into a folder\. **Do not extract into the game folder\.**
3. Move `CosmeticResync_Fixed.dll` from the mod's`plugins` folder into `<Game Directory>\BepInEx\plugins`\.

### License

This fix is provided in good faith for the community; all credit goes to the original author\.
---

<details>
<summary>Support the original mod</summary>

> [![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/BlueAmulet)<br>Saves cosmetic names instead of just their indices, preventing random cosmetics from loading\.<br>Download via [Thunderstore](https://thunderstore.io/c/peak/p/BlueAmulet/CosmeticResync/)<br>Libraries included in this project are stripped of code and used as reference only\.

## Features

> - Equipped cosmetics are saved by name instead of index.
> - Cosmetic indices are resynchronized by name when loading.
> - Prevents random cosmetics from appearing when a cosmetic mod is added, updated, or removed.
> - Compatible with More Customizations and MultiCosmetics.
</details>
