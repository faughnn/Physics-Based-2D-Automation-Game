using UnityEngine;
using System.Collections.Generic;
using GoldRush.Core;

namespace GoldRush.Building
{
    public class BuildPreview : MonoBehaviour
    {
        private BuildSystem buildSystem;
        private List<GameObject> previewObjects = new List<GameObject>();
        private Color validColor = new Color(1f, 1f, 1f, 0.5f);
        private Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.5f);

        public void Initialize(BuildSystem system)
        {
            buildSystem = system;
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (!visible)
            {
                ClearPreviews();
            }
        }

        public void ShowSingle(Vector2Int gridPos, BuildType type, bool directionPositive, bool canPlace)
        {
            ClearPreviews();

            GameObject preview = CreatePreviewObject(type, directionPositive);
            if (preview == null) return;

            Vector2 worldPos = GetWorldPosForType(gridPos, type);
            preview.transform.position = worldPos;

            SpriteRenderer sr = preview.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = canPlace ? validColor : invalidColor;
            }

            previewObjects.Add(preview);
        }

        public void ShowMultiple(List<Vector2Int> positions, BuildType type, bool directionPositive)
        {
            ClearPreviews();

            foreach (Vector2Int pos in positions)
            {
                bool canPlace = buildSystem.CanPlaceAt(pos);

                GameObject preview = CreatePreviewObject(type, directionPositive);
                if (preview == null) continue;

                Vector2 worldPos = GetWorldPosForType(pos, type);
                preview.transform.position = worldPos;

                SpriteRenderer sr = preview.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = canPlace ? validColor : invalidColor;
                }

                previewObjects.Add(preview);
            }
        }

        private Vector2 GetWorldPosForType(Vector2Int gridPos, BuildType type)
        {
            if (!BuildTypeData.TryGet(type, out var info))
            {
                return GameSettings.GridToWorld(gridPos.x, gridPos.y);
            }

            Vector2 pos = info.Grid.ToWorld(gridPos.x, gridPos.y);

            // Offset for multi-cell buildings (they're centered on the span)
            if (info.CellSpanX > 1)
            {
                pos.x += (info.CellSpanX - 1) * info.Grid.CellWidth / 2f / GameSettings.PixelsPerUnit;
            }
            if (info.CellSpanY > 1)
            {
                pos.y -= (info.CellSpanY - 1) * info.Grid.CellHeight / 2f / GameSettings.PixelsPerUnit;
            }

            return pos;
        }

        private GameObject CreatePreviewObject(BuildType type, bool directionPositive)
        {
            if (!BuildTypeData.TryGet(type, out var info))
            {
                return null;
            }

            GameObject preview = new GameObject("Preview");
            preview.transform.SetParent(transform);

            SpriteRenderer sr = preview.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 100; // Above everything

            int w = info.VisualWidth;
            int h = info.VisualHeight;

            // Type-specific sprite creation (using metadata for sizes)
            switch (type)
            {
                case BuildType.Wall:
                    sr.sprite = SpriteGenerator.CreateSolidWallSprite(w, GameSettings.WallColor);
                    break;
                case BuildType.Belt:
                    sr.sprite = SpriteGenerator.CreateArrowSprite(w, h, GameSettings.BeltColor, true, directionPositive);
                    break;
                case BuildType.Lift:
                    sr.sprite = SpriteGenerator.CreateHollowLiftSprite(w, GameSettings.LiftColor, directionPositive);
                    break;
                case BuildType.Shaker:
                    sr.sprite = SpriteGenerator.CreateArrowSprite(w, h, GameSettings.ShakerColor, true, directionPositive);
                    break;
                case BuildType.GoldStore:
                    sr.sprite = SpriteGenerator.GetSprite("GoldStore");
                    break;
                case BuildType.Blower:
                    sr.sprite = SpriteGenerator.CreateHollowBlowerSprite(w, GameSettings.BlowerColor, directionPositive);
                    break;
                case BuildType.FilterBelt:
                    sr.sprite = SpriteGenerator.CreateArrowSprite(w, h, new Color(0.3f, 0.3f, 0.5f), true, directionPositive);
                    break;
                case BuildType.BigCrusher:
                    sr.sprite = SpriteGenerator.CreateSolidSprite(w, h, new Color(0.35f, 0.35f, 0.4f));
                    break;
                case BuildType.SmallCrusher:
                    sr.sprite = SpriteGenerator.CreateSolidSprite(w, h, new Color(0.4f, 0.35f, 0.35f));
                    break;
                case BuildType.Grinder:
                    sr.sprite = SpriteGenerator.CreateSolidSprite(w, h, new Color(0.6f, 0.55f, 0.5f));
                    break;
                case BuildType.Smelter:
                    sr.sprite = SpriteGenerator.CreateSolidSprite(w, h, new Color(0.5f, 0.25f, 0.15f));
                    break;
                default:
                    Destroy(preview);
                    return null;
            }

            return preview;
        }

        private void ClearPreviews()
        {
            foreach (GameObject obj in previewObjects)
            {
                if (obj != null) Destroy(obj);
            }
            previewObjects.Clear();
        }
    }
}
