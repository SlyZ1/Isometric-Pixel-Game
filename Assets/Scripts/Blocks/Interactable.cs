using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


public class Interactable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject outline;

    private bool isTriggered = false;
    private bool isHovered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform != GameManager.instance.players[GameManager.instance.playerId]) return;

        isTriggered = true;
        Select();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.transform != GameManager.instance.players[GameManager.instance.playerId]) return;

        isTriggered = false;
        if(!isHovered) Unselect();
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        if (IsoDistance(mousePos, (Vector2)GameManager.instance.players[GameManager.instance.playerId].position) > ItemBarScroll.instance._maxBuildDistance)
        {
            outline.SetActive(false);
            return;
        }

        Select();
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        if (IsoDistance(mousePos, (Vector2)GameManager.instance.players[GameManager.instance.playerId].position) > ItemBarScroll.instance._maxBuildDistance)
        {
            outline.SetActive(false);
            return;
        }

        if(!isTriggered) Unselect();
    }

    public void Select()
    {
        GameObject hover = ItemBarScroll.instance.hoverObject;
        if(hover != null)
        {
            hover.GetComponent<Interactable>()?.Unselect();
        }
        ItemBarScroll.instance.hoverObject = gameObject;
        outline.SetActive(true);
    }


    public void Unselect()
    {
        outline.SetActive(false);
        if (ItemBarScroll.instance.hoverObject == gameObject) ItemBarScroll.instance.hoverObject = null;
    }

    private float IsoDistance(Vector2 a, Vector2 b)
    {
        return Mathf.Sqrt(Mathf.Pow(a.x - b.x, 2) + Mathf.Pow((a.y - b.y) * 2, 2));
    }
}
