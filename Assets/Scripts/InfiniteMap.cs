using UnityEngine;
using System.Collections.Generic;

public class InfiniteMap : MonoBehaviour
{
    [Header("Settings")]
    public GameObject mTilePrefab; 
    public float mTileSize = 100f;
    public int mViewDistance = 2; // Chunk ray around the player

    private Dictionary<Vector2Int, GameObject> mActiveTiles = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int mCurrentChunk;

    void Start()
    {
        mCurrentChunk = GetChunkCoords(Player.Instance.transform.position);
        UpdateGrid();
    }

    void Update()
    {
        Vector2Int lPlayerChunk = GetChunkCoords(Player.Instance.transform.position);

        if (lPlayerChunk != mCurrentChunk)
        {
            mCurrentChunk = lPlayerChunk;
            UpdateGrid();
        }
    }

    void UpdateGrid()
    {
        // Compute the chunks that should be active based on the player's current chunk and view distance
        List<Vector2Int> lRequiredChunks = new List<Vector2Int>();
        for (int x = -mViewDistance; x <= mViewDistance; x++)
        {
            for (int y = -mViewDistance; y <= mViewDistance; y++)
            {
                lRequiredChunks.Add(new Vector2Int(mCurrentChunk.x + x, mCurrentChunk.y + y));
            }
        }

        // Identify chunks to remove
        List<Vector2Int> lChunksToRemove = new List<Vector2Int>();
        foreach (var lEntry in mActiveTiles)
        {
            if (!lRequiredChunks.Contains(lEntry.Key))
            {
                lChunksToRemove.Add(lEntry.Key);
            }
        }

        // Recycle or create tiles for required chunks
        foreach (Vector2Int lCoord in lRequiredChunks)
        {
            if (!mActiveTiles.ContainsKey(lCoord))
            {
                GameObject lTile;
                if (lChunksToRemove.Count > 0)
                {
                    // Recycle existing tile from the chunks to remove
                    Vector2Int lOldCoord = lChunksToRemove[0];
                    lTile = mActiveTiles[lOldCoord];
                    mActiveTiles.Remove(lOldCoord);
                    lChunksToRemove.RemoveAt(0);
                }
                else
                {
                    // If not enough tiles to recycle, instantiate a new one
                    lTile = Instantiate(mTilePrefab, transform);
                }

                lTile.transform.position = new Vector3(lCoord.x * mTileSize, lCoord.y * mTileSize, 0);
                mActiveTiles.Add(lCoord, lTile);
            }
        }
    }

    Vector2Int GetChunkCoords(Vector3 pPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(pPos.x / mTileSize),
            Mathf.RoundToInt(pPos.y / mTileSize)
        );
    }
}