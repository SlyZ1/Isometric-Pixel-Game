using UnityEngine;

public class SLight : MonoBehaviour
{
    public bool isGlobal;
    public bool isStatic;

    public Color color;
    public float intensity;
    public float radius;

    private bool hasAwakened = false;

    private void Start()
    {
        gameObject.isStatic = isStatic;
        LightsManager.AddLight(transform, isStatic);
        hasAwakened = true;
    }

    private void OnEnable()
    {
        if (hasAwakened)
        {
            Debug.Log("enabled");
            gameObject.isStatic = isStatic;
            LightsManager.AddLight(transform, isStatic);
        }
    }


    private void OnDisable()
    {
        LightsManager.RemoveLight(transform, isStatic);
    }
}
