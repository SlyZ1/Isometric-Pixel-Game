using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlainGen : MonoBehaviour
{
    [SerializeField] private ChunksGen chunksGen;
    [Space(30)]
    [Header("Tiles")]
    [Space]
    [SerializeField] private Tile[] originalGrass;
    [SerializeField] private CRT waterRuleTile;
    [SerializeField] private CRT groundRuleTile;
    [SerializeField] private Tile waterCenter;
    [SerializeField] private GameObject tilemapTemplate;
    [SerializeField] private Transform map;
    [SerializeField] private Object[] prop;
    [Space]

    [Space]
    [Header("Params")]
    [Space]
    [SerializeField] private Color[] grassColors;
    [Space]
    [SerializeField] private int colorSteps;
    [Space]
    [SerializeField] private AnimationCurve saturationCurve;
    [Space]
    [SerializeField] private Color[] waterColors;
    [SerializeField] private int waterColorSteps;
    [Space]

    [Space]
    [Header("Noise")]
    [Space]
    [SerializeField] private float treeNoiseScale = 1f;
    [Space]
    [SerializeField] private float waterNoiseScale = 1f;
    [SerializeField, Range(0f, 1f)] private float waterThreshold;
    [SerializeField, Range(0f, 1f)] private float waterTreeThreshold;

    private const int GROUND = 0;
    private const int GRASS = 1;
    private Tilemap grassmap;
    private Tilemap[] tilemaps;

    private Tile[,] grass;
    private Tile[] customWater;

    private int numOfGrass;

    public Vector2 grassSeed;
    public Vector2 waterSeed;

    public Dictionary<Vector2Int, Dictionary<Vector2Int, Prop>> propChunks = new Dictionary<Vector2Int, Dictionary<Vector2Int, Prop>>();
    public Dictionary<int, Vector2Int> coordsId = new Dictionary<int, Vector2Int>();

    private Vector2Int[] directions = new Vector2Int[8];

    public static PlainGen instance;


    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(instance);
        }
        else
        {
            instance = this;
        }

        directions[0] = Vector2Int.down;
        directions[1] = Vector2Int.left;
        directions[2] = Vector2Int.right;
        directions[3] = Vector2Int.up;
        directions[4] = Vector2Int.down + Vector2Int.left;
        directions[5] = Vector2Int.down + Vector2Int.right;
        directions[6] = Vector2Int.up + Vector2Int.left;
        directions[7] = Vector2Int.up + Vector2Int.right;

        numOfGrass = originalGrass.Length;

        while (GenWaterNoise(Vector2Int.zero) <= waterThreshold + waterTreeThreshold)
        {
            waterSeed = new Vector2(Random.value + Random.Range(0, 100000), Random.value + Random.Range(0, 100000));
        }

        GameObject grassMap = Instantiate(tilemapTemplate, Vector3.zero + 1.07f * Vector3.forward , Quaternion.identity, map);
        grassMap.name = "grassMap";
        grassMap.GetComponent<TilemapRenderer>().sortingLayerName = "Grass";
        grassmap = grassMap.GetComponent<Tilemap>();

        tilemaps = new Tilemap[] { grassmap };

        grass = new Tile[numOfGrass, 2 + colorSteps];
        customWater = new Tile[2 + waterColorSteps];

        for (int j = 0; j < 2 + colorSteps; j++)
        {
            for (int i = 0; i < numOfGrass; i++)
            {
                grass[i, j] = Instantiate(originalGrass[i]);
                grass[i, j].name = j + "";
                grass[i, j].color = Color.Lerp(grassColors[0], grassColors[1], (float)j / (1 + colorSteps));
            }
        }

        customWater[0] = Instantiate(waterCenter);
        customWater[0].name = 0 + "";
        customWater[0].color = waterColors[0];

        for (int i = 1; i < 2 + waterColorSteps; i++)
        {
            customWater[i] = Instantiate(waterCenter);
            customWater[i].name = i + "";
            customWater[i].color = Color.Lerp(waterColors[0], waterColors[1], (float)i / (1 + waterColorSteps));
        }
    }

    public void GenerateSeeds()
    {
        grassSeed = new Vector2(Random.value + Random.Range(0f, 100000f), Random.value + Random.Range(0f, 100000f)); 
        waterSeed = new Vector2(Random.value + Random.Range(0f, 100000f), Random.value + Random.Range(0f, 100000f));

        while(GenWaterNoise(Vector2Int.zero) <= waterThreshold + waterTreeThreshold)
        {
            waterSeed = new Vector2(Random.value + Random.Range(0f, 100000f), Random.value + Random.Range(0f, 100000f));
        }
    }

    public void AssignSeeds(Vector2 grass, Vector2 water)
    {
        grassSeed = grass;
        waterSeed = water;
    }


    public Chunk GenChunk(Vector2Int coords, int chunkHalfSize, GameObject chunkParent)
    {
        ShadowChunk shadowChunk = chunkParent.GetComponent<ShadowChunk>();

        Dictionary<Tilemap, List<Vector3Int>> pos = new Dictionary<Tilemap, List<Vector3Int>>();
        Dictionary<Tilemap, List<TileBase>> tls = new Dictionary<Tilemap, List<TileBase>>();

        if(NetworkManager.Singleton.IsHost) propChunks[coords] = new Dictionary<Vector2Int, Prop>();

        foreach (Tilemap _tilemap in tilemaps)
        {
            tls[_tilemap] = new List<TileBase>();
            pos[_tilemap] = new List<Vector3Int>();
        }

        for (int y = -chunkHalfSize; y < chunkHalfSize; y++)
        {
            for (int x = -chunkHalfSize; x < chunkHalfSize; x++)
            {
                ChunkTile[] tiles = PlainTiles(x, y, coords, chunkHalfSize, shadowChunk);

                foreach (ChunkTile _tile in tiles)
                {
                    if (!_tile.isProp)
                    {
                        pos[_tile.tilemap].Add(_tile.coord);
                        tls[_tile.tilemap].Add(_tile.tile);
                    }
                    else if (NetworkManager.Singleton.IsHost)
                    {
                        propChunks[coords][_tile.prop.pos] = (_tile.prop);
                    }
                }
            }
        }
        

        List<ChunkData> chunkData = new List<ChunkData>();

        foreach (Tilemap _tilemap in tilemaps)
        {
            chunkData.Add(new ChunkData
            {
                tilemap = _tilemap,
                pos = pos[_tilemap].ToArray(),
                tiles = tls[_tilemap].ToArray()
            });
        }

        Chunk chunk = new Chunk
        {
            data = chunkData.ToArray(),
            parent = chunkParent
        };

        shadowChunk.ManualAwake();

        chunkParent.SetActive(false);

        return chunk;
    }


    private ChunkTile[] PlainTiles(int x, int y, Vector2Int coords, int chunkHalfSize, ShadowChunk shadowChunk)
    {
        // Water

        Vector2Int coord = new Vector2Int(x + coords.x * chunkHalfSize * 2, y + coords.y * chunkHalfSize * 2);
        float waterNoise = GenWaterNoise(coord);
        if (waterNoise <= waterThreshold)
        {
            return new ChunkTile[] { GenWater(coord, waterNoise) };
        }


        // Grass + Trees

        return SpawnNature(x, y, coords, chunkHalfSize, waterNoise, shadowChunk);
    }


    private ChunkTile[] SpawnNature(int x, int y, Vector2Int coords, int chunkHalfSize, float waterNoise, ShadowChunk shadowChunk)
    {
        List<ChunkTile> tiles = new List<ChunkTile>();
        Vector2Int coord = new Vector2Int(x + coords.x * chunkHalfSize * 2, y + coords.y * chunkHalfSize * 2);

        float grassNoise = GrassNoise(coord);

        ChunkTile[] grass = SpawnGrass(coord, waterNoise, shadowChunk);

        for (int i = 0; i < grass.Length; i++)
        {
            if (grass[i].tile != null) tiles.Add(grass[i]);
        }

        if (NetworkManager.Singleton.IsHost)
        {
            int j = 0;
            foreach (Object _p in prop)
            {
                if ((x - _p.offset.x) % _p.modulo.x == 0 && (y - _p.offset.y) % _p.modulo.y == 0)
                {
                    ChunkTile pTile = SpawnPropHost(_p, coord, grassNoise, waterNoise, shadowChunk, j);

                    if (pTile.isProp)
                    {
                        tiles.Add(pTile);
                        break;
                    }
                }

                j++;
            }
        }
        else if(propChunks.ContainsKey(coords)) 
        {
            if (!propChunks[coords].ContainsKey(coord)) return tiles.ToArray();

            SpawnPropClient(propChunks[coords][coord], coord, grassNoise, waterNoise, shadowChunk);
        }
        

        return tiles.ToArray();
    }


    public float GrassNoise(Vector2Int coord)
    {
        float waterNoise = GenWaterNoise(coord);
        float grassNoise = Perlin(coord.x, coord.y, treeNoiseScale, 2, grassSeed, 1.5f, 1.5f);
        grassNoise -= Mathf.Clamp((waterThreshold + waterTreeThreshold - waterNoise) * 4f, 0f, 1f);
        return Saturate(grassNoise);
    }


    public bool OnEarth(Vector2Int coord)
    {
        return GenWaterNoise(coord) > waterThreshold;
    }


    private ChunkTile[] SpawnGrass(Vector2Int pos, float waterNoise, ShadowChunk shadowChunk)
    {
        ChunkTile[] tile = new ChunkTile[2];

        Dictionary<Vector2Int, float> noises = new Dictionary<Vector2Int, float>();

        foreach (Vector2Int direction in directions)
        {
            noises[direction] = GenWaterNoise(pos + direction);
        }

        bool isWhite;
        tile[GROUND].coord = new Vector3Int(pos.x+2, pos.y+2, 1);
        tile[GROUND].tile = CustomRuleTile.GetTileCRT(groundRuleTile, noises, waterThreshold, out isWhite);
        tile[GROUND].tilemap = grassmap;

        tile[GRASS].coord = new Vector3Int(pos.x+1, pos.y+1, 0);

        int color = GetColorIndex(GrassNoise(pos), 2 + colorSteps, 0, 1);
        int grassIndex = 1;

        if (isWhite) grassIndex = 0;

        tile[GRASS].tile = grass[grassIndex, color];
        tile[GRASS].tilemap = grassmap;

        return tile;
    }



    private ChunkTile SpawnPropHost(Object currentProp, Vector2Int pos, float grassNoise, float waterNoise, ShadowChunk shadowChunk, int j)
    {
        ChunkTile _prop = new ChunkTile();
        Vector3 position = grassmap.layoutGrid.GetCellCenterWorld(new Vector3Int(pos.x, pos.y, 0));

        bool grassBool = grassNoise > currentProp.grassThresholdOffset;
        bool waterBool = waterNoise < currentProp.waterThresholdOffset + waterThreshold;

        if (currentProp.inverseWaterThresh)
        {
            waterBool = waterNoise > currentProp.waterThresholdOffset + waterThreshold;
        }
        if (currentProp.inverseGrassThresh)
        {
            grassBool = grassNoise < currentProp.grassThresholdOffset;
        }

        if (Random.value > currentProp.mainThreshold || waterBool || grassBool)
        {
            _prop.isProp = false;
            return _prop;
        }

        if (currentProp.isSolid)
        {
            position = (position +
                grassmap.layoutGrid.GetCellCenterWorld(new Vector3Int(pos.x + currentProp.scale.x-1, pos.y + currentProp.scale.y -1, 0)))/2;

            for (int x = 0; x < currentProp.scale.x; x++)
            {
                for (int y = 0; y < currentProp.scale.y; y++)
                {
                    Vector2Int coord = pos + new Vector2Int(x, y);

                    if (chunksGen.blocksData.ContainsKey(coord))
                    {
                        GameObject go = chunksGen.blocksData[coord];
                        if (go != null)
                        {
                            _prop.isProp = false;
                            return _prop;
                        }
                    }
                }
            }
        }

        GameObject _mainObject = new GameObject();

        Variation finalVar = new Variation();

        int life = 0;
        sbyte objectVarIndex = 0;
        List<sbyte> objectVarIndexes = new List<sbyte>();
        foreach(Variation variation in currentProp.variations)
        {
            if (Random.value > variation.probability)
            {
                objectVarIndex++;
                continue;
            };

            GameObject _varObject = Instantiate(variation.prefab, position, Quaternion.identity);
            SpriteRenderer sr;
            _varObject.TryGetComponent(out sr);

            if (variation.spriteBased)
            {
                int length = variation.instances.Length;
                int index;

                if (variation.colorBased)
                {
                    index = GetColorIndex(grassNoise, length, 1 - currentProp.mainThreshold, 1);
                    sr.sprite = variation.instances[index];
                }
                else
                {
                    index = Random.Range(0, length);
                    sr.sprite = variation.instances[index];
                }

                if (variation.interactable)
                {
                    _varObject.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = variation.outlines[index];
                }
            }

            if (!variation.isAdditive)
            {
                finalVar = variation;
                Destroy(_mainObject);
                _mainObject = _varObject;
                if(objectVarIndexes.Count > 0) objectVarIndexes.RemoveAt(0);
            }
            else
            {
                _varObject.transform.parent = _mainObject.transform;
                _varObject.transform.position += new Vector3(0, 0, -0.01f);
            }

            if (variation.isDrop)
            {
                Destroyable dest = _mainObject.GetComponent<Destroyable>();
                dest.drop.Add(variation.drop);
                dest.dropCount.Add(variation.dropCount);
            }

            objectVarIndexes.Add(objectVarIndex);
            objectVarIndex++;
        }

        _mainObject.transform.parent = shadowChunk.transform;

        Destroyable destroyable = _mainObject.GetComponent<Destroyable>();

        if(destroyable != null)
        {
            life = destroyable.MaxLife();
        }

        if (currentProp.isSolid)
        {
            for (int x = 0; x < currentProp.scale.x; x++)
            {
                for (int y = 0; y < currentProp.scale.y; y++)
                {
                    Vector2Int coord = pos + new Vector2Int(x, y);
                    chunksGen.blocksData[coord] = _mainObject;
                }
            }
        }

        if (finalVar.isShadowed)
        {
            ShadowInstance shadow = _mainObject.GetComponent<ShadowInstance>();

            shadow.chunk = shadowChunk;
            shadow.shadowLength = finalVar.shadowLength;
            shadow.ManualAwake();
        }

        _prop.prop = new Prop()
        {
            pos = pos,
            objectType = (sbyte)j,
            objectVar = objectVarIndexes.ToArray(),
            life = (short)life

        };
        _prop.coord = new Vector3Int(pos.x, pos.y, 0);
        _prop.isProp = true;

        return _prop;
    }


    private void SpawnPropClient(Prop _prop, Vector2Int pos, float grassNoise, float waterNoise, ShadowChunk shadowChunk)
    {
        Object obj = prop[_prop.objectType];
        Vector3 position = grassmap.layoutGrid.GetCellCenterWorld(new Vector3Int(pos.x, pos.y, 0));

        GameObject _mainObject = new GameObject();

        int len = _prop.objectVar.Length;

        Variation finalVar = new Variation();

        if (obj.isSolid)
        {
            position = (position +
                grassmap.layoutGrid.GetCellCenterWorld(new Vector3Int(pos.x + obj.scale.x - 1, pos.y + obj.scale.y - 1, 0))) / 2;
        }

        for (int i = 0; i < len; i++)
        {
            int j = _prop.objectVar[i];
            Variation variation = obj.variations[j];
            
            GameObject _varObject = Instantiate(variation.prefab, position, Quaternion.identity);
            SpriteRenderer sr;
            _varObject.TryGetComponent(out sr);

            if (variation.spriteBased)
            {
                int length = variation.instances.Length;
                int index;

                if (variation.colorBased)
                {
                    index = GetColorIndex(grassNoise, length, 1 - obj.mainThreshold, 1);
                    sr.sprite = variation.instances[index];
                }
                else
                {
                    index = Random.Range(0, length);
                    sr.sprite = variation.instances[index];
                }

                if (variation.interactable)
                {
                    _varObject.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = variation.outlines[index];
                }
            }

            if (!variation.isAdditive)
            {
                finalVar = variation;
                Destroy(_mainObject);
                _mainObject = _varObject;
            }
            else
            {
                _varObject.transform.parent = _mainObject.transform;
                _varObject.transform.position -= new Vector3(0, 0.000001f, 0);

                if (variation.isDrop)
                {
                    Destroyable dest = _mainObject.GetComponent<Destroyable>();
                    dest.drop.Add(variation.drop);
                    dest.dropCount.Add(variation.dropCount);
                }
            }
        }

        _mainObject.transform.parent = shadowChunk.transform;

        Destroyable destroyable = _mainObject.GetComponent<Destroyable>();

        if(destroyable != null)
        {
            destroyable.SetLife(_prop.life);

            byte key = (byte)Random.Range(0, byte.MaxValue);
            destroyable.UpdateLife(false, 0, key);
        }

        if (obj.isSolid)
        {
            for (int x = 0; x < obj.scale.x; x++)
            {
                for (int y = 0; y < obj.scale.y; y++)
                {
                    Vector2Int coord = pos + new Vector2Int(x, y);
                    chunksGen.blocksData[coord] = _mainObject;
                }
            }
        }
        chunksGen.blocksData[pos] = _mainObject;

        if (finalVar.isShadowed)
        {
            ShadowInstance shadow = _mainObject.GetComponent<ShadowInstance>();

            shadow.chunk = shadowChunk;
            shadow.shadowLength = finalVar.shadowLength;
            shadow.ManualAwake();
        }
    }


    private ChunkTile GenWater(Vector2Int pos, float noise)
    {

        Dictionary<Vector2Int, float> noises = new Dictionary<Vector2Int, float>();

        foreach(Vector2Int direction in directions)
        {
            noises[direction] = GenWaterNoise(pos + direction);
        }

        bool isWhite;

        ChunkTile waterTile = new ChunkTile();
        waterTile.coord = new Vector3Int(pos.x+2, pos.y+2, 1);
        waterTile.tilemap = grassmap;
        Tile _waterTile = CustomRuleTile.GetTileCRT(waterRuleTile, noises, waterThreshold, out isWhite);

        if (isWhite)
        {
            int color = GetColorIndex(noise, 2 + waterColorSteps, 0, waterThreshold);
            _waterTile = customWater[1 + waterColorSteps - color];
        }

        waterTile.tile = _waterTile;
        return waterTile;
    }


    private float GenWaterNoise(Vector2Int coord)
    {
        return Perlin(coord.x, coord.y, waterNoiseScale, 3, waterSeed, 3f, 1.5f);
    }


    private float Perlin(int x, int y, float scale, int number, Vector2 seed, float decreaseAmplitude, float decreaseFrequency)
    {
        float perlin = 0;
        float frequency = scale;
        float amplitude = 1;
        float totalAmplitude = 0;

        float X = x + 0.2f + seed.x;
        float Y = y + 0.2f + seed.y;

        for (int i = 0; i < number; i++)
        {
            totalAmplitude += amplitude;
            perlin += Mathf.PerlinNoise(X / frequency, Y / frequency) * amplitude;
            amplitude = amplitude / decreaseAmplitude;
            frequency = frequency / decreaseFrequency;
        }

        perlin = perlin / totalAmplitude;

        return perlin;
    }


    public int GetColorIndex(float x, int number, float min, float max)
    {
        float X = (x - min) / (max - min);
        int i = Mathf.Clamp(Mathf.RoundToInt(X * number - 0.5f), 0, number - 1);
        return i;
    }


    private float Saturate(float x)
    {
        return saturationCurve.Evaluate(x);
    }
}
