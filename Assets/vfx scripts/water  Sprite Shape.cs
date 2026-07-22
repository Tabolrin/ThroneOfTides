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

    private SpriteShapeController _spriteShape;
    private Spline _spline;

    private float[] _heights;
    private float[] _velocities;
    private float[] _restHeights;
    private Vector3 _lastBottlePosition;

    void Awake()
    {
        _spriteShape = GetComponent<SpriteShapeController>();
        _spline = _spriteShape.spline;

        int pointCount = _spline.GetPointCount();
        _heights = new float[pointCount];
        _velocities = new float[pointCount];
        _restHeights = new float[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            _heights[i] = _spline.GetPosition(i).y;
            _restHeights[i] = _heights[i]; // starting height is the rest height
        }

        _lastBottlePosition = transform.position;
    }

    void Update()
    {
        // how fast the bottle moved since last frame
        Vector3 bottleVelocity = (transform.position - _lastBottlePosition) / Time.deltaTime;
        _lastBottlePosition = transform.position;

        int pointCount = _spline.GetPointCount();

        // Step 1: spring force per point
        for (int i = 0; i < pointCount; i++)
        {
            float force = springStiffness * (_restHeights[i] - _heights[i]) - damping * _velocities[i];
            force -= bottleVelocity.x * velocityMultiplier; // horizontal movement "pushes" the water
            _velocities[i] += force;
            _heights[i] += _velocities[i];
        }

        // Step 2: spread the wave to neighboring points
        for (int iter = 0; iter < spreadIterations; iter++)
        {
            for (int i = 0; i < pointCount; i++)
            {
                if (i > 0)
                {
                    float delta = spread * (_heights[i] - _heights[i - 1]);
                    _velocities[i - 1] += delta;
                    _heights[i - 1] += delta;
                }
                if (i < pointCount - 1)
                {
                    float delta = spread * (_heights[i] - _heights[i + 1]);
                    _velocities[i + 1] += delta;
                    _heights[i + 1] += delta;
                }
            }
        }

        // Step 3: write heights back to the spline
        for (int i = 0; i < pointCount; i++)
        {
            Vector3 pos = _spline.GetPosition(i);
            pos.y = _heights[i];
            _spline.SetPosition(i, pos);
        }

        _spriteShape.BakeMesh();
    }

    // Call this externally to "splash" a specific point (e.g. on collision with an object)
    public void Splash(int pointIndex, float force)
    {
        if (pointIndex >= 0 && pointIndex < _velocities.Length)
            _velocities[pointIndex] += force;
    }
}