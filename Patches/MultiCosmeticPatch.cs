using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using System.Collections;
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
		private static bool _restoreQueued;
		private static bool _restorePending;
		private static bool _pendingSaveLogged;

		private enum RestoreResult
		{
			Restored,
			Retry,
			NoSavedData,
			InvalidData
		}

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
			_restorePending = PlayerPrefs.HasKey("CosmeticResync_MultiCosmetic");
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

			if (_currentData == null)
			{
				CosmeticResync.Logger.LogError("MultiCosmetic.currentData field not found, cannot Resync Multicosmetic");
				return true;
			}

			RestoreResult result = TryRestoreSavedCosmetics(out _, out _);
			return result != RestoreResult.Restored;
		}

		internal static void PostfixUpdateCustomPropertiesValue()
		{
			// Make sure we have the other mod
			if (MultiCosmetic == null)
			{
				CosmeticResync.Logger.LogError("Missing MultiCosmetic instance? Cannot save MultiCosmetic names");
				return;
			}
			
			if (_currentData == null)
			{
				CosmeticResync.Logger.LogError("MultiCosmetic.currentData field not found, cannot save MultiCosmetic names");
				return;
			}

			if (_restorePending)
			{
				if (!_pendingSaveLogged)
				{
					CosmeticResync.Logger.LogInfo("Skipping MultiCosmetic save while saved cosmetic names are awaiting restoration");
					_pendingSaveLogged = true;
				}
				return;
			}

			Customization customization = Customization.Instance;
			if (!customization)
			{
				CosmeticResync.Logger.LogError("Customization not initialized? Cannot save MultiCosmetic names");
				return;
			}

			int[][] cosmeticData = (int[][])_currentData.GetValue(MultiCosmetic);
			if (cosmeticData == null)
			{
				CosmeticResync.Logger.LogError($"MultiCosmetic.currentData is null");
				return;
			}
			if (cosmeticData.Length != 2)
			{
				CosmeticResync.Logger.LogError($"Expected MultiCosmetic.Plugin.currentData to contain 2 int[], not {cosmeticData.Length}");
				return;
			}
			if (cosmeticData[0] == null || cosmeticData[1] == null)
			{
				CosmeticResync.Logger.LogError("MultiCosmetic.currentData contains a null array");
				return;
			}

			if (!TryGetCosmeticNames(customization.accessories, cosmeticData[0], "Accessory", out string[] accessories) ||
				!TryGetCosmeticNames(customization.hats, cosmeticData[1], "Hat", out string[] hats))
			{
				return;
			}

			string jsonData = JsonConvert.SerializeObject(new[] { accessories, hats });
			PlayerPrefs.SetString("CosmeticResync_MultiCosmetic", jsonData);
			PlayerPrefs.Save();
			_pendingSaveLogged = false;
			CosmeticResync.Logger.LogInfo($"Saved {accessories.Length} accessories and {hats.Length} hats");
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
				if (!_restoreQueued)
				{
					_restoreQueued = true;
					CosmeticResync.Logger.LogInfo("Queued MultiCosmetic restore; waiting for cosmetic options to become available");
					CosmeticResync.Instance.StartCoroutine(RestoreWhenReady());
				}
			}
			else
			{
				CosmeticResync.Logger.LogError("PassportManager not with Customization? Cannot resync MultiCosmetic");
			}
		}

		// Utility methods

		private static IEnumerator RestoreWhenReady()
		{
			string[] missingAccessories = new string[0];
			string[] missingHats = new string[0];
			for (int attempt = 0; attempt < 30; attempt++)
			{
				RestoreResult result = TryRestoreSavedCosmetics(out missingAccessories, out missingHats);
				if (result == RestoreResult.NoSavedData)
				{
					CosmeticResync.Logger.LogInfo("No saved MultiCosmetic names found; keeping MultiCosmetic's current data");
					_restoreQueued = false;
					yield break;
				}
				if (result != RestoreResult.Retry)
				{
					_restoreQueued = false;
					_pendingSaveLogged = false;
					yield break;
				}

				yield return new WaitForSecondsRealtime(1f);
			}

			foreach (string name in missingAccessories)
			{
				CosmeticResync.Logger.LogWarning($"Accessory {name} is still unavailable after waiting for cosmetic registration");
			}
			foreach (string name in missingHats)
			{
				CosmeticResync.Logger.LogWarning($"Hat {name} is still unavailable after waiting for cosmetic registration");
			}
			if (missingAccessories.Length == 0 && missingHats.Length == 0)
			{
				CosmeticResync.Logger.LogError("Customization was not ready in time; could not resync MultiCosmetic");
			}
			else
			{
				CosmeticResync.Logger.LogError("Could not resync every saved cosmetic after 30 seconds; preserving saved names and skipping MultiCosmetic saves");
			}
			_restoreQueued = false;
		}

		private static RestoreResult TryRestoreSavedCosmetics(out string[] missingAccessories, out string[] missingHats)
		{
			missingAccessories = new string[0];
			missingHats = new string[0];

			if (MultiCosmetic == null || _currentData == null)
			{
				CosmeticResync.Logger.LogError("MultiCosmetic is not ready; cannot restore saved cosmetics");
				return RestoreResult.InvalidData;
			}

			if (!PlayerPrefs.HasKey("CosmeticResync_MultiCosmetic"))
			{
				_restorePending = false;
				return RestoreResult.NoSavedData;
			}

			string multiData = PlayerPrefs.GetString("CosmeticResync_MultiCosmetic");
			if (string.IsNullOrWhiteSpace(multiData))
			{
				CosmeticResync.Logger.LogWarning("CosmeticResync_MultiCosmetic has no data, nothing to resync");
				_restorePending = false;
				return RestoreResult.InvalidData;
			}

			try
			{
				string[][] cosmeticNames = JsonConvert.DeserializeObject<string[][]>(multiData);
				if (cosmeticNames == null || cosmeticNames.Length != 2 ||
					cosmeticNames[0] == null || cosmeticNames[1] == null)
				{
					CosmeticResync.Logger.LogWarning("CosmeticResync_MultiCosmetic is malformed; expected two name arrays");
					_restorePending = false;
					return RestoreResult.InvalidData;
				}

				Customization customization = Customization.Instance;
				if (!customization)
				{
					return RestoreResult.Retry;
				}

				bool accessoriesResolved = TryGetCosmeticIndices(customization.accessories, cosmeticNames[0], out int[] accessories, out missingAccessories);
				bool hatsResolved = TryGetCosmeticIndices(customization.hats, cosmeticNames[1], out int[] hats, out missingHats);
				if (!accessoriesResolved || !hatsResolved)
				{
					return RestoreResult.Retry;
				}

				_currentData.SetValue(MultiCosmetic, new[] { accessories, hats });
				_restorePending = false;
				CosmeticResync.Logger.LogInfo($"Loaded {accessories.Length} accessories and {hats.Length} hats");
				return RestoreResult.Restored;
			}
			catch (JsonException)
			{
				CosmeticResync.Logger.LogWarning("Failed to deserialize CosmeticResync_MultiCosmetic");
				_restorePending = false;
				return RestoreResult.InvalidData;
			}
		}

		private static bool TryGetCosmeticIndices(CustomizationOption[] options, string[] names, out int[] indices, out string[] missingNames)
		{
			Dictionary<string, int> optionMap = new Dictionary<string, int>();
			if (options != null)
			{
				for (int i = 0; i < options.Length; i++)
				{
					CustomizationOption option = options[i];
					if (option != null && !string.IsNullOrEmpty(option.name))
					{
						optionMap[option.name] = i;
					}
				}
			}

			List<int> resolvedIndices = new List<int>();
			List<string> missing = new List<string>();
			foreach (string optionName in names)
			{
				if (!string.IsNullOrEmpty(optionName) && optionMap.TryGetValue(optionName, out int index))
				{
					resolvedIndices.Add(index);
				}
				else
				{
					missing.Add(optionName ?? "<null>");
				}
			}

			indices = resolvedIndices.ToArray();
			missingNames = missing.ToArray();
			return missingNames.Length == 0;
		}

		private static bool TryGetCosmeticNames(CustomizationOption[] options, int[] indices, string cosmeticType, out string[] names)
		{
			List<string> resolvedNames = new List<string>();
			names = new string[0];

			if (options == null || indices == null)
			{
				CosmeticResync.Logger.LogError($"{cosmeticType} options or indices are null; cannot save MultiCosmetic names");
				return false;
			}

			int optionsLength = options.Length;
			foreach (int index in indices)
			{
				if (index >= 0 && index < optionsLength && options[index] != null && !string.IsNullOrEmpty(options[index].name))
				{
					resolvedNames.Add(options[index].name);
				}
				else
				{
					CosmeticResync.Logger.LogError($"{cosmeticType} index #{index} cannot be resolved; preserving previously saved MultiCosmetic names");
					return false;
				}
			}

			names = resolvedNames.ToArray();
			return true;
		}
	}
}
