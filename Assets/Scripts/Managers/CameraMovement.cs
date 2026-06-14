using UnityEngine;
using UnityEngine.UI;

public class CameraMovement : MonoBehaviour
{
    private Transform cam;
    [SerializeField] private bool pixelSnap;
    [Space]
    [SerializeField] private float speed;
    [SerializeField] private float resolution;
    [SerializeField] private RawImage image;

    private Vector3 offset;
    private Vector2 correction;

    private void Awake()
    {
        cam = transform;
        offset = cam.transform.position;
    }
}
