using HarmonyLib;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

using Bundles;
using Missions;
using Missions.Nodes;
using Missions.Nodes.Scenario;

using Modding;
using Utility;

using XNode;

namespace Perditio
{
    public class ModEntryPoint : IModEntryPoint
    {
        public const string DENSITY_FIELD_NAME = "Perditio Terrain Density";
        public static readonly string[] DENSITY_FIELD_OPTIONS = new string[4] { "Random", "High", "Medium", "Low" };

        public void PreLoad()
        {
            Debug.Log("Perditio PreLoad");
        }

        public void PostLoad()
        {
            Debug.Log("Perditio PostLoad");

            Harmony harmony = new Harmony("nebulous.perditio");
            harmony.PatchAll();
        }

        public static List<string> PatchedScenarios = new List<string>();

        public static void PatchAllScenarios()
        {
            Debug.Log("Perditio PatchAllScenarios");

            List<ScenarioGraph> _scenarios = Utils.GetPrivateValue<List<ScenarioGraph>>(BundleManager.Instance, "_scenarios");
            List<ScenarioGraph> patchedList = new List<ScenarioGraph>();

            foreach (ScenarioGraph scenario in _scenarios)
            {
                if (PatchedScenarios.Contains(scenario.ScenarioKey))
                {
                    patchedList.Add(scenario);
                    continue;
                }

                PatchedScenarios.Add(scenario.ScenarioKey);
                ScenarioGraph patchedScenario = CreatePatchedScenario(scenario);
                patchedList.Add(patchedScenario);
                Debug.Log($"Patched {scenario.ScenarioName}");
            }
            Utils.SetPrivateValue(BundleManager.Instance, "_scenarios", patchedList);
        }

        private static ScenarioGraph CreatePatchedScenario(ScenarioGraph scenario)
        {
            ScenarioGraph newScenario = UnityEngine.Object.Instantiate(scenario);
            PatchScenario(newScenario);
            return newScenario;
        }

        private static void PatchScenario(ScenarioGraph scenario)
        {
            ScenarioSetupNode setupNode = Enumerable.FirstOrDefault<XNode.Node>(scenario.nodes, (x => x is ScenarioSetupNode)) as ScenarioSetupNode;
            IntegerOptionNode some_node1 = scenario.InsertNode<IntegerOptionNode>(0);
            Guid guid1 = new Guid("79b174ec-fcb2-47a1-8c7e-a7542d32ecc5");
            ShortGuid shortGuid1 = new ShortGuid(guid1);
            Utils.SetPrivateValue(some_node1, "_name", "some magic option 1");
            Utils.SetPrivateValue(some_node1, "_options", Enumerable.Range(1, 50).ToArray<int>());
            Utils.SetPrivateValue(some_node1, "_initialOptionIndex", 0);
            Utils.SetPrivateValue(some_node1, "_key", shortGuid1.ToString(), typeof(KeyedNode));

            IntegerOptionNode some_node2 = scenario.InsertNode<IntegerOptionNode>(0);
            Guid guid2 = new Guid("c0eb24a7-d6e2-49fd-8836-507270de50b8");
            ShortGuid shortGuid2 = new ShortGuid(guid2);
            Utils.SetPrivateValue(some_node2, "_name", "some magic option 2");
            Utils.SetPrivateValue(some_node2, "_options", Enumerable.Range(1, 50).ToArray<int>());
            Utils.SetPrivateValue(some_node2, "_initialOptionIndex", 0);
            Utils.SetPrivateValue(some_node2, "_key", shortGuid2.ToString(), typeof(KeyedNode));

            IntegerOptionNode some_node3 = scenario.InsertNode<IntegerOptionNode>(0);
            Guid guid3 = new Guid("1b12b171-ee67-455c-8817-4dff28da81b7");
            ShortGuid shortGuid3 = new ShortGuid(guid3);
            Utils.SetPrivateValue(some_node3, "_name", "some magic option 3");
            Utils.SetPrivateValue(some_node3, "_options", Enumerable.Range(1, 50).ToArray<int>());
            Utils.SetPrivateValue(some_node3, "_initialOptionIndex", 0);
            Utils.SetPrivateValue(some_node3, "_key", shortGuid3.ToString(), typeof(KeyedNode));


            foreach (Node node in scenario.nodes)
            {
                node.graph = scenario;
            }
            Debug.Log("priv 6");
        }
    }
}
