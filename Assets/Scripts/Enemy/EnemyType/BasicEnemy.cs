using UnityEngine;
using Unity.Netcode;
using Pathfinding;
using System.Collections;

public class BasicEnemy : MonoBehaviour, IEnemy
{
    [SerializeField] private float speed = 200f;
    [SerializeField] private float maxDistance;
    [SerializeField] private ushort attack;
    [SerializeField] private float knockback;

    private Rigidbody2D enemy;
    private EnemyAStar astar;

    public Transform target;
    private float distance = 0;
    private byte targetId = 0;

    [Space]
    [SerializeField] private ushort life;

    private void Awake()
    {
        astar = GetComponent<EnemyAStar>();
        enemy = GetComponent<Rigidbody2D>();
        astar.speed = speed / 100f;
    }

    private void FixedUpdate()
    {
        if (!GameManager.instance.IsServer)
        {
            if(Vector2.SqrMagnitude(target.position - transform.position) > 0.01f)
            {
                enemy.AddForce((target.position - transform.position).normalized * speed * Time.fixedDeltaTime);
            }
        }
        else
        {
            if (EnemyManager.instance.graphs.ContainsKey(targetId))
            {
                astar.seeker.graphMask = GraphMask.FromGraph(EnemyManager.instance.graphs[targetId].graph);
            }

            if (!astar.reached) enemy.AddForce(astar.dir * speed * Time.fixedDeltaTime);

            UpdateTarget();
        }
    }


    public byte TargetId()
    {
        return targetId;
    }


    public void SetTarget(byte _targetId)
    {
        targetId = _targetId;

        if (!GameManager.instance.IsServer) return;

        target = GameManager.instance.players[targetId];
        astar.target = target;
    }


    public void SetFakeTarget(Transform fakeTarget)
    {
        if(GameManager.instance.IsServer) astar.target = fakeTarget;
        target = fakeTarget;
    }


    public void RemoveTarget()
    {
        if (GameManager.instance.IsServer)
        {
            target = null;
            astar.target = null;
        }
    }


    private void UpdateTarget()
    {
        if (transform == null) return;

        int tempId = -1;

        if (target != null)
        {
            distance = Vector2.Distance(transform.position, GameManager.instance.players[targetId].transform.position);
        }

        foreach (var _player in GameManager.instance.players)
        {
            float _dist = Vector2.Distance(transform.position, _player.Value.position);

            if (_dist < distance || target == null)
            {
                distance = _dist;
                target = _player.Value;
                tempId = (int)_player.Value.GetComponent<NetworkObject>().OwnerClientId;
            }
        }

        if (distance <= maxDistance && tempId != -1 && tempId != targetId)
        {
            targetId = (byte)tempId;
            EnemyManager.instance.ChangeTargetEnemyServer(EnemyManager.instance.enemiesKey[gameObject], targetId);
        }
        else if (distance > maxDistance)
        {
            target = null;
            EnemyManager.instance.RemoveTargetEnemyServer(EnemyManager.instance.enemiesKey[gameObject]);
        }

        astar.target = target;
    }


    public void Attack(byte clientId)
    {

    }


    public void Damage(ushort damage)
    {
        if (life <= damage) EnemyManager.instance.DespawnEnemyNetwork(gameObject);

        life -= damage;
    }


    public void Knockback(Vector2 knockback)
    {
        enemy.AddForce(knockback, ForceMode2D.Impulse);
    }


    public ushort Life()
    {
        return life;
    }


    public void SetLife(ushort life)
    {
        this.life = life;
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Attack")) StartCoroutine(GetDir(collision));
    }


    private void OnTriggerStay2D(Collider2D collision)
    {
        if (GameManager.instance.IsServer && collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Push((transform.position - collision.transform.position).normalized);
        }
    }


    public void Push(Vector2 push)
    {
        enemy.AddForce(push);
    }


    private IEnumerator GetDir(Collider2D collision)
    {
        if (collision == null) yield break;

        Vector3 dir = collision.transform.position;

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        if (collision == null) yield break;

        dir = collision.transform.position - dir;

        CombatManager.instance.HitEnemy(gameObject, collision, dir);
    } 


    public ushort GetAttack()
    {
        return attack;
    }


    public float GetKnockback()
    {
        return knockback;
    }
}
