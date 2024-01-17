using System;

namespace Perditio
{
    public enum TerrainDensity
    {
        Random,
        High,
        Medium,
        Low
    }

    public enum TerrainRoughness
    {
        VeryLow,
        Low,
        Default,
        High,
        VeryHigh
    }

    public class LobbySettings
    {
        public LobbySettings(String _scenario, TerrainDensity _terrain_density, TerrainRoughness _terrain_roughness, int _seed)
        {
            scenario = _scenario;
            terrain_density = _terrain_density;
            terrain_roughness = _terrain_roughness;
            seed = _seed;
        }

        public String scenario;
        public TerrainDensity terrain_density;
        public TerrainRoughness terrain_roughness;
        public int seed;

        private static LobbySettings _instance;
        public static LobbySettings instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LobbySettings("Control", TerrainDensity.Random, TerrainRoughness.Default, 0);
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
