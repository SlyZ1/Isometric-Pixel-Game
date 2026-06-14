using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI")]
    [SerializeField] private Image image;
    [SerializeField] private Text countText;
    [SerializeField] private GameObject healthBar;
    [SerializeField] private Slider health;

    [HideInInspector] public Transform parent;
    [HideInInspector] public Item item;
    [HideInInspector] public int count = 1;
    [HideInInspector] public int life;

    public void InitializeItem(Item newItem)
    {
        item = newItem;
        image.sprite = item.sprite;
        float value = (float)life / item.durability;

        if (item.type == ItemType.Tool && value < 1)
        {
            healthBar.SetActive(true);
            health.value = value;
            health.fillRect.GetComponent<RawImage>().color = GameManager.instance.healthColors.Evaluate(1 - value);
        }

        UpdateItem();
    }

    public void UpdateItem()
    {
        if(count <= 0)
        {
            DestroyImmediate(gameObject);
        }

        if(count <= 1)
        {
            countText.text = string.Empty;
        }
        else
        {
            countText.text = count + "";
        }
    }


    public void UseTool()
    {
        if(!healthBar.activeSelf) healthBar.SetActive(true);

        life -= 1;
        float value = (float)life / item.durability;
        health.value = value;
        health.fillRect.GetComponent<RawImage>().color = GameManager.instance.healthColors.Evaluate(1-value);

        if(life <= 0)
        {
            DestroyImmediate(gameObject);
        }
    }

    private void OnDisable()
    {
        InventorySlot parent = GetComponentInParent<InventorySlot>();
        if(parent != null)
        {
            InventoryManager.instance.DeReferenceSlot(item, parent);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        InventoryManager.instance.EnableDropping(true);

        if (eventData.button == PointerEventData.InputButton.Right && item.stackable && count > 1)
        {
            GameObject half = Instantiate(gameObject, transform.parent);
            InventoryItem halfItem = half.GetComponent<InventoryItem>();
            halfItem.parent = transform.parent;
            halfItem.count = count / 2 + count % 2;
            count /= 2;
            halfItem.UpdateItem();
            UpdateItem();
        }
        else if (eventData.button == PointerEventData.InputButton.Middle && item.stackable && count > 1)
        {
            GameObject half = Instantiate(gameObject, transform.parent);
            InventoryItem halfItem = half.GetComponent<InventoryItem>();
            halfItem.parent = transform.parent;
            halfItem.count = count - 1;
            count = 1;
            halfItem.UpdateItem();
            UpdateItem();
        }
        else
        {
            InventoryManager.instance.items[item].Remove(transform.parent.GetComponent<InventorySlot>());
        }

        image.raycastTarget = false;
        parent = transform.parent;
        transform.SetParent(transform.root);
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Mouse.current.position.ReadValue();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        InventoryManager.instance.EnableDropping(false);
        image.raycastTarget = true;
        transform.SetParent(parent);
    }
}
