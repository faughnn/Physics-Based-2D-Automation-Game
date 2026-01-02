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

            Vector2 worldPos = GameSettings.GridToWorld(gridPos.x, gridPos.y);
            AdjustPositionForType(ref worldPos, type);
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

                Vector2 worldPos = GameSettings.GridToWorld(pos.x, pos.y);
                AdjustPositionForType(ref worldPos, type);
                preview.transform.position = worldPos;

                SpriteRenderer sr = preview.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = canPlace ? validColor : invalidColor;
                }

                previewObjects.Add(preview);
            }
        }

        private GameObject CreatePreviewObject(BuildType type, bool directionPositive)
        {
            GameObject preview = new GameObject("Preview");
            preview.transform.SetParent(transform);

            SpriteRenderer sr = preview.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 100; // Above everything

            switch (type)
            {
                case BuildType.Wall:
                    sr.sprite = SpriteGenerator.GetSprite("Wall");
                    break;
                case BuildType.Belt:
                    sr.sprite = SpriteGenerator.CreateArrowSprite(GameSettings.GridSize, 8, GameSettings.BeltColor, true, directionPositive);
                    break;
                case BuildType.Lift:
                    sr.sprite = SpriteGenerator.CreateArrowSprite(GameSettings.GridSize, GameSettings.GridSize, GameSettings.LiftColor, false, directionPositive);
                    break;
                case BuildType.Shaker:
                    sr.sprite = SpriteGenerator.CreateArrowSprite(GameSettings.GridSize, 16, GameSettings.ShakerColor, true, directionPositive);
                    break;
                case BuildType.GoldStore:
                    sr.sprite = SpriteGenerator.GetSprite("GoldStore");
                    break;
                default:
                    Destroy(preview);
                    return null;
            }

            return preview;
        }

        private void AdjustPositionForType(ref Vector2 worldPos, BuildType type)
        {
            switch (type)
            {
                case BuildType.Belt:
                    worldPos.y -= (GameSettings.GridSize - 8) / 2f / GameSettings.PixelsPerUnit;
                    break;
                case BuildType.Shaker:
                    worldPos.y -= (GameSettings.GridSize - 16) / 2f / GameSettings.PixelsPerUnit;
                    break;
                case BuildType.GoldStore:
                    worldPos.x += GameSettings.GridSize / 2f / GameSettings.PixelsPerUnit;
                    break;
            }
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
