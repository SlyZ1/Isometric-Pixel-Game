using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

public class GameManager : NetworkBehaviour
{
    [Header("Managers")]
    [Space]
    public GameObject managers;
    public GameObject generators;
    public UIManager uiManager;
    public ProceduralGridMover pgm;
    public DayNightCycle dnc;
    public ItemBarScroll itemBarScroll;
    public AnimationCurve damageSquash;
    public Gradient healthColors;
    public Tilemap refMap;
    public int packetSize;

    [HideInInspector] public Camera camera;
    [HideInInspector] public Dictionary<ulong, Transform> players = new Dictionary<ulong, Transform>();
    [HideInInspector] public ulong playerId;
    [HideInInspector] public PlainGen plainGen;
    [HideInInspector] public ChunksGen chunksGen;
    [HideInInspector] public Transform front;
    [HideInInspector] public string playerName;

    public Vector2Int screen;

    public uint tick { get { return Tick; } private set { tick = value; } }
    private uint Tick = 0;
    public uint tickDesync = 0;

    public bool isConnected = false;

    public UnityEvent playersInitialized = new UnityEvent();

    public static GameManager instance { get; private set; }

    public override void OnNetworkSpawn()
    {
        isConnected = true;

        plainGen = generators.GetComponent<PlainGen>();
        chunksGen = generators.GetComponent<ChunksGen>();

        playerId = NetworkManager.Singleton.LocalClientId;

        if (IsHost)
        {
            plainGen.GenerateSeeds();
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;

            StartGame();
        }
    }


    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    public void SendPlayerNameServerRpc(byte clientId, string name)
    {
        SendPlayerNameClientRpc(clientId, name);
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void SendPlayerNameClientRpc(byte clientId, string name)
    {
        Transform _transform = players[clientId];
        _transform.GetComponent<NetworkPlayer>().playerName.text = name;
    }
    

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId) return;

        StartCoroutine(InitializeWorldData(clientId));
    }

    
    private void OnClientDisconnectCallback(ulong clientId)
    {

    }


    private IEnumerator InitializeWorldData(ulong clientId)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        InitializeTickClientRpc(Tick, clientRpcParams);

        float porcentage = 75 / 100;
        int maxNumberOfProp = Mathf.FloorToInt((UnityTransport.InitialMaxPayloadSize / 32) * porcentage);
        int i = 0;
        List<Prop> propPacket = new List<Prop>();
        List<BlockData> blockPacket = new List<BlockData>();

        foreach (var chunk in plainGen.propChunks.Values)
        {
            foreach (Prop prop in chunk.Values)
            {
                propPacket.Add(prop);
                i++;

                if(i == maxNumberOfProp)
                {
                    i = 0;
                    
                    SyncChunksPropClientRpc(propPacket.ToArray(), clientRpcParams);
                    propPacket = new List<Prop>();

                    yield return new WaitForFixedUpdate();
                }
            }
        }

        if (i > 0)
        {
            SyncChunksPropClientRpc(propPacket.ToArray(), clientRpcParams);
        }
        ConnectClientRpc(dnc.time, plainGen.grassSeed, plainGen.waterSeed, clientRpcParams);

        foreach (var chunk in ItemManager.instance.blocks.Values)
        {
            foreach (var block in chunk)
            {
                blockPacket.Add(new BlockData()
                {
                    itemId = block.Value.id,
                    life = (ushort)ChunksGen.instance.blocksData[block.Key].GetComponent<IDamageable>().Life(),
                    pos = block.Key
                });
            }

            SyncChunksBlockClientRpc(blockPacket.ToArray(), clientRpcParams);

            yield return new WaitForFixedUpdate();
        }

        foreach (var player in players)
        {
            if (player.Key == clientId) continue;
            InitNamesClientRpc(player.Value.GetComponent<NetworkPlayer>().playerName.text, (byte)player.Key, clientRpcParams);

            yield return new WaitForFixedUpdate();
        }
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void InitNamesClientRpc(string name, byte nameId, ClientRpcParams clientRpcParams = default)
    {
        players[nameId].GetComponent<NetworkPlayer>().playerName.text = name;
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void ConnectClientRpc(float time, Vector2 grassSeed, Vector2 waterSeed, ClientRpcParams clientRpcParams = default)
    {
        dnc.time = time;
        plainGen.AssignSeeds(grassSeed, waterSeed);

        StartGame();
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void SyncChunksBlockClientRpc(BlockData[] blockData, ClientRpcParams clientRpcParams = default)
    {
        StartCoroutine(SyncChunksBlock(blockData));
    }

    private IEnumerator SyncChunksBlock(BlockData[] blockData)
    {
        yield return new WaitForFixedUpdate();

        foreach (BlockData data in blockData)
        {
            if (!chunksGen.SpawnBlock(InventoryManager.instance.itemDictId[data.itemId], data.pos)) continue;

            chunksGen.blocksData[data.pos].GetComponent<IDamageable>().SetLife(data.life);
        }
    }


    [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
    public void GetChunkServerRpc(Vector2Int coords, byte clientId)
    {
        StartCoroutine(SendChunk(coords, clientId));
    }


    private IEnumerator SendChunk(Vector2Int coords, byte clientId)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        chunksGen.GenerateChunk(coords, false);

        if (!plainGen.propChunks.ContainsKey(coords)) yield return null;

        float porcentage = 75 / 100;
        int maxNumberOfProp = Mathf.FloorToInt((UnityTransport.InitialMaxPayloadSize / 32) * porcentage);
        int i = 0;
        List<Prop> packet = new List<Prop>();

        foreach (Prop prop in plainGen.propChunks[coords].Values)
        {
            packet.Add(prop);
            i++;

            if (i == maxNumberOfProp)
            {
                i = 0;

                SyncChunksPropClientRpc(packet.ToArray(), clientRpcParams);
                packet = new List<Prop>();

                yield return new WaitForFixedUpdate();
            }
        }

        if (i > 0)
        {
            SyncChunksPropClientRpc(packet.ToArray(), clientRpcParams);
        }
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void SyncChunksPropClientRpc(Prop[] props, ClientRpcParams clientRpcParams = default)
    {
        foreach (Prop prop in props)
        {
            Vector2Int chunk = chunksGen.PosToChunk(prop.pos);

            chunksGen.isInit[chunk] = true;

            if (!plainGen.propChunks.ContainsKey(chunk)) plainGen.propChunks[chunk] = new Dictionary<Vector2Int, Prop>();

            plainGen.propChunks[chunk][prop.pos] = prop;
        }
    }


    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(instance);
        }
        else
        {
            instance = this;
        }

        screen = new Vector2Int(Screen.width, Screen.height);
        Physics2D.queriesHitTriggers = true;
        Physics.queriesHitTriggers = true;
    }


    public void StartGame()
    {
        Physics2D.queriesHitTriggers = true;
        camera = Camera.main;
        managers.SetActive(true);
        generators.SetActive(true);
        uiManager.UpdateCanvas(camera);
        dnc.Initialize();
    }


    public delegate IEnumerator enumerator();
    public void TellStartCoroutine(enumerator enumerator)
    {
        StartCoroutine(enumerator());
    }

    private void FixedUpdate()
    {
        UpdateTicks();
    }


    private void UpdateTicks()
    {
        if (!isConnected) return;

        Tick++;

        if (IsHost && Tick % 100 == 0) ReSyncTickClientRpc(Tick);
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void InitializeTickClientRpc(uint _tick, ClientRpcParams clientRpcParams = default)
    {
        tickDesync = Tick;
        Tick += _tick;

        ReturnTickServerRpc(_tick, (byte)playerId);
    }


    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void ReturnTickServerRpc(uint _tick, byte clientId)
    {
        ClientRpcParams clientRpcParams = EnemyManager.instance.paramsExclude(new ulong[] { clientId });
    }



    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void ReSyncTickClientRpc(uint _tick)
    {
        Tick = _tick + tickDesync;
    }
}
