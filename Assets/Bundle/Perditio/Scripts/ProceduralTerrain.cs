using System.Collections;
using System.Collections.Generic;
using System;
using QFSW.QC;
using UnityEngine;
using Mirror;

public class ProceduralTerrain : MonoBehaviour
{
    MeshFilter mesh_filter;
    MeshRenderer mesh_renderer;
    MeshCollider mesh_collider;

    [SerializeField]
    Material terrain_material;

    [Header("Procedural Settings")]
    [SerializeField]
    int grid_resolution = 128;
    [SerializeField]
    float mesh_size = 1800f;
    [SerializeField]
    float drop_off_start = 700f;
    [SerializeField]
    float drop_off_end = 1100f;
    [SerializeField]
    int octaves = 10;
    [SerializeField]
    float perlin_scale = 40f;
    [SerializeField]
    float octaves_persistence = 0.8f;
    [SerializeField]
    float octaves_lacunarity = 1.25f;

    [Header("Read only")]
    [SerializeField]
    float density_cut_off;
    [SerializeField]
    Vector3 perlin_offset;
    [SerializeField]
    Quaternion perlin_rotation;

    float[,,] density_field;
    Vector3[] DistributedObjectives;
    Game.Map.Battlespace battlespace;

    System.Random rand;

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
            amplitude *= octaves_persistence;
            frequency *= octaves_lacunarity;
        }

        if (1f - octaves_persistence == 0)
        {
            octaves_persistence = 1.000001f;
        }

        return result / ((1f - Mathf.Pow(octaves_persistence, octaves)) / (1f - octaves_persistence));
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

    Vector3 GetPositionFromGrid(Vector3Int root_position)
    {
        return (((Vector3)root_position / grid_resolution) - Vector3.one * 0.5f) * mesh_size;
    }

    Vector3 GetSurfacePosition(Vector3Int root_position)
    {
        if (
            root_position.x < 1 || root_position.x >= (grid_resolution - 1) ||
            root_position.y < 1 || root_position.y >= (grid_resolution - 1) ||
            root_position.z < 1 || root_position.z >= (grid_resolution - 1)
        ) {
            return GetPositionFromGrid(root_position);
        }

        Vector3Int[,] edges = new Vector3Int[12, 2]
        {
            { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0) },
            { new Vector3Int(0, 1, 0), new Vector3Int(1, 1, 0) },
            { new Vector3Int(0, 0, 1), new Vector3Int(1, 0, 1) },
            { new Vector3Int(0, 1, 1), new Vector3Int(1, 1, 1) },

            { new Vector3Int(0, 0, 0), new Vector3Int(0, 1, 0) },
            { new Vector3Int(1, 0, 0), new Vector3Int(1, 1, 0) },
            { new Vector3Int(0, 0, 1), new Vector3Int(0, 1, 1) },
            { new Vector3Int(1, 0, 1), new Vector3Int(1, 1, 1) },

            { new Vector3Int(0, 0, 0), new Vector3Int(0, 0, 1) },
            { new Vector3Int(1, 0, 0), new Vector3Int(1, 0, 1) },
            { new Vector3Int(0, 1, 0), new Vector3Int(0, 1, 1) },
            { new Vector3Int(1, 1, 0), new Vector3Int(1, 1, 1) },
        };

        Vector3 result = Vector3.zero;
        int surface_edges_n = 0;

        for (int i = 0; i < 12; i++)
        {
            Vector3Int position_a_int = root_position + edges[i, 0];
            Vector3 position_a = GetPositionFromGrid(position_a_int);
            float density_sample_a = density_field[position_a_int.x, position_a_int.y, position_a_int.z];

            Vector3Int position_b_int = root_position + edges[i, 1];
            Vector3 position_b = GetPositionFromGrid(position_b_int);
            float density_sample_b = density_field[position_b_int.x, position_b_int.y, position_b_int.z];

            if (
                Mathf.Min(density_sample_a, density_sample_b) < density_cut_off &&
                Mathf.Max(density_sample_a, density_sample_b) >= density_cut_off
            ) {
                surface_edges_n++;
                float lerp_factor = (density_cut_off - density_sample_a) / (density_sample_b - density_sample_a);
                result += Vector3.Lerp(position_a, position_b, lerp_factor);
            }
        }

        if (surface_edges_n > 0)
        {
            return result / surface_edges_n;
        }
        return GetPositionFromGrid(root_position);
    }

    Mesh GenerateMesh()
    {
        Debug.Log("flag 7");
        List<Vector3> vertices = new List<Vector3>();
        Debug.Log("flag 8");
        List<int> triangles = new List<int>();

        Debug.Log("flag 9");
        float half_grid_size = (mesh_size * 0.5f) / grid_resolution;

        Debug.Log("flag 10");
        Vector3[] offsets_x = new Vector3[4]
        {
            new Vector3(half_grid_size, half_grid_size, -half_grid_size),
            new Vector3(half_grid_size, half_grid_size, half_grid_size),
            new Vector3(half_grid_size, -half_grid_size, half_grid_size),
            new Vector3(half_grid_size, -half_grid_size, -half_grid_size)
        };

        Vector3[] offsets_y = new Vector3[4]
        {
            new Vector3(half_grid_size, half_grid_size, -half_grid_size),
            new Vector3(-half_grid_size, half_grid_size, -half_grid_size),
            new Vector3(-half_grid_size, half_grid_size, half_grid_size),
            new Vector3(half_grid_size, half_grid_size, half_grid_size)
        };

        Vector3[] offsets_z = new Vector3[4]
        {
            new Vector3(half_grid_size, half_grid_size, half_grid_size),
            new Vector3(-half_grid_size, half_grid_size, half_grid_size),
            new Vector3(-half_grid_size, -half_grid_size, half_grid_size),
            new Vector3(half_grid_size, -half_grid_size, half_grid_size)
        };

        Debug.Log("flag 11");
        Debug.Log("flag 12 " + density_field[0, 0, 0]);
        for (int x = 0; x < grid_resolution; x++)
        {
            for (int y = 0; y < grid_resolution; y++)
            {
                for (int z = 0; z < grid_resolution; z++)
                {
                    bool root_inside = density_field[x, y, z] >= density_cut_off;
                    Vector3 root_positon = GetPositionFromGrid(new Vector3Int(x, y, z));

                    if (x < grid_resolution - 1 && (density_field[x + 1, y, z] >= density_cut_off != root_inside))
                    {
                        int vertices_offset = vertices.Count;
                        vertices.Add(GetSurfacePosition(new Vector3Int(x, y, z - 1)));
                        vertices.Add(GetSurfacePosition(new Vector3Int(x, y, z)));
                        vertices.Add(GetSurfacePosition(new Vector3Int(x, y - 1, z)));
                        vertices.Add(GetSurfacePosition(new Vector3Int(x, y - 1, z - 1)));

                        if (root_inside)
                        {
                            triangles.Add(vertices_offset);
                            triangles.Add(vertices_offset + 1);
                            triangles.Add(vertices_offset + 2);

                            triangles.Add(vertices_offset + 2);
                            triangles.Add(vertices_offset + 3);
                            triangles.Add(vertices_offset);
                        } else
                        {
                            triangles.Add(vertices_offset + 2);
                            triangles.Add(vertices_offset + 1);
                            triangles.Add(vertices_offset);

                            triangles.Add(vertices_offset);
                            triangles.Add(vertices_offset + 3);
                            triangles.Add(vertices_offset + 2);
                        }
                    }

                    if (y < grid_resolution - 1 && (density_field[x, y + 1, z] >= density_cut_off != root_inside))
                    {
                        int vertices_offset = vertices.Count;
                        vertices.Add(GetSurfacePosition(new Vector3Int(x, y, z - 1)));
                        vertices.Add(GetSurfacePosition(new Vector3Int(x - 1, y, z - 1)));
                        vertices.Add(GetSurfacePosition(new Vector3Int(x - 1, y, z)));
                        vertices.Add(GetSurfacePosition(new Vector3Int(x, y, z)));


                        if (root_inside)
                        {
                            triangles.Add(vertices_offset);
                            triangles.Add(vertices_offset + 1);
                            triangles.Add(vertices_offset + 2);

                            triangles.Add(vertices_offset + 2);
                            triangles.Add(vertices_offset + 3);
                            triangles.Add(vertices_offset);
                        }
                        else
                        {
                            triangles.Add(vertices_offset + 2);
                            triangles.Add(vertices_offset + 1);
                            triangles.Add(vertices_offset);

                            triangles.Add(vertices_offset);
                            triangles.Add(vertices_offset + 3);
                            triangles.Add(vertices_offset + 2);
                        }
                    }

                    if (z < grid_resolution - 1 && (density_field[x, y, z + 1] >= density_cut_off != root_inside))
                    {
                        int vertices_offset = vertices.Count;
                        vertices.Add(GetSurfacePosition(new Vector3Int(x, y, z)));
                        vertices.Add(GetSurfacePosition(new Vector3Int(x - 1, y, z)));
                        vertices.Add(GetSurfacePosition(new Vector3Int(x - 1, y - 1, z)));
                        vertices.Add(GetSurfacePosition(new Vector3Int(x, y - 1, z)));

                        if (root_inside)
                        {
                            triangles.Add(vertices_offset);
                            triangles.Add(vertices_offset + 1);
                            triangles.Add(vertices_offset + 2);

                            triangles.Add(vertices_offset + 2);
                            triangles.Add(vertices_offset + 3);
                            triangles.Add(vertices_offset);
                        }
                        else
                        {
                            triangles.Add(vertices_offset + 2);
                            triangles.Add(vertices_offset + 1);
                            triangles.Add(vertices_offset);

                            triangles.Add(vertices_offset);
                            triangles.Add(vertices_offset + 3);
                            triangles.Add(vertices_offset + 2);
                        }
                    }
                }
            }
        }

        Debug.Log("flag 13");
        Mesh new_mesh = new Mesh();
        new_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        new_mesh.vertices = vertices.ToArray();
        new_mesh.triangles = triangles.ToArray();
        new_mesh.RecalculateBounds();
        new_mesh.RecalculateNormals();
        new_mesh.RecalculateTangents();

        return new_mesh;
    }

    void LogEntireFucker(Transform child_transform, int depth = 0)
    {
        string padding = "";
        for (int i = 0; i <= depth; i++)
        {
            padding += '\t';
        }

        Debug.Log(string.Format("{0}name: {1}", padding, child_transform.name));

        if (depth >= 10)
        {
            return;
        }

        foreach (Transform child_child in child_transform)
        {
            LogEntireFucker(child_child, depth + 1);
        }
    }

    void LogEntireScene()
    {
        UnityEngine.SceneManagement.Scene scene = gameObject.scene;
        Debug.Log(string.Format("Scene {0}:", scene.name));
        foreach (GameObject root_child in scene.GetRootGameObjects())
        {
            LogEntireFucker(root_child.transform);
        }
    }

    float NextFloat(float min, float max)
    {
        return (float)(rand.NextDouble() * (max - min) + min);
    }

    Quaternion NextQuaternion()
    {
        return Quaternion.Euler(new Vector3(NextFloat(0f, 360f), NextFloat(0f, 360f), NextFloat(0f, 360f))); ;
    }

    Vector3 NextUnitVector()
    {
        return (NextQuaternion() * Vector3.up);
    }

    void LogQuantumConsole(string message)
    {
        try
        {
            QuantumConsole.Instance.LogToConsole(message);
        }
        catch (Exception e)
        {
            Debug.Log(string.Format("Quantum Console Failed With: {0}", e.ToString()));
        }
    }

    void Start()
    {
        mesh_filter = gameObject.GetComponent<MeshFilter>();
        Debug.Log("flag 1");
        mesh_renderer = gameObject.GetComponent<MeshRenderer>();
        Debug.Log("flag 2");
        mesh_collider = gameObject.GetComponent<MeshCollider>();
        Debug.Log("flag 3");
        mesh_renderer.material = terrain_material;

        battlespace = gameObject.GetComponentInParent<Game.Map.Battlespace>();

        LogEntireScene();
        //Game.SkirmishGameManager game_manager = GameObject.Find("_SKIRMISH GAME MANAGER_").GetComponent<Game.SkirmishGameManager>();
        int random_seed;
        try
        {
            random_seed = (int)(NetworkManager.singleton as Networking.PortableNetworkManager).LobbyInfo.LobbyID.Value;
        }
        catch (Exception e)
        {
            random_seed = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
            Debug.Log(string.Format("Getting lobby id failed with: {0}", e.ToString()));
        }
        

        Debug.Log(string.Format("Map Seed: {0}", random_seed));
        LogQuantumConsole(string.Format("Map Seed: {0}", random_seed));
        
        rand = new System.Random(random_seed);

        for (int i = 0; i < battlespace.DistributedObjectives.Length; i++)
        {
            if (i == 0)
            {
                battlespace.DistributedObjectives[i] = NextUnitVector() * NextFloat(0f, mesh_size / 2f * 0.1f);
            }
            else if (i % 2 == 1)
            {
                bool good_placement;
                do
                {
                    Vector3 random_vec = NextUnitVector();
                    float random_vec_lenght = NextFloat(mesh_size / 2f * 0.1f + 400f, mesh_size / 2f - 200f);
                    Debug.Log("random_vec: " + random_vec + " random_vec_lenght " + random_vec_lenght);
                    battlespace.DistributedObjectives[i] = random_vec * random_vec_lenght;
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

        perlin_offset = new Vector3(NextFloat(-100000f, 100000f), NextFloat(-100000f, 100000f), NextFloat(-100000f, 100000f));
        perlin_rotation = NextQuaternion();
        density_cut_off = NextFloat(0.48f, 0.5f);

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

        Mesh new_mesh = GenerateMesh();
        mesh_filter.mesh = new_mesh;
        mesh_collider.sharedMesh = new_mesh;
    }
}
