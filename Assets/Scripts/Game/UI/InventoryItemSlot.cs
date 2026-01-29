using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace FallingSand
{
    public class InventoryItemSlot : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        private ItemDefinition item;
        private bool isAvailable;
        private Image iconImage;
        private Text nameText;
        private InventoryMenu menu;

        // Drag state
        private GameObject dragVisual;
        private Canvas rootCanvas;

        public void Setup(ItemDefinition itemDef, bool available, InventoryMenu parentMenu, Canvas canvas)
        {
            item = itemDef;
            isAvailable = available;
            menu = parentMenu;
            rootCanvas = canvas;
        }

        public void SetAvailable(bool available) => isAvailable = available;
        public void SetIcon(Image icon) => iconImage = icon;
        public void SetNameText(Text text) => nameText = text;
        public ItemDefinition Item => item;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isAvailable || item == null) return;

            dragVisual = new GameObject("DragVisual");
            dragVisual.transform.SetParent(rootCanvas.transform, false);
            var img = dragVisual.AddComponent<Image>();
            if (iconImage != null && iconImage.sprite != null)
                img.sprite = iconImage.sprite;
            img.color = new Color(1f, 1f, 1f, 0.7f);
            img.raycastTarget = false;

            var rt = dragVisual.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(48, 48);

            menu.OnDragStarted(item);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragVisual == null) return;
            dragVisual.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragVisual != null)
            {
                Object.Destroy(dragVisual);
                dragVisual = null;
            }

            menu.OnDragEnded(eventData.position);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (item != null)
                menu.ShowTooltip(item);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            menu.HideTooltip();
        }
    }
}
