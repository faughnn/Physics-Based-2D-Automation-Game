namespace FallingSand.Debugging
{
    /// <summary>
    /// Interface for debug overlay sections.
    /// Each section is responsible for displaying a group of related debug information.
    /// </summary>
    public interface IDebugSection
    {
        /// <summary>
        /// Display name for this section.
        /// </summary>
        string SectionName { get; }

        /// <summary>
        /// Ordering priority. Lower values appear higher on screen.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Toggle for GUI text visibility.
        /// </summary>
        bool IsGUIEnabled { get; set; }

        /// <summary>
        /// Toggle for gizmo visibility.
        /// </summary>
        bool AreGizmosEnabled { get; set; }

        /// <summary>
        /// Called each frame to update cached values.
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// Draw the GUI for this section.
        /// </summary>
        /// <param name="labelStyle">GUIStyle to use for labels.</param>
        /// <param name="x">X position for labels.</param>
        /// <param name="y">Starting Y position.</param>
        /// <param name="lineHeight">Height per line.</param>
        /// <returns>Number of lines drawn.</returns>
        int DrawGUI(UnityEngine.GUIStyle labelStyle, float x, float y, float lineHeight);

        /// <summary>
        /// Draw gizmos for this section. Called from OnDrawGizmos.
        /// </summary>
        void DrawGizmos();
    }
}
