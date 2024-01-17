using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using HarmonyLib;

using Game;
using Networking;

namespace Perditio
{
    public class Patches
    {
        static bool is_interface_dirty = false;

        static SkirmishLobbyManager lobby_manager;

        static void SetSyncedOption(string name, int value, SkirmishGameSettings game_settings)
        {
            List<SyncedOption> synced_options = Utils.GetPrivateValue<SyncListGameSettings>(game_settings, "_syncedSettings").Where<SyncedOption>((Func<SyncedOption, bool>)(x => !x.Universal)).ToList<SyncedOption>();
            synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == name)).SetValue(value);
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
                chat_service.SendSystemMessageToIndividual(fromPlayer, "Command format is: !voteperditio <Option Name (string)> <Option (integer)>.");
                return false;
            }

            string input_name = chatArgs.Substring(0, length);
            string input_value = chatArgs.Substring(length + 1);

            int input_value_parsed;
            if (!int.TryParse(input_value, out input_value_parsed))
            {
                chat_service.SendSystemMessageToIndividual(fromPlayer, "Command format is: !voteperditio <Option Name (string)> <Option (integer)>.");
                return false;
            }

            List<SyncedOption> synced_options = Utils.GetPrivateValue<SyncListGameSettings>(game_settings, "_syncedSettings").Where<SyncedOption>((Func<SyncedOption, bool>)(x => !x.Universal)).ToList<SyncedOption>();

            SyncedOption synced_option = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name.Contains("Perditio") && x.Name.Contains(input_name)));

            if (synced_option == null)
            {
                chat_service.SendSystemMessageToIndividual(fromPlayer, "Unknown perditio option name");
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
                case ModEntryPoint.SEED_FIELD_NAME_1:
                case ModEntryPoint.SEED_FIELD_NAME_2:
                case ModEntryPoint.SEED_FIELD_NAME_3:
                case ModEntryPoint.SEED_FIELD_NAME_4:
                    good = (input_value_parsed >= 0 && input_value_parsed < ModEntryPoint.MAX_SEED);
                    break;
            }

            if (good)
            {
                option_name = input_name;
                option_value = input_value_parsed;
                return true;
            }

            chat_service.SendSystemMessageToIndividual(fromPlayer, "Option index out of range.");
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

            if (lobby_manager == null)
            {
                Debug.Log("Perditio lobby_manager is null");
                return;
            }

            ChatService chat_service = Utils.GetPrivateValue<ChatService>(lobby_manager, "_chatService");

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
                    chat_service.RegisterChatCommand("!voteperditio", new ChatService.ChatCommandCallback(VotePerditioSettings));
                    chat_service.RegisterChatCommand("!changeperditio", new ChatService.ChatCommandCallback(ChangePerditioSettings), true);
                }
            }
        }

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
                }
            }
        }

        [HarmonyPatch(typeof(SkirmishGameSettings), "GetLaunchOptions")]
        public class PatchSkirmishGameSettingsGetLaunchOptions
        {
            static void Postfix(ref SkirmishGameSettings __instance, ref SkirmishScenarioLaunchOptions __result)
            {
                Debug.Log("Perditio GetLaunchOptions");

                //if (Utils.GetPrivateValue<SkirmishLobbyManager>(__instance, "_lobbyManager").IsDedicatedServer)
                //{
                //    Debug.Log("Perditio No settings exposing for dedicated servers");
               //     return;
                //}

                

                List<SyncedOption> synced_options = Utils.GetPrivateValue<SyncListGameSettings>(__instance, "_syncedSettings").Where<SyncedOption>((Func<SyncedOption, bool>)(x => !x.Universal)).ToList<SyncedOption>();

                TerrainDensity density = (TerrainDensity)synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.DENSITY_FIELD_NAME)).Value;
                TerrainRoughness roughness = (TerrainRoughness)synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.ROUGHNESS_FIELD_NAME)).Value;

                int seed_1 = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.SEED_FIELD_NAME_1)).Value;
                int seed_2 = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.SEED_FIELD_NAME_2)).Value;
                int seed_3 = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.SEED_FIELD_NAME_3)).Value;
                int seed_4 = synced_options.FirstOrDefault<SyncedOption>((Func<SyncedOption, bool>)(x => x.Name == ModEntryPoint.SEED_FIELD_NAME_4)).Value;

                LobbySettings.instance = new LobbySettings(
                    __result.Scenario.ScenarioName,
                    density,
                    roughness,
                    seed_1 + 256 * (seed_2 + 256 * (seed_3 + 256 * (seed_4)))
                );
            }
        }
    }
}
