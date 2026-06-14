using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryDrop : MonoBehaviour, IDropHandler
{
    [SerializeField] private InventoryManager inventoryManager;

    public void OnDrop(PointerEventData eventData)
    {
        EventSystem.current.SetSelectedGameObject(null);

        GameObject obj = eventData.pointerDrag.gameObject;

        InventoryItem inventoryItem = obj.GetComponent<InventoryItem>();

        if (inventoryItem != null)
        {
            inventoryManager.DropItemFromPlayer(inventoryItem, eventData.position);

            Destroy(obj);
        }

        gameObject.SetActive(false);
    }
}
