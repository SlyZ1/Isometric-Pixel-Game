using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Tree : MonoBehaviour, ICustomBlock
{
    [SerializeField] private SpriteRenderer tree;
    [SerializeField] private Collider2D trigger;
    [Space]
    [SerializeField] private Sprite[] trees;
    [SerializeField] private Sprite[] cutTrees;
    [SerializeField] private Sprite[] shadowTrees;
    [SerializeField] private Sprite[] shadowCutTrees;
    [Space]
    [SerializeField] private int lifeThreshold;
    [Space]
    [SerializeField] private List<Item> drop;
    [SerializeField] private List<WeightedRandomList<int>> dropCount;

    private int numTrees;
    private int index = 0;
    private Destroyable destroyable;

    private void Awake()
    {
        numTrees = cutTrees.Length;
        destroyable = gameObject.GetComponent<Destroyable>();
    }

    private void Start()
    {
        int j = 0;
        for (int i = 0; i < numTrees; i++)
        {
            if (tree.sprite == trees[i])
            {
                j = i;
                break;
            }
        }
        index = Mathf.FloorToInt(j / 2);

        transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = shadowTrees[j];

        trees = new Sprite[0];
        shadowTrees = new Sprite[0];
    }

    private bool isCut = false;
    public void Damaged(bool automatic, ulong clientId, byte key)
    {
        if(destroyable.Life() <= lifeThreshold && !isCut) CutTree(automatic, clientId, key);
    }

    private void CutTree(bool automatic, ulong clientId, byte key)
    {
        isCut = true;

        int len = drop.Count;

        if(clientId == GameManager.instance.playerId || GameManager.instance.IsHost)
        {
            for (int i = 0; i < len; i++)
            {
                InventoryItem inventoryItem = gameObject.AddComponent<InventoryItem>();
                inventoryItem.item = drop[i];
                inventoryItem.count = dropCount[i].GetRandom(key);
                InventoryManager.instance.DropItem(inventoryItem, transform.position, GameManager.instance.IsHost, automatic ? (int)clientId : -1);
                Destroy(inventoryItem);
            }
        }

        

        tree.sprite = cutTrees[index];
        tree.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = shadowCutTrees[index];
        tree.color = new Color(tree.color.r, tree.color.g, tree.color.b, 0);
        trigger.enabled = false;

        cutTrees = new Sprite[0];
        shadowCutTrees = new Sprite[0];
    }

    public void Interacted()
    {
    }
}
