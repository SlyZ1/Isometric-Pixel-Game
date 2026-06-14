using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    private const int MAXCOUNT = 99;


    public void OnDrop(PointerEventData eventData)
    {
        EventSystem.current.SetSelectedGameObject(null);

        InventoryItem inventoryItem = eventData.pointerDrag.GetComponent<InventoryItem>();

        InventoryItem child = GetComponentInChildren<InventoryItem>();

        if (child != null)
        {
            if ((child.item == inventoryItem.item) && child.item.stackable && child.count < MAXCOUNT)
            {
                Distribute(inventoryItem, child);
                return;
            }

            InventoryItem back = inventoryItem.parent.GetComponentInChildren<InventoryItem>();

            if (back != null)
            {
                Distribute(inventoryItem, back);
            }
            else
            {
                child.transform.SetParent(inventoryItem.parent); //ICI
                InventorySlot parentSlot = inventoryItem.parent.GetComponent<InventorySlot>();
                InventoryManager.instance.DeReferenceSlot(child.item, this);
                InventoryManager.instance.ReferenceSlot(child.item, parentSlot);
                InventoryManager.instance.DeReferenceSlot(inventoryItem.item, parentSlot);
                InventoryManager.instance.ReferenceSlot(inventoryItem.item, this);
            }
        }
        else
        {
            InventoryManager.instance.ReferenceSlot(inventoryItem.item, this);
        } 
        inventoryItem.parent = transform;
    }


    private void Distribute(InventoryItem from, InventoryItem to)
    {
        if(from.item != to.item)
        {
            InventoryManager.instance.DropItemFromPlayer(from, from.GetComponent<RectTransform>().position);
            Destroy(from.gameObject);
            return;
        }

        if(from.parent == to.transform.parent && from.count + to.count > MAXCOUNT)
        {
            from.count = from.count + to.count - MAXCOUNT;
            to.count = MAXCOUNT;
            from.UpdateItem();
            to.UpdateItem();
            InventoryManager.instance.DropItemFromPlayer(from, from.GetComponent<RectTransform>().position);
            Destroy(from.gameObject);
            return;
        }

        if(to.count + from.count <= MAXCOUNT)
        {
            to.count += from.count;
            to.UpdateItem();
            Destroy(from.gameObject);
        }
        else
        {
            from.count -= MAXCOUNT - to.count;
            to.count = MAXCOUNT;
            to.UpdateItem();
            from.UpdateItem();

            InventoryItem left = from.parent.GetComponentInChildren<InventoryItem>();

            if(left != null)
            {
                Distribute(from, left);
            }
            else
            {
                InventoryManager.instance.ReferenceSlot(from.item, from.parent.GetComponent<InventorySlot>());
            }
        }
    }
}
