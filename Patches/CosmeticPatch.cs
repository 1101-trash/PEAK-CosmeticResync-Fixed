using HarmonyLib;
using UnityEngine;
using Zorro.Core;

namespace CosmeticResync.Patches
{
	[HarmonyPatch(typeof(CharacterCustomization))]
	internal static class CosmeticPatch
	{
		[HarmonyPrefix]
		[HarmonyPatch(nameof(CharacterCustomization.TryGetCosmeticsFromSteam))]
		private static bool PrefixTryGetCosmeticsFromSteam(ref CharacterCustomization __instance)
		{
			Customization customization = Customization.Instance;
			if (!customization)
			{
				CosmeticResync.Logger.LogError("Customization not initialized? Cannot Resync cosmetics");
				return true;
			}

			// Fetch cosmetics from player prefs string first, falling back to steam stat if missing
			int skinIndex = GetCosmeticFromPrefsOrSteam("CosmeticResync_Skin", STEAMSTATTYPE.Cosmetic_Skin, customization.skins);
			if (skinIndex > 0)
			{
				CharacterCustomization.SetCharacterSkinColor(skinIndex);
			}
			else
			{
				__instance.SetRandomSkinColor();
			}

			int eyesIndex = GetCosmeticFromPrefsOrSteam("CosmeticResync_Eyes", STEAMSTATTYPE.Cosmetic_Eyes, customization.eyes);
			if (eyesIndex > 0)
			{
				CharacterCustomization.SetCharacterEyes(eyesIndex);
			}
			else
			{
				__instance.SetRandomEyes();
			}

			int mouthIndex = GetCosmeticFromPrefsOrSteam("CosmeticResync_Mouth", STEAMSTATTYPE.Cosmetic_Mouth, customization.mouths);
			if (mouthIndex > 0)
			{
				CharacterCustomization.SetCharacterMouth(mouthIndex);
			}
			else
			{
				__instance.SetRandomMouth();
			}

			int accessoryIndex = GetCosmeticFromPrefsOrSteam("CosmeticResync_Accessory", STEAMSTATTYPE.Cosmetic_Accessory, customization.accessories);
			if (accessoryIndex > 0)
			{
				CharacterCustomization.SetCharacterAccessory(accessoryIndex);
			}
			else
			{
				__instance.SetRandomAccessory();
			}

			int outfitIndex = GetCosmeticFromPrefsOrSteam("CosmeticResync_Outfit", STEAMSTATTYPE.Cosmetic_Outfit, customization.fits);
			if (outfitIndex > 0)
			{
				CharacterCustomization.SetCharacterOutfit(outfitIndex);
			}
			else
			{
				__instance.SetRandomOutfit();
			}

			int hatIndex = GetCosmeticFromPrefsOrSteam("CosmeticResync_Hat", STEAMSTATTYPE.Cosmetic_Hat, customization.hats);
			if (hatIndex > 0)
			{
				CharacterCustomization.SetCharacterHat(hatIndex);
			}
			else
			{
				__instance.SetRandomHat();
			}

			int sashIndex = GetCosmeticFromPrefsOrSteam("CosmeticResync_Sash", STEAMSTATTYPE.Cosmetic_Sash, customization.sashes);
			if (sashIndex > 0)
			{
				CharacterCustomization.SetCharacterSash(sashIndex);
			}
			else
			{
				// SetRandomSash does not exist and original code sets 0
				CharacterCustomization.SetCharacterSash(0);
			}

			return false;
		}

		// Hook CharacterCustomization Set functions to save name to PlayerPrefs

		[HarmonyPostfix]
		[HarmonyPatch(nameof(CharacterCustomization.SetCharacterSkinColor))]
		private static void PostfixSetCharacterSkinColor(int index)
		{
			Customization customization = Customization.Instance;
			if (!customization)
			{
				CosmeticResync.Logger.LogError("Customization not initialized? Cannot save skin color");
				return;
			}
			ValidateAndSaveCosmetic(index, "CosmeticResync_Skin", customization.skins);
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(CharacterCustomization.SetCharacterEyes))]
		private static void PostfixSetCharacterEyes(int index)
		{
			Customization customization = Customization.Instance;
			if (!customization)
			{
				CosmeticResync.Logger.LogError("Customization not initialized? Cannot save eyes");
				return;
			}
			ValidateAndSaveCosmetic(index, "CosmeticResync_Eyes", customization.eyes);
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(CharacterCustomization.SetCharacterMouth))]
		private static void PostfixSetCharacterMouth(int index)
		{
			Customization customization = Customization.Instance;
			if (!customization)
			{
				CosmeticResync.Logger.LogError("Customization not initialized? Cannot save mouth");
				return;
			}
			ValidateAndSaveCosmetic(index, "CosmeticResync_Mouth", customization.mouths);
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(CharacterCustomization.SetCharacterAccessory))]
		private static void PostfixSetCharacterAccessory(int index)
		{
			Customization customization = Customization.Instance;
			if (!customization)
			{
				CosmeticResync.Logger.LogError("Customization not initialized? Cannot save accessory name");
				return;
			}
			ValidateAndSaveCosmetic(index, "CosmeticResync_Accessory", customization.accessories);
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(CharacterCustomization.SetCharacterOutfit))]
		private static void PostfixSetCharacterOutfit(int index)
		{
			Customization customization = Customization.Instance;
			if (!customization)
			{
				CosmeticResync.Logger.LogError("Customization not initialized? Cannot save outfit name");
				return;
			}
			ValidateAndSaveCosmetic(index, "CosmeticResync_Outfit", customization.fits);
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(CharacterCustomization.SetCharacterHat))]
		private static void PostfixSetCharacterHat(int index)
		{
			Customization customization = Customization.Instance;
			if (!customization)
			{
				CosmeticResync.Logger.LogError("Customization not initialized? Cannot save hat name");
				return;
			}
			ValidateAndSaveCosmetic(index, "CosmeticResync_Hat", customization.hats);
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(CharacterCustomization.SetCharacterSash))]
		private static void PostfixSetCharacterSash(int index)
		{
			Customization customization = Customization.Instance;
			if (!customization)
			{
				CosmeticResync.Logger.LogError("Customization not initialized? Cannot save sash name");
				return;
			}
			ValidateAndSaveCosmetic(index, "CosmeticResync_Sash", customization.sashes);
		}

		// Utility methods

		private static int GetCosmeticFromPrefsOrSteam(string prefKey, STEAMSTATTYPE steamStatType, CustomizationOption[] options)
		{
			int index = -1;
			// Check if we have the player pref
			if (PlayerPrefs.HasKey(prefKey))
			{
				string optionName = PlayerPrefs.GetString(prefKey);
				if (!string.IsNullOrWhiteSpace(optionName))
				{
					// Locate our desired cosmetic from the options
					for (int i = 0; i < options.Length; i++)
					{
						CustomizationOption option = options[i];
						if (option.name == optionName)
						{
							index = i;
							break;
						}
					}
					if (index != -1)
					{
						// Actual debug log so just log to Unity
						Debug.Log($"[CosmeticResync] Detected {steamStatType} {optionName} as #{index}");
					}
					else
					{
						CosmeticResync.Logger.LogWarning($"{steamStatType} {optionName} is no longer available, picking a random option");
					}
				}
			}
			else
			{
				// Fallback to steam
				if (Singleton<AchievementManager>.Instance.GetSteamStatInt(steamStatType, out index))
				{
					// Make sure the value isn't out of range
					if (index >= options.Length)
					{
						// Cosmetic must have been removed, default to random
						index = -1;
					}
				}
				else
				{
					// Steam stat missing, default to random
					index = -1;
				}
			}
			return index;
		}

		private static void ValidateAndSaveCosmetic(int index, string prefKey, CustomizationOption[] options)
		{
			if (index < 0 || index >= options.Length)
			{
				CosmeticResync.Logger.LogError($"Customization index #{index} invalid? Cannot save customization for {prefKey}");
			}
			else
			{
				PlayerPrefs.SetString(prefKey, options[index].name);
			}
		}
	}
}
