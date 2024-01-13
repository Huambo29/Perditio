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
        public LobbySettings(String _scenario, TerrainDensity _terrain_density)
        {
            scenario = _scenario;
            terrain_density = _terrain_density;
        }

        public String scenario;
        public TerrainDensity terrain_density;

        public static LobbySettings instance;
    }
}
