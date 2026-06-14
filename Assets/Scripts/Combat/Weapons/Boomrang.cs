using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "ScriptableObjects/Boomrang")]
public class Boomrang : ScriptableObject, IAttack
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
    [SerializeField] private float roundness;

    public Vector2 CalculatePosition(Vector2 pos, Vector2 dir, float lerp, byte shooterId)
    {
        Vector2 position = GameManager.instance.players[shooterId].position;
        float interpolatedLerp = curve.Evaluate(lerp);
        float interpolatedLerp2 = curve.Evaluate(lerp / 2);
        Vector2 realDir = Quaternion.Euler(0, 0, 0) * new Vector3(dir.x, dir.y, 0);
        Vector2 newDir = Quaternion.Euler(0, 0, roundness) * new Vector3(dir.x, dir.y, 0);

        if (lerp <= 0.5f)
        {
            return Vector2.Lerp(pos, pos + (Vector2)Vector3.Slerp(realDir, newDir, interpolatedLerp2), interpolatedLerp);
        }
        else
        {
            return Vector2.Lerp(position, pos + (Vector2)Vector3.Slerp(realDir, newDir, interpolatedLerp2), interpolatedLerp);
        }
    }
}
