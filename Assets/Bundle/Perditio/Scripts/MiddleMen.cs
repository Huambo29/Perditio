using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MiddleMen : NetworkBehaviour
{
    [SerializeField]
    GameObject procedural_terrain_prefab;

    public bool test_sync = false;

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("MiddleMen OnStartServer");

        Debug.Log("test_sync " + test_sync);

        GameObject procedural_terrain = Instantiate(procedural_terrain_prefab);

        ProceduralTerrain procedural_terrain_component = procedural_terrain.GetComponent<ProceduralTerrain>();
        procedural_terrain_component.test_sync = !procedural_terrain_component.test_sync;

        NetworkServer.Spawn(procedural_terrain);  
    }

    void Start()
    {
        Debug.Log("MiddleMen isServer: " + isServer + " isServerOnly: " + isServerOnly + " isClient: " + isClient + " isClientOnly: " + isClientOnly + " isLocalPlayer: " + isLocalPlayer);
    }
}
