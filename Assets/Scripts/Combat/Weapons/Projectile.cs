using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "ScriptableObjects/Projectile")]
public class Projectile : ScriptableObject, IAttack
{
    [SerializeField] private GameObject _prefab;
    public GameObject prefab { get { return _prefab; } }


    [SerializeField] private int _passThroughValue;
    public int passTroughValue { get { return _passThroughValue; } set { _passThroughValue = value; } }


    [SerializeField] private ushort _damages;
    public ushort damages { get { return _damages; } set { _damages = value; } }


    [SerializeField] private float _speed;
    public float speed { get { return _speed; } }


    [SerializeField] private float _knockback;
    public float knockback { get { return _knockback; } }


    [SerializeField] private LayerMask _targetMask;
    public LayerMask targetMask { get { return _targetMask; } }


    [SerializeField] private GameObject _hit;
    public GameObject hit { get { return _hit; } }

    [Space]
    [Header("Custom")]
    [Space]
    [SerializeField] private AnimationCurve curve;

    public Vector2 CalculatePosition(Vector2 pos, Vector2 dir, float lerp, byte shooterId)
    {
        return Vector2.Lerp(pos, pos + dir, curve.Evaluate(lerp));
    }
}
