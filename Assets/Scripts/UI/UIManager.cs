using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject inventory;
    [Space]
    [SerializeField] private List<Canvas> canvas;
    private EventSystem e_sys;

    public static UIManager instance { get; private set; }

    private void OnEnable()
    {
        e_sys = EventSystem.current;
    }


    public void UpdateCanvas(Camera cam)
    {
        foreach(Canvas can in canvas)
        {
            can.worldCamera = cam;
        }
    }
    

    public void DisableSelection()
    {
        e_sys.SetSelectedGameObject(null);
    }


    private void OnInventory(InputValue value)
    {
        if (value.isPressed)
        {
            ToggleInventory();
        }
    }


    public void ToggleInventory()
    {
        inventory.SetActive(!inventory.activeSelf);
    }
}
