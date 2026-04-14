using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public class InfiniteMap : MonoBehaviour
    {
        [Header("Settings")]
        public GameObject mTilePrefab;

        public float mTileSize = 100f;

        public int mViewDistance = 2; // Chunk ray around the player

        private readonly Dictionary<Vector2Int, GameObject> mActiveTiles = new();

        private Vector2Int mCurrentChunk;

        private readonly List<Vector2Int> mRequiredChunksList = new();

        private readonly HashSet<Vector2Int> mRequiredChunksSet = new();

        private List<Vector2Int> mChunksToRemove = new();

        private void UpdateGrid()
        {
            mRequiredChunksList.Clear();
            mRequiredChunksSet.Clear();

            // Compute the chunks that should be active based on the player's current chunk and view distance
            for (int x = -mViewDistance; x <= mViewDistance; x++)
            {
                for (int y = -mViewDistance; y <= mViewDistance; y++)
                {
                    Vector2Int lCoord = new(mCurrentChunk.x + x, mCurrentChunk.y + y);
                    mRequiredChunksList.Add(lCoord);
                    mRequiredChunksSet.Add(lCoord);
                }
            }

            // Identify chunks to remove
            mChunksToRemove.Clear();

            // We iterate over active tiles and check if they are still required. If not, we mark them for removal.
            foreach (var kv in mActiveTiles) // Ignore LINQ and ToList to avoid extra allocations
            {
                if (!mRequiredChunksSet.Contains(kv.Key))
                {
                    mChunksToRemove.Add(kv.Key);
                }
            }

            // Place required chunks, either by recycling or instantiating new tiles
            foreach (Vector2Int coord in mRequiredChunksList) // Ignore LINQ and ToList to avoid extra allocations
            {
                if (!mActiveTiles.ContainsKey(coord))
                {
                    GameObject tile = GetTileToPlace(ref mChunksToRemove);
                    tile.transform.position = new Vector3(coord.x * mTileSize, coord.y * mTileSize, 0);
                    mActiveTiles.Add(coord, tile);
                }
            }
        }

        // Utility method to get a tile to place, either by recycling an old one or instantiating a new one
        private GameObject GetTileToPlace(ref List<Vector2Int> pChunksToRemove)
        {

            if (pChunksToRemove.Count > 0)
            {
                int lLastIndex = pChunksToRemove.Count - 1;
                Vector2Int lOldCoord = pChunksToRemove[lLastIndex];
                GameObject lRecycledTile = mActiveTiles[lOldCoord];

                mActiveTiles.Remove(lOldCoord);
                pChunksToRemove.RemoveAt(lLastIndex);

                return lRecycledTile;
            }

            return Instantiate(mTilePrefab, transform);
        }

        private Vector2Int GetChunkCoords(Vector3 pPos)
        {
            return new Vector2Int(Mathf.FloorToInt(pPos.x / mTileSize),Mathf.FloorToInt(pPos.y / mTileSize));
        }

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
    }
}