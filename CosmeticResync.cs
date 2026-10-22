using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using CosmeticResync.Patches;
using HarmonyLib;
using System;
using System.Reflection;

namespace CosmeticResync
{
	[BepInPlugin(ID, Name, Version)]
	[BepInDependency("MoreCustomizations", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("com.github.Spider-XD-Gaming.Multi-Cosmetic", BepInDependency.DependencyFlags.SoftDependency)]
	public class CosmeticResync : BaseUnityPlugin
	{
		internal const string Name = "CosmeticResync-Fixed";
		internal const string Author = "1101trash";
		internal const string ID = Author + "." + Name;
		internal const string Version = "1.1.0";

		internal static CosmeticResync Instance;
		internal new static ManualLogSource Logger;

		public void Awake()
		{
			Instance = this;
			Logger = base.Logger;

			Harmony harmony = new Harmony(ID);
			Logger.LogInfo("Applying Harmony patches");
			harmony.PatchAll(Assembly.GetExecutingAssembly());

			// Add Resync support to MultiCosmetic Mod
			if (Chainloader.PluginInfos.TryGetValue("com.github.Spider-XD-Gaming.Multi-Cosmetic", out PluginInfo multiCosmetic))
			{
				MultiCosmeticPatch.MultiCosmetic = multiCosmetic.Instance;
				if (MultiCosmeticPatch.GetCurrentDataField())
				{
					// Find dataFromString method
					Type multiCosmeticType = multiCosmetic.Instance.GetType();
					MethodInfo targetMethod = AccessTools.Method(multiCosmeticType, "dataFromString");
					if (targetMethod != null)
					{
						harmony.Patch(targetMethod, new HarmonyMethod(AccessTools.Method(typeof(MultiCosmeticPatch), nameof(MultiCosmeticPatch.PrefixMCDataFromString))));
					}
					else
					{
						Logger.LogError("Failed to find Multi_Cosmetic.Plugin.dataFromString()");
					}

					// Find UpdateCustomPropertiesValue method
					targetMethod = AccessTools.Method(multiCosmeticType, "UpdateCustomPropertiesValue");
					if (targetMethod != null)
					{
						harmony.Patch(targetMethod, null, new HarmonyMethod(AccessTools.Method(typeof(MultiCosmeticPatch), nameof(MultiCosmeticPatch.PostfixUpdateCustomPropertiesValue))));
					}
					else
					{
						Logger.LogError("Failed to find Multi_Cosmetic.Plugin.UpdateCustomPropertiesValue()");
					}
				}
			}

			int patchedMethods = 0;
			foreach (MethodBase method in harmony.GetPatchedMethods())
			{
				Logger.LogInfo("Patched " + method.DeclaringType.FullName + "." + method.Name);
				patchedMethods++;
			}
			Logger.LogInfo(patchedMethods + " patches applied");
		}
	}
}
