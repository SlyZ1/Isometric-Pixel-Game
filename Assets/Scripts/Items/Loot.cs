using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Loot : NetworkBehaviour
{
    [SerializeField] private Collider2D detector;
    [SerializeField] private float moveSpeed;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Text text;
    [SerializeField] private SpriteRenderer renderer;
    [SerializeField] private GameObject healthBar;
    [SerializeField] private Slider health;
    public Item item;

    public int count = 1;

    public int life;

    public byte key;

    private NetworkObject netObject;

    private ulong ownerId;
    public ulong spawnerId;
    private bool hiddenOnServer = false;
    public bool fromPlayer;
    public Vector3 to;

    private AddInfo info;

    private void Awake()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, 1);
        netObject = GetComponent<NetworkObject>();
        detector.enabled = false;
        renderer.enabled = false;
        canvas.worldCamera = Camera.main;
    }



    public void UpdateLoot()
    {
        if(count == 1)
        {
            text.text = "";
        }
        else
        {
            text.text = count + "";
        }
    }



    public override void OnNetworkSpawn()
    {
        RequestInitialisationServerRpc((byte)NetworkManager.Singleton.LocalClientId);
        if(IsHost) StartCoroutine(MaxTimeDespawn());
    }



    private IEnumerator ShiftAway(Vector3 pos)
    {
        while(Vector3.Distance(transform.position, pos) > 0.05f)
        {
            transform.position = Vector3.Lerp(transform.position, pos, moveSpeed * 2 * Time.deltaTime);
            yield return 0;
        }

        yield return new WaitForSeconds(0.15f);

        if (detector != null)
        {
            detector.enabled = true;
        }
    }



    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void RequestInitialisationServerRpc(byte clientId)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        if(clientId != spawnerId)
        {
            if (fromPlayer)
            {
                InitializeFromPlayerClientRpc(item.id, (byte)count, (ushort)life, (byte)spawnerId, to, clientRpcParams);
            }
            else
            {
                InitializeClientRpc(item.id, (byte)count, (ushort)life, (byte)spawnerId, to, key, clientRpcParams);
            }
        }
        else
        {
            if (fromPlayer)
            {
                ReturnFromPlayerClientRpc((byte)count, key, clientRpcParams);
            }
            else
            {
                ReturnClientRpc((byte)count, key, clientRpcParams);
            }
        }
    }



    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void ReturnFromPlayerClientRpc(byte count, byte _key, ClientRpcParams clientRpcParams = default)
    {
        GameObject fakeVersion = InventoryManager.instance.fakePrefabs[_key][0];

        InventoryManager.instance.fakePrefabs[_key].Remove(fakeVersion);
        if (InventoryManager.instance.fakePrefabs[_key].Count <= 0) InventoryManager.instance.fakePrefabs.Remove(_key);

        FakePrefab fake = fakeVersion.GetComponent<FakePrefab>();

        item = fake.item;
        life = fake.life;
        transform.position = fakeVersion.transform.position;
        renderer.enabled = true;
        renderer.sprite = item.sprite;
        spawnerId = NetworkManager.Singleton.LocalClientId;
        this.count = count;
        Vector2 to = fake.to;
        UpdateLoot();

        Destroy(fakeVersion);

        if (item.type == ItemType.Tool)
        {
            float value = (float)life / item.durability;

            if(value < 1) healthBar.SetActive(true);

            health.value = value;
            health.fillRect.GetComponent<RawImage>().color = GameManager.instance.healthColors.Evaluate(1 - value);
        }

        StartCoroutine(ShiftAway(new Vector3(to.x, to.y, 1)));
    }



    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void ReturnClientRpc(byte count, byte _key, ClientRpcParams clientRpcParams = default)
    {
        GameObject fakeVersion = InventoryManager.instance.fakePrefabs[_key][0];

        InventoryManager.instance.fakePrefabs[_key].Remove(fakeVersion);
        if (InventoryManager.instance.fakePrefabs[_key].Count <= 0) InventoryManager.instance.fakePrefabs.Remove(_key);

        FakePrefab fake = fakeVersion.GetComponent<FakePrefab>();

        if (detector != null)
        {
            detector.enabled = true;
        }

        item = fake.item;
        life = fake.life;
        transform.position = fakeVersion.transform.position;
        renderer.enabled = true;
        renderer.sprite = item.sprite;
        spawnerId = NetworkManager.Singleton.LocalClientId;
        this.count = count;
        UpdateLoot();

        if(fake.requestTick < uint.MaxValue) RequestLootingServerRpc((byte)NetworkManager.Singleton.LocalClientId, info.finalCount, fake.requestTick);

        Destroy(fakeVersion);

        if(item.type == ItemType.Tool)
        {
            healthBar.SetActive(true);

            float value = (float)life / item.durability;
            health.value = value;
            health.fillRect.GetComponent<RawImage>().color = GameManager.instance.healthColors.Evaluate(1 - value);
        }
            
        StartCoroutine(ShiftAway(transform.position));
    }



    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void InitializeFromPlayerClientRpc(ushort itemId, byte count, ushort life, byte spawnerId, Vector2 to, ClientRpcParams clientRpcParams = default)
    {
        Vector3 _to = new Vector3(to.x, to.y, 1);
        transform.position = GameManager.instance.players[spawnerId].position + 0.01f * Vector3.forward;

        if (detector != null)
        {
            detector.enabled = true;
        }

        item = InventoryManager.instance.itemDictId[itemId];
        renderer.enabled = true;
        renderer.sprite = item.sprite;
        this.spawnerId = spawnerId;
        this.count = count;
        this.life = life;
        UpdateLoot();

        if (item.type == ItemType.Tool)
        {
            healthBar.SetActive(true);

            float value = (float)life / item.durability;
            health.value = value;
            health.fillRect.GetComponent<RawImage>().color = GameManager.instance.healthColors.Evaluate(1 - value);
        }

        if (gameObject.activeSelf)
        {
            StartCoroutine(ShiftAway(_to));
        }
        else
        {
            transform.position = _to;
            if (detector != null) detector.enabled = true;
        }
    }



    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void InitializeClientRpc(ushort itemId, byte count, ushort life, byte spawnerId, Vector2 to, byte _key, ClientRpcParams clientRpcParams = default)
    {
        Random.InitState(_key);
        Vector3 _to = new Vector3(to.x, to.y, 1) + (Vector3)Random.insideUnitCircle * InventoryManager.instance.dropDistance;
        transform.position = _to;

        if (detector != null)
        {
            detector.enabled = true;
        }

        item = InventoryManager.instance.itemDictId[itemId];

        if(InventoryManager.instance.fakePrefabs.ContainsKey(key))
        {
            foreach(GameObject pref in InventoryManager.instance.fakePrefabs[key]) Destroy(pref);
            InventoryManager.instance.fakePrefabs.Remove(key);
        }

        renderer.enabled = true;
        renderer.sprite = item.sprite;
        this.spawnerId = spawnerId;
        this.count = count;
        this.life = life;
        UpdateLoot();

        if (item.type == ItemType.Tool)
        {
            healthBar.SetActive(true);

            float value = (float)life / item.durability;
            health.value = value;
            health.fillRect.GetComponent<RawImage>().color = GameManager.instance.healthColors.Evaluate(1 - value);
        }

        if (gameObject.activeSelf)
        {
            StartCoroutine(ShiftAway(_to));
        }
        else
        {
            transform.position = _to;
            if (detector != null) detector.enabled = true;
        }
    }



    private bool requested = false;
    private void OnTriggerStay2D(Collider2D collision)
    {
        GameObject collider = collision.gameObject;
        if (collider.layer != LayerMask.NameToLayer("Player")) return;

        info = InventoryManager.instance.DelayedAddItem(item, count);

        if (info.addable && !requested)
        {
            requested = true;
            RequestLootingServerRpc((byte)NetworkManager.Singleton.LocalClientId, info.finalCount, GameManager.instance.tick);
        }
    }


    private void OnTriggerExit2D(Collider2D collision)
    {
        GameObject collider = collision.gameObject;
        if (collider.layer != LayerMask.NameToLayer("Player")) return;

        requested = false;
    }



    uint requestTick = uint.MaxValue;
    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void RequestLootingServerRpc(byte clientId, byte finalCount, uint _tick)
    {
        if (!text.enabled) return;

        if (requestTick <= _tick + 5) return;

        requestTick = _tick;

        StopCoroutine(MaxTimeDespawn());

        CollectClientRpc(clientId, finalCount);
    }



    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void CollectClientRpc(byte clientId, byte finalCount)
    {
        detector.enabled = false;

        ownerId = clientId;

        StopAllCoroutines();

        if (gameObject.activeSelf)
        {
            StartCoroutine(MoveToPlayer(GameManager.instance.players[clientId], finalCount));
        }
        else
        {
            Done(finalCount);
        }
    }


    private IEnumerator MoveToPlayer(Transform player, byte finalCount)
    {
        float accelerator = 1;
        float distance = Vector2.Distance(transform.position, player.position);

        while (distance >= 0.05f)
        {
            distance = Vector2.Distance(transform.position, player.position);

            transform.position = Vector3.MoveTowards(transform.position, player.position, moveSpeed * accelerator * Time.deltaTime / (distance*5));

            accelerator += Time.deltaTime;

            yield return 0;
        }

        Done(finalCount);
    }



    private void Done(byte finalCount)
    {
        if (NetworkManager.Singleton.LocalClientId == ownerId)
        {
            InventoryManager.instance.AddItem(item, life, count);
        }

        if (finalCount > 0)
        {
            count = finalCount;
            detector.enabled = true;
            UpdateLoot();

            if (NetworkManager.Singleton.IsHost)
            {
                requestTick = uint.MaxValue;
            }
        }
        else
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0);
                text.enabled = false;
                DestroyLootServerRpc((byte)NetworkManager.Singleton.LocalClientId);
            }
            else
            {
                GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0);
                text.enabled = false;
                hiddenOnServer = true;

                if (IsTotallyHiddenServer())
                {
                    netObject.Despawn();
                }
            }
        }
    }



    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void DestroyLootServerRpc(byte clientId)
    {
        netObject.NetworkHide(clientId);

        if (IsTotallyHiddenServer())
        {
            netObject.Despawn();
        }
    }


    private bool IsTotallyHiddenServer()
    {
        IEnumerator _enum = netObject.GetObservers();
        _enum.Reset();
        int i = 0;
        while (_enum.MoveNext())
        {
            if ((ulong)_enum.Current == NetworkManager.Singleton.LocalClientId && hiddenOnServer) continue;
            i++;
        }

        if(i <= 0)
        {
            return true;
        }

        return false;
    }



    private IEnumerator MaxTimeDespawn()
    {
        yield return new WaitForSeconds(30);

        AutoDespawn();
    }


    private void AutoDespawn()
    {
        netObject.Despawn();
    }
}
