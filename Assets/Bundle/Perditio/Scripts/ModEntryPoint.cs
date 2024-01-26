using System.Linq;
using HarmonyLib;
using UnityEngine;
using Modding;

namespace Perditio
{
    public class ModEntryPoint : IModEntryPoint
    {
        public const string DENSITY_FIELD_NAME = "Perditio Density";
        public static readonly string[] DENSITY_FIELD_OPTIONS = new string[4] { "Random", "High", "Medium", "Low" };

        public const string ROUGHNESS_FIELD_NAME = "Perditio Roughness";
        public static readonly string[] ROUGHNESS_FIELD_OPTIONS = new string[5] { "Very Low", "Low", "Default", "High", "Very High" };

        public const int MIN_MAP_RADIUS = 3;
        public const int MAX_MAP_RADIUS = 51;
        public const string RADIUS_FIELD_NAME = "Perditio Radius";
        public static readonly string[] RADIUS_FIELD_OPTIONS = Enumerable.Range(MIN_MAP_RADIUS, MAX_MAP_RADIUS - MIN_MAP_RADIUS).Select(x => $"{x.ToString()}km").ToArray();

        public const int MIN_TEAM_SIZE = 1;
        public const int MAX_TEAM_SIZE = 101;
        public const string TEAM_SIZE_FIELD_NAME = "Perditio Teams Size";
        public static readonly string[] TEAM_SIZE_FIELD_OPTIONS = Enumerable.Range(MIN_TEAM_SIZE, MAX_TEAM_SIZE - MIN_TEAM_SIZE).Select(x => x.ToString()).ToArray();

        public const int MAX_SEED = 256;
        public static readonly string[] SEED_FIELD_OPTIONS = Enumerable.Range(0, MAX_SEED).Select(x => x.ToString()).ToArray();
        public const string SEED_FIELD_NAME_1 = "Perditio Seed 1";
        public const string SEED_FIELD_NAME_2 = "Perditio Seed 2";
        public const string SEED_FIELD_NAME_3 = "Perditio Seed 3";
        public const string SEED_FIELD_NAME_4 = "Perditio Seed 4";

        public static readonly string[] OTHER_TEAM_SIZE_MOD_NAMES = new string[1] { "Custom Player Count" };

        public const int MIN_CAPS_POINTS = 1;
        public const int MAX_CAPS_POINTS = 11;
        public const string CAPS_POINTS_FIELD_NAME = "Perditio Cap points";
        public static readonly string[] CAPS_POINTS_FIELD_OPTIONS = Enumerable.Range(MIN_CAPS_POINTS, MAX_CAPS_POINTS - MIN_CAPS_POINTS).Select(x => x.ToString()).ToArray();

        public void PreLoad()
        {
            Debug.Log("Perditio Preload");
        }

        public void PostLoad()
        {
            Debug.Log("Perditio PostLoad");

            Harmony harmony = new Harmony("nebulous.perditio");
            harmony.PatchAll();
        }
    }
}
