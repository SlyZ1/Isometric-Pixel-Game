using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;


[Serializable]
public struct StartItem
{
    [SerializeField] public Item item;
    [SerializeField] public int count;
}

public class ItemBarScroll : NetworkBehaviour
{
    [Header("References")]
    [Space]
    [SerializeField] private Tilemap refMap;
    [SerializeField] private SpriteRenderer blockSelector;
    [SerializeField] private int blockSelectorPixelSize;
    [SerializeField] private RectTransform itemSelector;
    [SerializeField] private ItemActions itemActions;
    [Space]
    [SerializeField] private RectTransform[] itemSlots;
    [SerializeField] private StartItem[] starterItems;
    [Space]

    [Space]
    [Header("Parameters")]
    [Space]
    [SerializeField, Range(0f, 1f)] private float itemSelectorSpeed;
    [SerializeField] private float selectorPositionThreshold;
    [SerializeField] private Vector2Int screenPixelUnit;
    [SerializeField] private float maxBuildDistance;

    public int posIndex;

    private delegate void ItemState(CallType type);
    private ItemState state;

    public Item holdingItem = null;

    public GameObject hoverObject;
    public float _maxBuildDistance;
    public RectTransform[] _itemSlots;

    public static ItemBarScroll instance;

    public bool isClicking = false;
    public bool isRightClicking = false;
    public bool isMiddleClicking = false;

    private void Start()
    {
        _maxBuildDistance = maxBuildDistance;
        GameManager.instance.playersInitialized.AddListener(InitListener);
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

        _itemSlots = itemSlots;
    }


    private void InitListener()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        foreach (StartItem startItem in starterItems)
        {
            GetComponent<InventoryManager>().AddItem(startItem.item, startItem.item.durability, startItem.count);
        }
        ChangeSelector(0);
    }


    private void OnClientConnectedCallback(ulong clientId)
    {
        InventoryItem invItem = itemSlots[posIndex].GetComponentInChildren<InventoryItem>();
        UpdateHoldServerRpc(ushort.MaxValue, invItem == null ? ushort.MaxValue : invItem.item.id, (byte)NetworkManager.Singleton.LocalClientId);
    }


    private void OnOne()
    {
        ChangeSelector(0);
    }

    private void OnTwo()
    {
        ChangeSelector(1);
    }

    private void OnThree()
    {
        ChangeSelector(2);
    }

    private void OnFour()
    {
        ChangeSelector(3);
    }

    private void OnFive()
    {
        ChangeSelector(4);
    }

    private void OnSix()
    {
        ChangeSelector(5);
    }

    private void OnSeven()
    {
        ChangeSelector(6);
    }

    private void OnEight()
    {
        ChangeSelector(7);
    }

    private void OnNine()
    {
        ChangeSelector(8);
    }



    private void OnScrollWheel(InputValue value)
    {
        
        float scroll = value.Get<Vector2>().y;

        if (scroll != 0)
        {
            int newPos = (int) ((posIndex - Mathf.Sign(scroll)) % 9 + 9) % 9;
            ChangeSelector(newPos);
        }
    }


    private void OnRightClick(InputValue value)
    {
        isRightClicking = value.isPressed;
        if (!value.isPressed || GameManager.instance.players.Count <= 0) return;
        if (EventSystem.current.IsPointerOverGameObject() && hoverObject == null) return;

        UpdateState(posIndex);
        ExecuteSecondState();
        UpdateState(posIndex);
    }


    private void OnMiddleClick(InputValue value)
    {
        isMiddleClicking = value.isPressed;
        if (!value.isPressed || GameManager.instance.players.Count <= 0) return;
        if (EventSystem.current.IsPointerOverGameObject() && hoverObject == null) return;

        UpdateState(posIndex);
        ExecuteThirdState();
        UpdateState(posIndex);
    }


    bool coolDown = true;
    private void OnClick(InputValue value)
    {
        isClicking = value.isPressed;

        if (!value.isPressed || !coolDown || GameManager.instance.players.Count <= 0) return;
        if (EventSystem.current.IsPointerOverGameObject() && hoverObject == null) return;

        coolDown = false;
        UpdateState(posIndex);
        ExecuteMainState();
        UpdateState(posIndex);

        if (holdingItem == null)
        {
            StartCoroutine(CoolDown(0.8f));
            return;
        }

        coolDown = true;
    }


    private void ExecuteMainState()
    {
        if (holdingItem == null)
        {
            itemActions.SimpleDestroy();
            return;
        }

        switch (holdingItem.item)
        {
            case Tool tool:
                itemActions.UseTool(tool);
                break;

            case Block block:
                itemActions.SimpleDestroy();
                break;

            case Weapon weapon:
                StartCoroutine(itemActions.Attack(weapon, 1));
                break;

            case null:
                itemActions.SimpleDestroy();
                break;
        }
    }


    private void ExecuteSecondState()
    {
        if (holdingItem == null) return;

        switch (holdingItem.item)
        {
            case Tool tool:
                break;

            case Block block:
                itemActions.Build(block);
                break;

            case Weapon weapon:
                StartCoroutine(itemActions.Attack(weapon, 2));
                break;

            default:
                break;
        }
    }


    private void ExecuteThirdState()
    {
        if (holdingItem == null) return;

        switch (holdingItem.item)
        {
            case Tool tool:
                break;

            case Block block:
                break;

            case Weapon weapon:
                StartCoroutine(itemActions.Attack(weapon, 3));
                break;

            default:
                break;
        }
    }


    public void ChangeSelector(int index)
    {
        itemSelector.position = itemSlots[index].position;
        posIndex = index;
        UpdateState(index); 
    }


    public void UpdateState(int index)
    {
        //Get item in current slot
        InventoryItem invItem = itemSlots[index].GetComponentInChildren<InventoryItem>();
        
        if (invItem != null)
        {
            //Replace current holding item with new item
            if (holdingItem != invItem.item)
            {
                ushort hItemId = holdingItem == null ? ushort.MaxValue : holdingItem.id;

                UpdateItemState(holdingItem, invItem.item, NetworkManager.Singleton.LocalClientId);
                UpdateHoldServerRpc(hItemId, invItem.item.id, (byte)NetworkManager.Singleton.LocalClientId);
            }

            holdingItem = invItem.item;
        }
        else
        {
            //Just set holding item to null
            if (holdingItem != null)
            {
                UpdateItemState(holdingItem, null, NetworkManager.Singleton.LocalClientId);
                UpdateHoldServerRpc(holdingItem.id, ushort.MaxValue, (byte)NetworkManager.Singleton.LocalClientId);
            }
            holdingItem = null;
            //state(CallType.Exit);
            return;
        }
    }


    private void UpdateItemState(Item hItem, Item newItem, ulong clientId)
    {
        if(hItem != null)
        {
            switch (hItem.name)
            {
                case "Torch":
                    GameObject player = GameManager.instance.players[clientId].gameObject;
                    Destroy(player.GetComponent<SLight>());
                    break;
            }
        }

        if(newItem != null)
        {
            switch (newItem.item)
            {
                case Block block:
                    if(newItem.name == "Torch")
                    {
                        GameObject player = GameManager.instance.players[clientId].gameObject;
                        SLight light = block.prefab.GetComponent<SLight>();
                        if (player.GetComponent<SLight>() != null) Destroy(player.GetComponent<SLight>());
                        SLight newLight = player.AddComponent<SLight>();
                        newLight.color = light.color;
                        newLight.intensity = light.intensity * 3f / 4f;
                        newLight.radius = light.radius * 2f / 3f;
                    }
                    break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void UpdateHoldServerRpc(ushort hItem, ushort newItem, byte clientId)
    {
        UpdateHoldClientRpc(hItem, newItem, clientId);
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void UpdateHoldClientRpc(ushort hItem, ushort newItem, byte clientId)
    {
        if (clientId == GameManager.instance.playerId) return;

        Item item;
        Item holdItem;

        //Finds the new item
        if (newItem == ushort.MaxValue)
        {
            item = null;
        }
        else
        {
            item = InventoryManager.instance.itemDictId[newItem];
        }

        //Finds the old holding item
        if (hItem == ushort.MaxValue)
        {
            holdItem = null;
        }
        else
        {
            holdItem = InventoryManager.instance.itemDictId[hItem];
        }

        UpdateItemState(holdItem, item, clientId);
    }


    private IEnumerator CoolDown(float time)
    {
        yield return new WaitForSeconds(time);
        coolDown = true;
    }


    private float IsoDistance(Vector2 a, Vector2 b)
    {
        return Mathf.Sqrt(Mathf.Pow(a.x - b.x, 2) + Mathf.Pow((a.y - b.y) * 2, 2));
    }


    private void Update()
    {
        if(hoverObject != null)
        {
            Interactable inter = hoverObject.GetComponent<Interactable>();

            if(inter != null)
            {
                if (IsoDistance(hoverObject.transform.position, GameManager.instance.players[GameManager.instance.playerId].position) > maxBuildDistance)
                inter.Unselect();
            }
        }
    }
}

public enum CallType
{
    Enter,
    Exit,
    Main,
    Secondary
}
