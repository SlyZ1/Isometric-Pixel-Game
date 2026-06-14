using System.Collections;
using UnityEngine;

public interface IHit
{
    void Init(IAttack attack, float range, float lerp);
    void ManualStart();
}