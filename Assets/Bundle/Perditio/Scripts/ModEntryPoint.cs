using HarmonyLib;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

using Modding;
using Game;
using Game.Map;
using Bundles;
using Networking;
using Missions;
using Missions.Nodes;
using Missions.Nodes.Scenario;
using Utility;

using XNode;

namespace Perditio
{
    public class ModEntryPoint : IModEntryPoint
    {
        public const string DENSITY_FIELD_NAME = "Perditio Map Density";
        public static readonly string[] DENSITY_FIELD_OPTIONS = new string[4] { "Random", "High", "Medium", "Low" };

        public const string ROUGHNESS_FIELD_NAME = "Perditio Map Roughness";
        public static readonly string[] ROUGHNESS_FIELD_OPTIONS = new string[5] { "Very Low", "Low", "Default", "High", "Very High" };

        public void PreLoad()
        {
            
        }

        public void PostLoad()
        {
            Debug.Log("Perditio PostLoad");

            Harmony harmony = new Harmony("nebulous.perditio");
            harmony.PatchAll();
        }
    }
}
