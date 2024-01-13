using System.Collections;
using System.Collections.Generic;
using System;
using QFSW.QC;
using UnityEngine;
using Mirror;

namespace Perditio
{
    public class ProceduralTerrain : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        Transform team_a_home;
        [SerializeField]
        Transform team_b_home;
        [SerializeField]
        Transform central_objective;

        [SerializeField]
        MeshFilter mesh_filter_terrain;
        [SerializeField]
        MeshRenderer mesh_renderer_terrain;
        [SerializeField]
        MeshFilter mesh_filter_tacview;
        [SerializeField]
        MeshRenderer mesh_renderer_tacview;
        [SerializeField]
        MeshCollider mesh_collider;

        [SerializeField]
        Material terrain_material;
        [SerializeField]
        Material terrain_material_performance;

        [SerializeField]
        Material tacview_material;

        [SerializeField]
        GameObject lighting;

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
        List<Vector3> scenario_interest_points = new List<Vector3>();
        Game.Map.Battlespace battlespace;
        GameObject default_light;
        LobbySettings lobby_settings;


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
            pos /= grid_resolution;

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

            foreach (Vector3 scenario_point in scenario_interest_points)
            {
                Vector3 cap_grid_position = scenario_point / mesh_size;

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
            )
            {
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
                )
                {
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

        int[,,] middle_points;

        int AppendVertex(int x, int y, int z, ref List<Vector3> vertices)
        {
            if (middle_points[x, y, z] >= 0)
            {
                return middle_points[x, y, z];
            }

            vertices.Add(GetSurfacePosition(new Vector3Int(x, y, z)));
            middle_points[x, y, z] = vertices.Count - 1;
            return vertices.Count - 1;
        }

        Mesh GenerateMesh()
        {
            Debug.Log("Perditio flag 7");
            List<Vector3> vertices = new List<Vector3>();
            Debug.Log("Perditio flag 8");
            List<int> triangles = new List<int>();

            middle_points = new int[grid_resolution, grid_resolution, grid_resolution];
            for (int x = 0; x < grid_resolution; x++)
            {
                for (int y = 0; y < grid_resolution; y++)
                {
                    for (int z = 0; z < grid_resolution; z++)
                    {
                        middle_points[x, y, z] = -1;
                    }
                }
            }

            Debug.Log("Perditio flag 9");
            float half_grid_size = (mesh_size * 0.5f) / grid_resolution;

            Debug.Log("Perditio flag 10");
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

            Debug.Log("Perditio flag 11");
            Debug.Log("Perditio flag 12 " + density_field[0, 0, 0]);
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
                            int[] quad_vertices = new int[4] {
                            AppendVertex(x, y, z - 1, ref vertices),
                            AppendVertex(x, y, z, ref vertices),
                            AppendVertex(x, y - 1, z, ref vertices),
                            AppendVertex(x, y - 1, z - 1, ref vertices)
                        };

                            if (root_inside)
                            {
                                triangles.Add(quad_vertices[0]);
                                triangles.Add(quad_vertices[1]);
                                triangles.Add(quad_vertices[2]);

                                triangles.Add(quad_vertices[2]);
                                triangles.Add(quad_vertices[3]);
                                triangles.Add(quad_vertices[0]);
                            }
                            else
                            {
                                triangles.Add(quad_vertices[2]);
                                triangles.Add(quad_vertices[1]);
                                triangles.Add(quad_vertices[0]);

                                triangles.Add(quad_vertices[0]);
                                triangles.Add(quad_vertices[3]);
                                triangles.Add(quad_vertices[2]);
                            }
                        }

                        if (y < grid_resolution - 1 && (density_field[x, y + 1, z] >= density_cut_off != root_inside))
                        {
                            int[] quad_vertices = new int[4] {
                            AppendVertex(x, y, z - 1, ref vertices),
                            AppendVertex(x - 1, y, z - 1, ref vertices),
                            AppendVertex(x - 1, y, z, ref vertices),
                            AppendVertex(x, y, z, ref vertices)
                        };

                            if (root_inside)
                            {
                                triangles.Add(quad_vertices[0]);
                                triangles.Add(quad_vertices[1]);
                                triangles.Add(quad_vertices[2]);

                                triangles.Add(quad_vertices[2]);
                                triangles.Add(quad_vertices[3]);
                                triangles.Add(quad_vertices[0]);
                            }
                            else
                            {
                                triangles.Add(quad_vertices[2]);
                                triangles.Add(quad_vertices[1]);
                                triangles.Add(quad_vertices[0]);

                                triangles.Add(quad_vertices[0]);
                                triangles.Add(quad_vertices[3]);
                                triangles.Add(quad_vertices[2]);
                            }
                        }

                        if (z < grid_resolution - 1 && (density_field[x, y, z + 1] >= density_cut_off != root_inside))
                        {
                            int[] quad_vertices = new int[4] {
                            AppendVertex(x, y, z, ref vertices),
                            AppendVertex(x - 1, y, z, ref vertices),
                            AppendVertex(x - 1, y - 1, z, ref vertices),
                            AppendVertex(x, y - 1, z, ref vertices)
                        };

                            if (root_inside)
                            {
                                triangles.Add(quad_vertices[0]);
                                triangles.Add(quad_vertices[1]);
                                triangles.Add(quad_vertices[2]);

                                triangles.Add(quad_vertices[2]);
                                triangles.Add(quad_vertices[3]);
                                triangles.Add(quad_vertices[0]);
                            }
                            else
                            {
                                triangles.Add(quad_vertices[2]);
                                triangles.Add(quad_vertices[1]);
                                triangles.Add(quad_vertices[0]);

                                triangles.Add(quad_vertices[0]);
                                triangles.Add(quad_vertices[3]);
                                triangles.Add(quad_vertices[2]);
                            }
                        }
                    }
                }
            }

            Debug.Log("Perditio flag 13");
            Mesh new_mesh = new Mesh();
            new_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            new_mesh.vertices = vertices.ToArray();
            new_mesh.triangles = triangles.ToArray();

            new_mesh.RecalculateBounds();
            new_mesh.RecalculateNormals();
            new_mesh.RecalculateTangents();

            return new_mesh;
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

        void SetupScenario()
        {
            switch (LobbySettings.instance.scenario)
            {
                case "Annihilation":
                    break;
                case "Capture The Flag":
                    scenario_interest_points.Add(team_a_home.position);
                    scenario_interest_points.Add(team_b_home.position);
                    break;
                case "Centerflag":
                    scenario_interest_points.Add(team_a_home.position);
                    scenario_interest_points.Add(team_b_home.position);
                    scenario_interest_points.Add(central_objective.position);
                    break;
                case "Control":
                case "Tug Of War":
                    for (int i = 0; i < battlespace.DistributedObjectives.Length; i++)
                    {
                        scenario_interest_points.Add(battlespace.DistributedObjectives[i]);
                    }
                    break;
                case "Station Capture":
                    scenario_interest_points.Add(team_b_home.position);
                    break;
                default:
                    Debug.Log("Perditio Unknown Scenario");
                    for (int i = 0; i < battlespace.DistributedObjectives.Length; i++)
                    {
                        scenario_interest_points.Add(battlespace.DistributedObjectives[i]);
                    }
                    break;
            }
        }

        void RandomizeScenarioPoints()
        {
            team_a_home.position += NextUnitVector() * (mesh_size / 18f);
            team_b_home.position = -team_a_home.position;
            //central_objective.position += NextUnitVector() * 100f;

            for (int i = 0; i < battlespace.DistributedObjectives.Length; i++)
            {
                if (i == 0)
                {
                    battlespace.DistributedObjectives[i] = new Vector3(NextFloat(-mesh_size / 2f * 0.5f, mesh_size / 2f * 0.5f), NextFloat(-mesh_size / 2f * 0.5f, mesh_size / 2f * 0.5f), 0f);
                }
                else if (i % 2 == 1)
                {
                    bool good_placement;
                    do
                    {
                        Vector3 random_vec = NextUnitVector();
                        float random_vec_lenght = NextFloat(mesh_size / 2f * 0.1f + mesh_size / 4.5f, mesh_size / 2f - mesh_size / 9f);
                        battlespace.DistributedObjectives[i] = random_vec * random_vec_lenght;
                        good_placement = true;
                        for (int k = 0; k < i; k++)
                        {
                            float minimum_caps_distance = 700f;
                            if (
                                Vector3.Distance(battlespace.DistributedObjectives[k], battlespace.DistributedObjectives[i]) <= minimum_caps_distance ||
                                Vector3.Distance(battlespace.DistributedObjectives[k], -battlespace.DistributedObjectives[i]) <= minimum_caps_distance
                            )
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
        }

        void SetupDensityField()
        {
            density_field = new float[grid_resolution, grid_resolution, grid_resolution];

            perlin_offset = new Vector3(NextFloat(-100000f, 100000f), NextFloat(-100000f, 100000f), NextFloat(-100000f, 100000f));
            perlin_rotation = NextQuaternion();

            switch (LobbySettings.instance.terrain_density)
            { 
                case TerrainDensity.High:
                    density_cut_off = NextFloat(0.48f, 0.48f);
                    break;
                case TerrainDensity.Medium:
                    density_cut_off = NextFloat(0.49f, 0.49f);
                    break;
                case TerrainDensity.Low:
                    density_cut_off = NextFloat(0.50f, 0.50f);
                    break;
                case TerrainDensity.Random:
                default:
                    density_cut_off = NextFloat(0.48f, 0.50f);
                    break;
            }
            
            Debug.Log(string.Format("Perditio: density_cut_off: {0}", density_cut_off));

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
        }

        void Start()
        {
            Debug.Log("Perditio Starte");
            mesh_renderer_terrain.material = terrain_material;
            mesh_renderer_tacview.material = tacview_material;
            battlespace = gameObject.GetComponentInParent<Game.Map.Battlespace>();

            if (LobbySettings.instance == null)
            {
                LobbySettings.instance = new LobbySettings("Control", TerrainDensity.Random);
            }

            //lobby_settings = LobbySettings.LoadLobbySettings();
            
            Utils.LogQuantumConsole($"Perditio scenario: {LobbySettings.instance.scenario} density: {LobbySettings.instance.terrain_density}");

            //LogEntireScene();

            int random_seed;
            try
            {
                random_seed = (int)(NetworkManager.singleton as Networking.PortableNetworkManager).LobbyInfo.LobbyID.Value;
            }
            catch (Exception e)
            {
                random_seed = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                Debug.Log(string.Format("Perditio Getting lobby id failed with: {0}", e.ToString()));
            }


            Debug.Log(string.Format("Perditio Map Seed: {0}", random_seed));
            Utils.LogQuantumConsole(string.Format("Map Seed: {0}", random_seed));

            rand = new System.Random(random_seed);

            RandomizeScenarioPoints();
            SetupScenario();
            SetupDensityField();

            Mesh new_mesh = GenerateMesh();
            mesh_filter_terrain.mesh = new_mesh;
            mesh_filter_tacview.mesh = new_mesh;
            mesh_collider.sharedMesh = new_mesh;

            lighting.GetComponent<Transform>().rotation = NextQuaternion();

            try
            {
                Game.Map.SpacePartitioner space_partitioner = GameObject.Find("_SPACE PARTITIONER_").GetComponent<Game.Map.SpacePartitioner>();
                space_partitioner.Editor_Build();
                space_partitioner.Editor_BuildGraph();
            }
            catch (Exception e)
            {
                Debug.Log(string.Format("Perditio Finding Space Partitioner Failed With: {0}", e.ToString()));
            }

            try
            {
                default_light = GameObject.Find("Default Skirmish Map Lighting");
                lighting.SetActive(true);
                default_light.SetActive(false);
            }
            catch (Exception e)
            {
                Debug.Log(string.Format("Perditio Finding Default Skirmish Map Lighting Failed With: {0}", e.ToString()));
            }
        }

        bool performance_mode = false;
        bool default_lighting_mode = false;

        void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Comma))
            {
                if (performance_mode)
                {
                    Debug.Log("Perditio Performance Mode Disabled");
                    mesh_renderer_terrain.material = terrain_material;
                    performance_mode = false;
                }
                else
                {
                    Debug.Log("Perditio Performance Mode Enabled");
                    performance_mode = true;
                    mesh_renderer_terrain.material = terrain_material_performance;
                }
            }

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Period))
            {
                if (default_lighting_mode)
                {
                    Debug.Log("Perditio Default Lighting Mode Disabled");
                    default_lighting_mode = false;

                    lighting.SetActive(true);
                    default_light.SetActive(false);
                }
                else
                {
                    Debug.Log("Perditio Default Lighting Mode Enabled");

                    default_lighting_mode = true;

                    lighting.SetActive(false);
                    default_light.SetActive(true);
                }
            }
        }
    }
}
