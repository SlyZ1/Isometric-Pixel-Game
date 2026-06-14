using UnityEngine;
using UnityEngine.Events;

public interface IEnemy
{
    ushort Life();

    byte TargetId();

    void SetTarget(byte targetId);

    void SetFakeTarget(Transform fakeTarget);

    void RemoveTarget();

    void Attack(byte clientId);

    void Damage(ushort damage);

    void Push(Vector2 push);

    void Knockback(Vector2 knockback);

    void SetLife(ushort life);

    ushort GetAttack();

    float GetKnockback();
}