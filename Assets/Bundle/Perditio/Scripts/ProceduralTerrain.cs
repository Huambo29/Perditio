using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using Networking;
using Game;
using Steamworks;
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
        Transform team_a_spawns;
        [SerializeField]
        Transform team_b_spawns;
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

        [Header("Terrain Settings")]
        [SerializeField]
        int grid_resolution = 128;

        float mesh_size;
        float drop_off_start;
        float drop_off_end;
        float caps_min_distance;

        [SerializeField]
        int octaves = 10;
        [SerializeField]
        float perlin_scale = 40f;
        [SerializeField]
        float octaves_persistence = 0.8f;
        [SerializeField]
        float octaves_lacunarity = 1.25f;

        

        [Header("Skybox Settings")]
        [SerializeField]
        ComputeShader compute_shader;
        [SerializeField]
        int skybox_resolution = 2048;
        [SerializeField]
        RenderTexture[] skybox_fblrud;
        [SerializeField]
        Texture2D[] new_textures;

        Cubemap skybox_cubemap;

        float density_cut_off;
        Vector3 perlin_offset;
        Quaternion perlin_rotation;

        float[,,] density_field;
        List<Quaternion> octaves_rotations;
        List<Vector3> scenario_interest_points = new List<Vector3>();
        Game.Map.Battlespace battlespace;
        GameObject default_light;
        LobbySettings lobby_settings;

        bool is_dedicated_server;


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
                result += amplitude * SamplePerlinNoise(octaves_rotations[i] * (pos * frequency));
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

        float CalculateDensity(Vector3Int position, bool omit_caps = false)
        {
            float center_distance = Mathf.Sqrt(Mathf.Pow((float)position.x / grid_resolution - 0.5f, 2f) + Mathf.Pow((float)position.y / grid_resolution - 0.5f, 2f) + Mathf.Pow((float)position.z / grid_resolution - 0.5f, 2f)) * mesh_size;
            float drop_off = ((Mathf.Max(drop_off_start, Mathf.Min(center_distance, drop_off_end)) - drop_off_start) / (drop_off_end - drop_off_start));

            return PerlinOctaves((perlin_rotation * ((Vector3)position) + perlin_offset) * 1.41421356237f / perlin_scale) - Mathf.Pow(drop_off, 2f) + interest_points_drop_off((Vector3)position) * (omit_caps ? 0f : 1f);

            // return 1f - FractalDistanceFunction(fractal_rotation * new Vector3((float)x / grid_resolution - 0.5f, (float)y / grid_resolution - 0.5f, (float)z / grid_resolution - 0.5f) + fractal_offset) / mesh_size;
        }

        Vector3 GetPositionFromGrid(Vector3Int root_position)
        {
            return (((Vector3)root_position / grid_resolution) - Vector3.one * 0.5f) * mesh_size;
        }

        Vector3Int GetPositionToGrid(Vector3 root_position)
        {
            return Vector3Int.RoundToInt(((root_position / mesh_size) + Vector3.one * 0.5f) * grid_resolution);
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

        void GenerateSkybox()
        {
            if (!is_dedicated_server)
            {
                Utility.Skybox skybox = battlespace.Skybox;
                skybox_cubemap = skybox.HDRI;
                skybox_fblrud = new RenderTexture[6];

                for (int i = 0; i < 6; i++)
                {
                    skybox_fblrud[i] = new RenderTexture(skybox_resolution, skybox_resolution, 24, RenderTextureFormat.ARGBFloat);
                    skybox_fblrud[i].enableRandomWrite = true;
                    skybox_fblrud[i].Create();
                }
            }

            compute_shader.SetFloat("InputResolution", (float)skybox_resolution);

            compute_shader.SetFloat("InputSkyboxDistance", NextFloat(0.9f, 1.1f));

            compute_shader.SetFloat("InputIntensityFirst", NextFloat(0.9f, 1.1f));
            compute_shader.SetFloat("InputIntensitySecond", NextFloat(0.9f, 1.1f));
            compute_shader.SetFloat("InputIntensityThird", NextFloat(0.9f, 1.1f));

            compute_shader.SetVector("InputOffsetFirst", new Vector3(NextFloat(-10000f, 10000f), NextFloat(-10000f, 10000f), NextFloat(-10000f, 10000f)));
            compute_shader.SetVector("InputOffsetSecond", new Vector3(NextFloat(-10000f, 10000f), NextFloat(-10000f, 10000f), NextFloat(-10000f, 10000f)));
            compute_shader.SetVector("InputOffsetThird", new Vector3(NextFloat(-10000f, 10000f), NextFloat(-10000f, 10000f), NextFloat(-10000f, 10000f)));

            compute_shader.SetFloat("InputFalloff", NextFloat(3.0f, 5.0f));

            compute_shader.SetInt("InputColorOctaves", Mathf.FloorToInt(NextFloat(1.0f, 3.5f)));

            compute_shader.SetVector("InputColorFirst", new Vector3(NextFloat(0.0f, 1.0f), NextFloat(0.0f, 1.0f), NextFloat(0.0f, 1.0f)));
            compute_shader.SetVector("InputColorSecond", new Vector3(NextFloat(0.0f, 1.0f), NextFloat(0.0f, 1.0f), NextFloat(0.0f, 1.0f)));
            compute_shader.SetVector("InputColorThird", new Vector3(NextFloat(0.0f, 1.0f), NextFloat(0.0f, 1.0f), NextFloat(0.0f, 1.0f)));

            compute_shader.SetFloat("InputStarsDensity", 400f);
            compute_shader.SetFloat("InputStarsCutoff", 0.8f);

            if (is_dedicated_server)
            {
                return;
            }

            compute_shader.SetTexture(0, "ResultF", skybox_fblrud[0]);
            compute_shader.SetTexture(0, "ResultB", skybox_fblrud[1]);
            compute_shader.SetTexture(0, "ResultL", skybox_fblrud[2]);
            compute_shader.SetTexture(0, "ResultR", skybox_fblrud[3]);
            compute_shader.SetTexture(0, "ResultU", skybox_fblrud[4]);
            compute_shader.SetTexture(0, "ResultD", skybox_fblrud[5]);

            compute_shader.Dispatch(0, skybox_resolution / 8, skybox_resolution / 8, 1);

            RenderTexture old_render_texture = RenderTexture.active;
            Texture2D new_texture = new Texture2D(skybox_resolution, skybox_resolution, TextureFormat.RGBAFloat, false);
            
			RenderTexture.active = skybox_fblrud[0];
            new_texture.ReadPixels(new Rect(0, 0, skybox_resolution, skybox_resolution), 0, 0);
            new_texture.Apply();
            skybox_cubemap.SetPixels(new_texture.GetPixels(), CubemapFace.PositiveZ);

            RenderTexture.active = skybox_fblrud[1];
            new_texture.ReadPixels(new Rect(0, 0, skybox_resolution, skybox_resolution), 0, 0);
            new_texture.Apply();
            skybox_cubemap.SetPixels(new_texture.GetPixels(), CubemapFace.NegativeZ);

            RenderTexture.active = skybox_fblrud[2];
            new_texture.ReadPixels(new Rect(0, 0, skybox_resolution, skybox_resolution), 0, 0);
            new_texture.Apply();
            skybox_cubemap.SetPixels(new_texture.GetPixels(), CubemapFace.PositiveX);

            RenderTexture.active = skybox_fblrud[3];
            new_texture.ReadPixels(new Rect(0, 0, skybox_resolution, skybox_resolution), 0, 0);
            new_texture.Apply();
            skybox_cubemap.SetPixels(new_texture.GetPixels(), CubemapFace.NegativeX);

            RenderTexture.active = skybox_fblrud[4];
            new_texture.ReadPixels(new Rect(0, 0, skybox_resolution, skybox_resolution), 0, 0);
            new_texture.Apply();
            skybox_cubemap.SetPixels(new_texture.GetPixels(), CubemapFace.PositiveY);

            RenderTexture.active = skybox_fblrud[5];
            new_texture.ReadPixels(new Rect(0, 0, skybox_resolution, skybox_resolution), 0, 0);
            new_texture.Apply();
            skybox_cubemap.SetPixels(new_texture.GetPixels(), CubemapFace.NegativeY);

            skybox_cubemap.Apply();
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

        bool IsPointOutside(Vector3 position)
        {
            return
                CalculateDensity(GetPositionToGrid(position), true) < density_cut_off ||
                CalculateDensity(GetPositionToGrid(position + Vector3.right * 100f), true) < density_cut_off ||
                CalculateDensity(GetPositionToGrid(position + Vector3.left * 100f), true) < density_cut_off ||
                CalculateDensity(GetPositionToGrid(position + Vector3.up * 100f), true) < density_cut_off ||
                CalculateDensity(GetPositionToGrid(position + Vector3.down * 100f), true) < density_cut_off ||
                CalculateDensity(GetPositionToGrid(position + Vector3.forward * 100f), true) < density_cut_off ||
                CalculateDensity(GetPositionToGrid(position + Vector3.back * 100f), true) < density_cut_off;
        }

        void RandomizeScenarioPoints()
        {
            team_a_home.position += NextUnitVector() * (mesh_size / 18f);
            team_b_home.position = -team_a_home.position;
            //central_objective.position += NextUnitVector() * 100f;

            bool even_caps = LobbySettings.instance.caps_number % 2 == 0;

            Vector3[] cap_points = new Vector3[LobbySettings.instance.caps_number];

            for (int i = 0; i < cap_points.Length; i++)
            {
                if (i == 0 && !even_caps)
                {
                    int tries = 0;
                    while (tries < 10000)
                    {
                        tries++;
                        cap_points[i] = new Vector3(NextFloat(-mesh_size / 2f * 0.5f, mesh_size / 2f * 0.5f), NextFloat(-mesh_size / 2f * 0.5f, mesh_size / 2f * 0.5f), 0f);

                        if (IsPointOutside(cap_points[i]))
                        {
                            Debug.Log($"Perditio found good A cap point. tries: {tries}");
                            break;
                        }
                        else
                        {
                            Debug.Log($"Perditio too many A cap point tries. tries: {tries}");
                        }
                    } 
                }
                else if ((i % 2 == 1 && !even_caps) || (i % 2 == 0 && even_caps))
                {
                    bool good_placement;
                    int tries = 0;
                    do
                    {
                        tries++;
                        if (tries >= 100000)
                        {
                            Debug.Log($"Perditio too many cap points tries. only {i} good");
                            Utils.SetPrivateValue(battlespace, "_distributedObjectivePositions", cap_points.Take(i).ToArray());
                            return;
                        }

                        Vector3 random_vec = NextUnitVector();
                        float random_vec_lenght = NextFloat(caps_min_distance * 0.5f, LobbySettings.instance.radius - 200f);
                        cap_points[i] = random_vec * random_vec_lenght;

                        good_placement = IsPointOutside(cap_points[i]) && IsPointOutside(-cap_points[i]);

                        for (int k = 0; k < i; k++)
                        {
                            if (
                                Vector3.Distance(cap_points[k], cap_points[i]) <= caps_min_distance ||
                                Vector3.Distance(cap_points[k], -cap_points[i]) <= caps_min_distance ||
                                !good_placement 
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
                    cap_points[i] = -cap_points[i - 1];
                }
            }

            Utils.SetPrivateValue(battlespace, "_distributedObjectivePositions", cap_points.Take(LobbySettings.instance.caps_number).ToArray());
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

            octaves_rotations = new List<Quaternion>();
            for (int i = 0; i < octaves; i++)
            {
                octaves_rotations.Add(NextQuaternion());
            }

            Debug.Log(string.Format("Perditio: density_cut_off: {0}", density_cut_off));
        }

        void CalculateDensityField()
        {
            for (int x = 0; x < grid_resolution; x++)
            {
                for (int y = 0; y < grid_resolution; y++)
                {
                    for (int z = 0; z < grid_resolution; z++)
                    {
                        density_field[x, y, z] = CalculateDensity(new Vector3Int(x, y, z));

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

        void Awake()
        {
            try
            {
                SkirmishGameManager.Instance.CameraRig.gameObject.GetComponent<VisualEffect>().enabled = false;
            }
            catch (Exception e)
            {
                Debug.Log(string.Format("Perditio Disabling default stars Failed With: {0}", e.ToString()));
            }
        }

        float start_time = 0f;
        void MeasureTime(string name)
        {
            if (start_time == 0f)
            {
                start_time = Time.realtimeSinceStartup;
            }

            Debug.Log($"Perditio {name}: {Time.realtimeSinceStartup - start_time} seconds");
            start_time = Time.realtimeSinceStartup;
        }

        void Start()
        {
            Debug.Log("Perditio Starte");
            start_time = Time.realtimeSinceStartup;

            try
            {
                SkirmishGameManager game_manager = GameObject.Find("_SKIRMISH GAME MANAGER_").GetComponent<SkirmishGameManager>();
                is_dedicated_server = game_manager.IsDedicatedServer;
            }
            catch (Exception e)
            {
                is_dedicated_server = false;
            }
                

            if (!is_dedicated_server)
            {
                mesh_renderer_terrain.material = terrain_material;
                mesh_renderer_tacview.material = tacview_material;
            }  

            Utils.LogQuantumConsole($"Perditio scenario: {LobbySettings.instance.scenario}");
            Utils.LogQuantumConsole($"Perditio density: {LobbySettings.instance.terrain_density}");
            Utils.LogQuantumConsole($"Perditio roughness: {LobbySettings.instance.terrain_roughness}");
            Utils.LogQuantumConsole($"Perditio radius: {LobbySettings.instance.radius}");
            // Utils.LogQuantumConsole($"Perditio team size: {LobbySettings.instance.team_size}");
            Utils.LogQuantumConsole($"Perditio seed: {LobbySettings.instance.seed}");

            battlespace = gameObject.GetComponentInParent<Game.Map.Battlespace>();

            team_a_spawns.position = new Vector3(0, 0, -LobbySettings.instance.radius);
            team_b_spawns.position = new Vector3(0, 0, LobbySettings.instance.radius);

            team_a_home.position = new Vector3(0, 0, -LobbySettings.instance.radius * 0.5f);
            team_b_home.position = new Vector3(0, 0, LobbySettings.instance.radius * 0.5f);

            mesh_size = 2f * LobbySettings.instance.radius;
            drop_off_start = 0.7777f * LobbySettings.instance.radius;
            drop_off_end = 1.2222f * LobbySettings.instance.radius;

            caps_min_distance = 0.7777f * LobbySettings.instance.radius;

            switch (LobbySettings.instance.terrain_roughness)
            {
                case TerrainRoughness.VeryLow:
                    octaves_persistence = 0.6f;
                    break;
                case TerrainRoughness.Low:
                    octaves_persistence = 0.7f;
                    break;
                case TerrainRoughness.Default:
                    octaves_persistence = 0.8f;
                    break;
                case TerrainRoughness.High:
                    octaves_persistence = 0.9f;
                    break;
                case TerrainRoughness.VeryHigh:
                    octaves_persistence = 1.0f;
                    break;
            }

			MeasureTime("Init");

			rand = new System.Random(LobbySettings.instance.seed);
			string sector = ModEntryPoint.SECTOR_NAMES_WORDLIST[rand.Next() % ModEntryPoint.SECTOR_NAMES_WORDLIST.Length];
			string system = ModEntryPoint.SYSTEM_NAMES_WORDLIST[rand.Next() % ModEntryPoint.SYSTEM_NAMES_WORDLIST.Length];
			Utils.SetPrivateValue(battlespace, "_locationName", $"{sector} Sector, {system} System");
            MeasureTime("LocationName");

            rand = new System.Random(LobbySettings.instance.seed);
            GenerateSkybox();
            MeasureTime("Skybox");

            rand = new System.Random(LobbySettings.instance.seed + 2);
            SetupDensityField();
            MeasureTime("SetupDensityField");

            rand = new System.Random(LobbySettings.instance.seed + 1);
            RandomizeScenarioPoints();
            MeasureTime("RandomizeScenarioPoints");

            SetupScenario();
            MeasureTime("SetupScenario");

            CalculateDensityField();
            MeasureTime("CalculateDensityField");

            Mesh new_mesh = GenerateMesh();
            MeasureTime("GenerateMesh");

            mesh_filter_terrain.mesh = new_mesh;
            mesh_filter_tacview.mesh = new_mesh;
            mesh_collider.sharedMesh = new_mesh;
            MeasureTime("Mesh Assigment");

            try
            {
                Game.Map.SpacePartitioner space_partitioner = GameObject.Find("_SPACE PARTITIONER_").GetComponent<Game.Map.SpacePartitioner>();
                Utils.SetPrivateValue(space_partitioner, "_leafRadius", Mathf.CeilToInt(64f / 1000f * LobbySettings.instance.radius));
                Utils.SetPrivateValue(space_partitioner, "_minDepth", 1);
                Utils.SetPrivateValue(space_partitioner, "_maxDepth", 4);

                space_partitioner.Editor_Build();
                space_partitioner.Editor_BuildGraph();
            }
            catch (Exception e)
            {
                Debug.Log(string.Format("Perditio Finding Space Partitioner Failed With: {0}", e.ToString()));
            }
            MeasureTime("SpacePartitioner");

            lighting.GetComponent<Transform>().rotation = NextQuaternion();
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
            MeasureTime("Lighting");
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
