using UnityEngine;
using UnityEngine.U2D;

[RequireComponent(typeof(SpriteShapeController))]
public class WaterSpriteShape : MonoBehaviour
{
    [Header("Spring Settings")]
    public float springStiffness = 0.03f;
    public float damping = 0.06f;
    public float spread = 0.006f;
    public int spreadIterations = 4;

    [Header("Bottle Movement Response")]
    public float velocityMultiplier = 0.03f;

    private SpriteShapeController spriteShape;
    private Spline spline;

    private float[] heights;
    private float[] velocities;
    private float[] restHeights;
    private Vector3 lastBottlePosition;

    void Awake()
    {
        spriteShape = GetComponent<SpriteShapeController>();
        spline = spriteShape.spline;

        int pointCount = spline.GetPointCount();
        heights = new float[pointCount];
        velocities = new float[pointCount];
        restHeights = new float[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            heights[i] = spline.GetPosition(i).y;
            restHeights[i] = heights[i]; // starting height is the rest height
        }

        lastBottlePosition = transform.position;
    }

    void Update()
    {
        // how fast the bottle moved since last frame
        Vector3 bottleVelocity = (transform.position - lastBottlePosition) / Time.deltaTime;
        lastBottlePosition = transform.position;

        int pointCount = spline.GetPointCount();

        // Step 1: spring force per point
        for (int i = 0; i < pointCount; i++)
        {
            float force = springStiffness * (restHeights[i] - heights[i]) - damping * velocities[i];
            force -= bottleVelocity.x * velocityMultiplier; // horizontal movement "pushes" the water
            velocities[i] += force;
            heights[i] += velocities[i];
        }

        // Step 2: spread the wave to neighboring points
        for (int iter = 0; iter < spreadIterations; iter++)
        {
            for (int i = 0; i < pointCount; i++)
            {
                if (i > 0)
                {
                    float delta = spread * (heights[i] - heights[i - 1]);
                    velocities[i - 1] += delta;
                    heights[i - 1] += delta;
                }
                if (i < pointCount - 1)
                {
                    float delta = spread * (heights[i] - heights[i + 1]);
                    velocities[i + 1] += delta;
                    heights[i + 1] += delta;
                }
            }
        }

        // Step 3: write heights back to the spline
        for (int i = 0; i < pointCount; i++)
        {
            Vector3 pos = spline.GetPosition(i);
            pos.y = heights[i];
            spline.SetPosition(i, pos);
        }

        spriteShape.BakeMesh();
    }

    // Call this externally to "splash" a specific point (e.g. on collision with an object)
    public void Splash(int pointIndex, float force)
    {
        if (pointIndex >= 0 && pointIndex < velocities.Length)
            velocities[pointIndex] += force;
    }
}