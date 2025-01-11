using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tester : MonoBehaviour
{
    // Colors used for drawing visualizations
    [SerializeField] private Color wallColor;
    [SerializeField] private Color pPArrowColor;
    [SerializeField] private Color aStarArrowColor;

    [SerializeField] private int xCells;
    [SerializeField] private int yCells;
    

    // Reference to the PressurePathfinder component
    PressurePathfinder pressurePathfinder;

    // Reference to the AStarPathfinding component
    AStarPathfinding aStarPathfinder;


    // Stores the path vectors returned by pressurePathfinder
    private Vector2Int[,] pPPathVectors;

    // Stores the path vectors returned by aStarPathfinder
    private List<Vector2Int> aStarPathVectors;

    // Keeps track of all tiles visited by the A* algorithm during execution to prevent redundancies
    private List<Vector2Int> aStarVisitedPoints;

    // Stores all generated A* paths for visualization using arrows
    private List<List<Vector2Int>> aStarPaths;

    // Stores the origin point of terrain grid ((0,0) is bottom left corner)
    private Vector2Int terrainOrigin = Vector2Int.zero;

    // Stores the point currently being pathfinded to
    private Vector2Int pathsOrigin = Vector2Int.zero;


    // Keeps track of whether or not terrain is present at a given x-y coordinate
    private bool[,] terrain;

    // Keeps track of the total time it has taken for the A* pathfinder to execute over all tests
    private float totalAStarTime = 0;

    // Keeps track of the total time it has taken for the pressure pathfinder to execute over all tests
    private float totalPressurePathfinderTime = 0;

    // Keeps track of the number of times the algorithms have been tested so their averages over every iteration can be computed correctly
    private int averageTimeIterations = 0;

    // Keeps track of how fast the pressure pathfinder runs on average as a percentage of A*'s average speed
    private float averagePercentDifference = 0;


    private void Awake()
    {
        pressurePathfinder = GetComponent<PressurePathfinder>();
        aStarPathfinder = GetComponent<AStarPathfinding>();

        pPPathVectors = new Vector2Int[xCells, yCells];
        aStarPathVectors = new();
        aStarVisitedPoints = new();
        aStarPaths = new();
        terrain = new bool[xCells, yCells];
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Visualizer.Draw2DGrid(new Vector2(-0.5f, -0.5f), Vector2.right, Vector2.up, xCells, yCells, 1, Mathf.Infinity, Color.white);
    }

    // Update is called once per frame
    void Update()
    {
        // Place walls on grid when left mouse button is clicked
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPosition = Vector2Int.RoundToInt(mousePosition) - terrainOrigin;
            TryModifyTerrain(gridPosition);
        }

        // Find paths when F is pressed
        if (Input.GetKeyDown(KeyCode.F))
        {
            // Reset arrays
            pPPathVectors = new Vector2Int[xCells, yCells];
            aStarPathVectors = new();
            aStarVisitedPoints = new();
            aStarPaths = new();

            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            pathsOrigin = Vector2Int.RoundToInt(mousePosition) - terrainOrigin;


            // Test compute times for algorithms:

            averageTimeIterations++;

            // A*
            float startTime = Time.realtimeSinceStartup;
            GetAllAStarPaths(pathsOrigin);
            float endTime = Time.realtimeSinceStartup;
            totalAStarTime += endTime - startTime;
            Debug.Log($"A* Pathfinder Time: {endTime - startTime}");

            // Pressure Pathfinder
            startTime = Time.realtimeSinceStartup;
            GetAllPressurePaths(pathsOrigin);
            endTime = Time.realtimeSinceStartup;
            totalPressurePathfinderTime += endTime - startTime;
            Debug.Log($"Pressure Pathfinder Time: {endTime - startTime}");

            float averageAStarTime = totalAStarTime / averageTimeIterations;
            float averagePressurePathfinderTime = totalPressurePathfinderTime / averageTimeIterations;

            averagePercentDifference = (averageAStarTime - averagePressurePathfinderTime) / averageAStarTime * 100;
            Debug.Log($"Pressure Pathfinder is {averagePercentDifference}% faster than A*!");


            // Draw path visualizations
            StartCoroutine(VisualizePaths());
        }
    }

    /// <summary>
    /// Draws animated arrows to show the paths generated from the different pathfinding algorithms.
    /// </summary>
    private IEnumerator VisualizePaths()
    {
        Vector2Int startPosition;
        float waitTime = 0.001f;

        // Pressure paths
        for (int x = 0; x < terrain.GetLength(0); x++)
        {
            for (int y = 0; y < terrain.GetLength(1); y++)
            {
                startPosition = new(x, y);
                Visualizer.DrawArrow((Vector2)startPosition, startPosition + ((Vector2)pPPathVectors[x, y]).normalized * 0.4f, 1, 1, pPArrowColor);
                yield return new WaitForSeconds(waitTime);
            }
        }

        // A* Paths
        aStarVisitedPoints = new();
        foreach (List<Vector2Int> path in aStarPaths)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (aStarVisitedPoints.Contains(path[i])) continue;
                aStarVisitedPoints.Add(path[i]);

                Visualizer.DrawArrow((Vector2)path[i], path[i] + ((Vector2)(path[i + 1] - path[i])).normalized * 0.4f, 1, 1, aStarArrowColor);
                yield return new WaitForSeconds(waitTime);
            }
        }
    }

    /// <summary>
    /// Generates paths from the AStarPathfinding script.
    /// </summary>
    /// <param name="endPosition">The position to pathfind to.</param>
    private void GetAllAStarPaths(Vector2Int endPosition)
    {
        Vector2Int startPosition;

        for (int x = 0; x < terrain.GetLength(0); x++)
        {
            for (int y = 0; y < terrain.GetLength(1); y++)
            {
                startPosition = new(x, y);
                if (aStarVisitedPoints.Contains(startPosition)) continue;

                aStarPathVectors = aStarPathfinder.FindPath(startPosition, endPosition, terrain) ?? new List<Vector2Int>();
                aStarPaths.Add(aStarPathVectors);


                foreach (Vector2Int point in aStarPathVectors) if (!aStarVisitedPoints.Contains(point)) aStarVisitedPoints.Add(point);
            }
        }
    }

    /// <summary>
    /// Generates paths from the PressurePathfinder script.
    /// </summary>
    /// <param name="endPosition">The position to pathfind to.</param>
    private void GetAllPressurePaths(Vector2Int endPosition)
    {
        pressurePathfinder.PropogatePaths(terrain, endPosition, ref pPPathVectors);
    }

    /// <summary>
    /// Attempts to add or remove terrain at gridPosition if it is within the bounds of terrain.
    /// </summary>
    /// <param name="gridPosition">The location to add/remove terrain.</param>
    private void TryModifyTerrain(Vector2Int gridPosition)
    {
        if (gridPosition.x < 0 || gridPosition.y < 0 ||
            gridPosition.x >= terrain.GetLength(0) || gridPosition.y >= terrain.GetLength(1)) return;

        terrain[gridPosition.x, gridPosition.y] = !terrain[gridPosition.x, gridPosition.y];
    }

    private void OnDrawGizmos()
    {
        if (terrain == null) return;

        // Draw walls
        Gizmos.color = wallColor;
        for (int x = 0; x < terrain.GetLength(0); x++)
        {
            for (int y = 0; y < terrain.GetLength(1); y++)
            {
                if (terrain[x, y])
                {
                    Vector2 cubePosition = new(x + terrainOrigin.x, y + terrainOrigin.y);
                    Gizmos.DrawCube(cubePosition, new Vector2(0.9f, 0.9f));
                }
            }
        }
    }
}