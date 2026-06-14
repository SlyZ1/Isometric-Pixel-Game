using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyManager : NetworkBehaviour
{
    [SerializeField] public GameObject[] enemyPrefabs;
    [SerializeField] private LayerMask obstacle;
    [SerializeField] private float maxDistanceGraph;

    public ProceduralGridMover pgm;
    public static EnemyManager instance;

    public Dictionary<byte, ProceduralGridMover> graphs = new Dictionary<byte, ProceduralGridMover>();

    public Dictionary<GameObject, int> enemyType = new Dictionary<GameObject, int>();
    public Dictionary<GameObject, ushort> enemiesKey = new Dictionary<GameObject, ushort>();
    public Dictionary<ushort, GameObject> enemies = new Dictionary<ushort, GameObject>();
    private Dictionary<ushort, Transform> targets = new Dictionary<ushort, Transform>();


    private int packetSize = 6;

    private const int verifRate = 20;
    private const float errorThreshold = 0.1f;


    private void Awake()
    {
        if(instance != this && instance != null)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }


    private void CreateGraph(byte clientId)
    {
        GridGraph graph = (GridGraph) AstarPath.active.data.AddGraph(typeof(GridGraph));

        graph.SetGridShape(InspectorGridMode.IsometricGrid);
        graph.SetDimensions(28, 28, 0.22625f);
        graph.isometricAngle = 60;
        graph.center = Vector3.zero;
        graph.rotation = new Vector3(0, 45, 0);
        graph.is2D = true;
        graph.collision.use2D = true;
        graph.collision.diameter = 0.9f;
        graph.collision.mask = obstacle;

        graph.Scan();

        ProceduralGridMover _pgm = transform.GetChild(0).gameObject.AddComponent<ProceduralGridMover>();
        if (GameManager.instance.players.ContainsKey(clientId)) _pgm.target = GameManager.instance.players[clientId];
        _pgm.updateDistance = 10;
        _pgm.graph = graph;

        graphs[clientId] = _pgm;
    }


    private void RemoveGraph(byte clientId)
    {
        AstarPath.active.data.RemoveGraph(graphs[clientId].graph);
        Destroy(graphs[clientId]);
        graphs.Remove(clientId);
    }


    public void Enable()
    {
        if (transform.GetChild(0).gameObject.activeSelf) return;

        transform.GetChild(0).gameObject.SetActive(true);
        graphs[(byte)NetworkManager.Singleton.LocalClientId] = pgm;
        pgm.graph?.Scan();
    }


    public void Disable()
    {
        transform.GetChild(0).gameObject.SetActive(false);
    }


    public ClientRpcParams paramsExclude(ulong[] clientIds)
    {
        List<ulong> clientList = new List<ulong>();
        int idLength = clientIds.Length;

        foreach (var player in GameManager.instance.players)
        {
            for (int j = 0; j < idLength; j++)
            {
                if (player.Key == clientIds[j]) break;

                if (j == idLength - 1)
                {
                    clientList.Add(player.Key);
                }
            }
        }

        ClientRpcParams clientRpcParams1 = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = clientList
            }
        };

        return clientRpcParams1;
    }


    public override void OnNetworkSpawn()
    {
        if (IsHost) 
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        }
        Enable();
    }


    private void OnClientConnectedCallback(ulong clientId)
    {
        if (clientId == GameManager.instance.playerId) return;

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        if(enemies.Count > 0)
        {
            List<EnemyData> enemyData = new List<EnemyData>();

            foreach (var enemy in enemies)
            {
                enemyData.Add(new EnemyData()
                {
                    key = enemy.Key,
                    index = (byte)enemyType[enemy.Value],
                    life = (ushort)Mathf.Max(0, enemy.Value.GetComponent<IEnemy>().Life()),
                    pos = enemy.Value.transform.position,
                    targetId = enemy.Value.GetComponent<IEnemy>().TargetId()
                });
            }
            
            InitializeEnemiesClientRpc(enemyData.ToArray(), clientRpcParams);
        }
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void InitializeEnemiesClientRpc(EnemyData[] enemyData, ClientRpcParams clientRpcParams = default)
    {
        foreach(EnemyData enemy in enemyData)
        {
            GameObject _enemy = CreateEnemy(enemy.index, enemy.pos, enemy.key);
            _enemy.GetComponent<IEnemy>().SetLife(enemy.life);
            _enemy.GetComponent<IEnemy>().SetTarget(enemy.targetId);
        }
    }



    private GameObject CreateEnemy(byte enemyIndex, Vector2 position, ushort key)
    {
        GameObject newEnemy = Instantiate(enemyPrefabs[enemyIndex], position, Quaternion.identity);
        enemies[key] = newEnemy;
        enemiesKey[newEnemy] = key;
        enemyType[newEnemy] = enemyIndex;
        if (!IsServer)
        {
            targets[key] = new GameObject().transform;
            newEnemy.GetComponent<IEnemy>().SetFakeTarget(targets[key]);
        }
        return newEnemy;
    }


    private void RemoveEnemy(ushort key)
    {
        if (!enemies.ContainsKey(key)) return;

        GameObject enemy = enemies[key];
        enemies.Remove(key);
        enemiesKey.Remove(enemy);
        enemyType.Remove(enemy);
        Destroy(enemy);
    }



    public void DespawnEnemyNetwork(GameObject enemy)
    {
        if (!enemiesKey.ContainsKey(enemy)) return;

        ushort key = enemiesKey[enemy];
        RemoveEnemy(key);
        DespawnEnemyServerRpc(key, (byte)GameManager.instance.playerId);
    }


    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void DespawnEnemyServerRpc(ushort key, byte clientId)
    {
        DespawnEnemyClientRpc(key, clientId);
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void DespawnEnemyClientRpc(ushort key, byte clientId)
    {
        if (clientId == GameManager.instance.playerId) return;
        RemoveEnemy(key);
    }


    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void SpawnEnemyServerRpc(byte enemyIndex, Vector2 position)
    {
        ushort key = (ushort)Random.Range(0, ushort.MaxValue);
        int i = 0;
        while (enemies.ContainsKey(key))
        {
            if (i > 10000) return;
            key = (ushort)Random.Range(0, ushort.MaxValue);
            i += 1;
        }

        SpawnEnemyClientRpc(enemyIndex, position, key);
    }



    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void SpawnEnemyClientRpc(byte enemyIndex, Vector2 position, ushort key)
    {
        CreateEnemy(enemyIndex, position, key);
    }


    public void ChangeTargetEnemyServer(ushort enemyKey, byte targetId)
    {
        ChangeTargetEnemyClientRpc(enemyKey, targetId, paramsExclude(new ulong[] { 0 }));
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void ChangeTargetEnemyClientRpc(ushort enemyKey, byte targetId, ClientRpcParams clientRpcParams = default)
    {
        if(!enemies.ContainsKey(enemyKey))
        {
            Debug.Log("No enemy is identified with key " + enemyKey);
            return;
        }

        enemies[enemyKey].GetComponent<IEnemy>().SetTarget(targetId);
    }


    public void RemoveTargetEnemyServer(ushort enemyKey)
    {
        RemoveTargetEnemyClientRpc(enemyKey);
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void RemoveTargetEnemyClientRpc(ushort enemyKey)
    {
        if (!enemies.ContainsKey(enemyKey))
        {
            Debug.Log("No enemy is identified with key " + enemyKey);
            return;
        }

        enemies[enemyKey]?.GetComponent<IEnemy>().RemoveTarget();
    }



    public void DamageEnemyNetwork(ushort key, ushort damage, Vector2 knockback)
    {
        KnockbackEnemy(key, knockback);
        DamageEnemyServerRpc(key, damage, knockback, (byte)GameManager.instance.playerId);
        enemies[key]?.GetComponent<IEnemy>().Damage(damage);
    }


    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void DamageEnemyServerRpc(ushort key, ushort damage, Vector2 knockback, byte clientId)
    {
        if (!enemies.ContainsKey(key)) return;
        DamageEnemyClientRpc(key, damage, knockback, paramsExclude(new ulong[] { clientId }));
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void DamageEnemyClientRpc(ushort key, ushort damage, Vector2 knockback, ClientRpcParams clientRpcParams = default)
    {
        KnockbackEnemy(key, knockback);
        enemies[key]?.GetComponent<IEnemy>().Damage(damage);
    }



    private void KnockbackEnemy(ushort key, Vector2 knockback)
    {
        enemies[key]?.GetComponent<IEnemy>()?.Knockback(knockback);
    }


    public void PushEnemy(GameObject enemy, Vector2 push)
    {
        PushEnemyServerRpc(enemiesKey[enemy], push, (byte)GameManager.instance.playerId);
    }


    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void PushEnemyServerRpc(ushort key, Vector2 push, byte clientId)
    {
        PushEnemyClientRpc(key, push, clientId);
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void PushEnemyClientRpc(ushort key, Vector2 push, byte clientId)
    {
        if (clientId == GameManager.instance.playerId) return;

        enemies[key].GetComponent<IEnemy>().Push(push);
    }



    private void FixedUpdate()
    {
        if (GameManager.instance.players.Count <= 0) return;

        if (IsServer)
        {
            SendVerification();
            UpdateGraphs();
        }
    }



    private void SendVerification()
    {
        if (GameManager.instance.tick % verifRate != 0 || GameManager.instance.tick == 0 || enemies.Count <= 0) return;

        List<ushort> keys = new List<ushort>();
        List<Vector2> pos = new List<Vector2>();
        List<Vector2> vel = new List<Vector2>();

        foreach(var enemy in enemies)
        {
            keys.Add(enemy.Key);
            pos.Add(enemy.Value.transform.position);
            vel.Add(enemy.Value.GetComponent<Rigidbody2D>().velocity);
        }

        VerificationClientRpc(keys.ToArray(), pos.ToArray(), vel.ToArray());
    }



    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void VerificationClientRpc(ushort[] keys, Vector2[] pos, Vector2[] vel)
    {
        if (IsServer) return;

        int len = keys.Length;
        for (int i = 0; i < len; i++)
        {
            if (!enemies.ContainsKey(keys[i])) continue;

            GameObject enemy = enemies[keys[i]];

            if (Vector2.SqrMagnitude(pos[i] - (Vector2)enemy.transform.position) >= errorThreshold)
            {
                enemy.transform.position = pos[i];
                enemy.GetComponent<Rigidbody2D>().velocity = vel[i];
            }
        }
    }



    public void SendCheckPoint(ushort key, Vector2 pos)
    {
        CheckPointClientRpc(key, pos);
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void CheckPointClientRpc(ushort key, Vector2 pos)
    {
        if(IsServer || !targets.ContainsKey(key)) return;

        targets[key].position = pos;
    }

    


    private void UpdateGraphs()
    {
        Transform mainPlayer = GameManager.instance.players[GameManager.instance.playerId];

        foreach (var player in GameManager.instance.players)
        {
            if (player.Value == mainPlayer) continue;

            float distance = Vector2.Distance(player.Value.position, mainPlayer.position);

            if (distance > maxDistanceGraph && !graphs.ContainsKey((byte)player.Key))
            {
                CreateGraph((byte)player.Key);
            }

            if (distance <= 0.8f * maxDistanceGraph && graphs.ContainsKey((byte)player.Key))
            {
                RemoveGraph((byte)player.Key);
            }
        }
    }
}


public struct EnemyData : INetworkSerializable
{
    public ushort key;
    public byte index;
    public ushort life;
    public byte targetId;
    public Vector2 pos;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref key);
        serializer.SerializeValue(ref index);
        serializer.SerializeValue(ref life);
        serializer.SerializeValue(ref pos);
    }
}
