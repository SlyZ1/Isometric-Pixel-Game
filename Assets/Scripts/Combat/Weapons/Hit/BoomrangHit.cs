using System.Collections;
using UnityEngine;

public class BoomrangHit : MonoBehaviour, IHit
{
    public IAttack boomrang;
    public float range;
    public float lerp;

    private SpriteRenderer sr;

    public void Init(IAttack attack, float range, float lerp)
    {
        boomrang = attack;
        this.range = range;
        this.lerp = lerp;
        sr = transform.GetChild(0).GetComponent<SpriteRenderer>();
    }

    public void ManualStart()
    {
        StartCoroutine(DestroyCountDown());
    }

    private IEnumerator DestroyCountDown()
    {
        while(lerp <= 1 - 0.03f)
        {
            yield return new WaitForFixedUpdate();
            lerp += (boomrang.speed / boomrang.speed) * Time.fixedDeltaTime;
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1 - lerp);
        }

        Destroy(gameObject);
    }
}