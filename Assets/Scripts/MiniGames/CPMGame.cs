using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Validates the Project 2 CPM dependency path mini-game.
/// </summary>
public class CPMGame
{
    private static readonly string[] OrderedNodeNames =
    {
        "服务器扩容",
        "数据库分库分表",
        "支付接口",
        "前端秒杀页面"
    };

    private readonly List<Vector2Int> _connections = new List<Vector2Int>();

    /// <summary>
    /// Gets the number of nodes required by the puzzle.
    /// </summary>
    public int NodeCount => OrderedNodeNames.Length;

    /// <summary>
    /// Gets the current directed connections.
    /// </summary>
    public IReadOnlyList<Vector2Int> Connections => _connections;

    /// <summary>
    /// Returns the display name of a node.
    /// </summary>
    /// <param name="index">Node index.</param>
    /// <returns>Display name for the node.</returns>
    public string GetNodeName(int index)
    {
        return index >= 0 && index < OrderedNodeNames.Length ? OrderedNodeNames[index] : string.Empty;
    }

    /// <summary>
    /// Clears all current connections.
    /// </summary>
    public void Reset()
    {
        _connections.Clear();
    }

    /// <summary>
    /// Creates or replaces a directed connection between two nodes.
    /// </summary>
    /// <param name="fromIndex">Source node index.</param>
    /// <param name="toIndex">Target node index.</param>
    /// <returns>True when the connection is valid and stored.</returns>
    public bool TrySetConnection(int fromIndex, int toIndex)
    {
        if (!IsValidNodeIndex(fromIndex) || !IsValidNodeIndex(toIndex) || fromIndex == toIndex)
        {
            return false;
        }

        RemoveOutgoingConnection(fromIndex);
        if (HasConnection(fromIndex, toIndex))
        {
            return true;
        }

        if (CreatesCycle(fromIndex, toIndex))
        {
            return false;
        }

        _connections.Add(new Vector2Int(fromIndex, toIndex));
        return true;
    }

    /// <summary>
    /// Evaluates whether the player connected the exact authored critical path.
    /// </summary>
    /// <returns>True when the path matches the required order.</returns>
    public bool IsSolved()
    {
        if (_connections.Count != OrderedNodeNames.Length - 1)
        {
            return false;
        }

        for (int index = 0; index < OrderedNodeNames.Length - 1; index += 1)
        {
            if (!HasConnection(index, index + 1))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasConnection(int fromIndex, int toIndex)
    {
        for (int index = 0; index < _connections.Count; index += 1)
        {
            if (_connections[index].x == fromIndex && _connections[index].y == toIndex)
            {
                return true;
            }
        }

        return false;
    }

    private bool CreatesCycle(int fromIndex, int toIndex)
    {
        List<int> pendingNodes = new List<int> { toIndex };
        HashSet<int> visitedNodes = new HashSet<int>();

        while (pendingNodes.Count > 0)
        {
            int currentIndex = pendingNodes[pendingNodes.Count - 1];
            pendingNodes.RemoveAt(pendingNodes.Count - 1);

            if (!visitedNodes.Add(currentIndex))
            {
                continue;
            }

            if (currentIndex == fromIndex)
            {
                return true;
            }

            for (int connectionIndex = 0; connectionIndex < _connections.Count; connectionIndex += 1)
            {
                Vector2Int connection = _connections[connectionIndex];
                if (connection.x == currentIndex)
                {
                    pendingNodes.Add(connection.y);
                }
            }
        }

        return false;
    }

    private void RemoveOutgoingConnection(int fromIndex)
    {
        for (int index = _connections.Count - 1; index >= 0; index -= 1)
        {
            if (_connections[index].x == fromIndex)
            {
                _connections.RemoveAt(index);
            }
        }
    }

    private bool IsValidNodeIndex(int index)
    {
        return index >= 0 && index < OrderedNodeNames.Length;
    }
}
