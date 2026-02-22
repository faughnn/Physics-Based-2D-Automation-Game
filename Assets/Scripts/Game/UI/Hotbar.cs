using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace FallingSand
{
    public class Hotbar : MonoBehaviour
    {
        private const int SlotCount = 6;
        private const float SlotSize = 64f;
        private const float SlotPadding = 8f;
        private const float BottomMargin = 20f;
        private const float NameLabelHeight = 20f;

        private ItemDefinition[] slots = new ItemDefinition[SlotCount];
        private int selectedIndex = 0;

        private PlayerController playerController;
        private ProgressionManager progressionManager;

        // UI elements
        private RectTransform containerRect;
        private Image[] slotBackgrounds = new Image[SlotCount];
        private Image[] slotIcons = new Image[SlotCount];
        private Text[] slotNumbers = new Text[SlotCount];
        private Text[] slotNames = new Text[SlotCount];

        // Colors
        private static readonly Color SelectedColor = new Color(1f, 1f, 1f, 0.9f);
        private static readonly Color NormalColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
        private static readonly Color LockedColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);
        private static readonly Color IconNormalColor = Color.white;
        private static readonly Color IconLockedColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        private static readonly Color SelectedBorderColor = new Color(1f, 0.85f, 0.2f, 1f);

        // Icon textures (procedural)
        private Sprite grabberSprite;
        private Sprite shovelSprite;
        private Sprite beltSprite;
        private Sprite liftSprite;
        private Sprite wallSprite;
        private Sprite pistonSprite;

        public void Initialize(PlayerController player, ProgressionManager progression)
        {
            playerController = player;
            progressionManager = progression;

            playerController.OnToolEquipped += OnToolEquippedExternally;

            CreateIconSprites();
            BuildUI();

            // Default layout
            slots[0] = ItemRegistry.Grabber;
            slots[1] = ItemRegistry.Shovel;
            slots[2] = ItemRegistry.Belt;
            slots[3] = ItemRegistry.Lift;
            slots[4] = ItemRegistry.Wall;
            slots[5] = ItemRegistry.Piston;

            selectedIndex = 0;
            RefreshSlotVisuals();
            EquipSlot(0);
        }

        private void OnDestroy()
        {
            if (playerController != null)
                playerController.OnToolEquipped -= OnToolEquippedExternally;
        }

        private void OnToolEquippedExternally(ToolType tool)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] != null && slots[i].Category == ItemCategory.Tool && slots[i].ToolType == tool)
                {
                    selectedIndex = i;
                    return;
                }
            }
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            // Number keys 1-5
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectSlot(2);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectSlot(3);
            if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectSlot(4);
            if (Keyboard.current.digit6Key.wasPressedThisFrame) SelectSlot(5);

            // Scroll wheel cycling
            if (Mouse.current != null && !GameInput.IsPointerOverUI())
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (scroll > 0f) CycleSlot(-1);
                else if (scroll < 0f) CycleSlot(1);
            }

            RefreshSlotVisuals();
        }

        private void CycleSlot(int direction)
        {
            int next = selectedIndex;
            for (int i = 0; i < SlotCount; i++)
            {
                next = (next + direction + SlotCount) % SlotCount;
                if (slots[next] != null && IsItemAvailable(slots[next]))
                {
                    SelectSlot(next);
                    return;
                }
            }
        }

        private void SelectSlot(int index)
        {
            if (index < 0 || index >= SlotCount) return;
            if (slots[index] == null) return;
            if (!IsItemAvailable(slots[index])) return;
            selectedIndex = index;
            EquipSlot(index);
        }

        private void EquipSlot(int index)
        {
            var item = slots[index];
            if (item == null) return;

            if (item.Category == ItemCategory.Tool)
                playerController.EquipTool(item.ToolType);
            else
                playerController.EquipStructure(item.StructureType);
        }

        public bool IsItemAvailable(ItemDefinition item)
        {
            if (item == null) return false;

            if (item.Category == ItemCategory.Tool)
            {
                if (item.ToolType == ToolType.Grabber) return true;
                return playerController != null && playerController.HasTool(item.ToolType);
            }
            else
            {
                if (progressionManager == null) return false;
                return progressionManager.IsUnlocked(GetRequiredAbility(item.StructureType));
            }
        }

        private Ability GetRequiredAbility(StructureType structureType)
        {
            switch (structureType)
            {
                case StructureType.Belt: return Ability.PlaceBelts;
                case StructureType.Lift: return Ability.PlaceLifts;
                case StructureType.Wall: return Ability.PlaceLifts;
                case StructureType.Piston: return Ability.PlacePistons;
                default: return Ability.None;
            }
        }

        public void AssignSlot(int index, ItemDefinition item)
        {
            if (index < 0 || index >= SlotCount) return;
            slots[index] = item;
            RefreshSlotVisuals();
        }

        public int GetSlotCount() => SlotCount;

        public Rect GetSlotScreenRect(int index)
        {
            if (index < 0 || index >= SlotCount) return Rect.zero;

            // Convert the slot RectTransform to screen-space rect
            var rt = slotBackgrounds[index].rectTransform;
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            // corners: 0=bottom-left, 1=top-left, 2=top-right, 3=bottom-right (in screen space for overlay canvas)
            float x = corners[0].x;
            float y = corners[0].y;
            float w = corners[2].x - corners[0].x;
            float h = corners[2].y - corners[0].y;
            return new Rect(x, y, w, h);
        }

        private void BuildUI()
        {
            // Container anchored to bottom-center
            containerRect = gameObject.GetComponent<RectTransform>();
            if (containerRect == null)
                containerRect = gameObject.AddComponent<RectTransform>();

            float totalWidth = SlotCount * SlotSize + (SlotCount - 1) * SlotPadding;
            float totalHeight = SlotSize + NameLabelHeight;

            containerRect.anchorMin = new Vector2(0.5f, 0f);
            containerRect.anchorMax = new Vector2(0.5f, 0f);
            containerRect.pivot = new Vector2(0.5f, 0f);
            containerRect.anchoredPosition = new Vector2(0f, BottomMargin);
            containerRect.sizeDelta = new Vector2(totalWidth, totalHeight);

            for (int i = 0; i < SlotCount; i++)
            {
                CreateSlotUI(i);
            }
        }

        private void CreateSlotUI(int index)
        {
            float totalWidth = SlotCount * SlotSize + (SlotCount - 1) * SlotPadding;
            float xOffset = index * (SlotSize + SlotPadding) - totalWidth / 2f + SlotSize / 2f;

            // Slot background
            GameObject slotObj = new GameObject($"Slot_{index}");
            slotObj.transform.SetParent(transform, false);
            var slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 1f);
            slotRect.anchorMax = new Vector2(0.5f, 1f);
            slotRect.pivot = new Vector2(0.5f, 1f);
            slotRect.anchoredPosition = new Vector2(xOffset, 0f);
            slotRect.sizeDelta = new Vector2(SlotSize, SlotSize);

            slotBackgrounds[index] = slotObj.AddComponent<Image>();
            slotBackgrounds[index].color = NormalColor;

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(8, 8);
            iconRect.offsetMax = new Vector2(-8, -8);

            slotIcons[index] = iconObj.AddComponent<Image>();
            slotIcons[index].preserveAspect = true;

            // Number label (top-left corner)
            GameObject numObj = new GameObject("Number");
            numObj.transform.SetParent(slotObj.transform, false);
            var numRect = numObj.AddComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0f, 1f);
            numRect.anchorMax = new Vector2(0f, 1f);
            numRect.pivot = new Vector2(0f, 1f);
            numRect.anchoredPosition = new Vector2(4, -2);
            numRect.sizeDelta = new Vector2(20, 20);

            slotNumbers[index] = numObj.AddComponent<Text>();
            slotNumbers[index].text = (index + 1).ToString();
            slotNumbers[index].font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            slotNumbers[index].fontSize = 14;
            slotNumbers[index].color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
            slotNumbers[index].alignment = TextAnchor.UpperLeft;

            // Name label (below slot)
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(transform, false);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 1f);
            nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(xOffset, -SlotSize - 2f);
            nameRect.sizeDelta = new Vector2(SlotSize + SlotPadding, NameLabelHeight);

            slotNames[index] = nameObj.AddComponent<Text>();
            slotNames[index].font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            slotNames[index].fontSize = 11;
            slotNames[index].color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
            slotNames[index].alignment = TextAnchor.UpperCenter;
        }

        private void RefreshSlotVisuals()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                var item = slots[i];
                bool available = item != null && IsItemAvailable(item);
                bool selected = i == selectedIndex;

                // Background color
                if (selected && available)
                    slotBackgrounds[i].color = SelectedColor;
                else if (available)
                    slotBackgrounds[i].color = NormalColor;
                else
                    slotBackgrounds[i].color = LockedColor;

                // Icon
                if (item != null)
                {
                    slotIcons[i].sprite = GetIconSprite(item);
                    slotIcons[i].color = available ? IconNormalColor : IconLockedColor;
                    slotIcons[i].enabled = true;
                }
                else
                {
                    slotIcons[i].enabled = false;
                }

                // Name
                slotNames[i].text = item != null ? item.DisplayName : "";
                slotNames[i].color = available
                    ? new Color(0.8f, 0.8f, 0.8f, 0.9f)
                    : new Color(0.4f, 0.4f, 0.4f, 0.5f);
            }
        }

        private Sprite GetIconSprite(ItemDefinition item)
        {
            if (item == ItemRegistry.Grabber) return grabberSprite;
            if (item == ItemRegistry.Shovel) return shovelSprite;
            if (item == ItemRegistry.Belt) return beltSprite;
            if (item == ItemRegistry.Lift) return liftSprite;
            if (item == ItemRegistry.Wall) return wallSprite;
            if (item == ItemRegistry.Piston) return pistonSprite;
            return null;
        }

        private void CreateIconSprites()
        {
            grabberSprite = IconFactory.CreateGrabberIcon();
            shovelSprite = IconFactory.CreateShovelIcon();
            beltSprite = IconFactory.CreateBeltIcon();
            liftSprite = IconFactory.CreateLiftIcon();
            wallSprite = IconFactory.CreateWallIcon();
            pistonSprite = IconFactory.CreatePistonIcon();
        }
    }
}

