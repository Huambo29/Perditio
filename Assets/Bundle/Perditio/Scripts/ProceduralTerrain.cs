using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ProceduralTerrain : NetworkBehaviour
{
    [SerializeField]
    bool debug = false;

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

    [SyncVar(hook = "UpdateMesh")]
    float[,,] density_field;

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
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        float half_grid_size = (mesh_size * 0.5f) / grid_resolution;

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

        //new_mesh.vertices = new Vector3[] {
        //    new Vector3(100f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0f, 100f, 0f),
        //    new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 100f), new Vector3(0f, 100f, 0f),
        //    new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), new Vector3(0f, 0f, 100f),
        //    new Vector3(100f, 0f, 0f), new Vector3(0f, 100f, 0f), new Vector3(0f, 0f, 100f)
        //
        //};
        //new_mesh.triangles = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

        Mesh new_mesh = new Mesh();
        new_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        new_mesh.vertices = vertices.ToArray();
        new_mesh.triangles = triangles.ToArray();
        new_mesh.RecalculateBounds();
        new_mesh.RecalculateNormals();
        new_mesh.RecalculateTangents();

        return new_mesh;
    }

    [SerializeField]
    float fractal_global_scale = 200f;
    [SerializeField]
    int Iterations = 14;
    [SerializeField]
    float fractal_scale = 2f;
    [SerializeField]
    float minRadius2 = 0.44f;
    [SerializeField]
    float fixedRadius2 = 5.48f;
    [SerializeField]
    float foldingLimit = 1.33f;
    [SerializeField]
    float Repetysion = 0f;
    [SerializeField]
    float fractal_minimum = 0.01f;
    [SerializeField]
    Vector3 fractal_offset;
    [SerializeField]
    Quaternion fractal_rotation;

    void sphereFold(ref Vector3 z, ref float dz)
    {
        float r2 = Vector3.Dot(z, z);
        if (r2 < minRadius2)
        {
            // linear inner scaling
            float temp = (fixedRadius2 / minRadius2);
            z *= temp;
            dz *= temp;
        }
        else if (r2 < fixedRadius2)
        {
            // this is the actual sphere inversion
            float temp = (fixedRadius2 / r2);
            z *= temp;
            dz *= temp;
        }
    }

    void boxFold(ref Vector3 z, ref float dz)
    {
        z.x = Mathf.Clamp(z.x, -foldingLimit, foldingLimit) * 2f - z.x;
        z.y = Mathf.Clamp(z.y, -foldingLimit, foldingLimit) * 2f - z.y;
        z.z = Mathf.Clamp(z.z, -foldingLimit, foldingLimit) * 2f - z.z;
    }


    float FractalDistanceFunction(Vector3 p)
    {
        p /= fractal_global_scale;

        if (Repetysion != 0)
        {
            p.x = p.x % Repetysion;
            p.y = p.y % Repetysion;
            p.y = p.z % Repetysion;

            p.x -= Mathf.Sign(p.x) * Repetysion / 2;
            p.y -= Mathf.Sign(p.y) * Repetysion / 2;
            p.z -= Mathf.Sign(p.z) * Repetysion / 2;
        }

        Vector3 offset = p;
        float dr = 1f;
        for (int n = 0; n < Iterations; n++)
        {
            boxFold(ref p, ref dr); 
            sphereFold(ref p, ref dr);

            p = fractal_scale * p + offset;
            dr = dr * Mathf.Abs(fractal_scale) + 1f;
        }
        float r = p.magnitude;
        return r / Mathf.Abs(dr) * fractal_global_scale;
    }

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

    float CalculateDensity(int x, int y, int z)
    {
        float center_distance = Mathf.Sqrt(Mathf.Pow((float)x / grid_resolution - 0.5f, 2f) + Mathf.Pow((float)y / grid_resolution - 0.5f, 2f) + Mathf.Pow((float)z / grid_resolution - 0.5f, 2f)) * mesh_size;
        float drop_off = ((Mathf.Max(drop_off_start, Mathf.Min(center_distance, drop_off_end)) - drop_off_start) / (drop_off_end - drop_off_start));

        return PerlinOctaves((perlin_rotation * new Vector3(x, y, z) + perlin_offset) * 1.41421356237f / perlin_scale) - Mathf.Pow(drop_off, 2f);

        // return 1f - FractalDistanceFunction(fractal_rotation * new Vector3((float)x / grid_resolution - 0.5f, (float)y / grid_resolution - 0.5f, (float)z / grid_resolution - 0.5f) + fractal_offset) / mesh_size;
    }

    public override void OnStartServer()
    {
        Debug.Log("OnStartServer " + gameObject.GetInstanceID());
        base.OnStartServer();
        Debug.Log("OnStartServer2");
        float[,,] new_density_field = new float[grid_resolution, grid_resolution, grid_resolution];

        for (int x = 0; x < grid_resolution; x++)
        {
            for (int y = 0; y < grid_resolution; y++)
            {
                for (int z = 0; z < grid_resolution; z++)
                {
                    new_density_field[x, y, z] = CalculateDensity(x, y, z);

                    if (
                        x == 0 || x == grid_resolution - 1 ||
                        y == 0 || y == grid_resolution - 1 ||
                        z == 0 || z == grid_resolution - 1
                    )
                    {
                        new_density_field[x, y, z] = 0f;
                    }
                }
            }
        }

        density_field = new_density_field;
    }

    MeshFilter mesh_filter;
    MeshRenderer mesh_renderer;
    MeshCollider mesh_collider;

    private void UpdateMesh(
        float[,,] old_value,
        float[,,] new_value
    ) {
        Debug.Log("UpdateMesh");
        return;

        if (mesh_filter.mesh)
        {
            Destroy(mesh_filter.mesh);
        }
        
        Mesh new_mesh = GenerateMesh();
        mesh_filter.mesh = new_mesh;
        mesh_collider.sharedMesh = new_mesh;
    }

    void Start()
    {
        perlin_offset = new Vector3(Random.Range(-100000f, 100000f), Random.Range(-100000f, 100000f), Random.Range(-100000f, 100000f));
        perlin_rotation = Random.rotation;
        density_cut_off = 0.5f - 0.02f * Random.value;

        NetworkServer.Spawn(gameObject);
        Debug.Log("Starte");
        Debug.Log("isServer: " + isServer + " isServerOnly: " + isServerOnly + " isClient: " + isClient + " isClientOnly: " + isClientOnly + " isLocalPlayer: " + isLocalPlayer);

        mesh_filter = gameObject.GetComponent<MeshFilter>();
        mesh_renderer = gameObject.GetComponent<MeshRenderer>();
        mesh_collider = gameObject.GetComponent<MeshCollider>();

        mesh_renderer.material = terrain_material;

        if (isServer)
        {
            OnStartServer();
        }

        OnStartServer();

        Mesh new_mesh = GenerateMesh();
        mesh_filter.mesh = new_mesh;
        mesh_collider.sharedMesh = new_mesh;
    }

    private void Update()
    {
        if (!this.isServer)
        {
            return;
        } else
        {
            Debug.Log("I am Server" + gameObject.GetInstanceID());
        }
    }
}
