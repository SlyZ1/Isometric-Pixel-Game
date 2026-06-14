using UnityEngine;

public delegate void damageFunction(IDamageable damageable, int damage);
public interface IDamageable
{
    damageFunction DamageFunction();

    Transform Transform();

    int Life();

    int MaxLife();

    void SetLife(int life);

    void UpdateLife(bool automatic, ulong clientId, byte key);

    void Damage(bool automatic, int damage, int clientId, byte key);

    void DamageAnimation();

    void Despawn(byte key, bool automatic, int clientId);
}