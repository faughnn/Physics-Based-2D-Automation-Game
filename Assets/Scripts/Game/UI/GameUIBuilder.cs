using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace FallingSand
{
    public static class GameUIBuilder
    {
        public static Canvas CreateCanvas()
        {
            GameObject canvasObj = new GameObject("GameCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Create EventSystem if none exists (use InputSystemUIInputModule for new Input System)
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventObj = new GameObject("EventSystem");
                eventObj.AddComponent<EventSystem>();
                eventObj.AddComponent<InputSystemUIInputModule>();
            }

            return canvas;
        }
    }
}
