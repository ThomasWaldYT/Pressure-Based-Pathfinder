using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class AStarPathfinding : MonoBehaviour
{
    private NodePool nodePool = new NodePool();

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, bool[,] grid)
    {
        var openSet = new MinHeap<Node>();
        var closedSet = new HashSet<Vector2Int>();

        var startNode = nodePool.GetNode(start, null, 0, HeuristicCostEstimate(start, goal));
        openSet.Add(startNode);

        Vector2Int[] neighbors = new Vector2Int[8]; // Reused array for neighbors

        while (openSet.Count > 0)
        {
            var current = openSet.PopMin();

            if (closedSet.Contains(current.Position))
            {
                nodePool.ReturnNode(current);
                continue;
            }

            closedSet.Add(current.Position);

            if (current.Position == goal)
            {
                var path = ReconstructPath(current);
                // Release all nodes back to the pool
                nodePool.ReleaseAll();
                return path;
            }

            int neighborCount = GetNeighbors(current.Position, grid, neighbors);
            for (int i = 0; i < neighborCount; i++)
            {
                var neighborPos = neighbors[i];

                if (closedSet.Contains(neighborPos))
                    continue;

                int newCost = current.GCost + GetMoveCost(current.Position, neighborPos);
                var neighborNode = nodePool.GetNode(neighborPos, current, newCost, HeuristicCostEstimate(neighborPos, goal));
                openSet.Add(neighborNode);
            }
        }

        // Release all nodes back to the pool if no path is found
        nodePool.ReleaseAll();
        return null; // No path found
    }

    private int HeuristicCostEstimate(Vector2Int a, Vector2Int b)
    {
        // Using Chebyshev distance for grids with diagonal movement
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return 10 * Mathf.Max(dx, dy);
    }

    private int GetNeighbors(Vector2Int position, bool[,] grid, Vector2Int[] neighbors)
    {
        int count = 0;
        int gridSizeX = grid.GetLength(0);
        int gridSizeY = grid.GetLength(1);

        for (int i = 0; i < neighborOffsets.Length; i++)
        {
            var offset = neighborOffsets[i];
            var checkPos = position + offset;

            if (checkPos.x < 0 || checkPos.x >= gridSizeX || checkPos.y < 0 || checkPos.y >= gridSizeY)
                continue;

            if (!IsValidMove(position, checkPos, grid))
                continue;

            neighbors[count++] = checkPos;
        }

        return count;
    }

    private static readonly Vector2Int[] neighborOffsets = new Vector2Int[]
    {
        new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
        new Vector2Int(-1,  0),                    /* current */    new Vector2Int(1,  0),
        new Vector2Int(-1,  1), new Vector2Int(0,  1), new Vector2Int(1,  1)
    };

    private bool IsValidMove(Vector2Int from, Vector2Int to, bool[,] grid)
    {
        if (IsBlockedOrOutOfBounds(to.x, to.y, grid))
            return false;

        int dx = to.x - from.x;
        int dy = to.y - from.y;

        // If moving diagonally
        if (dx != 0 && dy != 0)
        {
            // Prevent cutting corners
            if (IsBlockedOrOutOfBounds(from.x + dx, from.y, grid) || IsBlockedOrOutOfBounds(from.x, from.y + dy, grid))
                return false;
        }

        return true;
    }

    private bool IsBlockedOrOutOfBounds(int x, int y, bool[,] grid)
    {
        int gridSizeX = grid.GetLength(0);
        int gridSizeY = grid.GetLength(1);

        if (x < 0 || x >= gridSizeX || y < 0 || y >= gridSizeY)
            return true;

        return grid[x, y];
    }

    private int GetMoveCost(Vector2Int from, Vector2Int to)
    {
        int dx = Mathf.Abs(from.x - to.x);
        int dy = Mathf.Abs(from.y - to.y);

        // Diagonal movement cost is higher
        return (dx + dy == 2) ? 14 : 10;
    }

    private List<Vector2Int> ReconstructPath(Node endNode)
    {
        var path = new List<Vector2Int>();
        var current = endNode;

        while (current != null)
        {
            path.Add(current.Position);
            current = current.Parent;
        }

        path.Reverse();
        return path;
    }

    private class Node : IComparable<Node>
    {
        public Vector2Int Position;
        public Node Parent;
        public int GCost;
        public int HCost;
        public int FCost => GCost + HCost;

        private NodePool pool;

        public Node(Vector2Int pos, Node parent, int gCost, int hCost, NodePool pool)
        {
            Position = pos;
            Parent = parent;
            GCost = gCost;
            HCost = hCost;
            this.pool = pool;
        }

        public void Reset(Vector2Int pos, Node parent, int gCost, int hCost)
        {
            Position = pos;
            Parent = parent;
            GCost = gCost;
            HCost = hCost;
        }

        public void Release()
        {
            Parent = null;
            pool.ReturnNode(this);
        }

        public int CompareTo(Node other)
        {
            int compare = FCost.CompareTo(other.FCost);
            if (compare == 0)
            {
                compare = HCost.CompareTo(other.HCost);
            }
            return compare;
        }
    }

    private class NodePool
    {
        private Stack<Node> pool = new Stack<Node>();

        public Node GetNode(Vector2Int position, Node parent, int gCost, int hCost)
        {
            if (pool.Count > 0)
            {
                var node = pool.Pop();
                node.Reset(position, parent, gCost, hCost);
                return node;
            }
            else
            {
                return new Node(position, parent, gCost, hCost, this);
            }
        }

        public void ReturnNode(Node node)
        {
            pool.Push(node);
        }

        public void ReleaseAll()
        {
            pool.Clear();
        }
    }
}

public class MinHeap<T> : IEnumerable<T> where T : IComparable<T>
{
    private List<T> items = new List<T>();

    public int Count => items.Count;

    public void Add(T item)
    {
        items.Add(item);
        HeapifyUp(items.Count - 1);
    }

    public T PopMin()
    {
        if (items.Count == 0)
            throw new System.InvalidOperationException("Heap is empty");

        T minItem = items[0];
        items[0] = items[items.Count - 1];
        items.RemoveAt(items.Count - 1);

        if (items.Count > 0)
            HeapifyDown(0);

        return minItem;
    }

    private void HeapifyUp(int index)
    {
        var item = items[index];
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            var parentItem = items[parentIndex];

            if (item.CompareTo(parentItem) >= 0)
                break;

            // Swap
            items[index] = parentItem;
            items[parentIndex] = item;

            index = parentIndex;
        }
    }

    private void HeapifyDown(int index)
    {
        int lastIndex = items.Count - 1;
        var item = items[index];

        while (index < lastIndex)
        {
            int leftChild = index * 2 + 1;
            int rightChild = leftChild + 1;
            int smallest = index;

            if (leftChild <= lastIndex && items[leftChild].CompareTo(items[smallest]) < 0)
                smallest = leftChild;

            if (rightChild <= lastIndex && items[rightChild].CompareTo(items[smallest]) < 0)
                smallest = rightChild;

            if (smallest == index)
                break;

            // Swap
            items[index] = items[smallest];
            items[smallest] = item;

            index = smallest;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}