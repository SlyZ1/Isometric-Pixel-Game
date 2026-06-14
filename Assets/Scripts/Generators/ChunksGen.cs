using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class ChunksGen : MonoBehaviour
{
    [Header("Requirements")]
    [Space]
    [SerializeField] private Tilemap refMap;
    [Space]
    [SerializeField] public Grid grid;
    [Space]
    [SerializeField] private PlainGen plainGen;
    [Space]
    [SerializeField] private GameManager gameManager;
    [Space]
    [SerializeField] private Transform cam;
    [Space]
    [SerializeField] private ShadowManager shadowManager;
    [Space]
    [SerializeField] private GameObject fakeGrass;
    [Space]
    [SerializeField] private CRG fakeGrassRG;

    [Space]
    [Header("Params")]
    [Space]
    public int chunkHalfSize;
    [SerializeField] private float chunkUpdateDistance;
    [Space]

    public Dictionary<Vector2Int, GameObject> blocksData = new Dictionary<Vector2Int, GameObject>();
    public Dictionary<Vector2Int, bool> isInit = new Dictionary<Vector2Int, bool>();
    Dictionary<Vector2Int, Chunk> chunksData = new Dictionary<Vector2Int, Chunk>();
    Dictionary<Vector2Int, Transform> chunkParents = new Dictionary<Vector2Int, Transform>();
    private List<Vector2Int> waitingChunks = new List<Vector2Int>();
    private List<Vector2Int> activeChunks = new List<Vector2Int>();
    private List<Vector2Int> chunkToUpdate;
    private List<Vector2Int> chunkToDespawn;

    private Vector2Int[] directions = new Vector2Int[8];
 
    private Grid layoutGrid;

    private TileBase[] nullChunk;

    public static ChunksGen instance;

    int t = 0;

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(this);
        }
        {
            instance = this;
        }

        chunkUpdateDistance *= Camera.main.orthographicSize / 2.7f;
    }

    private void Start()
    {
        SceneManager.SetActiveScene(SceneManager.GetSceneByName("Generation"));

        directions[0] = Vector2Int.down;
        directions[1] = Vector2Int.left;
        directions[2] = Vector2Int.right;
        directions[3] = Vector2Int.up;
        directions[4] = Vector2Int.down + Vector2Int.left;
        directions[5] = Vector2Int.down + Vector2Int.right;
        directions[6] = Vector2Int.up + Vector2Int.left;
        directions[7] = Vector2Int.up + Vector2Int.right;

        layoutGrid = refMap.layoutGrid;
        InitializeChunk();
    }


    private void InitializeChunk()
    {
        GenerateChunk(Vector2Int.zero, true);

        activeChunks.Add(Vector2Int.zero);

        UpdateWaitingChunks(Vector2Int.zero);
    }


    public bool SpawnBlock(Item item, Vector2Int coords)
    {
        for (int x = 0; x < item.block.size.x; x++)
        {
            for (int y = 0; y < item.block.size.y; y++)
            {
                Vector2Int coord = coords + new Vector2Int(x, y);

                if (blocksData.ContainsKey(coord))
                {
                    if (blocksData[coord] != null) return false;
                }
            }
        };

        GameObject obj = Instantiate(item.block.prefab, grid.GetCellCenterWorld((Vector3Int)coords), Quaternion.identity);


        if (!chunkParents.ContainsKey(PosToChunk(coords)))
        {
            chunkParents[PosToChunk(coords)] = new GameObject("" + coords).transform;
            chunkParents[PosToChunk(coords)].gameObject.SetActive(false);
        }

        GameObject parent = chunkParents[PosToChunk(coords)].gameObject;

        obj.transform.parent = parent.transform;

        ShadowInstance shadow = obj.GetComponent<ShadowInstance>();

        if (shadow != null)
        {
            ShadowChunk shadowChunk = parent.GetComponent<ShadowChunk>();
            shadow.chunk = shadowChunk;
            shadow.ManualAwake();
            shadowChunk.ManualAwake();
        }

        for (int x = 0; x < item.block.size.x; x++)
        {
            for (int y = 0; y < item.block.size.y; y++)
            {
                Vector2Int coord = coords + new Vector2Int(x, y);
                blocksData[coord] = obj;
            }
        }

        EnemyManager.instance.pgm.graph?.Scan();

        return true;
    }


    public bool RemoveBlock(Vector2Int coords, byte key, bool automatic, bool spawnLoot = true)
    {
        if (!blocksData.ContainsKey(coords))
        {
            GameObject go = blocksData[coords];
            if(go == null)
            {
                return false;
            }
        }

        if (LayerMask.LayerToName(blocksData[coords].layer) == "FakeGrass") return false;

        Destroyable destroyable = blocksData[coords].GetComponent<Destroyable>();
        if (destroyable != null)
        {
            if(spawnLoot) destroyable.Despawn(key, automatic);
        }
        else
        {
            return false;
        }

        Destroy(blocksData[coords]);
        blocksData.Remove(coords);

        EnemyManager.instance.pgm.graph?.Scan();

        return true;
    }


    private void Update()
    {
        UpdateChunks();
    }


    private void UpdateChunks()
    {
        Vector2 camPos2D = new Vector2(cam.position.x, cam.position.y);

        if(t == 0)
        {
            chunkToUpdate = new List<Vector2Int>();
            foreach (Vector2Int _chunk in waitingChunks)
            {
                Vector3 chunkPosition3D = layoutGrid.CellToWorld(chunkHalfSize * 2 * new Vector3Int(_chunk.x, _chunk.y, 0));
                Vector2 chunkPosition2D = new Vector2(chunkPosition3D.x, chunkPosition3D.y);

                float isometricDistance = Mathf.Sqrt(Mathf.Pow(camPos2D.x - chunkPosition2D.x, 2) + Mathf.Pow((camPos2D.y - 0.2f - chunkPosition2D.y) * 2f, 2));

                if (isometricDistance < chunkUpdateDistance)
                {
                    chunkToUpdate.Add(_chunk);
                }
            }

            foreach (Vector2Int _chunk in chunkToUpdate)
            {
                UpdateChunk(_chunk);
            }
        }
        
        if(t == 1)
        {
            chunkToDespawn = new List<Vector2Int>();
            foreach (Vector2Int _chunk in activeChunks)
            {
                Vector3 chunkPosition3D = layoutGrid.CellToWorld(chunkHalfSize * 2 * new Vector3Int(_chunk.x, _chunk.y, 0));
                Vector2 chunkPosition2D = new Vector2(chunkPosition3D.x, chunkPosition3D.y);

                float isometricDistance = Mathf.Sqrt(Mathf.Pow(camPos2D.x - chunkPosition2D.x, 2) + Mathf.Pow((camPos2D.y - 0.2f - chunkPosition2D.y) * 2f, 2));

                if (isometricDistance > chunkUpdateDistance + 0.2f)
                {
                    chunkToDespawn.Add(_chunk);

                }
            }

            
            foreach (Vector2Int _chunk in chunkToDespawn)
            {
                DespawnChunk(_chunk);
            }
        }

        t = (t + 1) % 2;
    }


    private void UpdateWaitingChunks(Vector2Int coords)
    {
        for (int i = 0; i < 8; i++)
        {
            AddWaitingChunks(coords + directions[i]);
        }
    }


    private void AddWaitingChunks(Vector2Int coords)
    {
        if (waitingChunks.Contains(coords) || activeChunks.Contains(coords)) return;

        if (!NetworkManager.Singleton.IsHost && !isInit.ContainsKey(coords))
        {
            gameManager.GetChunkServerRpc(coords, (byte)NetworkManager.Singleton.LocalClientId);
        }

        waitingChunks.Add(coords);
    }


    private void RemoveWaitingChunk(Vector2Int coords)
    {
        if (!waitingChunks.Contains(coords)) return;

        waitingChunks.Remove(coords);
    }


    private void UpdateChunk(Vector2Int coords)
    {
        if(activeChunks.Contains(coords)) return;

        if (!chunksData.ContainsKey(coords))
        {
            GenerateChunk(coords, true);
        }
        else
        {
            StartCoroutine(SpawnChunk(coords));
        }

        activeChunks.Add(coords);

        UpdateWaitingChunks(coords);
        RemoveWaitingChunk(coords);
    }


    private IEnumerator SpawnChunk(Vector2Int coords)
    {
        Chunk chunk = chunksData[coords];
        ChunkData[] data = chunk.data;

        int i = 0;

        foreach (ChunkData _data in data)
        {
            if(i >= chunkHalfSize * chunkHalfSize / 4) yield return new WaitForFixedUpdate();

            _data.tilemap.SetTiles(_data.pos, _data.tiles);
            _data.tilemap.CompressBounds();
            i++;
        }

        yield return new WaitForFixedUpdate();

        chunk.parent.SetActive(true);
    }


    private void DespawnChunk(Vector2Int coords)
    {
        Chunk chunk = chunksData[coords];
        ChunkData[] data = chunk.data;

        foreach (ChunkData _data in data)
        {
            int len = _data.pos.Length;
            nullChunk = new TileBase[len];
            for (int i = 0; i < len; i++)
            {
                nullChunk[i] = null;
            }

            _data.tilemap.SetTiles(_data.pos, nullChunk);
        }

        chunk.parent.SetActive(false);

        activeChunks.Remove(coords);
        waitingChunks.Add(coords);

        for (int i = 0; i < 8; i++)
        {
            if(waitingChunks.Contains(coords + directions[i]))
            {
                bool toDelete = true;

                for (int j = 0; j < 8; j++)
                {
                    if (activeChunks.Contains(coords + directions[i] + directions[j]))
                    {
                        toDelete = false;
                        break;
                    }
                }

                if(toDelete)
                {
                    waitingChunks.Remove(coords + directions[i]);
                }
            }
        }
    }


    public void GenerateChunk(Vector2Int coords, bool toSpawn)
    {
        if (chunksData.ContainsKey(coords)) return;

        GameObject chunkParent;
        if(chunkParents.ContainsKey(coords))
        {
            chunkParent = chunkParents[coords].gameObject;
        }
        else
        {
            chunkParent = new GameObject(coords.ToString());
            chunkParents[coords] = chunkParent.transform;
        }

        ShadowChunk shadowChunk = chunkParent.AddComponent<ShadowChunk>();
        shadowChunk.manager = shadowManager;

        if (activeChunks.Contains(coords)) return;

        Chunk chunk = plainGen.GenChunk(coords, chunkHalfSize, chunkParent);

        chunksData[coords] = chunk;

        if (toSpawn)
        {
            StartCoroutine(SpawnChunk(coords));
        }
    }


    public Vector2Int PosToChunk(Vector2Int pos)
    {
        Vector2 a = Vector2.one / 4 / chunkHalfSize + (Vector2)pos / 2 / chunkHalfSize;
        return Vector2Int.RoundToInt(a);
    }
}



public struct Chunk
{
    public ChunkData[] data;
    public GameObject parent;
}

public struct ChunkData
{
    public Tilemap tilemap;
    public Vector3Int[] pos;
    public TileBase[] tiles;
}

public struct ChunkTile
{
    internal Vector3Int coord;
    internal TileBase tile;
    internal Tilemap tilemap;
    internal Prop prop;
    internal bool isProp;

    internal ChunkTile(Tilemap tilemap, TileBase tile)
    {
        coord = Vector3Int.zero;
        this.tilemap = tilemap;
        this.tile = tile;
        isProp = false;
        prop = new Prop()
        {
            pos = Vector2Int.zero,
            objectType = -1,
            objectVar = null
        };
    }
}


public struct Prop : INetworkSerializable
{
    internal short life;
    internal Vector2Int pos;
    internal sbyte objectType;
    internal sbyte[] objectVar;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref life);
        serializer.SerializeValue(ref pos);
        serializer.SerializeValue(ref objectType);
        serializer.SerializeValue(ref objectVar);
    }
}
