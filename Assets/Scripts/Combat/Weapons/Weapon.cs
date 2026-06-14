using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public struct AttackData
{
    [SerializeField] public ScriptableObject type;
    [SerializeField] public float range;
    [SerializeField, Range(-180f, 180f)] public float directionOffset;
    [SerializeField, Range(0, 180f)] public float dispersion;
}

[CreateAssetMenu(menuName = "ScriptableObjects/Weapon")]
public class Weapon : ScriptableObject
{
    [SerializeField] private List<AttackData> attacks1 = new List<AttackData>();
    [SerializeField] private float coolDown1;
    [SerializeField] private bool isSpammable1;
    [Space(10)]
    [SerializeField] private List<AttackData> attacks2 = new List<AttackData>();
    [SerializeField] private float coolDown2;
    [SerializeField] private bool isSpammable2;
    [Space(10)]
    [SerializeField] private List<AttackData> attacks3 = new List<AttackData>();
    [SerializeField] private float coolDown3;
    [SerializeField] private bool isSpammable3;

    private bool cooledDown1 = true;
    private bool cooledDown2 = true;
    private bool cooledDown3 = true;

    public void InitWeapons()
    {
        cooledDown1 = true;
        cooledDown2 = true;
        cooledDown3 = true;
    }

    public void Attack1(Vector2 pos, Vector2 dir, bool pressed)
    {
        if (!pressed && !isSpammable1) return;
        if (!cooledDown1 || !cooledDown2 || !cooledDown3) return;
        cooledDown1 = false;

        foreach (AttackData attack in attacks1)
        {
            ushort key = (ushort)CombatManager.instance.attacks.IndexOf(attack.type);
            float dispersion = UnityEngine.Random.Range(-attack.dispersion, attack.dispersion);
            Vector2 direction = Quaternion.Euler(0, 0, attack.directionOffset + dispersion) * dir;

            CombatManager.instance.ShootAttackNetwork(key, pos, direction.normalized * attack.range);
        }

        GameManager.enumerator enumerator = EnterCoolDown1;
        GameManager.instance.TellStartCoroutine(enumerator);
    }


    public void Attack2(Vector2 pos, Vector2 dir, bool pressed)
    {
        if (attacks2.Count <= 0) return;

        if (!pressed && !isSpammable2) return;
        if (!cooledDown1 || !cooledDown2 || !cooledDown3) return;
        cooledDown2 = false;

        foreach (AttackData attack in attacks2)
        {
            ushort key = (ushort)CombatManager.instance.attacks.IndexOf(attack.type);
            float dispersion = UnityEngine.Random.Range(-attack.dispersion, attack.dispersion);
            Vector2 direction = Quaternion.Euler(0, 0, attack.directionOffset + dispersion) * dir;

            CombatManager.instance.ShootAttackNetwork(key, pos, direction.normalized * attack.range);
        }

        GameManager.enumerator enumerator = EnterCoolDown2;
        GameManager.instance.TellStartCoroutine(enumerator);
    }


    public void Attack3(Vector2 pos, Vector2 dir, bool pressed)
    {
        if(attacks3.Count <= 0) return;

        if (!pressed && !isSpammable3) return;
        if (!cooledDown1 || !cooledDown2 || !cooledDown3) return;
        cooledDown3 = false;

        foreach (AttackData attack in attacks3)
        {
            ushort key = (ushort)CombatManager.instance.attacks.IndexOf(attack.type);
            float dispersion = UnityEngine.Random.Range(-attack.dispersion, attack.dispersion);
            Vector2 direction = Quaternion.Euler(0, 0, attack.directionOffset + dispersion) * dir;

            CombatManager.instance.ShootAttackNetwork(key, pos, direction.normalized * attack.range);
        }

        GameManager.enumerator enumerator = EnterCoolDown3;
        GameManager.instance.TellStartCoroutine(enumerator);
    }


    private IEnumerator EnterCoolDown1()
    {
        yield return new WaitForSeconds(coolDown1);
        cooledDown1 = true;
    }

    private IEnumerator EnterCoolDown2()
    {
        yield return new WaitForSeconds(coolDown2);
        cooledDown2 = true;
    }

    private IEnumerator EnterCoolDown3()
    {
        yield return new WaitForSeconds(coolDown3);
        cooledDown3 = true;
    }
}
