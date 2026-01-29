using UnityEngine.EventSystems;

namespace FallingSand
{
    public static class GameInput
    {
        public static bool IsPointerOverUI()
        {
            return EventSystem.current != null
                && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
