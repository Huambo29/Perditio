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

        public const int MAX_SEED = 256;
        public static readonly string[] SEED_FIELD_OPTIONS = Enumerable.Range(0, MAX_SEED).Select(x => x.ToString()).ToArray();
        public const string SEED_FIELD_NAME_1 = "Perditio Seed 1";
        public const string SEED_FIELD_NAME_2 = "Perditio Seed 2";
        public const string SEED_FIELD_NAME_3 = "Perditio Seed 3";
        public const string SEED_FIELD_NAME_4 = "Perditio Seed 4";

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
