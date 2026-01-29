using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace FallingSand
{
    public class InventoryMenu : MonoBehaviour
    {
        private bool isOpen;
        private Hotbar hotbar;
        private PlayerController playerController;
        private ProgressionManager progressionManager;
        private Canvas rootCanvas;

        // UI elements
        private GameObject menuPanel;
        private Text tooltipName;
        private Text tooltipDesc;
        private List<InventoryItemSlot> itemSlots = new List<InventoryItemSlot>();

        // Drag state
        private ItemDefinition draggingItem;

        public bool IsOpen => isOpen;

        public void Initialize(Hotbar hotbarRef, PlayerController player, ProgressionManager progression, Canvas canvas)
        {
            hotbar = hotbarRef;
            playerController = player;
            progressionManager = progression;
            rootCanvas = canvas;

            BuildUI();
            menuPanel.SetActive(false);
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.tabKey.wasPressedThisFrame ||
                Keyboard.current.iKey.wasPressedThisFrame)
            {
                ToggleMenu();
            }

            if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseMenu();
            }

            if (isOpen)
            {
                RefreshItemAvailability();
            }
        }

        private void ToggleMenu()
        {
            if (isOpen)
                CloseMenu();
            else
                OpenMenu();
        }

        private void OpenMenu()
        {
            isOpen = true;
            menuPanel.SetActive(true);
            RefreshItemAvailability();
        }

        private void CloseMenu()
        {
            isOpen = false;
            menuPanel.SetActive(false);
            draggingItem = null;
        }

        public void OnDragStarted(ItemDefinition item)
        {
            draggingItem = item;
        }

        public void OnDragEnded(Vector2 screenPosition)
        {
            if (draggingItem == null) return;

            // Check if dropped on a hotbar slot
            for (int i = 0; i < hotbar.GetSlotCount(); i++)
            {
                Rect slotRect = hotbar.GetSlotScreenRect(i);
                if (slotRect.Contains(screenPosition))
                {
                    hotbar.AssignSlot(i, draggingItem);
                    break;
                }
            }

            draggingItem = null;
        }

        public void ShowTooltip(ItemDefinition item)
        {
            if (tooltipName != null)
                tooltipName.text = item.DisplayName;
            if (tooltipDesc != null)
            {
                string status = "";
                if (!IsItemAvailable(item))
                {
                    if (item.Category == ItemCategory.Tool)
                        status = " (not collected)";
                    else
                        status = " (locked)";
                }
                tooltipDesc.text = item.Description + status;
            }
        }

        public void HideTooltip()
        {
            if (tooltipName != null)
                tooltipName.text = "";
            if (tooltipDesc != null)
                tooltipDesc.text = "Drag items onto hotbar to assign slots";
        }

        private bool IsItemAvailable(ItemDefinition item)
        {
            return hotbar.IsItemAvailable(item);
        }

        private void RefreshItemAvailability()
        {
            foreach (var slot in itemSlots)
            {
                bool available = IsItemAvailable(slot.Item);
                slot.SetAvailable(available);
                var img = slot.GetComponentInChildren<Image>();
                if (img != null)
                {
                    img.color = available ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.5f);
                }
            }
        }

        private void BuildUI()
        {
            // Overlay background (semi-transparent)
            menuPanel = new GameObject("InventoryPanel");
            menuPanel.transform.SetParent(transform, false);
            var panelRect = menuPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var overlay = menuPanel.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.6f);

            // Content panel (centered)
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(menuPanel.transform, false);
            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(500, 450);

            var contentBg = contentObj.AddComponent<Image>();
            contentBg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);

            // Title
            CreateText(contentObj.transform, "Title", "INVENTORY",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -10), new Vector2(300, 35), 22, FontStyle.Bold, TextAnchor.MiddleCenter);

            // Close button hint
            CreateText(contentObj.transform, "CloseHint", "[Tab / Esc]",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-10, -10), new Vector2(120, 30), 13, FontStyle.Normal, TextAnchor.MiddleRight,
                new Color(0.5f, 0.5f, 0.5f));

            // Tools section
            float yOffset = -55f;
            CreateText(contentObj.transform, "ToolsHeader", "TOOLS",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20, yOffset), new Vector2(200, 25), 16, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Color(0.7f, 0.8f, 1f));

            yOffset -= 30f;
            float xStart = 30f;
            foreach (var item in ItemRegistry.Tools)
            {
                CreateItemSlot(contentObj.transform, item, xStart, yOffset);
                xStart += 90f;
            }

            // Structures section
            yOffset -= 110f;
            CreateText(contentObj.transform, "StructuresHeader", "STRUCTURES",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20, yOffset), new Vector2(200, 25), 16, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Color(0.7f, 0.8f, 1f));

            yOffset -= 30f;
            xStart = 30f;
            foreach (var item in ItemRegistry.Structures)
            {
                CreateItemSlot(contentObj.transform, item, xStart, yOffset);
                xStart += 90f;
            }

            // Tooltip area at bottom
            yOffset = 60f; // From bottom
            tooltipName = CreateText(contentObj.transform, "TooltipName", "",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, yOffset), new Vector2(-40, 25), 16, FontStyle.Bold, TextAnchor.MiddleCenter);

            tooltipDesc = CreateText(contentObj.transform, "TooltipDesc", "Drag items onto hotbar to assign slots",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, yOffset - 25f), new Vector2(-40, 22), 13, FontStyle.Normal, TextAnchor.MiddleCenter,
                new Color(0.6f, 0.6f, 0.6f));
        }

        private void CreateItemSlot(Transform parent, ItemDefinition item, float x, float y)
        {
            const float cellSize = 72f;

            GameObject slotObj = new GameObject($"Item_{item.Id}");
            slotObj.transform.SetParent(parent, false);
            var slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0f, 1f);
            slotRect.anchorMax = new Vector2(0f, 1f);
            slotRect.pivot = new Vector2(0f, 1f);
            slotRect.anchoredPosition = new Vector2(x, y);
            slotRect.sizeDelta = new Vector2(cellSize, cellSize + 20f);

            // Background
            GameObject bgObj = new GameObject("Bg");
            bgObj.transform.SetParent(slotObj.transform, false);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 1f);
            bgRect.anchorMax = new Vector2(0.5f, 1f);
            bgRect.pivot = new Vector2(0.5f, 1f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(cellSize, cellSize);

            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);

            // Icon (placeholder colored square)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(bgObj.transform, false);
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(10, 10);
            iconRect.offsetMax = new Vector2(-10, -10);

            var iconImg = iconObj.AddComponent<Image>();
            bool available = IsItemAvailable(item);
            iconImg.color = available ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.5f);

            // Name below
            var nameText = CreateText(slotObj.transform, "Name", item.DisplayName,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -cellSize - 2), new Vector2(cellSize + 10, 18), 11, FontStyle.Normal, TextAnchor.MiddleCenter,
                available ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.4f, 0.4f, 0.4f));

            // InventoryItemSlot component for drag/hover
            var slot = slotObj.AddComponent<InventoryItemSlot>();
            slot.Setup(item, available, this, rootCanvas);
            slot.SetIcon(iconImg);
            slot.SetNameText(nameText);
            itemSlots.Add(slot);
        }

        private Text CreateText(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 position, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment,
            Color? color = null)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.fontStyle = fontStyle;
            txt.alignment = alignment;
            txt.color = color ?? Color.white;
            return txt;
        }
    }
}
