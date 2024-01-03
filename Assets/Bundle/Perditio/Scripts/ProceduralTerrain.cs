using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ProceduralTerrain : NetworkBehaviour
{
    MeshFilter mesh_filter;
    MeshRenderer mesh_renderer;
    MeshCollider mesh_collider;

    [HideInInspector]
    public Game.Map.Battlespace battlespace;
    [HideInInspector]
    public Material terrain_material;

    [HideInInspector]
    public int grid_resolution = 100;
    [HideInInspector]
    public float mesh_size = 100f;
    [HideInInspector]
    public float density_cut_off = 0.5f;

    [HideInInspector]
    public float[,,] density_field;

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
        density_field = new_value;

        if (mesh_filter.mesh)
        {
            Destroy(mesh_filter.mesh);
        }

        Mesh new_mesh = GenerateMesh();
        mesh_filter.mesh = new_mesh;
        mesh_collider.sharedMesh = new_mesh;
    }
    */

    void Start()
    {
        if (!NetworkServer.active)
        {
            Debug.Log("You are not server");
            return;
        }

        Debug.Log("Terrain isServer: " + isServer + " isServerOnly: " + isServerOnly + " isClient: " + isClient + " isClientOnly: " + isClientOnly + " isLocalPlayer: " + isLocalPlayer);

        mesh_filter = gameObject.GetComponent<MeshFilter>();
        mesh_renderer = gameObject.GetComponent<MeshRenderer>();
        mesh_collider = gameObject.GetComponent<MeshCollider>();

        mesh_renderer.material = terrain_material;

        Mesh new_mesh = GenerateMesh();
        mesh_filter.mesh = new_mesh;
        mesh_collider.sharedMesh = new_mesh;

        Debug.Log("Density sample: " + density_field[32, 32, 32]);
    }
}
