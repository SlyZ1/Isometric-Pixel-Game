using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "ScriptableObjects/Item")]

public class Item : ScriptableObject
{
    [Header("General Section")]
    [Space]
    [SerializeField] public Sprite sprite;
    [SerializeField] public ItemType type;
    [SerializeField] public ActionType action;
    [SerializeField] public bool stackable;
    [SerializeField] public ushort id;
    [SerializeField] public ScriptableObject item;
    [Space(32)]
    [Header("Block Section")]
    [Space]
    [SerializeField] public Block block;
    [Space(32)]
    [Header("Tool Section")]
    [Space]
    [SerializeField] public LayerMask targetLayer;
    [SerializeField] public int damage;
    [SerializeField] public float range;
    [SerializeField] public float angleRange;
    [SerializeField] public int durability;
    [SerializeField] public float slowness;
}


public enum ItemType
{
    Block,
    Tool,
    Default,
    Weapon
}


public enum ActionType
{
    Close,
    Projectile,
    Zone,
    Custom
}
