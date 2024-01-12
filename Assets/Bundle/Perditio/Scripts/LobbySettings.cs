using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Perditio
{
    public struct LobbySettings
    {
        public LobbySettings(TerrainDensity _terrain_density)
        {
            terrain_density = _terrain_density;
        }

        public TerrainDensity terrain_density;
    }

    public enum TerrainDensity
    {
        Random, 
        High,
        Medium,
        Low
    }
}
