using System.Collections;
using UnityEngine;

public class LifeHandler : MonoBehaviour
{
    [SerializeField] private float respawnTime;
    [SerializeField] private ushort maxLife;
    private ushort life;
    private CharacterController controller;

    [HideInInspector] public bool isDead = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        life = maxLife;
    }

    public void Damage(ushort damage)
    {
        if(isDead) return;

        if(life <= damage)
        {
            if(controller != null) controller.enabled = false;
            isDead = true;
            StartCoroutine(Respawn());
            life = maxLife;
            return;
        }

        life -= damage;
    }


    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);

        isDead = false;

        if (controller == null) yield break;

        controller.enabled = true;
    }


    public void ChangeMaxLife(ushort maxLife)
    {
        this.maxLife = maxLife;
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        StartCoroutine(Damaged(collision));
    }

    private IEnumerator Damaged(Collider2D collision)
    {
        if(collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            IEnemy enemy = collision.GetComponent<IEnemy>();
            Damage(enemy.GetAttack());
            Vector2 knockback = (transform.position - collision.transform.position).normalized * enemy.GetKnockback();

            controller?.Knockback(knockback);
        }

        if(collision.gameObject.layer == LayerMask.NameToLayer("Attack"))
        {
            if((collision.GetComponent<IAttack>().targetMask & (1 << LayerMask.NameToLayer("Player"))) != 0)
            {
                Vector2 dir = transform.position;
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                dir = (Vector2)collision.transform.position - dir;

                CombatManager.instance.HitPlayer(gameObject, collision, dir);
            }
        }
    }
}