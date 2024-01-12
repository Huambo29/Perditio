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
        [HarmonyPatch(typeof(BundleManager), "ProcessAssetBundle")]
        public class PatchBundleManagerProcessAssetBundle
        {
            static void Postfix()
            {
                ModEntryPoint.PatchAllScenarios();
            }
        }

        [HarmonyPatch(typeof(ModRecord), "LoadMod")]
        public class PatchModRecordLoadMod
        {
            static void Postfix()
            {
                ModEntryPoint.PatchAllScenarios();
            }
        }

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
                    }
                    else if (!__instance.SelectedMap.MapName.Contains("Perditio") && is_interface_dirty)
                    {
                        Debug.Log("Perditio interface clean");
                        is_interface_dirty = false;

                        foreach (SyncedOption synced_option in Utils.GetPrivateValue<SyncListGameSettings>(__instance, "_syncedSettings").Where<SyncedOption>((Func<SyncedOption, bool>)(x => !x.Universal)).ToList<SyncedOption>())
                        {
                            Debug.Log($"syncedOption: {synced_option.Name}");
                            if (synced_option.Name == ModEntryPoint.DENSITY_FIELD_NAME)
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

                foreach (SyncedOption synced_option in Utils.GetPrivateValue<SyncListGameSettings>(__instance, "_syncedSettings").Where<SyncedOption>((Func<SyncedOption, bool>)(x => !x.Universal)).ToList<SyncedOption>())
                {
                    if (synced_option.Name == ModEntryPoint.DENSITY_FIELD_NAME)
                    {
                        Debug.Log("Perditio Duplicate");
                        return;
                    }
                }

                List<ScenarioOptionNode> option_nodes = Utils.GetPrivateValue<List<ScenarioOptionNode>>(__result.Scenario, "_optionNodes");

                IntegerOptionNode some_node = __result.Scenario.InsertNode<IntegerOptionNode>(0);
                Guid guid = new Guid();
                ShortGuid shortGuid = new ShortGuid(guid);
                Utils.SetPrivateValue(some_node, "_name", "perditio_transfer_density");
                Utils.SetPrivateValue(some_node, "_options", Enumerable.Range(1, 4).ToArray<int>());
                Utils.SetPrivateValue(some_node, "_initialOptionIndex", 0);
                Utils.SetPrivateValue(some_node, "_key", shortGuid.ToString(), typeof(KeyedNode));

                foreach (SyncedOption synced_option in Utils.GetPrivateValue<SyncListGameSettings>(__instance, "_syncedSettings").Where<SyncedOption>((Func<SyncedOption, bool>)(x => !x.Universal)).ToList<SyncedOption>())
                {
                    Debug.Log($"syncedOption: {synced_option.Name} | {synced_option.Value}");
                    if (synced_option.Name == ModEntryPoint.DENSITY_FIELD_NAME)
                    {
                        some_node.SelectedValue = synced_option.Value;
                        option_nodes.Add(some_node);
                    }
                } 

                

                foreach (ScenarioOptionNode option_node in option_nodes)
                {
                    Debug.Log($"option_node: {option_node.OptionName}");
                }
            }
        }

        [HarmonyPatch(typeof(SkirmishGameManager), "StateTransferringFleets")]
        public class PatchSkirmishGameManagerStateTransferringFleets
        {
            static void Prefix(ref SkirmishGameManager __instance)
            {
                Debug.Log("Perditio StateTransferringFleets");
                Battlespace battlespace = __instance.LoadedMap;
                ProceduralTerrain procedural_terrain = battlespace.GetComponentInChildren<ProceduralTerrain>();

                ScenarioGraph scenario_graph = Utils.GetPrivateValue<ScenarioGraph>(__instance, "_clientScenario");
                List<ScenarioOptionNode> option_nodes = Utils.GetPrivateValue<List<ScenarioOptionNode>>(scenario_graph, "_optionNodes");

                foreach (ScenarioOptionNode option_node in option_nodes)
                {
                    Debug.Log($"option_node: {option_node.OptionName}}");
                }

                IntegerOptionNode option_node_density = Enumerable.FirstOrDefault<Node>(scenario_graph.nodes, (x => x is IntegerOptionNode && ((IntegerOptionNode)x).OptionName == ModEntryPoint.DENSITY_FIELD_NAME)) as IntegerOptionNode;

                if (option_node_density == null)
                {
                    Debug.Log($"Perditio option_node_density null");
                    return;
                }

                int option_index_value = Utils.GetPrivateValue<int>(option_node_density, "_latchedOptionIndex");
                Debug.Log($"Perditio option_index_value {option_index_value}");
            }
        }
    }
}
