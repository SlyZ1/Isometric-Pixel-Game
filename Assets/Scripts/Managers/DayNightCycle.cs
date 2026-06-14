using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Transitions")]
    [Space]
    [SerializeField] private Gradient lightGradient;
    [SerializeField] private AnimationCurve angleCurve;
    [SerializeField] private AnimationCurve intensityCurve;
    [SerializeField] private AnimationCurve sizeCurve;
    [Space]

    [Space]
    [Header("References")]
    [Space]
    [SerializeField] private SLight globalLight;
    [SerializeField] private ShadowManager shadowManager;
    [Space]

    [Space]
    [Header("Params")]
    [Space]
    [SerializeField] private float startingTime;
    [SerializeField] private float timeSpeed;
    [SerializeField] private float angleRange;
    [Space]

    public float time = 0f;
    private float intensity;
    private float size;

    private bool isInitialized = false;

    public void Initialize()
    {
        intensity = shadowManager.sunIntensity;
        size = shadowManager.lengthFactor;
        isInitialized = true;
    }

    private void FixedUpdate()
    {
        if (!isInitialized) return;

        time = (GameManager.instance.tick - GameManager.instance.tickDesync) * Time.fixedDeltaTime * timeSpeed + startingTime;

        if(time > 1)
        {
            time -= 1;
        }

        globalLight.color = lightGradient.Evaluate(time);

        shadowManager.sunIntensity = intensity * intensityCurve.Evaluate(time);
        shadowManager.lengthFactor = size * sizeCurve.Evaluate(time);
        shadowManager.sunAngle = Quaternion.Euler(0, 0, angleRange * angleCurve.Evaluate(time)) * Vector2.up;

        shadowManager.sunColor = globalLight.color;
    }
}
