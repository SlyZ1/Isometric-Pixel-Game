using UnityEngine;
using UnityEngine.InputSystem;

public class CursorLight : MonoBehaviour
{
    [SerializeField] private Camera Camera;


    private void OnEnable()
    {
        Camera = Camera.main;
    }

    private void Update()
    {
        Vector2 mouse = Mouse.current.position.ReadValue();
        Vector3 mousePos = new Vector3(mouse.x, mouse.y, 0.01f);
        Vector3 worldPos = Camera.ScreenToWorldPoint(mousePos);
        transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
    }
}
