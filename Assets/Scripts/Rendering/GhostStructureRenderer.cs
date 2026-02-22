using System.Collections.Generic;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Renders semi-transparent overlays at ghost structure block positions.
    /// Ghost blocks are structures placed through terrain that haven't activated yet.
    /// Iterates over registered IStructureManager instances — no per-type code needed.
    /// </summary>
    public class GhostStructureRenderer : MonoBehaviour
    {
        private List<IStructureManager> structureManagers;
        private int worldWidth;
        private int worldHeight;

        // Sprite pool
        private readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
        private int activeCount = 0;

        // Shared texture (8x8 white block)
        private Texture2D ghostTexture;
        private Sprite ghostSprite;

        // Reusable list to avoid GC
        private readonly List<Vector2Int> positions = new List<Vector2Int>();

        public void Initialize(List<IStructureManager> structureManagers, int worldWidth, int worldHeight)
        {
            this.structureManagers = structureManagers;
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
            if (structureManagers == null) return;

            int idx = 0;

            for (int m = 0; m < structureManagers.Count; m++)
            {
                var manager = structureManagers[m];
                positions.Clear();
                manager.GetGhostBlockPositions(positions);

                if (positions.Count == 0) continue;

                Color ghostColor = manager.GhostColor;

                // Ensure pool has enough sprites
                while (pool.Count < idx + positions.Count)
                {
                    var go = new GameObject("GhostOverlay");
                    go.transform.SetParent(transform);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = ghostSprite;
                    sr.sortingOrder = 90;
                    go.SetActive(false);
                    pool.Add(sr);
                }

                for (int i = 0; i < positions.Count; i++)
                {
                    var pos = positions[i];
                    var sr = pool[idx++];
                    Vector2 worldPos = CoordinateUtils.CellToWorld(pos.x + 3.5f, pos.y + 3.5f, worldWidth, worldHeight);
                    sr.transform.position = new Vector3(worldPos.x, worldPos.y, 0);
                    sr.color = ghostColor;
                    sr.gameObject.SetActive(true);
                }
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
