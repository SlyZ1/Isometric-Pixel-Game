using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

public class LightsManager : MonoBehaviour
{
    [SerializeField] private Tilemap refMap;
    [SerializeField] private Camera camera;
    [SerializeField] private Color color;
    [SerializeField] private Texture2D tex;
    [SerializeField] private Material[] mats;

    private static List<Vector2Int> _staticLightPosInt = new List<Vector2Int>();
    private static List<Vector2> _staticLightsPos = new List<Vector2>();
    private static List<Vector2> _movingLightsPos = new List<Vector2>();
    private static List<Vector2> _totalLightsPos = new List<Vector2>();
    private static List<float> _staticLightsRad = new List<float>();
    private static List<float> _movingLightsRad = new List<float>();
    private static List<Vector4> _staticLightsColor = new List<Vector4>();
    private static List<Vector4> _movingLightsColor = new List<Vector4>();
    private static List<float> _staticLightsIntensity = new List<float>();
    private static List<float> _movingLightsIntensity = new List<float>();
    private static List<Transform> _movingLights = new List<Transform>();
    public static UnityEvent hasAddedLight = new UnityEvent();
    public static UnityEvent hasRemovedLight = new UnityEvent();
    public static bool isStatic;
    public static Transform changedLight;

    private static Tilemap _refMap;

    public static int numOfMovingLights = 0;
    public static int numOfStaticLights = 0;

    private static List<Material> _materials = new List<Material>();

    private static SLight sun = null;

    public static int maxStaticLights = 50;
    public static int maxMovingLights = 20;

    private Vector4[] movingVectorArray = new Vector4[200];
    private Vector4[] movingLightsColor = new Vector4[200];

    [SerializeField] private int UPDATE_RATIO = 2;
    private int updateCounter = 0;

    private void Awake()
    {
        foreach(Material _mat in mats)
        {
            _mat.SetVector("_Screen", new Vector4(GameManager.instance.screen.x, GameManager.instance.screen.y, 0, 0));
        }

        _refMap = refMap;
    }

    public static List<Vector2> GetStaticLights()
    {
        return _staticLightsPos;
    }


    public static List<Vector2Int> GetStaticLightPosInt()
    {
        return _staticLightPosInt;
    }

    public static List<Vector2> GetMovingLightsPos()
    {
        return _movingLightsPos;
    }

    public static List<Transform> GetMovingLights()
    {
        return _movingLights;
    }

    public static List<float> GetStaticLightsRad()
    {
        return _staticLightsRad;
    }

    public static List<float> GetMovingLightsRad()
    {
        return _movingLightsRad;
    }

    public static List<float> GetStaticLightsInt()
    {
        return _staticLightsIntensity;
    }

    public static List<float> GetMovingLightsInt()
    {
        return _movingLightsIntensity;
    }

    public static void AddLight(Transform light, bool _isStatic)
    {
        changedLight = light;
        isStatic = _isStatic;

        SLight _light = light.GetComponent<SLight>();

        if (_light.isGlobal)
        {
            sun = light.GetComponent<SLight>();
            return;
        }

        if (_isStatic)
        {
            
            if (_staticLightsPos.Contains(light.position)) return;
            
            _staticLightPosInt.Add((Vector2Int)_refMap.WorldToCell(light.position));
            _staticLightsPos.Add(light.position);
            _staticLightsColor.Add(_light.color);
            _staticLightsRad.Add(_light.radius);
            _staticLightsIntensity.Add(_light.intensity);
            numOfStaticLights++;

        }
        else
        {
            if (_movingLights.Contains(light)) return;

            _movingLights.Add(light);
            _movingLightsPos.Add(light.position);
            _movingLightsRad.Add(_light.radius);
            _movingLightsColor.Add(_light.color);
            _movingLightsIntensity.Add(_light.intensity);

            numOfMovingLights++;
        }

        hasAddedLight.Invoke();

        UpdateLightColors();
    }


    public static void RemoveLight(Transform light, bool _isStatic)
    {
        if (light == null) return;

        isStatic = _isStatic;
        changedLight = light;

        if (isStatic)
        {
            if (!_staticLightsPos.Contains(light.position)) return;

            int staticIndex = _staticLightsPos.IndexOf(light.position);
            _staticLightsPos.RemoveAt(staticIndex);
            _staticLightPosInt.RemoveAt(staticIndex);
            _staticLightsRad.RemoveAt(staticIndex);
            _staticLightsColor.RemoveAt(staticIndex);
            _staticLightsIntensity.RemoveAt(staticIndex);

            numOfStaticLights--;
        }
        else
        {
            if (!_movingLights.Contains(light)) return;

            int index = _movingLights.IndexOf(light);
            _movingLights.RemoveAt(index);
            _movingLightsRad.RemoveAt(index);
            _movingLightsColor.RemoveAt(index);
            _movingLightsIntensity.RemoveAt(index);
            numOfMovingLights--;

            _movingLightsPos = new List<Vector2>();
            foreach (Transform _light in _movingLights)
            {
                _movingLightsPos.Add(_light.position);
            }
        }

        hasRemovedLight.Invoke();

        UpdateLightColors();
    }

    static bool updateLightColors = false;
    private static void UpdateLightColors()
    {
        updateLightColors = true;
    }


    private void Update()
    {
        updateCounter = (updateCounter + 1) % UPDATE_RATIO;

        UpdateMats();
    }


    private void UpdateMats()
    {
        if (updateCounter % UPDATE_RATIO != 0) return;

        _movingLightsPos = new List<Vector2>();
        _totalLightsPos = new List<Vector2>();
        for (int i = 0; i < Mathf.Min(numOfMovingLights, maxMovingLights); i++)
        {
            Vector3 pos = _movingLights[i].position;

            _movingLightsPos.Add(pos);
            _totalLightsPos.Add(pos);

            Vector3 camPos = camera.ScreenToViewportPoint(camera.WorldToScreenPoint(pos));

            color = new Color(camPos.x, camPos.y, _movingLightsRad[i], _movingLightsIntensity[i]);
            
            movingVectorArray[i] = color;
            movingLightsColor[i] = _movingLightsColor[i];
        }

        for (int i = 0; i < Mathf.Min(numOfStaticLights, maxStaticLights); i++)
        {
            Vector3 pos = _staticLightsPos[i];

            _totalLightsPos.Add(pos);

            Vector3 camPos = camera.ScreenToViewportPoint(camera.WorldToScreenPoint(pos));

            color = new Color(camPos.x, camPos.y, _staticLightsRad[i], _staticLightsIntensity[i]);

            movingVectorArray[i + Mathf.Min(numOfMovingLights, maxMovingLights)] = color;
            movingLightsColor[i + Mathf.Min(numOfMovingLights, maxMovingLights)] = _staticLightsColor[i];
        }

        foreach (Material mat in mats)
        {
            if (numOfMovingLights > 0 || numOfStaticLights > 0) mat.SetVectorArray("_MovingLightArray", movingVectorArray);

            mat.SetInt("_NumOfMovingLights", Mathf.Min(numOfMovingLights,maxMovingLights) + Mathf.Min(numOfStaticLights, maxStaticLights));

            if (sun != null) mat.SetVector("_sunColor", sun.color);

            if (updateLightColors && (numOfMovingLights > 0 || numOfStaticLights > 0)) mat.SetVectorArray("_MovingLightColors", movingLightsColor);
        }

        updateLightColors = false;
    }


    public static void AddMaterials(List<Material> mats)
    {
        _materials.AddRange(mats);
    }


    public static void RemoveMaterials(List<Material> mats)
    {
        mats.ForEach(mat => _materials.Remove(mat));
    }
}
