using System;
using System.Collections;
using UnityEngine;


public interface IAttack
{
    public GameObject prefab { get; }
    public int passTroughValue { get; set; }
    public ushort damages { get; }
    public float speed { get; }
    public float knockback { get; }
    public LayerMask targetMask { get; }
    public GameObject hit { get; }
    public Vector2 CalculatePosition(Vector2 pos, Vector2 dir, float lerp, byte shooterId);
}