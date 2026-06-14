using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ExtrapolatedTransfrom : MonoBehaviour
{
    public Transform trans;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float extrapolationValue;


    private void Awake()
    {
        trans = new GameObject().transform;
    }


    private void FixedUpdate()
    {
        Vector2 extrapolation = (rb.velocity == Vector2.zero ? Random.insideUnitCircle.normalized : rb.velocity) * extrapolationValue;
        trans.position = ((Vector2) transform.position) + extrapolation;
    }
}
