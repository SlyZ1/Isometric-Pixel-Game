using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Variation
{
    public GameObject prefab;
    public bool spriteBased;
    public Sprite[] instances;
    public bool interactable;
    public Sprite[] outlines;
    public bool colorBased;
    public bool isAdditive;
    public bool isShadowed;
    [Range(0f, 1f)] public float probability;
    public float shadowLength;
    public bool isDrop;
    public Item drop;
    public WeightedRandomList<int> dropCount;
}

[CreateAssetMenu(menuName = "ScriptableObjects/Prop")]
public class Object : ScriptableObject
{
    public List<Variation> variations;
    [Range(0f, 1f)] public float mainThreshold;
    [Range(0f, 1f)] public float waterThresholdOffset;
    public bool inverseWaterThresh;
    [Range(0f, 1f)] public float grassThresholdOffset;
    public bool inverseGrassThresh;
    public Vector2Int offset;
    public Vector2Int modulo = Vector2Int.one;
    public Vector2Int scale = Vector2Int.one;
    public bool isSolid = true;
}
