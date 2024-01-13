using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Missions.Nodes.Scenario;
using Missions;

using XNode;

namespace Perditio
{
    public enum TerrainDensity
    {
        Random,
        High,
        Medium,
        Low
    }

    public class LobbySettings
    {
        public struct Config
        {
            /*public static LobbySettings LoadLobbySettings()
            {
                try
                {
                    Debug.Log("test 1");
                    Game.SkirmishGameManager game_manager = GameObject.Find("_SKIRMISH GAME MANAGER_").GetComponent<Game.SkirmishGameManager>();

                    Debug.Log("test 2");
                    ScenarioGraph scenario_graph = Utils.GetPrivateValue<ScenarioGraph>(game_manager, "_clientScenario");

                    foreach (Node option_node in scenario_graph.nodes)
                    {
                        if (option_node is ScenarioOptionNode)
                        {
                            Debug.Log($"option_node: {((ScenarioOptionNode)option_node).OptionName}");
                        }
                    }

                    Debug.Log("test 3");
                    IntegerOptionNode option_node_density = Enumerable.FirstOrDefault<Node>(scenario_graph.nodes, (x => x is ScenarioOptionNode && ((ScenarioOptionNode)x).OptionName == ModEntryPoint.DENSITY_FIELD_NAME_TRANSFER)) as IntegerOptionNode;

                    if (option_node_density == null)
                    {
                        Debug.Log("option_node_density null");
                    }

                    Debug.Log("test 4");
                    int density_value_index = Utils.GetPrivateValue<int>(option_node_density, "_latchedOptionIndex");
                    Debug.Log($"density_value_index: {density_value_index}");

                    Debug.Log("test 5");
                    Debug.Log(String.Format("Perditio: scenario: {0}", scenario_graph.ScenarioName));
                    return new LobbySettings(scenario_graph.ScenarioName, (TerrainDensity)density_value_index);
                }
                catch (Exception e)
                {
                    Debug.Log(String.Format("Perditio: Finding SKIRMISH GAME MANAGER Failed With: {0}", e.ToString()));
                    return new LobbySettings("Control", TerrainDensity.Random);
                }
            }*/

            public Config(String _scenario, TerrainDensity _terrain_density)
            {
                scenario = _scenario;
                terrain_density = _terrain_density;
            }

            public String scenario;
            public TerrainDensity terrain_density;
        }

        public static Config lobby_config;
    }
}
