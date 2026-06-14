using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

public class ShadowManager : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private ComputeShader compute;
    [SerializeField] private RawImage image;
    [SerializeField] private Tilemap refMap;
    [SerializeField] public Vector2 sunAngle;
    [HideInInspector] public Color sunColor;
    [SerializeField, Range(0f, 1f)] private float shadowIntensity;
    [SerializeField, Range(0f, 1f)] public float lengthFactor;
    [SerializeField, Range(0f, 1f)] private float shadowDivergence;
    [SerializeField, Range(0f, 1f)] private float shadowMin;
    [SerializeField, Range(0f, 1f)] private float shadowMax;

    public float sunIntensity;

    private RenderTexture renderTexture;

    private List<int> lightShadowPair = new List<int>();
    private List<int> shadowNumLight = new List<int>();

    private List<Vector2> staticLightPos = new List<Vector2>();
    private List<Vector2Int> staticLightPosInt = new List<Vector2Int>();
    private List<float> staticLightRad = new List<float>();
    private List<float> staticLightInt = new List<float>();
    private List<Vector2> movingLightPos = new List<Vector2>();
    private List<float> movingLightRad = new List<float>();
    private List<float> movingLightInt = new List<float>();

    private List<int> shadowNumVertices = new List<int>();
    private List<Vector2> shadowVertices = new List<Vector2>();
    private List<float> shadowLength = new List<float>();
    private List<Vector2> shadowPos = new List<Vector2>();

    public Dictionary<ShadowInstance, Vector2Int> shadowIndexes = new Dictionary<ShadowInstance, Vector2Int>();

    ComputeBuffer staticLightPosBuffer;
    ComputeBuffer staticLightRadBuffer;
    ComputeBuffer staticLightIntBuffer;
    ComputeBuffer lightShadowPairBuffer;
    ComputeBuffer shadowNumLightBuffer;
    ComputeBuffer movingLightPosBuffer;
    ComputeBuffer movingLightRadBuffer;
    ComputeBuffer movingLightIntBuffer;

    ComputeBuffer shadowNumVerticesBuffer;
    ComputeBuffer shadowVerticesBuffer;
    ComputeBuffer shadowLengthBuffer;
    ComputeBuffer shadowPosBuffer;

    private int numOfShadow = 0;
    private int numOfVertices = 0;

    private int numOfStaticLights = 0;
    private int numOfMovingLights = 0;

    private int numOfPair = 0;

    private float shadowSizeMultiplier;

    public void AddShadow(int _numVertices, Vector2[] _vertices, float _shadowLength, Vector2 _pos, ShadowInstance _shadow)
    {
        shadowNumVertices.Add(_numVertices);
        shadowVertices.AddRange(_vertices);
        shadowLength.Add(_shadowLength);
        shadowPos.Add(_pos);

        shadowIndexes[_shadow] = new Vector2Int(numOfShadow, numOfVertices);

        numOfShadow++;
        numOfVertices += _numVertices;

        UpdateLightShadowPairs();

        UpdateShadowBuffers();
    }


    public void AddShadows(List<int> _numVertices, List<Vector2[]> _vertices, List<float> _shadowLength, List<Vector2> _pos, List<ShadowInstance> _shadow, int _numOfShadows)
    {
        for (int i = 0; i < _numOfShadows; i++)
        {
            shadowNumVertices.Add(_numVertices[i]);
            shadowVertices.AddRange(_vertices[i]);
            shadowLength.Add(_shadowLength[i]);
            shadowPos.Add(_pos[i]);

            shadowIndexes[_shadow[i]] = new Vector2Int(numOfShadow, numOfVertices);

            numOfShadow++;
            numOfVertices += _numVertices[i];
        }

        UpdateLightShadowPairs();

        UpdateShadowBuffers();
    }

    public void RemoveShadow(ShadowInstance _shadow)
    {
        Vector2Int _shadowIndexes = shadowIndexes[_shadow];
        int index = _shadowIndexes.x;
        int verticesIndex = _shadowIndexes.y;

        int numVertices = shadowNumVertices[index];

        for (int i = numVertices - 1; i >= 0; i--)
        {
            shadowVertices.RemoveAt(verticesIndex + i);
        }

        shadowLength.RemoveAt(index);
        shadowPos.RemoveAt(index);
        shadowNumVertices.RemoveAt(index);

        List<ShadowInstance> _ = new List<ShadowInstance>(shadowIndexes.Keys);
        foreach(ShadowInstance shadow in _)
        {
            Vector2Int value = shadowIndexes[shadow];
            if (value.x > index)
            {
                shadowIndexes[shadow] = new Vector2Int(value.x - 1, value.y - numVertices);
            }
        }

        shadowIndexes.Remove(_shadow);

        numOfShadow--;
        numOfVertices -= numVertices;

        UpdateLightShadowPairs();

        UpdateShadowBuffers();
    }


    public void RemoveShadows(List<ShadowInstance> _shadow, int _numOfShadow)
    {
        for (int i = 0; i < _numOfShadow; i++)
        {
            if (_shadow[i] == null) continue;
            if (!shadowIndexes.ContainsKey(_shadow[i])) continue;

            Vector2Int _shadowIndexes = shadowIndexes[_shadow[i]];
            int index = _shadowIndexes.x;
            int verticesIndex = _shadowIndexes.y;

            int numVertices = shadowNumVertices[index];

            for (int j = numVertices - 1; j >= 0; j--)
            {
                shadowVertices.RemoveAt(verticesIndex + j);
            }

            shadowLength.RemoveAt(index);
            shadowPos.RemoveAt(index);
            shadowNumVertices.RemoveAt(index);

            shadowIndexes.Remove(_shadow[i]);

            List<ShadowInstance> _ = new List<ShadowInstance>(shadowIndexes.Keys);
            foreach (ShadowInstance shadow in _)
            {
                Vector2Int value = shadowIndexes[shadow];
                if (value.x > index)
                {
                    shadowIndexes[shadow] = new Vector2Int(value.x - 1, value.y - numVertices);
                }
            }

            numOfShadow--;
            numOfVertices -= numVertices;
        }

        UpdateLightShadowPairs();

        UpdateShadowBuffers();
    }



    private void AddLight()
    {
        if (LightsManager.isStatic)
        {
            staticLightPosInt = LightsManager.GetStaticLightPosInt();
            staticLightPos = LightsManager.GetStaticLights();
            staticLightRad = LightsManager.GetStaticLightsRad();
            staticLightInt = LightsManager.GetStaticLightsInt();

            numOfStaticLights++;

            UpdateLightShadowPairs();

            UpdateStaticLightBuffers(true);
        }
        else
        {
            movingLightRad = LightsManager.GetMovingLightsRad();
            movingLightInt = LightsManager.GetMovingLightsInt();

            numOfMovingLights++;

            UpdateMovingLightBuffers(true);
        }
    }


    private void RemoveLight()
    {
        if (LightsManager.isStatic)
        {
            staticLightPosInt = LightsManager.GetStaticLightPosInt();
            staticLightPos = LightsManager.GetStaticLights();
            staticLightRad = LightsManager.GetStaticLightsRad();
            staticLightInt = LightsManager.GetStaticLightsInt();

            numOfStaticLights--;

            UpdateLightShadowPairs();

            UpdateStaticLightBuffers(true);
        }
        else
        {
            movingLightRad = LightsManager.GetMovingLightsRad();
            movingLightInt = LightsManager.GetMovingLightsInt();

            numOfMovingLights--;

            UpdateMovingLightBuffers(true);
        }
    }


    private void UpdateShadowBuffers()
    {
        return;
        if (this == null) return;

        compute.SetInt("numOfShadows", numOfShadow);

        if (numOfShadow == 0) return;

        if (shadowNumVerticesBuffer != null)
        {
            shadowNumVerticesBuffer.Release();
            shadowVerticesBuffer.Release();
            shadowLengthBuffer.Release();
            shadowPosBuffer.Release();
        }

        shadowNumVerticesBuffer = new ComputeBuffer(numOfShadow, sizeof(int));
        shadowNumVerticesBuffer.SetData(shadowNumVertices);

        shadowVerticesBuffer = new ComputeBuffer(numOfVertices, 2 * sizeof(float));
        shadowVerticesBuffer.SetData(shadowVertices);

        shadowLengthBuffer = new ComputeBuffer(numOfShadow, sizeof(float));
        shadowLengthBuffer.SetData(shadowLength);

        shadowPosBuffer = new ComputeBuffer(numOfShadow, 2 * sizeof(float));
        shadowPosBuffer.SetData(shadowPos);

        compute.SetBuffer(0, "shadowNumVertices", shadowNumVerticesBuffer);
        compute.SetBuffer(0, "shadowVertices", shadowVerticesBuffer);
        compute.SetBuffer(0, "shadowLength", shadowLengthBuffer);
        compute.SetBuffer(0, "shadowPos", shadowPosBuffer);
    }


    private void UpdateStaticLightBuffers(bool updateRad)
    {
        compute.SetInt("numOfStaticLights", numOfStaticLights);

        if (numOfStaticLights == 0) return;

        staticLightPosBuffer.Release();
        staticLightPosBuffer = new ComputeBuffer(numOfStaticLights, 2 * sizeof(float));
        staticLightPosBuffer.SetData(staticLightPos);

        if(numOfPair > 0)
        {
            lightShadowPairBuffer.Release();
            lightShadowPairBuffer = new ComputeBuffer(numOfPair, sizeof(int));
            lightShadowPairBuffer.SetData(lightShadowPair);
            compute.SetBuffer(0, "lightShadowPair", lightShadowPairBuffer);

            shadowNumLightBuffer.Release();
            shadowNumLightBuffer = new ComputeBuffer(numOfStaticLights, sizeof(int));
            shadowNumLightBuffer.SetData(shadowNumLight);
            compute.SetBuffer(0, "shadowNumLight", shadowNumLightBuffer);
        }

        compute.SetInt("numOfPair", numOfPair);
        compute.SetBuffer(0, "staticLightPos", staticLightPosBuffer);

        if (updateRad)
        {
            staticLightRadBuffer.Release();
            staticLightRadBuffer = new ComputeBuffer(numOfStaticLights, sizeof(float));
            staticLightRadBuffer.SetData(staticLightRad);
            compute.SetBuffer(0, "staticLightRad", staticLightRadBuffer);

            staticLightIntBuffer.Release();
            staticLightIntBuffer = new ComputeBuffer(numOfStaticLights, sizeof(float));
            staticLightIntBuffer.SetData(staticLightInt);
            compute.SetBuffer(0, "staticLightIntensity", staticLightIntBuffer);
        }
    }


    private void UpdateMovingLightBuffers(bool updateRad)
    {
        compute.SetInt("numOfMovingLights", numOfMovingLights);

        if (numOfMovingLights == 0) return;

        movingLightPosBuffer.Release();
        movingLightPos = LightsManager.GetMovingLightsPos();
        movingLightPosBuffer = new ComputeBuffer(numOfMovingLights, 2 * sizeof(float));
        movingLightPosBuffer.SetData(movingLightPos);
        compute.SetBuffer(0, "movingLightPos", movingLightPosBuffer);

        if (updateRad)
        {
            movingLightRadBuffer.Release();
            movingLightRadBuffer = new ComputeBuffer(numOfMovingLights, sizeof(float));
            movingLightRadBuffer.SetData(movingLightRad);
            compute.SetBuffer(0, "movingLightRad", movingLightRadBuffer);

            movingLightIntBuffer.Release();
            movingLightIntBuffer = new ComputeBuffer(numOfMovingLights, sizeof(float));
            movingLightIntBuffer.SetData(movingLightInt);
            compute.SetBuffer(0, "movingLightIntensity", movingLightIntBuffer);
        }
    }


    private void UpdateLightShadowPairs()
    {
        if (numOfStaticLights <= 0 || numOfShadow <= 0 || refMap == null) return;

        numOfPair = 0;

        List<int> tempLightShadowPair = new List<int>();
        List<int> tempShadowNumLight = new List<int>();

        for (int i = 0; i < numOfShadow; i++)
        {
            float rad = shadowLength[i] * shadowSizeMultiplier / 4;
            int len = refMap.WorldToCell(shadowPos[i]).x - refMap.WorldToCell(shadowPos[i] + rad * Vector2.right).x;
            len = Mathf.Abs(len);

            int numOfLight = 0;

            for (int x = 0; x < len; x++)
            {
                for (int y = 0; y < len; y++)
                {
                    if (!staticLightPosInt.Contains(new Vector2Int(x, y))) continue;

                    numOfLight++;
                    numOfPair++;
                    lightShadowPair.Add(staticLightPosInt.IndexOf(new Vector2Int(x, y)));
                }
            }

            shadowNumLight.Add(numOfLight);
        }

        lightShadowPair = tempLightShadowPair;
        shadowNumLight = tempShadowNumLight;
    }


    private void Awake()
    {
        return;
        renderTexture = new RenderTexture(GameManager.instance.screen.x / 2, GameManager.instance.screen.y / 2, 0);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        image.texture = renderTexture;

        shadowSizeMultiplier = renderTexture.width / 100;

        staticLightPosBuffer = new ComputeBuffer(1, 2 * sizeof(float));
        staticLightRadBuffer = new ComputeBuffer(1, sizeof(float));
        staticLightIntBuffer = new ComputeBuffer(1, sizeof(float));
        lightShadowPairBuffer = new ComputeBuffer(1, sizeof(int));
        shadowNumLightBuffer = new ComputeBuffer(1, sizeof(int));
        movingLightPosBuffer = new ComputeBuffer(1, 2 * sizeof(float));
        movingLightRadBuffer = new ComputeBuffer(1, sizeof(float));
        movingLightIntBuffer = new ComputeBuffer(1, sizeof(float));

        shadowNumVerticesBuffer = new ComputeBuffer(1, sizeof(int));
        shadowVerticesBuffer = new ComputeBuffer(1, sizeof(float) * 2);
        shadowLengthBuffer = new ComputeBuffer(1, sizeof(float));
        shadowPosBuffer = new ComputeBuffer(1, sizeof(float) * 2);

        staticLightInt = LightsManager.GetStaticLightsInt();
        staticLightPos = LightsManager.GetStaticLights();
        staticLightRad = LightsManager.GetStaticLightsRad();
        movingLightInt = LightsManager.GetMovingLightsInt();
        movingLightRad = LightsManager.GetMovingLightsRad();

        numOfStaticLights = staticLightInt.Count;
        numOfMovingLights = movingLightInt.Count;


        UpdateMovingLightBuffers(true);
        UpdateStaticLightBuffers(true);

        sunIntensity = shadowIntensity;

        compute.SetVector("resolution", new Vector2(renderTexture.width, renderTexture.height));
        compute.SetFloat("camSize", cam.orthographicSize);
        compute.SetFloat("sizeMultiplier", shadowSizeMultiplier);
        compute.SetFloat("shadowDivergence", shadowDivergence);
        compute.SetFloat("shadowIntensity", shadowIntensity);
        compute.SetFloat("shadowMin", shadowMin);
        compute.SetFloat("shadowMax", shadowMax);
        compute.SetBuffer(0, "staticLightPos", staticLightPosBuffer);
        compute.SetBuffer(0, "staticLightRad", staticLightRadBuffer);
        compute.SetBuffer(0, "staticLightIntensity", staticLightIntBuffer);
        compute.SetBuffer(0, "lightShadowPair", lightShadowPairBuffer);
        compute.SetBuffer(0, "shadowNumLight", shadowNumLightBuffer);
        compute.SetBuffer(0, "movingLightIntensity", movingLightIntBuffer);
        compute.SetBuffer(0, "movingLightPos", movingLightPosBuffer);
        compute.SetBuffer(0, "movingLightRad", movingLightRadBuffer);
        compute.SetBuffer(0, "shadowNumVertices", shadowNumVerticesBuffer);
        compute.SetBuffer(0, "shadowVertices", shadowVerticesBuffer);
        compute.SetBuffer(0, "shadowLength", shadowLengthBuffer);
        compute.SetBuffer(0, "shadowPos", shadowPosBuffer);
        compute.SetInt("numOfStaticLights", 0);
        compute.SetInt("numOfMovingLights", 0);

        LightsManager.hasAddedLight.AddListener(AddLight);
        LightsManager.hasRemovedLight.AddListener(RemoveLight);
    }

    Vector3 screenToWorld;
    Vector2 imagePos = Vector2.zero;
    private void Update()
    {
        UpdateMovingLightBuffers(false);
        UpdateStaticLightBuffers(false);

        screenToWorld = cam.ViewportToWorldPoint(Vector3.zero);

        compute.SetVector("bottomLeft", new Vector2(screenToWorld.x, screenToWorld.y));
        compute.SetTexture(0, "Result", renderTexture);

        compute.SetFloat("sunIntensity", sunIntensity);
        compute.SetFloat("lengthFactor", lengthFactor);
        compute.SetVector("sunAngle", sunAngle);
        compute.SetVector("sunColor", sunColor);
    }

    private Queue<Vector2> bottomQueue = new Queue<Vector2>();
    private void LateUpdate()
    {
        compute.Dispatch(0, renderTexture.width / 32, renderTexture.height / 32, 1);

        bottomQueue.Enqueue(new Vector2(screenToWorld.x, screenToWorld.y));

        Vector2 offset = new Vector2(screenToWorld.x, screenToWorld.y) - imagePos;
        RectTransform tr = image.GetComponent<RectTransform>();
        tr.position =
            new Vector3(cam.transform.position.x, cam.transform.position.y, tr.position.z)
            + new Vector3(offset.x, offset.y, 0);

        AsyncGPUReadback.Request(staticLightIntBuffer, (req) => {
            imagePos = bottomQueue.Dequeue();
        });
    }

    private void OnDisable()
    {
        shadowNumVerticesBuffer.Release();
        shadowVerticesBuffer.Release();
        shadowLengthBuffer.Release();
        shadowPosBuffer.Release();

        staticLightPosBuffer.Release();
        staticLightRadBuffer.Release();
        staticLightIntBuffer.Release();
        lightShadowPairBuffer.Release();
        shadowNumLightBuffer.Release();
        movingLightPosBuffer.Release();
        movingLightRadBuffer.Release();
        movingLightIntBuffer.Release();
    }
}
