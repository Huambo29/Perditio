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

    public enum TerrainFraying
    {
        VeryLow,
        Low,
        Default,
        High,
        VeryHigh
    }

    public class LobbySettings
    {
        public LobbySettings(String _scenario, TerrainDensity _terrain_density, TerrainFraying _terrain_fraying)
        {
            scenario = _scenario;
            terrain_density = _terrain_density;
            terrain_fraying = _terrain_fraying;
        }

        public String scenario;
        public TerrainDensity terrain_density;
        public TerrainFraying terrain_fraying;

        private static LobbySettings _instance;
        public static LobbySettings instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LobbySettings("Control", TerrainDensity.Random, TerrainFraying.Default);
                }
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
    }
}
