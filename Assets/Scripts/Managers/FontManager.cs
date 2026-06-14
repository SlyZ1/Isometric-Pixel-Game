using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class FontManager : MonoBehaviour
{
    [SerializeField] private Font[] fonts;

    private void Awake()
    {
        if (fonts == null) return;

        foreach(Font font in fonts)
        {
            font.material.mainTexture.filterMode = FilterMode.Point;
        }
    }
}
