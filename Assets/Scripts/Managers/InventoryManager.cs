 using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InventoryManager : NetworkBehaviour
{
    [Header("items")]
    [SerializeField] private List<Item> itemList;
    [Space(30)]
    [SerializeField] private InventorySlot[] slots;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private GameObject itemDrop;
    [SerializeField] private GameObject pref;
    [SerializeField] private GameObject fakePref;
    [SerializeField] private ItemBarScroll itemBarScroll;
    

    private int slotNumber;

    private const int MAXCOUNT = 99;

    private GameObject staticItemDrop;

    public float dropDistance = 0.1f;

    public Dictionary<Item, List<InventorySlot>> items = new Dictionary<Item, List<InventorySlot>>();
    public Dictionary<ushort, Item> itemDictId = new Dictionary<ushort, Item>();
    private Dictionary<byte, List<GameObject>> FakePrefabs = new Dictionary<byte, List<GameObject>>();

    public static InventoryManager instance { get; private set; }

    public Dictionary<byte, List<GameObject>> fakePrefabs
    {
        get { return FakePrefabs; }
        set { FakePrefabs = value; }
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

        staticItemDrop = itemDrop;
        slotNumber = slots.Length;

        int len = itemList.Count;
        for (ushort i = 0; i < len; i++)
        {
            itemDictId[i] = itemList[i];
            itemList[i].id = i;
        }

        foreach(Item item in itemList)
        {
            switch (item.item)
            {
                case Weapon weapon:
                    weapon.InitWeapons();
                    break;
            }
        }
    }

    public void ReferenceSlot(Item item, InventorySlot slot)
    {
        if (!items.ContainsKey(item))
        {
            items[item] = new List<InventorySlot>();
        }
        items[item].Add(slot);
    }


    public void DeReferenceSlot(Item item, InventorySlot slot)
    {
        if (!items.ContainsKey(item)) return;

        items[item].Remove(slot);
    }


    public AddInfo DelayedAddItem(Item item, int count = 1)
    {
        int c = count;
        AddInfo info = new AddInfo
        {
            addable = false,
            distribute = new List<InventoryItem>(),
            slot = null,
            finalCount = (byte)count
        };

        if (item.stackable && items.ContainsKey(item))
        {

            int len = items[item].Count;
            for (int i = 0; i < len; i++)
            {
                InventoryItem invItem = items[item][i].GetComponentInChildren<InventoryItem>();
                int given = Mathf.Min(invItem.count + count, MAXCOUNT) - invItem.count;
                c -= given;
                info.distribute.Add(invItem);

                if(given > 0) info.addable = true;


                if (c <= 0)
                {
                    info.finalCount = 0;
                    return info;
                }
            }

            info.finalCount = (byte)Mathf.Max(c, 0);
        }


        for (int i = 0; i < slotNumber; i++)
        {
            InventorySlot slot = slots[i];
            InventoryItem child = slot.GetComponentInChildren<InventoryItem>();

            if (child != null || slot.transform.childCount > 0)
            {
                continue;
            }

            info.addable = true;
            info.slot = slot;
            info.finalCount = 0;

            return info;
        }

        return info;
    }


    public void AddItem(Item item, int life, int count = 1)
    {
        int c = count;

        if(item.stackable && items.ContainsKey(item))
        {
            int len = items[item].Count;
            for (int i = 0; i < len; i++)
            {
                InventoryItem invItem = items[item][i].GetComponentInChildren<InventoryItem>();
                c -= StackItem(invItem, c);

                if (c <= 0)
                {
                    return;
                }
            }
        }


        for (int i = 0; i < slotNumber; i++)
        {
            InventorySlot slot = slots[i];
            InventoryItem child = slot.GetComponentInChildren<InventoryItem>();

            if (child != null)
            {
                continue;
            }

            SpawnItem(item, life, slot, c);

            return;
        }
    }

    public void SpawnItem(Item item, int life, InventorySlot slot, int count)
    {
        ReferenceSlot(item, slot);

        GameObject newItemGo = Instantiate(itemPrefab, slot.transform);
        InventoryItem newItem = newItemGo.GetComponent<InventoryItem>();
        newItem.life = life;
        newItem.count = count;

        newItem.InitializeItem(item);

        itemBarScroll.UpdateState(ItemBarScroll.instance.posIndex);
    }


    public int StackItem(InventoryItem child, int count)
    {
        int u = child.count;
        child.count = Mathf.Min(child.count + count, MAXCOUNT);
        child.UpdateItem();
        return child.count - u;
    }


    public void DropItemFromPlayer(InventoryItem item, Vector2 position)
    {
        Vector2 playerPos = GameManager.instance.players[NetworkManager.Singleton.LocalClientId].position;
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(position);
        Vector2 direction = (worldPos - playerPos).normalized * 0.8f;
        ulong clientId = NetworkManager.Singleton.LocalClientId;

        Vector2 newPos = (Vector2)GameManager.instance.players[clientId].position + direction;

        GameObject dropped = Instantiate(fakePref, GameManager.instance.players[clientId].position, Quaternion.identity);
        dropped.GetComponent<SpriteRenderer>().sprite = item.item.sprite;

        byte key = (byte)Random.Range(0, byte.MaxValue);
        while (fakePrefabs.ContainsKey(key))
        {
            key = (byte)Random.Range(0, byte.MaxValue);
        }

        fakePrefabs[key] = new List<GameObject>() { dropped };
        FakePrefab fakePrefab = dropped.GetComponent<FakePrefab>();
        fakePrefab.Initialize(item);
        fakePrefab.StartShiftingAway(newPos);

        DropItemFromPlayerServerRpc(item.item.id, (byte)item.count, (ushort) item.life, (byte)clientId, newPos, key);
    }


    [ServerRpc(RequireOwnership = false)]
    private void DropItemFromPlayerServerRpc(ushort itemId, byte count, ushort life, byte clientId, Vector2 position, byte key)
    {
        if (!NetworkManager.Singleton.IsHost) return;

        Item item = itemDictId[itemId];
        GameObject dropped = Instantiate(pref, GameManager.instance.players[clientId].position, Quaternion.identity);

        dropped.GetComponent<SpriteRenderer>().sprite = item.sprite;

        Loot loot = dropped.GetComponent<Loot>();
        loot.fromPlayer = true;
        loot.item = item;
        loot.to = (Vector3)position;
        loot.count = count;
        loot.UpdateLoot();
        loot.spawnerId = clientId;
        loot.life = life;
        loot.key = key;

        dropped.GetComponent<NetworkObject>().Spawn();
    }

    public void DropItem(InventoryItem item, Vector2 position, bool automaticNetworkSpawn, int _clientId = -1, ushort key = byte.MaxValue + 1)
    {
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        byte _key;

        if(key > byte.MaxValue)
        {
            _key = (byte)Random.Range(0, byte.MaxValue);
            while (fakePrefabs.ContainsKey((byte)key))
            {
                _key = (byte)Random.Range(0, byte.MaxValue);
            }
        }
        else _key = (byte)key;

        if (!fakePrefabs.ContainsKey(_key)) fakePrefabs[_key] = new List<GameObject>();

        if (_clientId < 0)
        {
            Random.InitState(_key);
            GameObject dropped = Instantiate(fakePref, position + Random.insideUnitCircle * dropDistance, Quaternion.identity);

            dropped.GetComponent<SpriteRenderer>().sprite = item.item.sprite;

            fakePrefabs[_key].Add(dropped);
            FakePrefab fakePrefab = dropped.GetComponent<FakePrefab>();
            fakePrefab.Initialize(item);
        }

        if (!automaticNetworkSpawn) return;
        
        DropItemServerRpc(item.item.id, (byte)item.count, (ushort)item.life, _clientId < 0 ? (byte)clientId : (byte)_clientId, position, _key);
    }


    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    public void DropItemServerRpc(ushort itemId, byte count, ushort life, byte clientId, Vector2 position, byte key)
    {
        Item item = itemDictId[itemId];

        Random.InitState(key);
        GameObject dropped = Instantiate(pref, position + Random.insideUnitCircle * dropDistance, Quaternion.identity);

        dropped.GetComponent<SpriteRenderer>().sprite = item.sprite;

        Loot loot = dropped.GetComponent<Loot>();
        loot.fromPlayer = false;
        loot.item = item;
        loot.to = (Vector3)position;
        loot.count = count;
        loot.UpdateLoot();
        loot.spawnerId = clientId;
        loot.life = life;
        loot.key = key;

        dropped.GetComponent<NetworkObject>().Spawn();
    }


    public void EnableDropping(bool isEnabled)
    {
        staticItemDrop.SetActive(isEnabled);
    }
}

public struct AddInfo
{
    internal List<InventoryItem> distribute;
    internal InventorySlot slot;
    internal bool addable;
    internal byte finalCount;
}
