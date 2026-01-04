using UnityEngine;
using UnityEngine.UI;
using GoldRush.Simulation;

namespace GoldRush.UI
{
    public class MaterialInspector : MonoBehaviour
    {
        private GameObject tooltipPanel;
        private Text materialNameText;
        private Text interactionsText;
        private RectTransform tooltipRect;
        private Camera mainCamera;

        private bool isInspecting;

        public void Initialize()
        {
            mainCamera = Camera.main;
            CreateTooltip();
            tooltipPanel.SetActive(false);
        }

        private void CreateTooltip()
        {
            // Create tooltip panel
            tooltipPanel = new GameObject("MaterialTooltip");
            tooltipPanel.transform.SetParent(transform);

            tooltipRect = tooltipPanel.AddComponent<RectTransform>();
            tooltipRect.sizeDelta = new Vector2(280, 160);

            // Background
            Image bg = tooltipPanel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Add outline
            Outline outline = tooltipPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            outline.effectDistance = new Vector2(2, -2);

            // Material name header
            GameObject nameGO = new GameObject("MaterialName");
            nameGO.transform.SetParent(tooltipPanel.transform);

            RectTransform nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.pivot = new Vector2(0.5f, 1);
            nameRect.anchoredPosition = new Vector2(0, -5);
            nameRect.sizeDelta = new Vector2(-20, 30);

            materialNameText = nameGO.AddComponent<Text>();
            materialNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            materialNameText.fontSize = 18;
            materialNameText.fontStyle = FontStyle.Bold;
            materialNameText.alignment = TextAnchor.MiddleCenter;
            materialNameText.color = Color.white;

            // Interactions text
            GameObject interactGO = new GameObject("Interactions");
            interactGO.transform.SetParent(tooltipPanel.transform);

            RectTransform interactRect = interactGO.AddComponent<RectTransform>();
            interactRect.anchorMin = new Vector2(0, 0);
            interactRect.anchorMax = new Vector2(1, 1);
            interactRect.pivot = new Vector2(0.5f, 0.5f);
            interactRect.anchoredPosition = new Vector2(0, -15);
            interactRect.sizeDelta = new Vector2(-20, -50);

            interactionsText = interactGO.AddComponent<Text>();
            interactionsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            interactionsText.fontSize = 13;
            interactionsText.alignment = TextAnchor.UpperLeft;
            interactionsText.color = new Color(0.9f, 0.9f, 0.9f);

            // Hint text at bottom
            GameObject hintGO = new GameObject("Hint");
            hintGO.transform.SetParent(tooltipPanel.transform);

            RectTransform hintRect = hintGO.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0, 0);
            hintRect.anchorMax = new Vector2(1, 0);
            hintRect.pivot = new Vector2(0.5f, 0);
            hintRect.anchoredPosition = new Vector2(0, 5);
            hintRect.sizeDelta = new Vector2(-20, 20);

            Text hintText = hintGO.AddComponent<Text>();
            hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hintText.fontSize = 11;
            hintText.fontStyle = FontStyle.Italic;
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.color = new Color(0.6f, 0.6f, 0.6f);
            hintText.text = "[Hold ALT to inspect materials]";
        }

        private void Update()
        {
            // Check if Alt is held
            bool altHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

            if (altHeld)
            {
                isInspecting = true;
                tooltipPanel.SetActive(true);
                UpdateInspection();
            }
            else if (isInspecting)
            {
                isInspecting = false;
                tooltipPanel.SetActive(false);
            }
        }

        private void UpdateInspection()
        {
            if (SimulationWorld.Instance == null || mainCamera == null) return;

            // Get mouse position in world
            Vector2 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = SimulationWorld.Instance.WorldToGrid(mouseWorld);

            // Get material at position
            MaterialType material = SimulationWorld.Instance.Grid.Get(gridPos.x, gridPos.y);

            // Update tooltip content
            UpdateTooltipContent(material, gridPos);

            // Position tooltip near mouse
            PositionTooltip();
        }

        private void UpdateTooltipContent(MaterialType material, Vector2Int gridPos)
        {
            // Get material color for display
            Color32 matColor = MaterialProperties.GetColor(material);
            string colorHex = ColorUtility.ToHtmlStringRGB(matColor);

            // Check if terrain has a vein - show what's inside
            if (material == MaterialType.Terrain && SimulationWorld.Instance != null)
            {
                MaterialType veinType = SimulationWorld.Instance.GetVeinType(gridPos.x, gridPos.y);
                if (veinType != MaterialType.Sand)
                {
                    // Show vein contents with its color
                    Color32 veinColor = MaterialProperties.GetColor(veinType);
                    string veinColorHex = ColorUtility.ToHtmlStringRGB(veinColor);
                    materialNameText.text = $"<color=#{colorHex}>\u25A0</color> Terrain (<color=#{veinColorHex}>{GetMaterialDisplayName(veinType)}</color>)";

                    // Show vein-specific interactions
                    string veinInfo = GetVeinInteractions(veinType);
                    interactionsText.text = veinInfo;
                    return;
                }
            }

            // Material name with color swatch
            materialNameText.text = $"<color=#{colorHex}>\u25A0</color> {GetMaterialDisplayName(material)}";

            // Build interactions text
            string interactions = GetMaterialInteractions(material);
            interactionsText.text = interactions;
        }

        private string GetVeinInteractions(MaterialType veinType)
        {
            return veinType switch
            {
                MaterialType.Rock => "Dig to release <color=#808080>Rock</color> clusters.\n\n" +
                    "<color=#FFD700>Processing:</color>\n" +
                    "  Rock \u2192 Small Crusher \u2192 Gravel\n" +
                    "  Gravel \u2192 Grinder \u2192 Sand",

                MaterialType.Boulder => "Dig to release <color=#606060>Boulder</color> clusters.\n\n" +
                    "<color=#FFD700>Processing:</color>\n" +
                    "  Boulder \u2192 Big Crusher \u2192 Rock\n" +
                    "  Rock \u2192 Small Crusher \u2192 Gravel\n" +
                    "  Gravel \u2192 Grinder \u2192 Sand",

                MaterialType.Ore => "Dig to release <color=#B8860B>Gold Ore</color>!\n\n" +
                    "<color=#FFD700>Processing:</color>\n" +
                    "  Ore \u2192 Crusher \u2192 Concentrate\n" +
                    "  + Coal \u2192 Smelter \u2192 Gold!",

                MaterialType.Coal => "Dig to release <color=#2F2F2F>Coal</color>.\n\n" +
                    "<color=#FFD700>Use:</color>\n" +
                    "  Fuel for Smelter\n" +
                    "  + Concentrate \u2192 Gold!",

                MaterialType.Gravel => "Dig to release <color=#A0A0A0>Gravel</color> clusters.\n\n" +
                    "<color=#FFD700>Processing:</color>\n" +
                    "  Gravel \u2192 Grinder \u2192 Sand",

                _ => $"Contains {GetMaterialDisplayName(veinType)}.\n\nDig to extract."
            };
        }

        private string GetMaterialDisplayName(MaterialType type)
        {
            return type switch
            {
                MaterialType.Air => "Air (Empty)",
                MaterialType.Water => "Water",
                MaterialType.Sand => "Sand",
                MaterialType.WetSand => "Wet Sand",
                MaterialType.Gold => "Gold",
                MaterialType.Slag => "Slag (Waste)",
                MaterialType.Rock => "Rock",
                MaterialType.Gravel => "Gravel",
                MaterialType.Boulder => "Boulder",
                MaterialType.Ore => "Gold Ore",
                MaterialType.Concentrate => "Concentrate",
                MaterialType.Coal => "Coal",
                MaterialType.Terrain => "Terrain (Solid)",
                _ => type.ToString()
            };
        }

        private string GetMaterialInteractions(MaterialType type)
        {
            return type switch
            {
                MaterialType.Air => "Empty space.\nMaterials can fall through.",

                MaterialType.Water => "Liquid, spreads horizontally.\n\n" +
                    "<color=#FFD700>Interactions:</color>\n" +
                    "  + Sand \u2192 Wet Sand",

                MaterialType.Sand => "Basic granular material.\n\n" +
                    "<color=#FFD700>Interactions:</color>\n" +
                    "  + Water \u2192 Wet Sand\n\n" +
                    "<color=#00BFFF>Source:</color> Dig terrain, or Grinder",

                MaterialType.WetSand => "Wet mixture, ready for processing.\n\n" +
                    "<color=#FFD700>Interactions:</color>\n" +
                    "  \u2192 Shaker \u2192 Concentrate + Slag\n\n" +
                    "<color=#00BFFF>Source:</color> Sand + Water",

                MaterialType.Gold => "Valuable! Collect in Gold Store.\n\n" +
                    "<color=#FFD700>Interactions:</color>\n" +
                    "  \u2192 Gold Store (collect)\n\n" +
                    "<color=#00BFFF>Source:</color> Smelter (Concentrate + Coal)",

                MaterialType.Slag => "Waste byproduct. No value.\n\n" +
                    "<color=#00BFFF>Source:</color> Shaker, Smelter",

                MaterialType.Rock => "Heavy stone, must be crushed.\n\n" +
                    "<color=#FFD700>Interactions:</color>\n" +
                    "  \u2192 Crusher \u2192 Gravel\n\n" +
                    "<color=#00BFFF>Source:</color> Dig rock veins",

                MaterialType.Gravel => "Crushed rock, needs grinding.\n\n" +
                    "<color=#FFD700>Interactions:</color>\n" +
                    "  \u2192 Grinder \u2192 Sand\n\n" +
                    "<color=#00BFFF>Source:</color> Crusher (from Rock/Ore)",

                MaterialType.Ore => "Gold-bearing ore! Very valuable.\n\n" +
                    "<color=#FFD700>Interactions:</color>\n" +
                    "  \u2192 Crusher \u2192 Gravel (40%)\n" +
                    "              + Concentrate (60%)\n\n" +
                    "<color=#00BFFF>Source:</color> Dig ore veins (deeper = more)",

                MaterialType.Concentrate => "Refined heavy minerals.\n\n" +
                    "<color=#FFD700>Interactions:</color>\n" +
                    "  \u2192 Grinder \u2192 Sand\n" +
                    "  + Coal \u2192 Smelter \u2192 Gold!\n\n" +
                    "<color=#00BFFF>Source:</color> Crusher (Ore), Shaker",

                MaterialType.Coal => "Fuel for smelting.\n\n" +
                    "<color=#FFD700>Interactions:</color>\n" +
                    "  + Concentrate \u2192 Smelter \u2192 Gold!\n\n" +
                    "<color=#00BFFF>Source:</color> Dig coal veins",

                MaterialType.Terrain => "Solid ground. Dig to extract materials.\n\n" +
                    "<color=#FFD700>Contains veins of:</color>\n" +
                    "  - Rock (15%)\n" +
                    "  - Ore (8%, more at depth)\n" +
                    "  - Coal (10%)\n" +
                    "  - Sand (default)",

                _ => "Unknown material."
            };
        }

        private void PositionTooltip()
        {
            // Position tooltip to the right of mouse cursor
            Vector2 mousePos = Input.mousePosition;
            float offsetX = 25f;  // Offset to the right of cursor

            // Get screen bounds
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            Vector2 size = tooltipRect.sizeDelta;

            // Position to the right of cursor, shifted down from cursor
            float x = mousePos.x + offsetX;
            float y = mousePos.y - size.y / 2f - 16f;  // Shifted down 16 pixels

            // If tooltip would go off right edge, flip to left side
            if (x + size.x > screenWidth - 10)
            {
                x = mousePos.x - size.x - offsetX;
            }

            // Keep within vertical bounds
            y = Mathf.Clamp(y, 10, screenHeight - size.y - 10);

            tooltipRect.position = new Vector2(x, y);
        }

        public bool IsInspecting => isInspecting;
    }
}
