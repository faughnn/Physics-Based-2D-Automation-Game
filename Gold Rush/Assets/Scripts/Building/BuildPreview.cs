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
            switch (type)
            {
                case BuildType.Belt:
                case BuildType.Wall:
                    return GameSettings.SubGridToWorld(gridPos.x, gridPos.y);
                case BuildType.Shaker:
                    return GameSettings.ShakerGridToWorld(gridPos.x, gridPos.y);
                case BuildType.Lift:
                case BuildType.Blower:
                case BuildType.StampMill:
                case BuildType.RollerCrusher:
                case BuildType.JawCrusher:
                    return GameSettings.InfraGridToWorld(gridPos.x, gridPos.y);
                case BuildType.GoldStore:
                    Vector2 pos = GameSettings.GridToWorld(gridPos.x, gridPos.y);
                    pos.x += GameSettings.GridSize / 2f / GameSettings.PixelsPerUnit;
                    return pos;
                default:
                    return GameSettings.GridToWorld(gridPos.x, gridPos.y);
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
                    // 16x16 solid wall preview
                    sr.sprite = SpriteGenerator.CreateSolidWallSprite(GameSettings.WallSize, GameSettings.WallColor);
                    break;
                case BuildType.Belt:
                    // 16x16 belt preview
                    sr.sprite = SpriteGenerator.CreateArrowSprite(GameSettings.BeltSize, GameSettings.BeltSize, GameSettings.BeltColor, true, directionPositive);
                    break;
                case BuildType.Lift:
                    // 32x32 lift preview
                    sr.sprite = SpriteGenerator.CreateHollowLiftSprite(GameSettings.InfraGridSize, GameSettings.LiftColor, directionPositive);
                    break;
                case BuildType.Shaker:
                    // 32x16 shaker preview
                    sr.sprite = SpriteGenerator.CreateArrowSprite(GameSettings.GridSize, 16, GameSettings.ShakerColor, true, directionPositive);
                    break;
                case BuildType.GoldStore:
                    sr.sprite = SpriteGenerator.GetSprite("GoldStore");
                    break;
                case BuildType.Blower:
                    // 32x32 blower preview (horizontal lift)
                    sr.sprite = SpriteGenerator.CreateHollowBlowerSprite(GameSettings.InfraGridSize, GameSettings.BlowerColor, directionPositive);
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
