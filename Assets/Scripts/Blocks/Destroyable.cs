using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroyable : MonoBehaviour, IDamageable
{
    public Block block;
    [SerializeField] private MonoBehaviour _customBlock;

    [SerializeField] private List<Item> Drop;
    [SerializeField] private List<WeightedRandomList<int>> DropCount;
    [SerializeField] private short maxLife;
    [SerializeField] private DestroyableType type;

    private enum DestroyableType
    {
        Object,
        Block
    }

    private int life;

    private ICustomBlock customBlock;

    public List<Item> drop
    {
        get { return Drop; }
        set { Drop = value; }
    }

    public List<WeightedRandomList<int>> dropCount
    {
        get { return DropCount; }
        set { DropCount = value; }
    }

    float height = 1;
    float width = 1;

    private void Awake()
    {
        customBlock = (ICustomBlock)_customBlock;
        life = maxLife;
        height = transform.localScale.y;
        width = transform.localScale.x;
    }

    public Transform Transform()
    {
        return transform;
    }

    public int Life()
    {
        return life;
    }

    public int MaxLife()
    {
        return maxLife;
    }

    public void SetLife(int life)
    {
        this.life = life;
    }

    public void Damage(bool automatic, int damage, int clientId, byte key)
    {
        try
        {
            life -= damage;

            Vector2Int coords = (Vector2Int)GameManager.instance.refMap.layoutGrid.WorldToCell(transform.position);
            Prop prop = PlainGen.instance.propChunks[ChunksGen.instance.PosToChunk(coords)][coords];
            prop.life = (short)life;
            PlainGen.instance.propChunks[ChunksGen.instance.PosToChunk(coords)][coords] = prop;

            UpdateLife(automatic, (ulong)clientId, key);
        }
        catch { }
    }

    public void DamageAnimation()
    {
        if (this != null)
        {
            if (!isDamaging) if (gameObject != null) if (gameObject.activeInHierarchy) StartCoroutine(Damage());
        }
    }

    public void UpdateLife(bool automatic, ulong clientId, byte key)
    {
        customBlock?.Damaged(automatic, clientId, key);
    }

    bool isDamaging = false;
    private IEnumerator Damage()
    {
        float time = 0;
        float amplifier = 0.2f;
        float speed = 3.5f;
        float colorDarkener = 0.2f;

        transform.localScale = new Vector3(width, height, transform.localScale.z);
        isDamaging = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color color = sr.color;

        while (time < 1)
        {
            float evaluation = GameManager.instance.damageSquash.Evaluate(time);

            transform.localScale = new Vector3(
                width + 0.1f * width * (1 - evaluation) * amplifier,
                height - (1 - evaluation) * height / 6 * amplifier,
                transform.localScale.z
            );

            time += Time.deltaTime * speed;
            yield return new WaitForEndOfFrame();

            Color tempColor = color * (1 - colorDarkener) + color * colorDarkener * evaluation;
            sr.color = new Color(tempColor.r, tempColor.g, tempColor.b, sr.color.a);
        }

        isDamaging = false;
    }

    //automatic = DropLoot on server or not
    //if clientId not -1, then it directly DropLoot on server without fakeprefab
    public void Despawn(byte key, bool automatic, int clientId = -1)
    {
        int len = drop.Count;

        for (int i = 0; i < len; i++)
        {
            InventoryItem inventoryItem = gameObject.AddComponent<InventoryItem>();
            inventoryItem.item = drop[i];
            inventoryItem.count = dropCount[i].GetRandom(key);
            inventoryItem.life = inventoryItem.item.durability;
            if(clientId < 0)
            {
                InventoryManager.instance.DropItem(inventoryItem, transform.position, automatic, -1, key);
            }
            else
            {
                InventoryManager.instance.DropItem(inventoryItem, transform.position, automatic, clientId, key);
            }
        }

        if (!automatic) return;

        Vector2Int coords = (Vector2Int)GameManager.instance.refMap.layoutGrid.WorldToCell(transform.position);
        Vector2Int chunk = ChunksGen.instance.PosToChunk(coords);
        switch (type)
        {
            case DestroyableType.Object:
                PlainGen.instance.propChunks[chunk].Remove(coords);
                if (PlainGen.instance.propChunks[chunk].Count <= 0) PlainGen.instance.propChunks.Remove(chunk);
                break;

            case DestroyableType.Block:
                ItemManager.instance.blocks[chunk].Remove(coords);
                if (ItemManager.instance.blocks[chunk].Count <= 0) ItemManager.instance.blocks.Remove(chunk);
                break;
        }
        
    }

    public damageFunction DamageFunction()
    {
        return ItemManager.instance.DamageBlockNetwork;
    }
}
