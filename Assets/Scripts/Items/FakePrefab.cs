using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FakePrefab : MonoBehaviour
{
    [SerializeField] private Text text;
    [SerializeField] private Canvas canvas;
    [SerializeField] private float moveSpeed;
    [SerializeField] private GameObject healthBar;
    [SerializeField] private Slider health;

    [HideInInspector] public int count = 1;
    [HideInInspector] public Item item;
    [HideInInspector] public int life;

    private void Awake()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, 1);
        canvas.worldCamera = Camera.main;
    }


    private void OnDisable()
    {
        StopAllCoroutines();
    }


    public void Initialize(InventoryItem item)
    {
        this.item = item.item;
        count = item.count;

        if(this.item.type == ItemType.Tool)
        {
            float value = (float)item.life / this.item.durability;

            if (value < 1) healthBar.SetActive(true);

            life = item.life;
            health.value = value;
            health.fillRect.GetComponent<RawImage>().color = GameManager.instance.healthColors.Evaluate(1 - value);
        }

        UpdateLoot();
        StartCoroutine(AutoDestroy());
    }


    private IEnumerator AutoDestroy()
    {
        yield return new WaitForSeconds(1f);
        if (gameObject == null) yield break;

        int key = -1;
        foreach(var fake in InventoryManager.instance.fakePrefabs)
        {
            if (fake.Value == null) continue;
            if (fake.Value.Contains(gameObject))
            {
                key = fake.Key;
                fake.Value.Remove(gameObject);
                break;
            }
        }
        if (key >= 0) if (InventoryManager.instance.fakePrefabs[(byte)key].Count <= 0) InventoryManager.instance.fakePrefabs.Remove((byte)key);
        Destroy(gameObject);
    }

    public void UpdateLoot()
    {
        if (count == 1)
        {
            text.text = "";
        }
        else
        {
            text.text = count + "";
        }
    }

    public Vector2 to;
    public void StartShiftingAway(Vector2 _to)
    {
        to = _to;
        UpdateLoot();
        StartCoroutine(ShiftAway(_to));
    }


    private IEnumerator ShiftAway(Vector2 _to)
    {
        Vector3 to3D = new Vector3(_to.x, _to.y, 1);
        while (Vector3.Distance(transform.position, to3D) > 0.05f)
        {
            transform.position = Vector3.Lerp(transform.position, to3D, moveSpeed * 2 * Time.deltaTime);
            yield return 0;
        }
    }


    public uint requestTick = uint.MaxValue;
    private void OnTriggerStay2D(Collider2D collision)
    {
        GameObject collider = collision.gameObject;
        if (collider.layer != LayerMask.NameToLayer("Player")) return;

        if (GameManager.instance.tick < requestTick)
        {
            requestTick = GameManager.instance.tick;
        }
    }
}
