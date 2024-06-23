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
        public LobbySettings(String _scenario, TerrainDensity _terrain_density, TerrainRoughness _terrain_roughness, float _radius, int _team_size, int _seed, int _caps_number)
        {
            scenario = _scenario;
            terrain_density = _terrain_density;
            terrain_roughness = _terrain_roughness;
            radius = _radius;
            team_size = _team_size;
            seed = _seed;
            caps_number = _caps_number;
        }

		public static LobbySettings DefaultSettings() {
			return new LobbySettings(
				"Control",
				TerrainDensity.Medium,
            	TerrainRoughness.Default,
            	1000,
            	4,
            	-1,
            	5
			);
		}

        public String scenario;
        public TerrainDensity terrain_density;
        public TerrainRoughness terrain_roughness;
        public float radius;
        public int team_size;
        public int seed;
        public int caps_number;
        

        private static LobbySettings _instance;
        public static LobbySettings instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = DefaultSettings();
                }
                return _instance;
            }
            set
            {
				if (_instance == null)
                {
                    _instance = DefaultSettings();
                }
                _instance = value;
            }
        }
    }
}
