using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using HarmonyLib;
using TMPro;

using Utility;
using Game;
using Modding;
using Game.Map;
using Networking;
using Missions.Nodes.Scenario;

namespace Perditio
{
    public class Patches
    {
        static bool is_interface_dirty = false;
        static bool other_team_size_mod = false;
        static string last_scenario = "";

        static SkirmishLobbyManager lobby_manager;

        static void HelpPerditio(IPlayer fromPlayer, string chatArgs)
        {
            if (lobby_manager == null)
            {
                Debug.Log("Perditio lobby_manager is null");
                return;
            }
            ChatService chat_service = Utils.GetPrivateValue<ChatService>(lobby_manager, "_chatService");

            if (!is_interface_dirty)
            {
                Debug.Log("Perditio map is not current one");
                chat_service.SendSystemMessageToIndividual(fromPlayer, "Perditio map is not currently selected");
                return;
            }

            chat_service.SendSystemMessageToIndividual(fromPlayer, "Commands are:");
            chat_service.SendSystemMessageToIndividual(fromPlayer, "!voteperditio <Option Name (string)> <Option (integer)>.");
            chat_service.SendSystemMessageToIndividual(fromPlayer, "!changeperditio <Option Name (string)> <Option (integer)>.");
            chat_service.SendSystemMessageToIndividual(fromPlayer, "You don't have to use full option names");

            chat_service.SendSystemMessageToIndividual(fromPlayer, "Available option values are:");
            chat_service.SendSystemMessageToIndividual(fromPlayer, $"Density: 0-{ModEntryPoint.DENSITY_FIELD_OPTIONS.Length - 1} ({String.Join(", ", ModEntryPoint.DENSITY_FIELD_OPTIONS)})");
            chat_service.SendSystemMessageToIndividual(fromPlayer, $"Roughness: 0-{ModEntryPoint.ROUGHNESS_FIELD_OPTIONS.Length - 1} ({String.Join(", ", ModEntryPoint.ROUGHNESS_FIELD_OPTIONS)})");
            chat_service.SendSystemMessageToIndividual(fromPlayer, $"Radius: {ModEntryPoint.MIN_MAP_RADIUS}-{ModEntryPoint.MAX_MAP_RADIUS - 1}km");
            chat_service.SendSystemMessageToIndividual(fromPlayer, $"Teams Size: {ModEntryPoint.MIN_TEAM_SIZE}-{ModEntryPoint.MAX_TEAM_SIZE - 1}");
            chat_service.SendSystemMessageToIndividual(fromPlayer, $"Seeds: 0-{ModEntryPoint.MAX_SEED - 1}");
            chat_service.SendSystemMessageToIndividual(fromPlayer, $"Cap points: {ModEntryPoint.MIN_CAPS_POINTS}-{ModEntryPoint.MAX_CAPS_POINTS - 1} (Maximum number of capture points)");
        }

        static void SetSyncedOption(string name, int value, SkirmishGameSettings game_settings)
        {
            Debug.Log($"Perditio changing option name {name} value {value}");

            Utils.GetPrivateMethod(game_settings, "ChangeOptionValue").Invoke(game_settings, new object[] { name, value });
            // Utils.GetPrivateMethod(game_settings, "__DoRpcChangeOptionValue").Invoke(game_settings, new object[] { name, value });
            // Utils.GetPrivateMethod(game_settings, "RpcChangeOptionValue").Invoke(game_settings, new object[] { name, value });
        }

        static bool ParseChatCommandPerditioSettings(
            IPlayer fromPlayer,
            string chatArgs,
            out string option_name,
            out int option_value,
            ChatService chat_service,
            SkirmishGameSettings game_settings
        ) {
            option_name = null;
            option_value = -1;

            int length = chatArgs.LastIndexOf(' ');
            if (length == -1)
            {
                chat_service.SendSystemMessageToIndividual(fromPlayer, "Invalid command format. Use !helpperditio for more information.");
                return false;
            }

            string input_name = chatArgs.Substring(0, length);
            string input_value = chatArgs.Substring(length + 1);

            int input_value_parsed;
            if (!int.TryParse(input_value, out input_value_parsed))
            {
                chat_service.SendSystemMessageToIndividual(fromPlayer, "Invalid command format. Use !helpperditio for more information.");
                return false;
            }

            List<SyncedOption> synced_options = Utils.GetPrivateValue<SyncListGameSettings>(game_settings, "_syncedSettings").Where<SyncedOption>((Func<SyncedOption, bool>)(x => !x.Universal)).ToList<SyncedOption>();

            SyncedOption synced_option = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name.Contains("Perditio") && x.Name.ToLower().Contains(input_name.ToLower())));

            if (synced_option == null)
            {
                chat_service.SendSystemMessageToIndividual(fromPlayer, "Unknown perditio option name. Use !helpperditio for more information.");
                return false;
            }

            bool good = false;
            switch (synced_option.Name)
            {
                case ModEntryPoint.DENSITY_FIELD_NAME:
                    good = (input_value_parsed >= 0 && input_value_parsed < ModEntryPoint.DENSITY_FIELD_OPTIONS.Length);
                    break;
                case ModEntryPoint.ROUGHNESS_FIELD_NAME:
                    good = (input_value_parsed >= 0 && input_value_parsed < ModEntryPoint.ROUGHNESS_FIELD_OPTIONS.Length);
                    break;
                case ModEntryPoint.RADIUS_FIELD_NAME:
                    good = (input_value_parsed >= ModEntryPoint.MIN_MAP_RADIUS && input_value_parsed < ModEntryPoint.MAX_MAP_RADIUS);
                    break;
                case ModEntryPoint.TEAM_SIZE_FIELD_NAME:
                    good = (input_value_parsed >= ModEntryPoint.MIN_TEAM_SIZE && input_value_parsed < ModEntryPoint.MAX_TEAM_SIZE);
                    break;
                case ModEntryPoint.SEED_FIELD_NAME_1:
                case ModEntryPoint.SEED_FIELD_NAME_2:
                case ModEntryPoint.SEED_FIELD_NAME_3:
                case ModEntryPoint.SEED_FIELD_NAME_4:
                    good = (input_value_parsed >= 0 && input_value_parsed < ModEntryPoint.MAX_SEED);
                    break;
                case ModEntryPoint.CAPS_POINTS_FIELD_NAME:
                    good = (input_value_parsed >= ModEntryPoint.MIN_CAPS_POINTS && input_value_parsed < ModEntryPoint.MAX_CAPS_POINTS);
                    break;
            }

            if (good)
            {
                option_name = synced_option.Name;
                option_value = input_value_parsed;
                return true;
            }

            chat_service.SendSystemMessageToIndividual(fromPlayer, $"Option index out of range. Use !helpperditio for more information.");
            
            return false;
        }

        static void VotePerditioSettings(IPlayer fromPlayer, string chatArgs)
        {
            Debug.Log($"Perditio VotePerditioSettings: {chatArgs}");

            if (lobby_manager == null)
            {
                Debug.Log("Perditio lobby_manager is null");
                return;
            }

            ChatService chat_service = Utils.GetPrivateValue<ChatService>(lobby_manager, "_chatService");

            if (!is_interface_dirty)
            {
                Debug.Log("Perditio map is not current one");
                chat_service.SendSystemMessageToIndividual(fromPlayer, "Perditio map is not currently selected");
                return;
            }

            string option_name;
            int option_value;

            SkirmishGameSettings game_settings = Utils.GetPrivateValue<SkirmishGameSettings>(lobby_manager, "_lobbySettings");

            if (!ParseChatCommandPerditioSettings(fromPlayer, chatArgs, out option_name, out option_value, chat_service, game_settings))
            {
                Debug.Log("Perditio Parsing failed");
                return;
            }

            Utils.GetPrivateValue<LobbyVoteTracker>(lobby_manager, "_voteTracker")
                .StartVoteOption(fromPlayer, option_name, option_value.ToString(),
                (Action)(() => SetSyncedOption(option_name, option_value, game_settings)));
        }

        static void ChangePerditioSettings(IPlayer fromPlayer, string chatArgs)
        {
            Debug.Log($"Perditio ChangePerditioSettings: {chatArgs}");

            if (lobby_manager == null)
            {
                Debug.Log("Perditio lobby_manager is null");
                return;
            }

            ChatService chat_service = Utils.GetPrivateValue<ChatService>(lobby_manager, "_chatService");

            if (!is_interface_dirty)
            {
                Debug.Log("Perditio map is not current one");
                chat_service.SendSystemMessageToIndividual(fromPlayer, "Perditio map is not currently selected");
                return;
            }

            string option_name;
            int option_value;

            SkirmishGameSettings game_settings = Utils.GetPrivateValue<SkirmishGameSettings>(lobby_manager, "_lobbySettings");

            if (!fromPlayer.ServerAdmin || !ParseChatCommandPerditioSettings(fromPlayer, chatArgs, out option_name, out option_value, chat_service, game_settings))
            {
                Debug.Log("Perditio Parsing failed");
                return;
            }

            SetSyncedOption(option_name, option_value, game_settings);
        }

        static int perditio_options_count = 0;

        [HarmonyPatch(typeof(SkirmishGameSettings), "ChangeOptionValue")]
        public class PatchChangeOptionValue
        {
            static void Prefix(ref SkirmishGameSettings __instance, ref string name, ref int newValue)
            {
                Debug.Log($"Perditio ChangeOptionValue Postfix name {name} newValue {newValue} perditio_options_count {perditio_options_count}");

                if (perditio_options_count < (7 + (!other_team_size_mod ? 1 : 0) + (last_scenario == "Control" ? 1 : 0)) && Utils.GetPrivateValue<SkirmishLobbyManager>(__instance, "_lobbyManager").IsDedicatedServer && name.ToLower().Contains("perditio"))
                {
                    Debug.Log($"Perditio Overriding option change");
                    perditio_options_count++;
                    name = "Victory Points";
                    newValue = 1;
                }
            }
        }


        [HarmonyPatch(typeof(SkirmishLobbyManager), "OnMatchCreated")]
        public class PatchOnMatchCreated
        {
            static void Postfix(ref SkirmishLobbyManager __instance)
            {
                Debug.Log("Perditio OnMatchCreated Postfix");
                if (__instance.IsDedicatedServer)
                {
                    Debug.Log("Perditio Registering Commands");
                    lobby_manager = __instance;
                    ChatService chat_service = Utils.GetPrivateValue<ChatService>(__instance, "_chatService");
                    chat_service.RegisterChatCommand("!helpperditio", new ChatService.ChatCommandCallback(HelpPerditio));
                    chat_service.RegisterChatCommand("!voteperditio", new ChatService.ChatCommandCallback(VotePerditioSettings));
                    chat_service.RegisterChatCommand("!changeperditio", new ChatService.ChatCommandCallback(ChangePerditioSettings), true);
                }
            }
        }

        public static bool CheckOtherTeamSizeMod()
        {
            foreach (ModRecord record in ModDatabase.Instance.AllMods.Reverse())
            {
                // Debug.Log($"Perditio mod {record.Info.ModName} Loaded {record.Loaded} MarkedForLoad {record.MarkedForLoad} LoadOrder {record.LoadOrder}");
                if (ModEntryPoint.OTHER_TEAM_SIZE_MOD_NAMES.Contains(record.Info.ModName) && record.MarkedForLoad)
                {
                    Debug.Log($"Perditio other_team_size_mod true {record.Info.ModName}");
                    return true;
                }
            }

            Debug.Log("Perditio other_team_size_mod false");
            return false;
        }

		static void FixSliderOption(string slider_name, int min_value, int max_value)
		{
			Debug.Log($"Perditio Fixing Slider: {slider_name}");
			
			foreach (RectTransform slider_transform in UnityEngine.Object.FindObjectsOfType<RectTransform>())
        	{
				if (slider_transform.gameObject.name != "Template - Slider(Clone)")
				{
					continue;
				}

				if (slider_transform.Find("Name").GetComponent<TextMeshProUGUI>().text == slider_name)
				{
					Debug.Log("Perditio Slider found");

					Slider slider_component = slider_transform.GetComponentInChildren<Slider>();
					slider_component.minValue = (float)min_value;
					slider_component.maxValue = (float)max_value;

					break;
				}
        	}
		}

        static void DirtyInterface(SkirmishGameSettings __instance)
        {
            Debug.Log("Perditio DirtyInterface");

            is_interface_dirty = true;
            last_scenario = __instance.SelectedScenario.ScenarioName;

            if (last_scenario == "Control" || last_scenario == "Tug Of War")
            {
				__instance.AddSliderOption(ModEntryPoint.CAPS_POINTS_FIELD_NAME, 0, 0, 5, false, "0", "Perditio", "", "");
				FixSliderOption(ModEntryPoint.CAPS_POINTS_FIELD_NAME, ModEntryPoint.MIN_CAPS_POINTS, ModEntryPoint.MAX_CAPS_POINTS - 1);
            }

            __instance.AddStringListOption(ModEntryPoint.DENSITY_FIELD_NAME, ModEntryPoint.DENSITY_FIELD_OPTIONS, 0);
            __instance.AddStringListOption(ModEntryPoint.ROUGHNESS_FIELD_NAME, ModEntryPoint.ROUGHNESS_FIELD_OPTIONS, 2);

			__instance.AddSliderOption(ModEntryPoint.RADIUS_FIELD_NAME, 0, 0, 10, false, "0", "Perditio", "", "km");
			FixSliderOption(ModEntryPoint.RADIUS_FIELD_NAME, ModEntryPoint.MIN_MAP_RADIUS, ModEntryPoint.MAX_MAP_RADIUS - 1);

            if (!other_team_size_mod)
            {
				__instance.AddSliderOption(ModEntryPoint.TEAM_SIZE_FIELD_NAME, 0, 0, 4, false, "0", "Perditio", "", "");
				FixSliderOption(ModEntryPoint.TEAM_SIZE_FIELD_NAME, ModEntryPoint.MIN_TEAM_SIZE, ModEntryPoint.MAX_TEAM_SIZE - 1);
            }

            System.Random seeds_generator = new System.Random((int)DateTimeOffset.Now.ToUnixTimeSeconds());

			__instance.AddSliderOption(ModEntryPoint.SEED_FIELD_NAME_1, 0, 0, seeds_generator.Next() % ModEntryPoint.MAX_SEED, false, "0", "Perditio", "", "");
			FixSliderOption(ModEntryPoint.SEED_FIELD_NAME_1, 0, ModEntryPoint.MAX_SEED - 1);

			__instance.AddSliderOption(ModEntryPoint.SEED_FIELD_NAME_2, 0, 0, seeds_generator.Next() % ModEntryPoint.MAX_SEED, false, "0", "Perditio", "", "");
			FixSliderOption(ModEntryPoint.SEED_FIELD_NAME_2, 0, ModEntryPoint.MAX_SEED - 1);

			__instance.AddSliderOption(ModEntryPoint.SEED_FIELD_NAME_3, 0, 0, seeds_generator.Next() % ModEntryPoint.MAX_SEED, false, "0", "Perditio", "", "");
			FixSliderOption(ModEntryPoint.SEED_FIELD_NAME_3, 0, ModEntryPoint.MAX_SEED - 1);

			__instance.AddSliderOption(ModEntryPoint.SEED_FIELD_NAME_4, 0, 0, seeds_generator.Next() % ModEntryPoint.MAX_SEED, false, "0", "Perditio", "", "");
			FixSliderOption(ModEntryPoint.SEED_FIELD_NAME_4, 0, ModEntryPoint.MAX_SEED - 1);

            Debug.Log("Perditio DirtyInterface End");
        }

        static void CleanInterface(SkirmishGameSettings __instance)
        {
            Debug.Log("Perditio CleanInterface");

            is_interface_dirty = false;
            last_scenario = "";

            if (!__instance.isServer)
            {
                Debug.Log($"Perditio you are not server");
                return;
            }

            foreach (SyncedOption synced_option in Utils.GetPrivateValue<SyncListGameSettings>(__instance, "_syncedSettings").Where<SyncedOption>((Func<SyncedOption, bool>)(x => !x.Universal)).ToList<SyncedOption>())
            {
                Debug.Log($"Perditio syncedOption: {synced_option.Name}");
                if (synced_option.Name.Contains("Perditio"))
                {
                    Utils.GetPrivateValue<SyncListGameSettings>(__instance, "_syncedSettings").Remove(synced_option);
                }
            }
        }

        static void ResolveMapOrScenario(SkirmishGameSettings __instance, bool force_reload = false)
        {
            other_team_size_mod = CheckOtherTeamSizeMod();

            if ((__instance.SelectedMap != null && __instance.SelectedMap.MapName.Contains("Perditio")) && !is_interface_dirty)
            {
                Debug.Log("Perditio interface dirty");
                DirtyInterface(__instance);
            }
            else if ((__instance.SelectedMap != null && __instance.SelectedMap.MapName.Contains("Perditio")) && is_interface_dirty && ((last_scenario != __instance.SelectedScenario.ScenarioName) || force_reload))
            {
                Debug.Log($"Perditio interface reload");
                CleanInterface(__instance);
                DirtyInterface(__instance);
            }
            else if ((__instance.SelectedMap != null && !__instance.SelectedMap.MapName.Contains("Perditio")) && is_interface_dirty)
            {
                Debug.Log("Perditio interface clean");
                CleanInterface(__instance);
                perditio_options_count = 0;
            }
            else
            {
                Debug.Log("Perditio interface already set");
            }
        }

		[HarmonyPatch(typeof(SyncedSliderOption), "BuildSliderValueText")]
        public class PatchBuildSliderValueText
        {
            static void Postfix(ref string __result, string negSideName)
            {
				Debug.Log("Perditio BuildSliderValueText Postfix");
                if (negSideName == "Perditio") {
					__result = __result.Replace("+", "");
				}
            }
        }

        [HarmonyPatch(typeof(SkirmishGameSettings), "ResolveSelectedScenario")]
        public class PatchResolveSelectedScenario
        {
            static void Postfix(ref SkirmishGameSettings __instance)
            {
                Debug.Log("Perditio ResolveSelectedScenario Postfix");
                ResolveMapOrScenario(__instance);
            }
        }

        [HarmonyPatch(typeof(SkirmishGameSettings), "ResolveSelectedMap")]
        public class PatchResolveSelectedMap
        {
            static void Postfix(ref SkirmishGameSettings __instance)
            {
                Debug.Log("Perditio ResolveSelectedMap Postfix");
                ResolveMapOrScenario(__instance);
            }
        }

        [HarmonyPatch(typeof(SkirmishGameSettings), "SetSelectedScenario")]
        public class PatchSetSelectedScenario
        {
            static void Postfix(ref SkirmishGameSettings __instance)
            {
                Debug.Log("Perditio SetSelectedScenario Postfix");
                ResolveMapOrScenario(__instance, true);
            }
        }

        [HarmonyPatch(typeof(SkirmishGameSettings), "GetLaunchOptions")]
        public class PatchSkirmishGameSettingsGetLaunchOptions
        {
            static void Postfix(ref SkirmishGameSettings __instance, ref SkirmishScenarioLaunchOptions __result)
            {
                Debug.Log("Perditio GetLaunchOptions");

                if (!is_interface_dirty)
                {
                    Debug.Log("Perditio interface is clean");
                    return;
                }

                List<SyncedOption> synced_options = Utils.GetPrivateValue<SyncListGameSettings>(__instance, "_syncedSettings").Where<SyncedOption>((Func<SyncedOption, bool>)(x => !x.Universal)).ToList<SyncedOption>();

                TerrainDensity density = (TerrainDensity)synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.DENSITY_FIELD_NAME)).Value;
                TerrainRoughness roughness = (TerrainRoughness)synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.ROUGHNESS_FIELD_NAME)).Value;
                float radius = 100f * synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.RADIUS_FIELD_NAME)).Value;
                int team_size;
                if (other_team_size_mod)
                {
                    team_size = 4;
                } else
                {
                    team_size = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.TEAM_SIZE_FIELD_NAME)).Value;
                }

                int seed_1 = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.SEED_FIELD_NAME_1)).Value;
                int seed_2 = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.SEED_FIELD_NAME_2)).Value;
                int seed_3 = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.SEED_FIELD_NAME_3)).Value;
                int seed_4 = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.SEED_FIELD_NAME_4)).Value;

                SyncedOption caps_option = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.CAPS_POINTS_FIELD_NAME));
                int caps_number = 5;
                if (caps_option != null)
                {
                    caps_number = caps_option.Value;
                }

                LobbySettings.instance = new LobbySettings(
                    __result.Scenario.ScenarioName,
                    density,
                    roughness,
                    radius,
                    team_size,
                    seed_1 + 256 * (seed_2 + 256 * (seed_3 + 256 * (seed_4))),
                    caps_number
                );
            }
        }

        [HarmonyPatch(typeof(SkirmishGameSettings), "GetMaxPlayersForTeam")]
        public class PatchGetMaxPlayersForTeam
        {
            static bool Prefix(ref SkirmishGameSettings __instance, ref int __result, TeamIdentifier team)
            {
                Debug.Log("Perditio GetMaxPlayersForTeam Prefix");
                if (other_team_size_mod)
                {
                    Debug.Log("Perditio Other team size mod");
                    return true;
                }

                if (!is_interface_dirty)
                {
                    Debug.Log("Perditio interface clean");
                    return true;
                }


                __result = 0;
                if (team == TeamIdentifier.None || __instance.SelectedMap == null)
                {
                    return false;
                }

                //Debug.Log("Perditio GetMaxPlayersForTeam Flag1");
                List<SyncedOption> synced_options = Utils.GetPrivateValue<SyncListGameSettings>(__instance, "_syncedSettings").Where<SyncedOption>((Func<SyncedOption, bool>)(x => !x.Universal)).ToList<SyncedOption>();

                SyncedOption team_size_option = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.TEAM_SIZE_FIELD_NAME));
                if (team_size_option == null)
                {
                    Debug.Log("Perditio GetMaxPlayersForTeam null flag");
                    return true;
                }
                
                __result = team_size_option.Value;
                return false;
            }
        }

		[HarmonyPatch(typeof(SpacePartitioner), "Build")]
        public class PatchBuildSpace
        {
            static void Prefix(Battlespace map)
            {
				Debug.Log("Perditio Build SpacePartitioner Prefix");
				if (is_interface_dirty)
				{
					Debug.Log("Perditio Setting battlespace radius");

					Utils.SetPrivateValue(map, "_radius", LobbySettings.instance.radius + 200f);
					Utils.SetPrivateValue(map, "_spawnRadius", LobbySettings.instance.radius + 50f);
				}
			}
		}

        [HarmonyPatch(typeof(SkirmishGameManager), "StateTransferringFleets")]
        public class PatchStateTransferringFleets
        {
            static void Prefix(ref SkirmishGameManager __instance)
            {
                Debug.Log("Perditio StateTransferringFleets Prefix");

                if (!other_team_size_mod && is_interface_dirty)
                {
                    Debug.Log("Perditio No other team size mod and interface dirty");

                    int team_size = LobbySettings.instance.team_size;
                    SpawnGroup _teamASpawns = Utils.GetPrivateValue<SpawnGroup>(__instance.LoadedMap, "_teamASpawns");
                    ExpandSpawnGroup(_teamASpawns, team_size);

                    SpawnGroup _teamBSpawns = Utils.GetPrivateValue<SpawnGroup>(__instance.LoadedMap, "_teamBSpawns");
                    ExpandSpawnGroup(_teamBSpawns, team_size);
                }
            }

            public static void ExpandSpawnGroup(SpawnGroup group, int newSpawnGroupSize)
            {
                SpawnPoint[] originalPoints = Utils.GetPrivateValue<SpawnPoint[]>(group, "_spawns");
                int numOriginalPoints = originalPoints.Length;
                if (numOriginalPoints == 0)
                    return;

                SpawnPoint[] newPoints = new SpawnPoint[newSpawnGroupSize];
                for (int i = 0; i < newSpawnGroupSize; i++)
                {
                    newPoints[i] = originalPoints[i % numOriginalPoints];
                }

                Utils.SetPrivateValue(group, "_spawns", newPoints);
            }
        }
    }
}
