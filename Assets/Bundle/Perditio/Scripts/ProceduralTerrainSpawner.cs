using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ProceduralTerrainSpawner : NetworkBehaviour
{
    [SerializeField]
    GameObject procedural_terrain_prefab;

    [SerializeField]
    GameObject middle_man_prefab;

    [SerializeField]
    Material terrain_material;
    [SerializeField]
    int grid_resolution = 100;
    [SerializeField]
    float mesh_size = 100f;
    [SerializeField]
    float density_cut_off = 0.5f;
    [SerializeField]
    float drop_off_start = 30f;
    [SerializeField]
    float drop_off_end = 40f;
    [SerializeField]
    int octaves = 4;
    [SerializeField]
    Vector3 perlin_offset = new Vector3(1123f, 12f, 5000f);
    [SerializeField]
    Quaternion perlin_rotation;
    [SerializeField]
    float perlin_scale = 100f;
    [SerializeField]
    float persistence = 0.5f;
    [SerializeField]
    float lacunarity = 2f;

    float[,,] density_field;
    Vector3[] DistributedObjectives;

    float SamplePerlinNoise(Vector3 pos)
    {
        float ab = Mathf.PerlinNoise(pos.x, pos.y);
        float bc = Mathf.PerlinNoise(pos.y, pos.z);
        float ac = Mathf.PerlinNoise(pos.x, pos.z);

        float ba = Mathf.PerlinNoise(pos.y, pos.x);
        float cb = Mathf.PerlinNoise(pos.z, pos.y);
        float ca = Mathf.PerlinNoise(pos.z, pos.x);

        return (ab + bc + ac + ba + cb + ca) / 6f;
    }

    float PerlinOctaves(Vector3 pos)
    {
        float result = 0f;

        float amplitude = 1f;
        float frequency = 1f;

        for (int i = 0; i < octaves; i++)
        {
            result += amplitude * SamplePerlinNoise(pos * frequency);
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        if (1f - persistence == 0)
        {
            persistence = 1.000001f;
        }

        return result / ((1f - Mathf.Pow(persistence, octaves)) / (1f - persistence));
    }

    float interest_points_drop_off(Vector3 pos)
    {
        float result = 0f;

        float cap_drop_off_start = 60f;
        float cap_drop_off_end = 150f;


        Vector3 sample_grid_positon = pos / grid_resolution - Vector3.one * 0.5f;

        Game.Map.Battlespace battlespace = gameObject.GetComponentInParent<Game.Map.Battlespace>();

        for (int i = 0; i < battlespace.DistributedObjectives.Length; i++)
        {
            Vector3 cap_grid_position = battlespace.DistributedObjectives[i] / mesh_size;

            float center_distance = Vector3.Distance(sample_grid_positon, cap_grid_position) * mesh_size;
            float drop_off = 1f - ((Mathf.Max(cap_drop_off_start, Mathf.Min(center_distance, cap_drop_off_end)) - cap_drop_off_start) / (cap_drop_off_end - cap_drop_off_start));
            result -= Mathf.Pow(drop_off, 2f);
        }

        return result;
    }

    float CalculateDensity(int x, int y, int z)
    {
        float center_distance = Mathf.Sqrt(Mathf.Pow((float)x / grid_resolution - 0.5f, 2f) + Mathf.Pow((float)y / grid_resolution - 0.5f, 2f) + Mathf.Pow((float)z / grid_resolution - 0.5f, 2f)) * mesh_size;
        float drop_off = ((Mathf.Max(drop_off_start, Mathf.Min(center_distance, drop_off_end)) - drop_off_start) / (drop_off_end - drop_off_start));

        return PerlinOctaves((perlin_rotation * new Vector3(x, y, z) + perlin_offset) * 1.41421356237f / perlin_scale) - Mathf.Pow(drop_off, 2f) + interest_points_drop_off(new Vector3(x, y, z));

        // return 1f - FractalDistanceFunction(fractal_rotation * new Vector3((float)x / grid_resolution - 0.5f, (float)y / grid_resolution - 0.5f, (float)z / grid_resolution - 0.5f) + fractal_offset) / mesh_size;
    }

    void Start()
    {
        NetworkClient.RegisterPrefab(procedural_terrain_prefab);
        NetworkClient.RegisterPrefab(middle_man_prefab);

        if (!NetworkServer.active)
        {
            Debug.Log("You are not server");
            return;
        }

        Debug.Log("You are server with resposibilities");
        Debug.Log("Spawner isServer: " + isServer + " isServerOnly: " + isServerOnly + " isClient: " + isClient + " isClientOnly: " + isClientOnly + " isLocalPlayer: " + isLocalPlayer);

        Game.Map.Battlespace battlespace = gameObject.GetComponentInParent<Game.Map.Battlespace>();

        for (int i = 0; i < battlespace.DistributedObjectives.Length; i++)
        {
            if (i == 0)
            {
                battlespace.DistributedObjectives[i] = Random.onUnitSphere * Random.Range(0f, mesh_size / 2f * 0.1f);
            }
            else if (i % 2 == 1)
            {
                bool good_placement;
                do
                {
                    battlespace.DistributedObjectives[i] = Random.onUnitSphere * Random.Range(mesh_size / 2f * 0.1f + 400f, mesh_size / 2f - 200f);
                    good_placement = true;
                    for (int k = 0; k < i; k++)
                    {
                        if (Vector3.Distance(battlespace.DistributedObjectives[k], battlespace.DistributedObjectives[i]) <= 500f)
                        {
                            good_placement = false;
                            break;
                        }
                    }
                } while (!good_placement);
            }
            else
            {
                battlespace.DistributedObjectives[i] = -battlespace.DistributedObjectives[i - 1];
            }
        }

        density_field = new float[grid_resolution, grid_resolution, grid_resolution];

        perlin_offset = new Vector3(Random.Range(-100000f, 100000f), Random.Range(-100000f, 100000f), Random.Range(-100000f, 100000f));
        perlin_rotation = Random.rotation;
        density_cut_off = 0.5f - 0.02f * Random.value;

        for (int x = 0; x < grid_resolution; x++)
        {
            for (int y = 0; y < grid_resolution; y++)
            {
                for (int z = 0; z < grid_resolution; z++)
                {
                    density_field[x, y, z] = CalculateDensity(x, y, z);

                    if (
                        x == 0 || x == grid_resolution - 1 ||
                        y == 0 || y == grid_resolution - 1 ||
                        z == 0 || z == grid_resolution - 1
                    )
                    {
                        density_field[x, y, z] = 0f;
                    }
                }
            }
        }
        

        //GameObject procedural_terrain = Instantiate(procedural_terrain_prefab);
        //NetworkServer.Spawn(procedural_terrain);

        GameObject middle_man = Instantiate(middle_man_prefab);
        middle_man.GetComponent<MiddleMen>().test_sync = true;

        NetworkServer.Spawn(middle_man);


        //ProceduralTerrain procedural_terrain_component = procedural_terrain.GetComponent<ProceduralTerrain>();
        //procedural_terrain_component.test_sync = !procedural_terrain_component.test_sync;
        /*
        ProceduralTerrainSettings terrain_settings = new ProceduralTerrainSettings();
        terrain_settings.terrain_material = terrain_material;
        terrain_settings.grid_resolution = grid_resolution;
        terrain_settings.mesh_size = mesh_size;
        terrain_settings.density_cut_off = density_cut_off;
        terrain_settings.density_field = density_field;

        procedural_terrain_component.terrain_settings = terrain_settings;
        */
    }

    void Update()
    {
        
    }
}
