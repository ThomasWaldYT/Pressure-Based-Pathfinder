using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visualizer : MonoBehaviour
{
    // Object with a trail renderer component attached; used to draw lines for visualizations (assigned in inspector)
    [SerializeField] private GameObject linePainterPrefab;


    [Header("Line Painter Trail Settings")]
    [SerializeField] private AnimationCurve widthCurve;
    [SerializeField] private float time;
    [SerializeField] private Color color;


    // Singleton of this class
    private static Visualizer instance;

    // Object with a trail renderer component attached; used to draw lines for visualizations (static version used for static methods)
    private static GameObject linePainter;

    // Used to store multiple linePainter objects when needed
    private static List<GameObject> linePainters;


    private void Awake()
    {
        // Set the singleton instance to the current game object; should only be one in the scene!
        instance = this;

        // Set the variables for the line painter's trail renderer
        linePainter = linePainterPrefab;
        TrailRenderer linePainterTrailRenderer = linePainter.GetComponent<TrailRenderer>();
        linePainterTrailRenderer.widthCurve = widthCurve;
        linePainterTrailRenderer.time = time;
        linePainterTrailRenderer.startColor = color;
        linePainterTrailRenderer.endColor = color;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }


    // ----------------------------------------------------------------------------------------------------------------------------- public getter methods

    /// <summary>
    /// Draws a line from startPosition to endPosition. <br/>
    /// See <see cref="DrawLineCoroutine"/>
    /// </summary>
    public static void DrawLine(Vector3 startPosition, Vector3 endPosition, float duration, float lifetime, float width, Color color, int orderInLayer = 0)
    {
        instance.StartCoroutine(DrawLineCoroutine(startPosition, endPosition, duration, lifetime, width, color));
    }

    /// <summary>
    /// Draws an arrow from startPosition to endPosition. <br/>
    /// See <see cref="DrawArrowCoroutine"/>
    /// </summary>
    public static void DrawArrow(Vector3 startPosition, Vector3 endPosition, float duration, float lifetime, Color color)
    {
        instance.StartCoroutine(DrawArrowCoroutine(startPosition, endPosition, duration, lifetime, color));
    }

    /// <summary>
    /// Draws a 2D X-Y grid in 3D space. Can be used to draw skewed grids as well as orthogonal ones. <br/>
    /// See <see cref="Draw2DGridCoroutine"/>
    /// </summary>
    public static void Draw2DGrid(Vector3 bottomLeftCorner, Vector3 xVector, Vector3 yVector, int xCells, int yCells, float gridCellSize, float lifetime, Color color)
    {
        instance.StartCoroutine(Draw2DGridCoroutine(bottomLeftCorner, xVector, yVector, xCells, yCells, gridCellSize, lifetime, color));
    }




    // --------------------------------------------------------------------------------------------------------------------------- private coroutines




    /// <summary>
    /// Draws a line from startPosition to endPosition.
    /// </summary>
    /// <param name="startPosition">The point where the line starts.</param>
    /// <param name="endPosition">The point where the line ends.</param>
    /// <param name="duration">How long the line should take to draw.</param>
    /// <param name="lifetime">How long the line should last.</param>
    /// <param name="width">The width of the line.</param>
    /// <param name="color">What color the line should be.</param>
    /// <param name="orderInLayer">The rendering order for the line, as dictated by Unity's rendering rules. The standard order for this library is:
    /// <br/> 1: Arrows
    /// <br/> 0: Other lines (default)
    /// </param>
    private static IEnumerator DrawLineCoroutine(Vector3 startPosition, Vector3 endPosition, float duration, float lifetime, float width, Color color, int orderInLayer = 0)
    {
        if (duration == 0) duration = 0.00000000001f;
        if (lifetime == 0) lifetime = 0.00000000001f;

        Transform lp = Instantiate(linePainter, startPosition, Quaternion.identity).GetComponent<Transform>();
        lp.GetComponent<TrailRenderer>().widthMultiplier = width;
        lp.GetComponent<TrailRenderer>().startColor = color;
        lp.GetComponent<TrailRenderer>().endColor = color;

        for (float time = 0; time < duration; time += Time.deltaTime)
        {
            lp.position = Vector3.Lerp(startPosition, endPosition, Mathf.Min(time/duration, 1));
            yield return null;
        }

        if (lp.position != endPosition) lp.position = endPosition;

        yield return new WaitForSeconds(lifetime);
        Destroy(lp.gameObject);
    }

    /// <summary>
    /// Draws an arrow from startPosition to endPosition.
    /// </summary>
    /// <param name="startPosition">The point where the arrow starts.</param>
    /// <param name="endPosition">The point where the arrow ends and draws the head.</param>
    /// <param name="duration">How long the arrow should take to draw.</param>
    /// <param name="lifetime">How long the arrow should last.</param>
    /// <param name="color">What color the arrow should be.</param>
    /// <returns></returns>
    private static IEnumerator DrawArrowCoroutine(Vector3 startPosition, Vector3 endPosition, float duration, float lifetime, Color color)
    {
        float bodyTime = 0.7f * duration;
        float headTime = duration - bodyTime;
        float width = 0.1f * Vector2.Distance(startPosition, endPosition);


        // Draw arrow body
        instance.StartCoroutine(DrawLineCoroutine(startPosition, endPosition, bodyTime, lifetime + headTime, width, color, 1));
        yield return new WaitForSeconds(bodyTime);

        // Draw arrow head
        Vector2 arrowHeadEnd = endPosition + Quaternion.AngleAxis(33, Camera.main.transform.forward) * (startPosition - endPosition) * 0.2f;
        instance.StartCoroutine(DrawLineCoroutine(endPosition, arrowHeadEnd, headTime, lifetime, width, color, 1));
        
        arrowHeadEnd = endPosition + Quaternion.AngleAxis(-33, Camera.main.transform.forward) * (startPosition - endPosition) * 0.2f;
        instance.StartCoroutine(DrawLineCoroutine(endPosition, arrowHeadEnd, headTime, lifetime, width, color, 1));
    }

    /// <summary>
    /// Draws a 2D X-Y grid in 3D space. Can be used to draw skewed grids as well as orthogonal ones.
    /// </summary>
    /// <param name="bottomLeftCorner">The position where the bottom left corner of the grid should be; used as a reference.</param>
    /// <param name="xVector">The direction in which horizontal lines will be drawn.</param>
    /// <param name="yVector">The direction in which vertical lines will be drawn.</param>
    /// <param name="xCells">The number of cells in the X direction.</param>
    /// <param name="yCells">The number of cells in the Y direction.</param>
    /// <param name="gridCellSize">The height and length of each grid cell.</param>
    /// <param name="lifetime">How long the grid should last.</param>
    /// <param name="color">What color the grid should be.</param>
    private static IEnumerator Draw2DGridCoroutine(Vector3 bottomLeftCorner, Vector3 xVector, Vector3 yVector, int xCells, int yCells, float gridCellSize, float lifetime, Color color)
    {
        Vector3 horizontalLineStartPos = bottomLeftCorner;
        Vector3 verticalLineStartPos = bottomLeftCorner;
        Vector3 horizontalLineEndPos = bottomLeftCorner + xVector.normalized * (gridCellSize * xCells);
        Vector3 verticalLineEndPos = bottomLeftCorner + yVector.normalized * (gridCellSize * yCells);

        float lineDuration = 1;
        float waitDuration = 0.1f;

        for (int xIndex = 0, yIndex = 0; xIndex < yCells + 1 || yIndex < xCells + 1; xIndex++, yIndex++)
        {
            if (xIndex < yCells + 1)
            {
                instance.StartCoroutine(DrawLineCoroutine(horizontalLineStartPos, horizontalLineEndPos, lineDuration, lifetime, 0.1f, color));
                horizontalLineStartPos += yVector.normalized * gridCellSize;
                horizontalLineEndPos += yVector.normalized * gridCellSize;
            }
            if (yIndex < xCells + 1)
            {
                instance.StartCoroutine(DrawLineCoroutine(verticalLineStartPos, verticalLineEndPos, lineDuration, lifetime, 0.1f, color));
                verticalLineStartPos += xVector.normalized * gridCellSize;
                verticalLineEndPos += xVector.normalized * gridCellSize;
            }

            yield return new WaitForSeconds(waitDuration);
        }
    }
}