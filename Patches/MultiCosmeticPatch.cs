using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CosmeticResync.Patches
{
	[HarmonyPatch]
	internal static class MultiCosmeticPatch
	{
		internal static BaseUnityPlugin MultiCosmetic;

		private static FieldInfo _currentData;

		internal static bool GetCurrentDataField()
		{
			// Make sure we have the other mod
			if (MultiCosmetic == null)
			{
				CosmeticResync.Logger.LogError("Missing MultiCosmetic instance");
				return false;
			}

			// Look for "currentData" field in the mod
			FieldInfo field = AccessTools.Field(MultiCosmetic.GetType(), "currentData");
			if (field == null)
			{
				CosmeticResync.Logger.LogError("MultiCosmetic missing currentData field");
				return false;
			}
			else if (field.FieldType != typeof(int[][]))
			{
				CosmeticResync.Logger.LogError("MultiCosmetic.currentData is no longer an int[][]");
				return false;
			}
			_currentData = field;
			return true;
		}

		// MultiCosmetic patches

		internal static bool PrefixMCDataFromString()
		{
			// Make sure we have the other mod
			if (MultiCosmetic == null)
			{
				CosmeticResync.Logger.LogError("Missing MultiCosmetic instance, cannot resync MultiCosmetic");
				return true;
			}

			Customization customization = Customization.Instance;
			if (!customization)
			{
				CosmeticResync.Logger.LogError("Customization not initialized, cannot resync MultiCosmetic");
				return true;
			}

			if (!PlayerPrefs.HasKey("CosmeticResync_MultiCosmetic"))
			{
				return true;
			}

			// Fill in the mod's data ourselves using cosmetic names
			string multiData = PlayerPrefs.GetString("CosmeticResync_MultiCosmetic");
			if (string.IsNullOrWhiteSpace(multiData))
			{
				CosmeticResync.Logger.LogWarning("CosmeticResync_MultiCosmetic has no data, nothing to resync");
				return true;
			}
			try
			{
				// Deserialize and validate
				string[][] cosmeticNames = JsonConvert.DeserializeObject<string[][]>(multiData);
				if (cosmeticNames.Length != 2)
				{
					CosmeticResync.Logger.LogWarning($"CosmeticResync_MultiCosmetic is malformed, expected 2 arrays, got {cosmeticNames.Length}");
					return true;
				}
				// Decode accessories and hats
				int[] accessories = GetCosmeticIndices(customization.accessories, cosmeticNames[0], "Accessory");
				int[] hats = GetCosmeticIndices(customization.hats, cosmeticNames[1], "Hat");
				CosmeticResync.Logger.LogInfo($"Loaded {accessories.Length} accessories and {hats.Length} hats");
				_currentData.SetValue(MultiCosmetic, new[] { accessories, hats });
				return false;
			}
			catch (JsonException)
			{
				CosmeticResync.Logger.LogWarning("Failed to deserialize CosmeticResync_MultiCosmetic");
				return true;
			}
		}

		internal static void PostfixUpdateCustomPropertiesValue()
		{
			// Make sure we have the other mod
			if (MultiCosmetic == null)
			{
				CosmeticResync.Logger.LogError("Missing MultiCosmetic instance? Cannot save MultiCosmetic names");
				return;
			}

			Customization customization = Customization.Instance;
			if (!customization)
			{
				CosmeticResync.Logger.LogError("Customization not initialized? Cannot save MultiCosmetic names");
				return;
			}

			int[][] cosmeticData = (int[][])_currentData.GetValue(MultiCosmetic);
			if (cosmeticData.Length != 2)
			{
				CosmeticResync.Logger.LogError($"Expected MultiCosmetic.Plugin.currentData to contain 2 int[], not {cosmeticData.Length}");
				return;
			}

			string[] accessories = GetCosmeticNames(customization.accessories, cosmeticData[0], "Accessory");
			string[] hats = GetCosmeticNames(customization.hats, cosmeticData[1], "Hat");
			string jsonData = JsonConvert.SerializeObject(new[] { accessories, hats });
			PlayerPrefs.SetString("CosmeticResync_MultiCosmetic", jsonData);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PassportManager), "Awake")]
		internal static void Awake(PassportManager __instance)
		{
			// MoreCustomization will have already patched Customization
			Customization customization = __instance.GetComponent<Customization>();
			if (customization)
			{
				// Fix up Customization.Instance
				Customization._instance = customization;
				CosmeticResync.Logger.LogInfo("Manually updating MultiCosmetic");
				PrefixMCDataFromString();
			}
			else
			{
				CosmeticResync.Logger.LogError("PassportManager not with Customization? Cannot resync MultiCosmetic");
			}
		}

		// Utility methods

		private static int[] GetCosmeticIndices(CustomizationOption[] options, string[] names, string cosmeticType)
		{
			// Build up a map of option names to indices
			Dictionary<string, int> optionMap = new Dictionary<string, int>(0);
			for (int i = 0; i < options.Length; i++)
			{
				CustomizationOption option = options[i];
				optionMap[option.name] = i;
			}
			// Locate the current index of each cosmetic
			List<int> indices = new List<int>();
			foreach (string optionName in names)
			{
				if (optionMap.TryGetValue(optionName, out int index))
				{
					indices.Add(index);
				}
				else
				{
					CosmeticResync.Logger.LogWarning($"{cosmeticType} {optionName} is no longer available");
				}
			}
			return indices.ToArray();
		}

		private static string[] GetCosmeticNames(CustomizationOption[] options, int[] indices, string cosmeticType)
		{
			List<string> names = new List<string>();
			int optionsLength = options.Length;
			foreach (int index in indices)
			{
				if (index >= 0 && index < optionsLength)
				{
					names.Add(options[index].name);
				}
				else
				{
					CosmeticResync.Logger.LogWarning($"{cosmeticType} #{index} is out of range");
				}
			}
			return names.ToArray();
		}
	}
}
