using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using HarmonyLib;

<<<<<<< HEAD
using Game;
=======
using Utility;
using Game;
using Modding;
using Game.Map;
>>>>>>> origin/main
using Networking;

namespace Perditio
{
    public class Patches
    {
        static bool is_interface_dirty = false;
<<<<<<< HEAD
=======
        static bool other_team_size_mod = false;
        static string last_scenario = "";
>>>>>>> origin/main

        static SkirmishLobbyManager lobby_manager;

        static void HelpPerditio(IPlayer fromPlayer, string chatArgs)
        {
            if (lobby_manager == null)
            {
                Debug.Log("Perditio lobby_manager is null");
                return;
            }
<<<<<<< HEAD

            ChatService chat_service = Utils.GetPrivateValue<ChatService>(lobby_manager, "_chatService");

=======
            ChatService chat_service = Utils.GetPrivateValue<ChatService>(lobby_manager, "_chatService");

            if (!is_interface_dirty)
            {
                Debug.Log("Perditio map is not current one");
                chat_service.SendSystemMessageToIndividual(fromPlayer, "Perditio map is not currently selected");
                return;
            }

>>>>>>> origin/main
            chat_service.SendSystemMessageToIndividual(fromPlayer, "Commands are:");
            chat_service.SendSystemMessageToIndividual(fromPlayer, "!voteperditio <Option Name (string)> <Option (integer)>.");
            chat_service.SendSystemMessageToIndividual(fromPlayer, "!changeperditio <Option Name (string)> <Option (integer)>.");
            chat_service.SendSystemMessageToIndividual(fromPlayer, "You don't have to use full option names");

            chat_service.SendSystemMessageToIndividual(fromPlayer, "Available option values are:");
            chat_service.SendSystemMessageToIndividual(fromPlayer, $"Density: 0-{ModEntryPoint.DENSITY_FIELD_OPTIONS.Length - 1} ({String.Join(", ", ModEntryPoint.DENSITY_FIELD_OPTIONS)})");
            chat_service.SendSystemMessageToIndividual(fromPlayer, $"Roughness: 0-{ModEntryPoint.ROUGHNESS_FIELD_OPTIONS.Length - 1} ({String.Join(", ", ModEntryPoint.ROUGHNESS_FIELD_OPTIONS)})");
<<<<<<< HEAD
            chat_service.SendSystemMessageToIndividual(fromPlayer, $"Seeds: 0-{ModEntryPoint.MAX_SEED - 1}");
=======
            chat_service.SendSystemMessageToIndividual(fromPlayer, $"Radius: {ModEntryPoint.MIN_MAP_RADIUS}-{ModEntryPoint.MAX_MAP_RADIUS - 1}km");
            chat_service.SendSystemMessageToIndividual(fromPlayer, $"Teams Size: {ModEntryPoint.MIN_TEAM_SIZE}-{ModEntryPoint.MAX_TEAM_SIZE - 1}");
            chat_service.SendSystemMessageToIndividual(fromPlayer, $"Seeds: 0-{ModEntryPoint.MAX_SEED - 1}");
            chat_service.SendSystemMessageToIndividual(fromPlayer, $"Cap points: {ModEntryPoint.MIN_CAPS_POINTS}-{ModEntryPoint.MAX_CAPS_POINTS - 1} (Maximum number of capture points)");
>>>>>>> origin/main
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
<<<<<<< HEAD
                chat_service.SendSystemMessageToIndividual(fromPlayer, "Command format is: !voteperditio <Option Name (string)> <Option (integer)>. Use !helpperditio for more information.");
=======
                chat_service.SendSystemMessageToIndividual(fromPlayer, "Invalid command format. Use !helpperditio for more information.");
>>>>>>> origin/main
                return false;
            }

            string input_name = chatArgs.Substring(0, length);
            string input_value = chatArgs.Substring(length + 1);

            int input_value_parsed;
            if (!int.TryParse(input_value, out input_value_parsed))
            {
<<<<<<< HEAD
                chat_service.SendSystemMessageToIndividual(fromPlayer, "Command format is: !voteperditio <Option Name (string)> <Option (integer)>. Use !helpperditio for more information.");
=======
                chat_service.SendSystemMessageToIndividual(fromPlayer, "Invalid command format. Use !helpperditio for more information.");
>>>>>>> origin/main
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
<<<<<<< HEAD
=======
                case ModEntryPoint.RADIUS_FIELD_NAME:
                    good = (input_value_parsed >= ModEntryPoint.MIN_MAP_RADIUS && input_value_parsed < ModEntryPoint.MAX_MAP_RADIUS);
                    input_value_parsed -= ModEntryPoint.MIN_MAP_RADIUS;
                    break;
                case ModEntryPoint.TEAM_SIZE_FIELD_NAME:
                    good = (input_value_parsed >= ModEntryPoint.MIN_TEAM_SIZE && input_value_parsed < ModEntryPoint.MAX_TEAM_SIZE);
                    input_value_parsed -= ModEntryPoint.MIN_TEAM_SIZE;
                    break;
>>>>>>> origin/main
                case ModEntryPoint.SEED_FIELD_NAME_1:
                case ModEntryPoint.SEED_FIELD_NAME_2:
                case ModEntryPoint.SEED_FIELD_NAME_3:
                case ModEntryPoint.SEED_FIELD_NAME_4:
                    good = (input_value_parsed >= 0 && input_value_parsed < ModEntryPoint.MAX_SEED);
                    break;
<<<<<<< HEAD
=======
                case ModEntryPoint.CAPS_POINTS_FIELD_NAME:
                    good = (input_value_parsed >= ModEntryPoint.MIN_CAPS_POINTS && input_value_parsed < ModEntryPoint.MAX_CAPS_POINTS);
                    input_value_parsed -= ModEntryPoint.MIN_CAPS_POINTS;
                    break;
>>>>>>> origin/main
            }

            if (good)
            {
                option_name = synced_option.Name;
                option_value = input_value_parsed;
                return true;
            }

<<<<<<< HEAD
            chat_service.SendSystemMessageToIndividual(fromPlayer, $"Option index out of range.");
=======
            chat_service.SendSystemMessageToIndividual(fromPlayer, $"Option index out of range. Use !helpperditio for more information.");
>>>>>>> origin/main
            
            return false;
        }

        static void VotePerditioSettings(IPlayer fromPlayer, string chatArgs)
        {
            Debug.Log($"Perditio VotePerditioSettings: {chatArgs}");

<<<<<<< HEAD
            if (!is_interface_dirty)
            {
                Debug.Log("Perditio map is not current one");
                return;
            }

=======
>>>>>>> origin/main
            if (lobby_manager == null)
            {
                Debug.Log("Perditio lobby_manager is null");
                return;
            }

            ChatService chat_service = Utils.GetPrivateValue<ChatService>(lobby_manager, "_chatService");

<<<<<<< HEAD
=======
            if (!is_interface_dirty)
            {
                Debug.Log("Perditio map is not current one");
                chat_service.SendSystemMessageToIndividual(fromPlayer, "Perditio map is not currently selected");
                return;
            }

>>>>>>> origin/main
            if (!Utils.GetPrivateValue<SkirmishDedicatedServerConfig>(lobby_manager, "_dediConfig").AllowMapVoting)
            {
                chat_service.SendSystemMessageToIndividual(fromPlayer, "Match settings voting is disabled on this server.");
            }
            else
            {
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
        }

        static void ChangePerditioSettings(IPlayer fromPlayer, string chatArgs)
        {
            Debug.Log($"Perditio ChangePerditioSettings: {chatArgs}");

<<<<<<< HEAD
            if (!is_interface_dirty)
            {
                Debug.Log("Perditio map is not current one");
                return;
            }

=======
>>>>>>> origin/main
            if (lobby_manager == null)
            {
                Debug.Log("Perditio lobby_manager is null");
                return;
            }

            ChatService chat_service = Utils.GetPrivateValue<ChatService>(lobby_manager, "_chatService");

<<<<<<< HEAD
=======
            if (!is_interface_dirty)
            {
                Debug.Log("Perditio map is not current one");
                chat_service.SendSystemMessageToIndividual(fromPlayer, "Perditio map is not currently selected");
                return;
            }

>>>>>>> origin/main
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

<<<<<<< HEAD
                if (perditio_options_count < 6 && Utils.GetPrivateValue<SkirmishLobbyManager>(__instance, "_lobbyManager").IsDedicatedServer)
                {
                    Debug.Log($"Perditio Overriding option change");

                    if (name.ToLower().Contains("perditio"))
                    {
                        perditio_options_count++;
                        name = "Victory Points";
                        newValue = 1;
                    }
=======
                if (perditio_options_count < (7 + (!other_team_size_mod ? 1 : 0) + (last_scenario == "Control" ? 1 : 0)) && Utils.GetPrivateValue<SkirmishLobbyManager>(__instance, "_lobbyManager").IsDedicatedServer && name.ToLower().Contains("perditio"))
                {
                    Debug.Log($"Perditio Overriding option change");
                    perditio_options_count++;
                    name = "Victory Points";
                    newValue = 1;
>>>>>>> origin/main
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

<<<<<<< HEAD
        [HarmonyPatch(typeof(SkirmishGameSettings), "ResolveSelectedMap")]
        public class PatchResolveSelectedMap
        {
            static void Postfix(ref SkirmishGameSettings __instance)
            {
                Debug.Log("Perditio ResolveSelectedMap Postfix");

                if ((__instance.SelectedMap && __instance.SelectedMap.MapName.Contains("Perditio")) && !is_interface_dirty)
                {
                    Debug.Log("Perditio interface dirty");
                    is_interface_dirty = true;

                    __instance.AddStringListOption(ModEntryPoint.DENSITY_FIELD_NAME, ModEntryPoint.DENSITY_FIELD_OPTIONS, 0);
                    __instance.AddStringListOption(ModEntryPoint.ROUGHNESS_FIELD_NAME, ModEntryPoint.ROUGHNESS_FIELD_OPTIONS, 2);

                    System.Random seeds_generator = new System.Random((int)DateTimeOffset.Now.ToUnixTimeSeconds());
                    __instance.AddStringListOption(ModEntryPoint.SEED_FIELD_NAME_1, ModEntryPoint.SEED_FIELD_OPTIONS, seeds_generator.Next() % ModEntryPoint.MAX_SEED);
                    __instance.AddStringListOption(ModEntryPoint.SEED_FIELD_NAME_2, ModEntryPoint.SEED_FIELD_OPTIONS, seeds_generator.Next() % ModEntryPoint.MAX_SEED);
                    __instance.AddStringListOption(ModEntryPoint.SEED_FIELD_NAME_3, ModEntryPoint.SEED_FIELD_OPTIONS, seeds_generator.Next() % ModEntryPoint.MAX_SEED);
                    __instance.AddStringListOption(ModEntryPoint.SEED_FIELD_NAME_4, ModEntryPoint.SEED_FIELD_OPTIONS, seeds_generator.Next() % ModEntryPoint.MAX_SEED);
                }
                else if ((__instance.SelectedMap && !__instance.SelectedMap.MapName.Contains("Perditio")) && is_interface_dirty)
                {
                    Debug.Log("Perditio interface clean");
                    is_interface_dirty = false;

                    perditio_options_count = 0;

                    foreach (SyncedOption synced_option in Utils.GetPrivateValue<SyncListGameSettings>(__instance, "_syncedSettings").Where<SyncedOption>((Func<SyncedOption, bool>)(x => !x.Universal)).ToList<SyncedOption>())
                    {
                        Debug.Log($"syncedOption: {synced_option.Name}");
                        if (synced_option.Name.Contains("Perditio"))
                        {
                            Utils.GetPrivateValue<SyncListGameSettings>(__instance, "_syncedSettings").Remove(synced_option);
                        }
                    }
                }
                else
                {
                    Debug.Log("Perditio interface already set");
=======
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

        static void DirtyInterface(SkirmishGameSettings __instance)
        {
            Debug.Log("Perditio DirtyInterface");

            is_interface_dirty = true;
            last_scenario = __instance.SelectedScenario.ScenarioName;

            if (last_scenario == "Control" || last_scenario == "Tug Of War")
            {
                __instance.AddStringListOption(ModEntryPoint.CAPS_POINTS_FIELD_NAME, ModEntryPoint.CAPS_POINTS_FIELD_OPTIONS, 4);
            }

            __instance.AddStringListOption(ModEntryPoint.DENSITY_FIELD_NAME, ModEntryPoint.DENSITY_FIELD_OPTIONS, 0);
            __instance.AddStringListOption(ModEntryPoint.ROUGHNESS_FIELD_NAME, ModEntryPoint.ROUGHNESS_FIELD_OPTIONS, 2);

            __instance.AddStringListOption(ModEntryPoint.RADIUS_FIELD_NAME, ModEntryPoint.RADIUS_FIELD_OPTIONS, 7);
            if (!other_team_size_mod)
            {
                __instance.AddStringListOption(ModEntryPoint.TEAM_SIZE_FIELD_NAME, ModEntryPoint.TEAM_SIZE_FIELD_OPTIONS, 3);
            }

            System.Random seeds_generator = new System.Random((int)DateTimeOffset.Now.ToUnixTimeSeconds());
            __instance.AddStringListOption(ModEntryPoint.SEED_FIELD_NAME_1, ModEntryPoint.SEED_FIELD_OPTIONS, seeds_generator.Next() % ModEntryPoint.MAX_SEED);
            __instance.AddStringListOption(ModEntryPoint.SEED_FIELD_NAME_2, ModEntryPoint.SEED_FIELD_OPTIONS, seeds_generator.Next() % ModEntryPoint.MAX_SEED);
            __instance.AddStringListOption(ModEntryPoint.SEED_FIELD_NAME_3, ModEntryPoint.SEED_FIELD_OPTIONS, seeds_generator.Next() % ModEntryPoint.MAX_SEED);
            __instance.AddStringListOption(ModEntryPoint.SEED_FIELD_NAME_4, ModEntryPoint.SEED_FIELD_OPTIONS, seeds_generator.Next() % ModEntryPoint.MAX_SEED);

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
>>>>>>> origin/main
                }
            }
        }

<<<<<<< HEAD
=======
        static void ResolveMapOrScenario(SkirmishGameSettings __instance, bool force_reload = false)
        {
            other_team_size_mod = CheckOtherTeamSizeMod();

            if ((__instance.SelectedMap && __instance.SelectedMap.MapName.Contains("Perditio")) && !is_interface_dirty)
            {
                Debug.Log("Perditio interface dirty");
                DirtyInterface(__instance);
            }
            else if ((__instance.SelectedMap && __instance.SelectedMap.MapName.Contains("Perditio")) && is_interface_dirty && ((last_scenario != __instance.SelectedScenario.ScenarioName) || force_reload))
            {
                Debug.Log($"Perditio interface reload");
                CleanInterface(__instance);
                DirtyInterface(__instance);
            }
            else if ((__instance.SelectedMap && !__instance.SelectedMap.MapName.Contains("Perditio")) && is_interface_dirty)
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

>>>>>>> origin/main
        [HarmonyPatch(typeof(SkirmishGameSettings), "GetLaunchOptions")]
        public class PatchSkirmishGameSettingsGetLaunchOptions
        {
            static void Postfix(ref SkirmishGameSettings __instance, ref SkirmishScenarioLaunchOptions __result)
            {
                Debug.Log("Perditio GetLaunchOptions");

<<<<<<< HEAD
                //if (Utils.GetPrivateValue<SkirmishLobbyManager>(__instance, "_lobbyManager").IsDedicatedServer)
                //{
                //    Debug.Log("Perditio No settings exposing for dedicated servers");
               //     return;
                //}

                
=======
                if (!is_interface_dirty)
                {
                    Debug.Log("Perditio interface is clean");
                    return;
                }
>>>>>>> origin/main

                List<SyncedOption> synced_options = Utils.GetPrivateValue<SyncListGameSettings>(__instance, "_syncedSettings").Where<SyncedOption>((Func<SyncedOption, bool>)(x => !x.Universal)).ToList<SyncedOption>();

                TerrainDensity density = (TerrainDensity)synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.DENSITY_FIELD_NAME)).Value;
                TerrainRoughness roughness = (TerrainRoughness)synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.ROUGHNESS_FIELD_NAME)).Value;
<<<<<<< HEAD
=======
                float radius = 100f * (ModEntryPoint.MIN_MAP_RADIUS + synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.RADIUS_FIELD_NAME)).Value);
                int team_size;
                if (other_team_size_mod)
                {
                    team_size = 4;
                } else
                {
                    team_size = ModEntryPoint.MIN_TEAM_SIZE + synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.TEAM_SIZE_FIELD_NAME)).Value;
                }
>>>>>>> origin/main

                int seed_1 = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.SEED_FIELD_NAME_1)).Value;
                int seed_2 = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.SEED_FIELD_NAME_2)).Value;
                int seed_3 = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.SEED_FIELD_NAME_3)).Value;
                int seed_4 = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.SEED_FIELD_NAME_4)).Value;

<<<<<<< HEAD
=======
                SyncedOption caps_option = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.CAPS_POINTS_FIELD_NAME));
                int caps_number = 5;
                if (caps_option != null)
                {
                    caps_number = ModEntryPoint.MIN_CAPS_POINTS + caps_option.Value;
                }

>>>>>>> origin/main
                LobbySettings.instance = new LobbySettings(
                    __result.Scenario.ScenarioName,
                    density,
                    roughness,
<<<<<<< HEAD
                    seed_1 + 256 * (seed_2 + 256 * (seed_3 + 256 * (seed_4)))
                );
=======
                    radius,
                    team_size,
                    seed_1 + 256 * (seed_2 + 256 * (seed_3 + 256 * (seed_4))),
                    caps_number
                );

                Utils.SetPrivateValue(__result.Map, "_radius", LobbySettings.instance.radius + 200f);
                Utils.SetPrivateValue(__result.Map, "_spawnRadius", LobbySettings.instance.radius + 50f);
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
                
                __result = ModEntryPoint.MIN_TEAM_SIZE + team_size_option.Value;
                return false;
            }
        }

        [HarmonyPatch(typeof(SkirmishGameManager), "StateTransferringFleets")]
        public class PatchStateTransferringFleets
        {
            static void Prefix(ref SkirmishGameManager __instance)
            {
                Debug.Log("Perditio StateTransferringFleets Prefix");


                Battlespace battlespace = __instance.LoadedMap;

                if (!other_team_size_mod && is_interface_dirty)
                {
                    Debug.Log("Perditio No other team size mod and interface dirty");

                    int team_size = LobbySettings.instance.team_size;
                    SpawnGroup _teamASpawns = Utils.GetPrivateValue<SpawnGroup>(battlespace, "_teamASpawns");
                    ExpandSpawnGroup(_teamASpawns, team_size);

                    SpawnGroup _teamBSpawns = Utils.GetPrivateValue<SpawnGroup>(battlespace, "_teamBSpawns");
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
>>>>>>> origin/main
            }
        }
    }
}
