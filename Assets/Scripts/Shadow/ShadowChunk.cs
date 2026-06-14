using System.Collections.Generic;
using UnityEngine;

public class ShadowChunk : MonoBehaviour
{
    [HideInInspector] public ShadowManager manager;

    private List<int> numVertices = new List<int>();
    private List<Vector2[]> vertices = new List<Vector2[]>();
    private List<Vector2> serializedVertices = new List<Vector2>();
    private List<float> lengths = new List<float>();
    private List<Vector2> positions = new List<Vector2>();
    private List<ShadowInstance> shadowInstances = new List<ShadowInstance>();

    private int numOfShadows = 0;

    private bool hasAwakened;

    public void AddShadow(int _numVertices, Vector2[] _vertices, float _length, Vector2 _position, ShadowInstance _shadow)
    {
        numVertices.Add(_numVertices);
        serializedVertices.AddRange(_vertices);
        vertices.Add(_vertices);
        lengths.Add(_length);
        positions.Add(_position);
        shadowInstances.Add(_shadow);
        numOfShadows++;
    }

    public void ManualAwake()
    {
        if (numOfShadows == 0) return;

        SpawnShadows();

        hasAwakened = true;
    }

    private void OnEnable()
    {
        if (numOfShadows == 0) return;
        if (!hasAwakened || manager.shadowIndexes.ContainsKey(shadowInstances[0])) return;

        SpawnShadows();
    }


    private void OnDisable()
    {
        if (numOfShadows == 0) return;

        DespawnShadows();
    }


    private void SpawnShadows()
    {
        manager.AddShadows(numVertices, vertices, lengths, positions, shadowInstances, numOfShadows);
    }

    private void DespawnShadows()
    {
        manager.RemoveShadows(shadowInstances, numOfShadows);
    }
}
