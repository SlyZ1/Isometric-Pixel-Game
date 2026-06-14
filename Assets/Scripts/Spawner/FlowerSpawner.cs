using UnityEngine;

public class FlowerSpawner : MonoBehaviour
{
    [SerializeField] private Sprite[] flowers;
    [SerializeField] private Material mat;
    [Space]
    [SerializeField] private float radius;
    [SerializeField] private int max;
    [SerializeField] private int min;
    [SerializeField] private bool toRotate;
    [SerializeField] private bool shadow;
    [SerializeField] private Color color;

    private GameObject[] flowerObjects;
    private int numOfFlowers;

    public float distance;

    private void Awake()
    {
        numOfFlowers = flowers.Length;
        SpawnFlowers();
    }


    public void SpawnFlowers()
    {
        if(flowerObjects != null)
        {
            int len = flowerObjects.Length;
            for (int i = 0; i < len; i++)
            {
                DestroyImmediate(flowerObjects[i]);
            }
        }

        int number = Random.Range(min, max);

        if (shadow)
        {
            flowerObjects = new GameObject[number*2];
        }
        else
        {
            flowerObjects = new GameObject[number];
        }

        for (int i = 0; i < number; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius * Mathf.Sqrt(number);
            Vector2 snappedPosition2D = new Vector2(Mathf.Floor(randomCircle.x / 0.01f), Mathf.Floor(0.5f * randomCircle.y / 0.01f)) * 0.01f;
            Vector3 snappedPosition3D = new Vector3(snappedPosition2D.x, snappedPosition2D.y, 0);

            GameObject flower = new GameObject("flower");
            SpriteRenderer sr = flower.AddComponent<SpriteRenderer>();

            sr.material = mat;
            sr.sprite = flowers[Random.Range(0, numOfFlowers - 1)];
            sr.sortingLayerName = "GrassDeco";

            flower.transform.position = transform.position + snappedPosition3D;
            if (toRotate)
            {
                flower.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 3) * 90);
            }
            flower.transform.parent = transform;

            if (shadow)
            {
                GameObject shadowGo = Instantiate(flower, flower.transform.position + 0.01f * Vector3.down, flower.transform.rotation, transform);
                SpriteRenderer ssr = shadowGo.GetComponent<SpriteRenderer>();
                ssr.sortingOrder = -1;
                ssr.color = color;
                flowerObjects[2 * i + 1] = shadowGo;
                flowerObjects[2 * i] = flower;
            }
            else
            {
                flowerObjects[i] = flower;
            }
        }

        distance = radius * Mathf.Sqrt(number);
    }
}
