using UnityEngine;

namespace FallingSand.Debugging
{
    /// <summary>
    /// Abstract base class for debug sections with common functionality.
    /// </summary>
    public abstract class DebugSectionBase : IDebugSection
    {
        public abstract string SectionName { get; }
        public abstract int Priority { get; }

        public bool IsGUIEnabled { get; set; } = true;
        public bool AreGizmosEnabled { get; set; } = true;

        protected float updateInterval = 0.5f;
        protected float timeSinceLastUpdate;

        /// <summary>
        /// Update cached values. Override UpdateCachedValues for the actual update logic.
        /// </summary>
        public void Update(float deltaTime)
        {
            timeSinceLastUpdate += deltaTime;
            if (timeSinceLastUpdate >= updateInterval)
            {
                timeSinceLastUpdate = 0f;
                UpdateCachedValues();
            }
        }

        /// <summary>
        /// Override to update cached values periodically.
        /// </summary>
        protected virtual void UpdateCachedValues() { }

        /// <summary>
        /// Draw the GUI for this section.
        /// </summary>
        public abstract int DrawGUI(GUIStyle labelStyle, float x, float y, float lineHeight);

        /// <summary>
        /// Draw gizmos for this section. Default implementation does nothing.
        /// </summary>
        public virtual void DrawGizmos() { }

        /// <summary>
        /// Helper to draw a colored label.
        /// </summary>
        protected void DrawLabel(string text, float x, float y, float width, float height, GUIStyle style, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.Label(new Rect(x, y, width, height), text, style);
            GUI.color = oldColor;
        }

        /// <summary>
        /// Get performance color where lower values are better.
        /// Green &lt; threshold1, Yellow &lt; threshold2, Red &gt;= threshold2
        /// </summary>
        protected Color GetPerformanceColor(float value, float threshold1, float threshold2)
        {
            if (value < threshold1) return Color.green;
            if (value < threshold2) return Color.yellow;
            return Color.red;
        }

        /// <summary>
        /// Get performance color where higher values are better.
        /// Green &gt;= threshold1, Yellow &gt;= threshold2, Red &lt; threshold2
        /// </summary>
        protected Color GetPerformanceColorInverse(float value, float threshold1, float threshold2)
        {
            if (value >= threshold1) return Color.green;
            if (value >= threshold2) return Color.yellow;
            return Color.red;
        }
    }
}
