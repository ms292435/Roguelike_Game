using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Manages dynamic background tile chunks around the player to provide an infinite map illusion.
    /// Recycles inactive chunks via custom pooling to eliminate allocation overhead during exploration.
    /// </summary>
    public class InfiniteMap : MonoBehaviour
    {
        [Header("Settings")]
        public GameObject mTilePrefab;
        public float mTileSize = 100f;
        public int mViewDistance = 2; // Chunk radius around the player

        [Tooltip("Depth of the ground. Enemies are opaque and sorted by the depth buffer between z = 0.05 and 0.51 " +
                 "(the camera is at z = 0): the ground must be further away to stay behind them.")]
        public float mGroundDepth = 1f;

        [Tooltip("Render queue of the ground: in the opaque range, before the Aura (2001) and the enemies (2450 and more), " +
                 "so that everything is drawn over it whatever the depth testing of the sprite shader.")]
        public int mGroundRenderQueue = 2000;

        // Copy of the ground material with its own render queue, shared by every tile
        private readonly Dictionary<Material, Material> mGroundMaterials = new();

        private readonly Dictionary<Vector2Int, GameObject> mActiveTiles = new();
        private Vector2Int mCurrentChunk;
        private readonly List<Vector2Int> mRequiredChunksList = new();
        private readonly HashSet<Vector2Int> mRequiredChunksSet = new();
        private readonly List<Vector2Int> mChunksToRemove = new();

        /// <summary>
        /// Recalculates visible chunks within view distance, recycles distant tiles, and places new tiles.
        /// </summary>
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

            // Iterate over active tiles and mark unneeded chunks for recycling (avoids LINQ allocations)
            foreach (var kv in mActiveTiles)
            {
                if (!mRequiredChunksSet.Contains(kv.Key))
                {
                    mChunksToRemove.Add(kv.Key);
                }
            }

            // Place required chunks, either by recycling or instantiating new tiles
            foreach (Vector2Int lCoord in mRequiredChunksList)
            {
                if (!mActiveTiles.ContainsKey(lCoord))
                {
                    GameObject lTile = GetTileToPlace(mChunksToRemove);
                    lTile.transform.position = new Vector3(lCoord.x * mTileSize, lCoord.y * mTileSize, mGroundDepth);
                    mActiveTiles.Add(lCoord, lTile);
                }
            }
        }

        /// <summary>
        /// Retrieves a tile GameObject by either recycling an out-of-range tile or instantiating a new one.
        /// </summary>
        /// <param name="pChunksToRemove">List of coordinates available for recycling.</param>
        /// <returns>A tile GameObject ready to be positioned.</returns>
        private GameObject GetTileToPlace(List<Vector2Int> pChunksToRemove)
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

            GameObject lTile = Instantiate(mTilePrefab, transform);
            ApplyGroundRenderQueue(lTile);
            return lTile;
        }

        /// <summary>
        /// Gives the renderers of a new tile a copy of their material drawn in the ground render queue.
        /// The copies are shared between tiles, so that they keep batching together.
        /// </summary>
        /// <param name="pTile">The newly instantiated tile.</param>
        private void ApplyGroundRenderQueue(GameObject pTile)
        {
            foreach (var lRenderer in pTile.GetComponentsInChildren<Renderer>())
            {
                Material lSource = lRenderer.sharedMaterial;
                if (lSource == null) continue;

                if (!mGroundMaterials.TryGetValue(lSource, out Material lGroundMaterial))
                {
                    lGroundMaterial = new Material(lSource) { renderQueue = mGroundRenderQueue };
                    mGroundMaterials[lSource] = lGroundMaterial;
                }
                lRenderer.sharedMaterial = lGroundMaterial;
            }
        }

        /// <summary>
        /// Converts a world coordinate into chunk grid coordinates based on tile size.
        /// </summary>
        /// <param name="pPos">World position.</param>
        /// <returns>2D chunk grid coordinate.</returns>
        private Vector2Int GetChunkCoords(Vector3 pPos)
        {
            return new Vector2Int(Mathf.FloorToInt(pPos.x / mTileSize), Mathf.FloorToInt(pPos.y / mTileSize));
        }

        private void Start()
        {
            mCurrentChunk = GetChunkCoords(Player.Instance.transform.position);
            UpdateGrid();
        }

        private void Update()
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