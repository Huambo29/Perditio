using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using HarmonyLib;

using Game;
using Game.Map;
using Bundles;
using Modding;
using Networking;
using Missions;
using Missions.Nodes;
using Missions.Nodes.Scenario;
using Utility;

using XNode;

namespace Perditio
{
    public class Patches
    {
        static bool is_interface_dirty = false;

        [HarmonyPatch(typeof(SkirmishGameSettings), "ResolveSelectedMap")]
        public class PatchResolveSelectedMap
        {
            static void Postfix(ref SkirmishGameSettings __instance)
            {
                Debug.Log("Perditio ResolveSelectedMap Postfix");
                if (__instance.SelectedMap)
                {
                    if (__instance.SelectedMap.MapName.Contains("Perditio") && !is_interface_dirty)
                    {
                        Debug.Log("Perditio interface dirty");
                        is_interface_dirty = true;

                        __instance.AddStringListOption(ModEntryPoint.DENSITY_FIELD_NAME, ModEntryPoint.DENSITY_FIELD_OPTIONS, 0);
                        __instance.AddStringListOption(ModEntryPoint.TORNESS_FIELD_NAME, ModEntryPoint.TORNESS_FIELD_OPTIONS, 2);
                    }
                    else if (!__instance.SelectedMap.MapName.Contains("Perditio") && is_interface_dirty)
                    {
                        Debug.Log("Perditio interface clean");
                        is_interface_dirty = false;

                        foreach (SyncedOption synced_option in Utils.GetPrivateValue<SyncListGameSettings>(__instance, "_syncedSettings").Where<SyncedOption>((Func<SyncedOption, bool>)(x => !x.Universal)).ToList<SyncedOption>())
                        {
                            Debug.Log($"syncedOption: {synced_option.Name}");
                            if (synced_option.Name == ModEntryPoint.DENSITY_FIELD_NAME || synced_option.Name == ModEntryPoint.TORNESS_FIELD_NAME)
                            {
                                Utils.GetPrivateValue<SyncListGameSettings>(__instance, "_syncedSettings").Remove(synced_option);
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("Perditio interface already set");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SkirmishGameSettings), "GetLaunchOptions")]
        public class PatchSkirmishGameSettingsGetLaunchOptions
        {
            static void Postfix(ref SkirmishGameSettings __instance, ref SkirmishScenarioLaunchOptions __result)
            {
                Debug.Log("Perditio GetLaunchOptions");

                TerrainDensity density = TerrainDensity.Random;
                TerrainFraying fraying = TerrainFraying.Default;

                foreach (SyncedOption synced_option in Utils.GetPrivateValue<SyncListGameSettings>(__instance, "_syncedSettings").Where<SyncedOption>((Func<SyncedOption, bool>)(x => !x.Universal)).ToList<SyncedOption>())
                {
                    Debug.Log($"syncedOption: {synced_option.Name} | {synced_option.Value}");
                    if (synced_option.Name == ModEntryPoint.DENSITY_FIELD_NAME)
                    {
                        density = (TerrainDensity)synced_option.Value;
                    }

                    if (synced_option.Name == ModEntryPoint.TORNESS_FIELD_NAME)
                    {
                        fraying = (TerrainFraying)synced_option.Value;
                    }
                }

                LobbySettings.instance = new LobbySettings(
                    __result.Scenario.ScenarioName,
                    density,
                    fraying
                );
            }
        }
    }
}
