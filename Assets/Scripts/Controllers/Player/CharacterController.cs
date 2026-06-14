using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class CharacterController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform front;
    [SerializeField] private Rigidbody2D player;
    [SerializeField] private float walkSpeed;

    public Vector2 direction;
    public bool canWalk = true;

    public void Disable()
    {
        canWalk = false;
    }

    public void OnMove(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();
        direction = new Vector2(input.x, input.y/2).normalized;

        if(Mathf.Abs(direction.x) > 0.01f || Mathf.Abs(direction.y) > 0.01f)
        {
            front.forward = new Vector3(direction.x, direction.y, 0);
        }
    }


    public void Knockback(Vector2 knockback)
    {
        player.AddForce(knockback, ForceMode2D.Impulse);
    }


    private void FixedUpdate()
    {
        //if (canWalk) player.AddForce(direction * walkSpeed);
        if (canWalk) player.transform.position += (Vector3)direction * walkSpeed * Time.fixedDeltaTime;
    }
}
