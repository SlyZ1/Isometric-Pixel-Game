using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class CombatManager : NetworkBehaviour
{
    [SerializeField] public List<ScriptableObject> attacks;
    [SerializeField] public List<ScriptableObject> weapons;
    [SerializeField] private Weapon weapon;
    [SerializeField] private LayerMask blockingMask;

    private struct attackData
    {
        internal ushort key;
        internal byte id;
    }

    private Dictionary<attackData, GameObject> shotAttacks = new Dictionary<attackData, GameObject>();
    private Dictionary<GameObject, IAttack> iattacks = new Dictionary<GameObject, IAttack>();
    private Dictionary<IAttack, attackData> attackDatas = new Dictionary<IAttack, attackData>();
    private List<ushort> attackKeys = new List<ushort>();

    public static CombatManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(instance);
        }
        else
        {
            instance = this;
        }
    }


    public void ShootAttackNetwork(ushort key, Vector2 pos, Vector2 dir)
    {
        ushort attackKey = (ushort)Random.Range(0, ushort.MaxValue);
        while (attackKeys.Contains(attackKey))
        {
            attackKey = (ushort)Random.Range(0, ushort.MaxValue);
        }
        attackKeys.Add(attackKey);

        StartCoroutine(ShootAttack(attackKey, key, pos, dir, (byte)GameManager.instance.playerId));
        ShootAttackServerRpc(attackKey, key, pos, dir, (byte)GameManager.instance.playerId);
    }



    public IEnumerator ShootAttack(ushort attackKey, ushort key, Vector2 pos, Vector2 dir, byte shooterId)
    {
        float lerp = 0f;
        IAttack attack = (IAttack)Instantiate(attacks[key]);
        GameObject prefab = Instantiate(attack.prefab, pos, Quaternion.identity);
        iattacks[prefab] = attack;
        prefab.transform.up = dir;

        attackData data = new attackData()
        {
            key = attackKey,
            id = shooterId
        };

        attackDatas[attack] = data;
        shotAttacks[data] = prefab;

        while (lerp <= 1f - 0.01f)
        {
            if (prefab == null) yield break;

            Vector2 forward = attack.CalculatePosition(pos, dir, lerp, shooterId) - (Vector2)prefab.transform.position;
            if(forward.magnitude > 0.001f) prefab.transform.up = forward;

            Vector2 nextPos = attack.CalculatePosition(pos, dir, lerp, shooterId);
            prefab.transform.position = new Vector3(nextPos.x, nextPos.y, 1);

            if(shooterId == GameManager.instance.playerId) HandleCollisions(data, prefab, attack, dir, pos, lerp);

            lerp += (attack.speed / dir.magnitude) * Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        Destroy(prefab);
    }


    private void HandleCollisions(attackData data, GameObject prefab, IAttack attack, Vector2 dir, Vector2 pos, float lerp)
    {
        Collider2D collider = prefab.GetComponent<Collider2D>();
        List<Collider2D> obstacles = new List<Collider2D>();
        ContactFilter2D contactFilter2D = new ContactFilter2D();
        contactFilter2D.useTriggers = true;
        collider.OverlapCollider(contactFilter2D, obstacles);

        foreach (Collider2D obstacle in obstacles)
        {
            if (obstacle == null) continue;

            if ((blockingMask & (1 << obstacle.gameObject.layer)) != 0)
            {
                if(obstacle.isTrigger) continue;

                if(attack.hit != null)
                {
                    float _lerp = lerp - (attack.speed / dir.magnitude) * Time.fixedDeltaTime;
                    Vector2 close = attack.CalculatePosition(pos, dir, _lerp, (byte)GameManager.instance.playerId);
                    Vector2 position = obstacle.ClosestPoint(close);

                    GameObject hitObject = Instantiate(attack.hit, new Vector3(position.x, position.y, 0.9f), prefab.transform.rotation);
                    IHit hit = hitObject.GetComponent<IHit>();
                    hit.Init(attack, dir.magnitude, lerp);
                    hit.ManualStart();
                }
                StartCoroutine(DestroyAttack(data.key, (byte)GameManager.instance.playerId));
                return;
            }
        }
    }


    public void HitEnemy(GameObject enemy, Collider2D _attack, Vector2 dir)
    {
        if (enemy == null || _attack == null || !iattacks.ContainsKey(_attack.gameObject)) return;

        IAttack attack = iattacks[_attack.gameObject];

        if ((attack.targetMask & (1 << enemy.layer)) == 0) return;

        ushort damages = (ushort)Mathf.CeilToInt(attack.damages);

        Vector2 knockback = dir.normalized * attack.knockback;

        if (EnemyManager.instance.enemiesKey.ContainsKey(enemy))
        {
            EnemyManager.instance.DamageEnemyNetwork(EnemyManager.instance.enemiesKey[enemy], damages, knockback);
        }

        if (attack.passTroughValue <= -1) return;

        attack.passTroughValue -= 1;
        if (attack.passTroughValue <= 0)
        {
            StartCoroutine(DestroyAttack(attackDatas[attack].key, (byte)GameManager.instance.playerId));
            return;
        }
    }



    public void HitPlayer(GameObject player, Collider2D _attack, Vector2 dir)
    {
        if (player == null || _attack == null || !iattacks.ContainsKey(_attack.gameObject)) return;

        IAttack attack = iattacks[_attack.gameObject];

        ushort damages = (ushort)Mathf.CeilToInt(attack.damages);
        player.GetComponent<LifeHandler>().Damage(damages);

        Vector2 knockback = dir.normalized * attack.knockback;

        player.GetComponent<CharacterController>()?.Knockback(knockback);

        if (attack.passTroughValue <= -1) return;

        attack.passTroughValue -= 1;
        if (attack.passTroughValue <= 0)
        {
            StartCoroutine(DestroyAttack(attackDatas[attack].key, (byte)GameManager.instance.playerId));
            return;
        }
    }



    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void ShootAttackServerRpc(ushort attackKey, ushort key, Vector2 pos, Vector2 dir, byte shooterId)
    {
        ShootAttackClientRpc(attackKey, key, pos, dir, shooterId, EnemyManager.instance.paramsExclude(new ulong[] { shooterId }));
    }



    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void ShootAttackClientRpc(ushort attackKey, ushort key, Vector2 pos, Vector2 dir, byte shooterId, ClientRpcParams clientRpcParams = default)
    {
        StartCoroutine(ShootAttack(attackKey, key, pos, dir, shooterId));
    }



    private IEnumerator DestroyAttack(ushort attackKey, byte shooterId)
    {
        attackData data = new attackData()
        {
            key = attackKey,
            id = shooterId
        };

        if (GameManager.instance.playerId == shooterId)
        {
            if (shotAttacks.ContainsKey(data))
            {
                DestroyAttackServerRpc(attackKey, shooterId);
                GameObject prefab = shotAttacks[data];
                if (prefab != null)
                {
                    attackDatas.Remove(iattacks[prefab]);
                    iattacks.Remove(prefab);
                    Destroy(prefab);
                }
                if (attackKeys.Contains(data.key)) attackKeys.Remove(data.key);
                shotAttacks.Remove(data);
                yield break;
            }
        }
        else
        {
            float time = 0;
            while(time < 1)
            {
                if (shotAttacks.ContainsKey(data))
                {
                    GameObject prefab = shotAttacks[data];
                    if (prefab != null)
                    {
                        attackDatas.Remove(iattacks[prefab]);
                        iattacks.Remove(prefab);
                        Destroy(prefab);
                    }
                    if (attackKeys.Contains(data.key)) attackKeys.Remove(data.key);
                    shotAttacks.Remove(data);
                    yield break;
                }

                time += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }
    }


    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void DestroyAttackServerRpc(ushort attackKey, byte shooterId)
    {
        DestroyAttackClientRpc(attackKey, shooterId, EnemyManager.instance.paramsExclude(new ulong[] { shooterId }));
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void DestroyAttackClientRpc(ushort attackKey, byte shooterId, ClientRpcParams clientRpcParams = default)
    {
        StartCoroutine(DestroyAttack(attackKey, shooterId));
    }
}
