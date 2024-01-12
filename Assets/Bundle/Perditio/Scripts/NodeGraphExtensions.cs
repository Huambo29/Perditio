using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using XNode;

public static class NodeGraphExtensions
{
    public static T InsertNode<T>(this NodeGraph graph, int index) where T : Node
    {
        return graph.InsertNode(index, typeof(T)) as T;
    }

    public static Node InsertNode(this NodeGraph graph, int index, Type type)
    {
        Node.graphHotfix = graph;
        Node node = ScriptableObject.CreateInstance(type) as Node;
        node.graph = graph;
        graph.nodes.Insert(index, node);
        return node;
    }
}
