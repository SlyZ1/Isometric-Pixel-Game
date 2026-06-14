using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public struct CRT_Prop
{
    public Tile tile;
    public List<Vector2Int> rules;
    public List<bool> ruleInversion;
}

[System.Serializable]
public struct CRT
{
    public Tile defaultTile;
    public List<CRT_Prop> props;
}

[System.Serializable]
public struct CRG_Prop
{
    public GameObject go;
    public List<Vector2Int> rules;
    public List<bool> ruleInversion;
}

[System.Serializable]
public struct CRG
{
    public GameObject defaultGo;
    public List<CRG_Prop> props;
}

public class CustomRuleTile : MonoBehaviour
{
    public static Tile GetTileCRT(CRT ruleTile, Dictionary<Vector2Int,float> noise, float threshold, out bool isWhite)
    {
        foreach(CRT_Prop prop in ruleTile.props)
        {
            bool isValid = true;
            int len = prop.rules.Count;

            for(int i = 0; i < len; i++)
            {
                if (noise[prop.rules[i]] > threshold == prop.ruleInversion[i])
                {
                    isValid = false;
                    break;
                }
            }

            if (isValid)
            {
                isWhite = false;
                return prop.tile;
            }
        }

        isWhite = true;
        return ruleTile.defaultTile;
    }


    public static GameObject GetGoCRG(CRG ruleGo, Dictionary<Vector2Int, float> noise, float threshold, out bool isWhite)
    {
        foreach (CRG_Prop prop in ruleGo.props)
        {
            bool isValid = true;
            int len = prop.rules.Count;

            for (int i = 0; i < len; i++)
            {
                if (noise[prop.rules[i]] > threshold == prop.ruleInversion[i])
                {
                    isValid = false;
                    break;
                }
            }

            if (isValid)
            {
                isWhite = false;
                return prop.go;
            }
        }

        isWhite = true;
        return ruleGo.defaultGo;
    }
}
