using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SmollExample : NetworkBehaviour
{
    [SyncVar(hook = nameof(PrintSomething))]
    public bool something = false;

    public override void OnStartServer()
    {
        // base.OnStartServer();
        Debug.Log("Server doing stuff");

        something = true;
    }

    void PrintSomething(bool old_value, bool new_value)
    {
        Debug.Log("Print Something");
    }
}
