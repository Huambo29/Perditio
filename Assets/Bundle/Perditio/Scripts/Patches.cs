using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using Mirror;
using HarmonyLib;
using TMPro;

using Bundles;
using Utility;
using Game;
using Modding;
using Game.Map;
using Networking;
using Missions.Nodes.Scenario;
using Conquest;
using Conquest.Map;
using Conquest.Persistence;

namespace Perditio
{
	public class Patches
	{
		[HarmonyPatch(typeof(LoadMapConquest), "ReadNetwork")]
		public class PatchLoadMapConquestReadNetwork
		{
			static void Postfix(ref LoadMapConquest __instance)
			{
				Debug.Log("Perditio ReadNetwork Postfix");

				LobbySettings.instance.seed = __instance.CQMapInfo.RandomSeed;
			}
		}


		[HarmonyPatch(typeof(SkirmishConquestHost), "SendLoadMapMessage")]
		public class PatchSkirmishConquestHostSendLoadMapMessage
		{
			static bool Prefix(ref SkirmishConquestHost __instance) 
			{
				Debug.Log("Perditio SendLoadMapMessage Postfix");

				foreach (ISkirmishBattlespaceInfo battlespace_info in ((IEnumerable<ISkirmishBattlespaceInfo>) BundleManager.Instance.AllSkirmishMaps))
        		{
        		    Debug.Log($"Perditio {battlespace_info.DisplayName} {battlespace_info.MapKey}");
        		}

				ConquestMapInformation _cqMapInfo = Utils.GetPrivateValue<ConquestMapInformation>(__instance, "_cqMapInfo");
				
				LoadMapMessage message;
				if (_cqMapInfo != null)
				{
					Debug.Log($"Perditio Case 1 {_cqMapInfo.MapAddress}");
					_cqMapInfo.MapAddress = ModEntryPoint.Perditio_Map_Address;
					message = new LoadMapMessage()
					{
						Command = (LoadMapCommand) new LoadMapConquest()
						{
							CQMapInfo = _cqMapInfo
						}
					};	
				} else {
					Debug.Log("Perditio Case 2");
					message = new LoadMapMessage()
					{
						Command = (LoadMapCommand) new LoadMapByKey()
						{
							MapKey = ModEntryPoint.Perditio_Map_Key
						}
					};
				}

				NetworkServer.SendToAll<LoadMapMessage>(message);
				SkirmishGameManager.ISkirmishManager client_manager = Utils.GetPrivateValue<SkirmishGameManager.ISkirmishManager>(__instance, "_clientManager");
				client_manager.DediServerMessageLoopback<LoadMapMessage>(message);

				return false;
			}
		}

		[HarmonyPatch(typeof(SkirmishGameSettings), "GetLaunchOptions")]
		public class PatchSkirmishGameSettingsGetLaunchOptions
		{
			static void Postfix(ref SkirmishGameSettings __instance, ref SkirmishScenarioLaunchOptions __result)
			{
				Debug.Log("Perditio GetLaunchOptions");

				LobbySettings.instance.scenario = __result.Scenario.ScenarioName;
			}
		}


		[HarmonyPatch(typeof(SpacePartitioner), "Build")]
		public class PatchBuildSpace
		{
			static void Prefix(Battlespace map)
			{
				Debug.Log("Perditio Build SpacePartitioner Prefix");

				Utils.SetPrivateValue(map, "_radius", LobbySettings.instance.radius + 200f);
				Utils.SetPrivateValue(map, "_spawnRadius", LobbySettings.instance.radius + 50f);
			}
		}


		[HarmonyPatch(typeof(SkirmishGameManager), "StateTransferringFleets")]
		public class PatchStateTransferringFleets
		{
			static void Prefix(ref SkirmishGameManager __instance)
			{
				Debug.Log("Perditio StateTransferringFleets Prefix");

				int team_size = LobbySettings.instance.team_size;

				SpawnGroup _teamASpawns = Utils.GetPrivateValue<SpawnGroup>(__instance.LoadedMap, "_teamASpawns");
				ExpandSpawnGroup(_teamASpawns, team_size);

				SpawnGroup _teamBSpawns = Utils.GetPrivateValue<SpawnGroup>(__instance.LoadedMap, "_teamBSpawns");
				ExpandSpawnGroup(_teamBSpawns, team_size);
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
