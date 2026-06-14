using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeTransparency : MonoBehaviour
{
    private SpriteRenderer sr;
    private float time = 0;
    private float speed = 2;
    [SerializeField] private AnimationCurve curve;
    [SerializeField] private LayerMask activator;

    private List<SpriteRenderer> childs = new List<SpriteRenderer>();

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (activator != (activator | (1 << collision.gameObject.layer))) return;
        StopAllCoroutines();
        StartCoroutine(FadeOut());
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (activator != (activator | (1 << collision.gameObject.layer))) return;
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }


    private IEnumerator FadeOut()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            SpriteRenderer child = transform.GetChild(i).GetComponent<SpriteRenderer>();
            if (child.sortingLayerName == "Shadow") continue;
            if (child != null) childs.Add(child);
        }


        while(time < 1)
        {
            Color color = new Color(sr.color.r, sr.color.g, sr.color.b, 1 - curve.Evaluate(time));

            foreach (SpriteRenderer child in childs)
            {
                child.color = color;
            }

            sr.color = color;
            time += Time.fixedDeltaTime * speed;

            yield return new WaitForFixedUpdate();
        }
    }


    private IEnumerator FadeIn()
    {
        while (time > 0)
        {
            Color color = new Color(sr.color.r, sr.color.g, sr.color.b, 1 - curve.Evaluate(time));

            foreach (SpriteRenderer child in childs)
            {
                child.color = color;
            }

            sr.color = color;

            time -= Time.fixedDeltaTime * speed;

            yield return new WaitForFixedUpdate();
        }
    }
}
