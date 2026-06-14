using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/CircleAttack")]
public class CircleAttack : ScriptableObject, IAttack
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
    [SerializeField] private int numberOfRound;
    [SerializeField] private AnimationCurve distanceCurve;

    public Vector2 CalculatePosition(Vector2 pos, Vector2 dir, float lerp, byte shooterId)
    {
        Vector2 position = GameManager.instance.players[shooterId].position;
        float evaluated = curve.Evaluate(lerp);
        Vector2 rot = Quaternion.Euler(0,0,evaluated * numberOfRound * 360) * dir * distanceCurve.Evaluate(lerp);
        return position + new Vector2(rot.x, rot.y / 2);
    }
}