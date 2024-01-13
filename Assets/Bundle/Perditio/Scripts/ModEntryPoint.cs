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
        public const string DENSITY_FIELD_NAME_TRANSFER = "Perditio Terrain Density Transfer";
        public static readonly string[] DENSITY_FIELD_OPTIONS = new string[4] { "Random", "High", "Medium", "Low" };

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
