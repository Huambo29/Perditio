using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public struct ProceduralTerrainSettings
{
    public int grid_resolution;
    public float mesh_size;
    public float density_cut_off;
    public float[,,] density_field;
}

public class ProceduralTerrain : NetworkBehaviour
{
    MeshFilter mesh_filter;
    MeshRenderer mesh_renderer;
    MeshCollider mesh_collider;

    public Material terrain_material;

    //[SyncVar(hook = nameof(TerrainChanged))]
    public ProceduralTerrainSettings terrain_settings;

    [SyncVar(hook = nameof(TestChanged))]
    public bool test_sync = false;

    void TestChanged(bool old_value, bool new_value)
    {
        Debug.Log("test_sync Changed");
    }

    //void TerrainChanged(ProceduralTerrainSettings old_value, ProceduralTerrainSettings new_value)
    //{
    //    Debug.Log("Terrain Changed");
    //}

    Vector3 GetPositionFromGrid(Vector3Int root_position)
    {
        return (((Vector3)root_position / terrain_settings.grid_resolution) - Vector3.one * 0.5f) * terrain_settings.mesh_size;
    }

    Vector3 GetSurfacePosition(Vector3Int root_position)
    {
        if (
            root_position.x < 1 || root_position.x >= (terrain_settings.grid_resolution - 1) ||
            root_position.y < 1 || root_position.y >= (terrain_settings.grid_resolution - 1) ||
            root_position.z < 1 || root_position.z >= (terrain_settings.grid_resolution - 1)
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
            float density_sample_a = terrain_settings.density_field[position_a_int.x, position_a_int.y, position_a_int.z];

            Vector3Int position_b_int = root_position + edges[i, 1];
            Vector3 position_b = GetPositionFromGrid(position_b_int);
            float density_sample_b = terrain_settings.density_field[position_b_int.x, position_b_int.y, position_b_int.z];

            if (
                Mathf.Min(density_sample_a, density_sample_b) < terrain_settings.density_cut_off &&
                Mathf.Max(density_sample_a, density_sample_b) >= terrain_settings.density_cut_off
            ) {
                surface_edges_n++;
                float lerp_factor = (terrain_settings.density_cut_off - density_sample_a) / (density_sample_b - density_sample_a);
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
        float half_grid_size = (terrain_settings.mesh_size * 0.5f) / terrain_settings.grid_resolution;

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
        Debug.Log("flag 12 " + terrain_settings.density_field[0, 0, 0]);
        for (int x = 0; x < terrain_settings.grid_resolution; x++)
        {
            for (int y = 0; y < terrain_settings.grid_resolution; y++)
            {
                for (int z = 0; z < terrain_settings.grid_resolution; z++)
                {
                    bool root_inside = terrain_settings.density_field[x, y, z] >= terrain_settings.density_cut_off;
                    Vector3 root_positon = GetPositionFromGrid(new Vector3Int(x, y, z));

                    if (x < terrain_settings.grid_resolution - 1 && (terrain_settings.density_field[x + 1, y, z] >= terrain_settings.density_cut_off != root_inside))
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

                    if (y < terrain_settings.grid_resolution - 1 && (terrain_settings.density_field[x, y + 1, z] >= terrain_settings.density_cut_off != root_inside))
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

                    if (z < terrain_settings.grid_resolution - 1 && (terrain_settings.density_field[x, y, z + 1] >= terrain_settings.density_cut_off != root_inside))
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

    /*
    private void UpdateMesh(
        float[,,] old_value,
        float[,,] new_value
    ) {
        Debug.Log("UpdateMesh");
        terrain_settings.density_field = new_value;

        if (mesh_filter.mesh)
        {
            Destroy(mesh_filter.mesh);
        }

        Debug.Log("flag 4");
        Mesh new_mesh = GenerateMesh();
        Debug.Log("flag 5");
        mesh_filter.mesh = new_mesh;
        Debug.Log("flag 6");
        mesh_collider.sharedMesh = new_mesh;
    }
    */

    void Start()
    {
        Debug.Log("Terrain isServer: " + isServer + " isServerOnly: " + isServerOnly + " isClient: " + isClient + " isClientOnly: " + isClientOnly + " isLocalPlayer: " + isLocalPlayer);

        mesh_filter = gameObject.GetComponent<MeshFilter>();
        Debug.Log("flag 1");
        mesh_renderer = gameObject.GetComponent<MeshRenderer>();
        Debug.Log("flag 2");
        mesh_collider = gameObject.GetComponent<MeshCollider>();

        //Debug.Log("grid_resolution: " + terrain_settings.grid_resolution);
        //Debug.Log("mesh_size: " + terrain_settings.mesh_size);
        //Debug.Log("density_cut_off: " + terrain_settings.density_cut_off);        

        Debug.Log("flag 3");
        //mesh_renderer.material = terrain_settings.terrain_material;

        if (isServer)
        {
            //UpdateMesh(terrain_settings.density_field, terrain_settings.density_field);
        }

        

        //Debug.Log("Density sample: " + terrain_settings.density_field[32, 32, 32]);
    }
}
