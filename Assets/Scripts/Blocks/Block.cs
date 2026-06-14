using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Block")]
public class Block : ScriptableObject
{
    public GameObject prefab;
    public Vector2Int size;
}
