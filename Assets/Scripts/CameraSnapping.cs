using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CameraSnapping : MonoBehaviour
{
    public Transform player;
    [SerializeField] private GameObject shadows;
    [SerializeField] private float updateThreshold;
    [SerializeField, Range(0f,1f)] private float moveSpeed;

    private float ratio;

    private void Awake()
    {
        ratio = Screen.width / Screen.height;
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Mathf.Sqrt(
            Mathf.Pow((transform.position.x - player.position.x)/ratio, 2) +
            Mathf.Pow((transform.position.y - player.position.y)*2, 2)
        );

        if(distance > updateThreshold)
        {
            posToMove = player.transform.position;
            
            StartCoroutine(MovePos());
        }
    }

    private Vector2 posToMove;
    private IEnumerator MovePos()
    {
        while(Vector2.Distance(new Vector2(transform.position.x, transform.position.y), posToMove) > 0.01f)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                new Vector3(posToMove.x, posToMove.y, transform.position.z),
                moveSpeed * Time.deltaTime / Time.fixedDeltaTime
            );

            yield return 0;
        }
    }
}
