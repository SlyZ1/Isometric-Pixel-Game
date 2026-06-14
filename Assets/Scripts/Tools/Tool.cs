using System.Collections;
using UnityEngine;


[CreateAssetMenu(menuName = "ScriptableObjects/Tool")]
public class Tool : ScriptableObject
{
    [SerializeField] public LayerMask targetLayer;
    [SerializeField] public int damage;
    [SerializeField] public float range;
    [SerializeField] public float angleRange;
    [SerializeField] public int durability;
    [SerializeField] public float slowness;
}