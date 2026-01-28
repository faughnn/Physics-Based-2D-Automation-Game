using System.Collections.Generic;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Renders semi-transparent overlays at ghost structure block positions.
    /// Ghost blocks are structures placed through terrain that haven't activated yet.
    /// </summary>
    public class GhostStructureRenderer : MonoBehaviour
    {
        private BeltManager beltManager;
        private LiftManager liftManager;
        private int worldWidth;
        private int worldHeight;

        // Sprite pool
        private readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
        private int activeCount = 0;

        // Shared texture (8x8 white block)
        private Texture2D ghostTexture;
        private Sprite ghostSprite;

        // Colors for ghost overlays
        private static readonly Color BeltGhostColor = new Color(0.3f, 0.3f, 0.4f, 0.35f);
        private static readonly Color LiftGhostColor = new Color(0.3f, 0.5f, 0.3f, 0.35f);

        // Reusable lists to avoid GC
        private readonly List<Vector2Int> beltPositions = new List<Vector2Int>();
        private readonly List<Vector2Int> liftPositions = new List<Vector2Int>();

        public void Initialize(BeltManager beltManager, LiftManager liftManager, int worldWidth, int worldHeight)
        {
            this.beltManager = beltManager;
            this.liftManager = liftManager;
            this.worldWidth = worldWidth;
            this.worldHeight = worldHeight;

            CreateTexture();
        }

        private void CreateTexture()
        {
            ghostTexture = new Texture2D(8, 8);
            ghostTexture.filterMode = FilterMode.Point;
            Color[] pixels = new Color[64];
            for (int i = 0; i < 64; i++)
                pixels[i] = Color.white;
            ghostTexture.SetPixels(pixels);
            ghostTexture.Apply();

            float blockWorldSize = 8 * CoordinateUtils.CellToWorldScale;
            ghostSprite = Sprite.Create(ghostTexture, new Rect(0, 0, 8, 8),
                new Vector2(0.5f, 0.5f), 8f / blockWorldSize);
        }

        private void LateUpdate()
        {
            if (beltManager == null || liftManager == null) return;

            beltPositions.Clear();
            liftPositions.Clear();

            beltManager.GetGhostBlockPositions(beltPositions);
            liftManager.GetGhostBlockPositions(liftPositions);

            int totalNeeded = beltPositions.Count + liftPositions.Count;

            // Ensure pool has enough sprites
            while (pool.Count < totalNeeded)
            {
                var go = new GameObject("GhostOverlay");
                go.transform.SetParent(transform);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = ghostSprite;
                sr.sortingOrder = 90;
                go.SetActive(false);
                pool.Add(sr);
            }

            int idx = 0;

            // Position belt ghost sprites
            for (int i = 0; i < beltPositions.Count; i++)
            {
                var pos = beltPositions[i];
                var sr = pool[idx++];
                Vector2 worldPos = CoordinateUtils.CellToWorld(pos.x + 3.5f, pos.y + 3.5f, worldWidth, worldHeight);
                sr.transform.position = new Vector3(worldPos.x, worldPos.y, 0);
                sr.color = BeltGhostColor;
                sr.gameObject.SetActive(true);
            }

            // Position lift ghost sprites
            for (int i = 0; i < liftPositions.Count; i++)
            {
                var pos = liftPositions[i];
                var sr = pool[idx++];
                Vector2 worldPos = CoordinateUtils.CellToWorld(pos.x + 3.5f, pos.y + 3.5f, worldWidth, worldHeight);
                sr.transform.position = new Vector3(worldPos.x, worldPos.y, 0);
                sr.color = LiftGhostColor;
                sr.gameObject.SetActive(true);
            }

            // Deactivate unused sprites
            for (int i = idx; i < activeCount; i++)
            {
                pool[i].gameObject.SetActive(false);
            }

            activeCount = idx;
        }

        private void OnDestroy()
        {
            if (ghostTexture != null)
                Destroy(ghostTexture);
        }
    }
}
