using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PressurePathfinder : MonoBehaviour
{
    /// <summary>
    /// Recursively generates the path vectors that lead to the given position.
    /// </summary>
    /// <param name="terrain">The terrain grid to determine where obstacles are.</param>
    /// <param name="gridPosition">The location to pathfind to, relative to the terrain grid.</param>
    /// <param name="pathVectors">The array to store the calculated path vectors; returned by modifying a reference.</param>
    /// <param name="positionsToVisit">The array to store the positions that still need to be calculated; only used during recursion.</param>
    /// <param name="pathsOrigin">Stores the original position being pathfinded to; only used during recursion.</param>
    /// <param name="currentIndex">Keeps track of which position in the positionsToVisit list is being calculated; only used during recursion.</param>
    public void PropogatePaths(bool[,] terrain, Vector2Int gridPosition, ref Vector2Int[,] pathVectors,
                               List<Vector2Int> positionsToVisit = null, Vector2Int pathsOrigin = default, int currentIndex = 0)
    {
        // Break out if the position is out of bounds
        if (PositionIsOutOfBounds(terrain, gridPosition)) return;

        // Break out if the position has a wall there
        if (terrain[gridPosition.x, gridPosition.y]) return;

        positionsToVisit ??= new List<Vector2Int> {gridPosition};
        if (currentIndex == 0) pathsOrigin = gridPosition;


        // Get path vectors
        pathVectors[gridPosition.x, gridPosition.y] = CalculatePathVector(terrain, gridPosition, pathVectors, currentIndex);

        AddNeighbors(terrain, gridPosition, ref positionsToVisit);

        // Recursive call to continue getting vectors
        if (++currentIndex < positionsToVisit.Count) PropogatePaths(terrain, positionsToVisit[currentIndex], ref pathVectors,
                                                                    positionsToVisit, pathsOrigin, currentIndex);
        else pathVectors[pathsOrigin.x, pathsOrigin.y] = Vector2Int.zero;
    }

    /// <summary>
    /// Returns whether or not the given position is out of bounds of the pathfinding array.
    /// See <see cref="PropogatePaths"/>
    /// </summary>
    /// <param name="gridPosition">The location to check.</param>
    private bool PositionIsOutOfBounds(bool[,] terrain, Vector2Int gridPosition)
    {
        if (gridPosition.x < 0 || gridPosition.y < 0 ||
            gridPosition.x >= terrain.GetLength(0) || gridPosition.y >= terrain.GetLength(1)) return true;
        else return false;
    }

    /// <summary>
    /// Calculates the path vector for the given position.
    /// See <see cref="PropogatePaths"/>
    /// </summary>
    /// <param name="terrain">The terrain grid to determine where obstacles are.</param>
    /// <param name="gridPosition">The position to calculate the vector for.</param>
    /// <param name="pathVectors">The array to store the calculated path vectors; returned by modifying a reference.</param>
    /// <param name="currentIndex">The current index in the list of grid positions to calculate; only used to set the vector for pathsOrigin.</param>
    /// <returns>The path vector that follows the "natural pressure" of the current grid. If the vector is zero, this will return a vector pointing
    /// to the first other path vector encountered besides itself.</returns>
    private Vector2Int CalculatePathVector(bool[,] terrain, Vector2Int gridPosition, Vector2Int[,] pathVectors, int currentIndex)
    {
        if (currentIndex == 0) return Vector2Int.one;

        int x = (!PositionIsOutOfBounds(terrain, new Vector2Int(gridPosition.x + 1, gridPosition.y)) &&
                 pathVectors[gridPosition.x + 1, gridPosition.y] != Vector2Int.zero ? 1 : 0) -
                (!PositionIsOutOfBounds(terrain, new Vector2Int(gridPosition.x - 1, gridPosition.y)) &&
                 pathVectors[gridPosition.x - 1, gridPosition.y] != Vector2Int.zero ? 1 : 0);
        int y = (!PositionIsOutOfBounds(terrain, new Vector2Int(gridPosition.x, gridPosition.y + 1)) &&
                 pathVectors[gridPosition.x, gridPosition.y + 1] != Vector2Int.zero ? 1 : 0) -
                (!PositionIsOutOfBounds(terrain, new Vector2Int(gridPosition.x, gridPosition.y - 1)) &&
                 pathVectors[gridPosition.x, gridPosition.y - 1] != Vector2Int.zero ? 1 : 0);

        Vector2Int defaultPathVector = !PositionIsOutOfBounds(terrain, new Vector2Int(gridPosition.x, gridPosition.y + 1)) &&
                                       pathVectors[gridPosition.x, gridPosition.y + 1] != Vector2Int.zero ? new Vector2Int(0, 1) :
                                       (!PositionIsOutOfBounds(terrain, new Vector2Int(gridPosition.x + 1, gridPosition.y)) &&
                                        pathVectors[gridPosition.x + 1, gridPosition.y] != Vector2Int.zero ? new Vector2Int(1, 0) :
                                       (!PositionIsOutOfBounds(terrain, new Vector2Int(gridPosition.x, gridPosition.y - 1)) &&
                                        pathVectors[gridPosition.x, gridPosition.y - 1] != Vector2Int.zero ? new Vector2Int(0, -1) :
                                       (!PositionIsOutOfBounds(terrain, new Vector2Int(gridPosition.x - 1, gridPosition.y)) &&
                                        pathVectors[gridPosition.x - 1, gridPosition.y] != Vector2Int.zero ? new Vector2Int(-1, 0) :
                                        Vector2Int.zero)));

        if ((x == 0 && y == 0) ||
            terrain[gridPosition.x + x, gridPosition.y + y]) return defaultPathVector;
        else return new Vector2Int(x, y);
    }

    /// <summary>
    /// Adds the neighboring empty tiles of the given position to the list of positions to visit and sets their generation.
    /// See <see cref="PropogatePaths"/>
    /// </summary>
    /// <param name="terrain">The terrain grid to determine where obstacles are.</param>
    /// <param name="gridPosition">The position to get neighbors from.</param>
    /// <param name="positionsToVisit">A reference to the list of positions to visit.</param>
    private void AddNeighbors(bool[,] terrain, Vector2Int gridPosition, ref List<Vector2Int> positionsToVisit)
    {
        Vector2Int neighbor1 = new(gridPosition.x, gridPosition.y + 1);
        Vector2Int neighbor2 = new(gridPosition.x + 1, gridPosition.y);
        Vector2Int neighbor3 = new(gridPosition.x, gridPosition.y - 1);
        Vector2Int neighbor4 = new(gridPosition.x - 1, gridPosition.y);

        if (!PositionIsOutOfBounds(terrain, neighbor1) && !positionsToVisit.Contains(neighbor1) && !terrain[neighbor1.x, neighbor1.y]) positionsToVisit.Add(neighbor1);
        if (!PositionIsOutOfBounds(terrain, neighbor2) && !positionsToVisit.Contains(neighbor2) && !terrain[neighbor2.x, neighbor2.y]) positionsToVisit.Add(neighbor2);
        if (!PositionIsOutOfBounds(terrain, neighbor3) && !positionsToVisit.Contains(neighbor3) && !terrain[neighbor3.x, neighbor3.y]) positionsToVisit.Add(neighbor3);
        if (!PositionIsOutOfBounds(terrain, neighbor4) && !positionsToVisit.Contains(neighbor4) && !terrain[neighbor4.x, neighbor4.y]) positionsToVisit.Add(neighbor4);
    }
}