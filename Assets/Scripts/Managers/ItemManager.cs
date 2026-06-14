using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Tilemaps;

public class ItemManager : NetworkBehaviour
{
    [SerializeField] private ChunksGen chunksGen;
    [SerializeField] private Tilemap refMap;

    public Dictionary<Vector2Int, Dictionary<Vector2Int, Item>> blocks = new Dictionary<Vector2Int, Dictionary<Vector2Int, Item>>();

    public static ItemManager instance { get;private set; }


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
    }

    private ClientRpcParams paramsExclude(ulong[] clientIds)
    {
        List<ulong> clientList = new List<ulong>();
        int len = NetworkManager.Singleton.ConnectedClientsList.Count;
        var list = NetworkManager.Singleton.ConnectedClientsList;
        int idLength = clientIds.Length;

        for (int i = 0; i < len; i++)
        {
            for (int j = 0; j < idLength; j++)
            {
                if (list[i].ClientId == clientIds[j]) break;

                if(j == idLength - 1)
                {
                    clientList.Add(list[i].ClientId);
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


    public bool SpawnBlockNetwork(ushort itemId, Vector2Int coords)
    {
        Item item = InventoryManager.instance.itemDictId[itemId];
        if(item.name == "Torch")
        {
            if (LightsManager.numOfStaticLights >= LightsManager.maxStaticLights)
            {
                Debug.Log("too many lights");
                return false;
            }
        }
        bool success = chunksGen.SpawnBlock(InventoryManager.instance.itemDictId[itemId], coords);
        if (success) SpawnBlockServerRpc(itemId, coords, (byte)NetworkManager.Singleton.LocalClientId);
        return success;
    }


    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void SpawnBlockServerRpc(ushort itemId, Vector2Int coords, byte clientId)
    {
        bool fromHost = clientId == NetworkManager.Singleton.LocalClientId;

        ClientRpcParams clientRpcParams1 = paramsExclude(new ulong[] { clientId });

        ClientRpcParams clientRpcParams2 = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        if (!fromHost && chunksGen.blocksData.ContainsKey(coords))
        {
            try
            {
                GameObject obj = chunksGen.blocksData[coords];
                ushort id = obj.GetComponent<Destroyable>().drop[0].id;
                CancelBlockClientRpc(id, coords, clientRpcParams2);
            }
            catch { Debug.Log("IM, l.107"); }
        }
        else
        {
            SpawnBlockClientRpc(itemId, coords, clientRpcParams1);

            Vector2Int chunk = ChunksGen.instance.PosToChunk(coords);

            if (!blocks.ContainsKey(chunk)) blocks[chunk] = new Dictionary<Vector2Int, Item>();

            blocks[chunk][coords] = InventoryManager.instance.itemDictId[itemId];
        }
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void SpawnBlockClientRpc(ushort itemId, Vector2Int coords, ClientRpcParams clientRpcParams = default)
    {
        chunksGen.SpawnBlock(InventoryManager.instance.itemDictId[itemId], coords);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void CancelBlockClientRpc(ushort itemId, Vector2Int coords, ClientRpcParams clientRpcParams = default)
    {
        chunksGen.SpawnBlock(InventoryManager.instance.itemDictId[itemId], coords);
    }





    public void DamageBlockNetwork(IDamageable damageable, int damage)
    {
        Vector2Int coords = (Vector2Int)refMap.WorldToCell(damageable.Transform().position);

        hostBlock = damageable;

        byte key = (byte)UnityEngine.Random.Range(0, byte.MaxValue);
        while (InventoryManager.instance.fakePrefabs.ContainsKey(key)) key = (byte)Random.Range(0, byte.MaxValue);

        DamageBlockServerRpc(coords, (byte)NetworkManager.Singleton.LocalClientId, (short)damage, key);
        StartCoroutine(DamageBlock(coords, false, (byte)GameManager.instance.playerId, damageable, damage, key));
    }


    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void DamageBlockServerRpc(Vector2Int coords, byte clientId, short damage, byte key)
    {
        ClientRpcParams clientRpcParams1 = paramsExclude(new ulong[] { clientId, 0 });

        if (clientId != GameManager.instance.playerId)
        {
            try
            {
                IDamageable destroyable = chunksGen.blocksData[coords].GetComponent<IDamageable>();

                StartCoroutine(DamageBlock(coords, true, clientId, destroyable, damage, key));
            }
            catch { Debug.Log("IM, l.160"); }
        }

        DamageBlockClientRpc(coords, clientId, damage, key, clientRpcParams1);
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void DamageBlockClientRpc(Vector2Int coords, byte clientId, short damage, byte key, ClientRpcParams clientRpcParams = default)
    {
        try
        {
            IDamageable block = chunksGen.blocksData[coords].GetComponent<IDamageable>();
            StartCoroutine(DamageBlock(coords, false, clientId, block, damage, key));
        }
        catch { Debug.Log("IM, l.172"); }
    }


    private IEnumerator DamageBlock(Vector2Int coords, bool automatic, byte clientId, IDamageable block, int damage, byte key)
    {
        if(damage > 1)
        {
            Transform player = GameManager.instance.players[clientId];
            Animator anim = player.GetComponent<Animator>();
            Vector2 distance = block.Transform().position - player.position;
            anim.SetFloat("directionX", distance.x);
            anim.SetFloat("directionY", distance.y);
            GameManager.instance.players[clientId].GetComponent<Animator>().SetTrigger("Hit");

            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                player.GetComponent<CharacterController>().Disable();
            }

            yield return new WaitForSeconds(0.4f);
        }

        block.Damage(automatic, damage, clientId, key);
        block.DamageAnimation();

        if (block.Life() <= 0)
        {
            if(NetworkManager.Singleton.LocalClientId == clientId) chunksGen.RemoveBlock(coords, key, false);

            if(IsHost)
            {
                RemoveBlockServerRpc(coords, clientId, key);
            }
        }
    }


    IDamageable hostBlock;
    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void RemoveBlockServerRpc(Vector2Int coords, byte clientId, byte key)
    {
        bool fromHost = clientId == NetworkManager.Singleton.LocalClientId;

        ClientRpcParams clientRpcParams1 = paramsExclude(new ulong[] { clientId });

        if (!fromHost)
        {
            try
            {
                IDamageable destroyable = chunksGen.blocksData[coords].GetComponent<IDamageable>();
                destroyable.Despawn(key, true, clientId);
            }
            catch { Debug.Log("IM, l.228"); }
        }
        else
        {
            hostBlock.Despawn(key, true, (int)NetworkManager.Singleton.LocalClientId);
        }

        RemoveBlockClientRpc(coords, clientRpcParams1);
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void RemoveBlockClientRpc(Vector2Int coords, ClientRpcParams clientRpcParams = default)
    {
        chunksGen.RemoveBlock(coords, 0, false, false);
    }
}


public struct BlockData : INetworkSerializable
{
    public ushort itemId;
    public ushort life;
    public Vector2Int pos;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref itemId);
        serializer.SerializeValue(ref life);
        serializer.SerializeValue(ref pos);
    }
}