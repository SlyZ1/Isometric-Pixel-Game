using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public Vector2 direction;

    public void Enable()
    {
        CharacterController cc = GetComponent<CharacterController>();
        if(cc != null) cc.canWalk = true;
    }

    private void Start()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, 1);

    }

    private void FixedUpdate()
    {
        if(direction.magnitude < 0.2f)
        {
            animator.SetBool("Idle", true);
        }
        else
        {
            animator.SetBool("Idle", false);
            animator.SetFloat("directionX", direction.normalized.x);
            animator.SetFloat("directionY", direction.normalized.y);
        }
    }
}
