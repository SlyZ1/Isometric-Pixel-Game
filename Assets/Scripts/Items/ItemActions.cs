using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class ItemActions : MonoBehaviour
{
    [Header("LayerMasks")]
    [Space]
    [SerializeField] private LayerMask handLayer;
    [Space]
    [SerializeField] private Tilemap refMap;
    [SerializeField] private float rayRate = 1;


    public void ToolState(CallType type)
    {
        switch (type)
        {
            case CallType.Main:

                if (!GameManager.instance.players.ContainsKey(GameManager.instance.playerId)) return;

                IDamageable damageable = null;

                if (ItemBarScroll.instance.hoverObject != null)
                {
                    damageable = ItemBarScroll.instance.hoverObject.GetComponent<IDamageable>();
                }

                if(damageable == null)
                {
                    Vector2 playerPos = GameManager.instance.players[GameManager.instance.playerId].transform.position;
                    Vector2 direction = ((Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - playerPos).normalized;

                    RaycastHit2D hit = Scan(ItemBarScroll.instance.holdingItem, direction, playerPos, ItemBarScroll.instance.holdingItem.targetLayer);

                    if (hit.collider == null) return;
                    damageable = hit.collider.GetComponent<IDamageable>();
                    if (damageable == null) return;
                }

                damageable.DamageFunction()(damageable, ItemBarScroll.instance.holdingItem.damage);
                ItemBarScroll.instance._itemSlots[ItemBarScroll.instance.posIndex].GetComponentInChildren<InventoryItem>().UseTool();
                break;
        }
        
    }


    private RaycastHit2D Scan(Item item, Vector2 direction, Vector2 playerPos, LayerMask mask)
    {
        int semiRange = Mathf.RoundToInt(item.angleRange / 2);
        Physics2D.queriesHitTriggers = false;
        Physics.queriesHitTriggers = false;

        for (int i = 0; i < semiRange; i+= Mathf.RoundToInt(1/rayRate))
        {
            Vector3 changedDirection = Quaternion.Euler(0, 0, i) * new Vector3(direction.x, direction.y, 0) * item.range;
            Vector2 isoDirection = new Vector2(changedDirection.x, changedDirection.y / 2);
            
            RaycastHit2D hit = Physics2D.Raycast(playerPos, isoDirection, isoDirection.magnitude, mask, -3, 3);

            if (hit.collider != null)
            {
                Physics2D.queriesHitTriggers = true;
                Physics.queriesHitTriggers = true;
                return hit;
            }

            Vector3 changedDirection2 = Quaternion.Euler(0, 0, -i) * new Vector3(direction.x, direction.y, 0) * item.range;
            Vector2 isoDirection2 = new Vector2(changedDirection2.x, changedDirection2.y / 2);

            RaycastHit2D hit2 = Physics2D.Raycast(playerPos, isoDirection2, isoDirection2.magnitude, mask, -3, 3);

            if (hit2.collider != null)
            {
                Physics2D.queriesHitTriggers = true;
                Physics.queriesHitTriggers = true;
                return hit2;
            }
        }

        Physics2D.queriesHitTriggers = true;
        Physics.queriesHitTriggers = true;
        return new RaycastHit2D();
    }



    public IEnumerator Attack(Weapon weapon, int type)
    {
        if (!GameManager.instance.players.ContainsKey(GameManager.instance.playerId)) yield break;

        CallAttack(weapon, type, true);

        while(  type == 1 ? ItemBarScroll.instance.isClicking : 
                type == 2 ? ItemBarScroll.instance.isRightClicking : 
                ItemBarScroll.instance.isClicking   )
        {
            CallAttack(weapon, type, false);

            yield return new WaitForFixedUpdate();
        }
    }


    private void CallAttack(Weapon weapon, int type, bool pressed)
    {
        Vector2 pos = GameManager.instance.players[GameManager.instance.playerId].position;
        if (type == 1)
        {
            weapon.Attack1(pos, ((Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - pos).normalized, pressed);
        }
        else if (type == 2)
        {
            weapon.Attack2(pos, ((Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - pos).normalized, pressed);
        }
        else
        {
            weapon.Attack3(pos, ((Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - pos).normalized, pressed);
        }
    }


    public void Build(Block block)
    {
        if (!GameManager.instance.players.ContainsKey(GameManager.instance.playerId)) return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector3Int cell = refMap.WorldToCell((Vector3)mousePos);

        if (IsoDistance(mousePos, (Vector2)GameManager.instance.players[GameManager.instance.playerId].position) > ItemBarScroll.instance._maxBuildDistance) return;

        bool success = ItemManager.instance.SpawnBlockNetwork(ItemBarScroll.instance.holdingItem.id, (Vector2Int)cell);
        if (success)
        {
            InventoryItem invItem = ItemBarScroll.instance._itemSlots[ItemBarScroll.instance.posIndex].GetComponentInChildren<InventoryItem>();
            invItem.count--;
            invItem.UpdateItem();
        }
    }


    public void SimpleDestroy()
    {
        if (!GameManager.instance.players.ContainsKey(GameManager.instance.playerId)) return;

        IDamageable damageable = null;
        if (ItemBarScroll.instance.hoverObject != null) damageable = ItemBarScroll.instance.hoverObject.GetComponent<IDamageable>();

        if (damageable == null)
        {
            Vector2 playerPos = GameManager.instance.players[GameManager.instance.playerId].transform.position;
            Vector2 direction = ((Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - playerPos).normalized;

            Item hand = (Item)ScriptableObject.CreateInstance("Item");
            hand.range = 0.4f;
            hand.angleRange = 80;

            RaycastHit2D hit = Scan(hand, direction, playerPos, handLayer);

            Destroy(hand);

            if (hit.collider == null) return;
            damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable == null) return;
        }

        damageable.DamageFunction()(damageable, 1);
    }


    public void UseTool(Tool tool)
    {
        if (!GameManager.instance.players.ContainsKey(GameManager.instance.playerId)) return;

        IDamageable damageable = null;

        if (ItemBarScroll.instance.hoverObject != null)
        {
            damageable = ItemBarScroll.instance.hoverObject.GetComponent<IDamageable>();
        }

        if (damageable == null)
        {
            Vector2 playerPos = GameManager.instance.players[GameManager.instance.playerId].transform.position;
            Vector2 direction = ((Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - playerPos).normalized;

            RaycastHit2D hit = Scan(ItemBarScroll.instance.holdingItem, direction, playerPos, tool.targetLayer);

            if (hit.collider == null) return;
            damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable == null) return;
        }

        damageable.DamageFunction()(damageable, tool.damage);
        ItemBarScroll.instance._itemSlots[ItemBarScroll.instance.posIndex].GetComponentInChildren<InventoryItem>().UseTool();
    }


    private float IsoDistance(Vector2 a, Vector2 b)
    {
        return Mathf.Sqrt(Mathf.Pow(a.x - b.x, 2) + Mathf.Pow((a.y - b.y) * 2, 2));
    }
}
