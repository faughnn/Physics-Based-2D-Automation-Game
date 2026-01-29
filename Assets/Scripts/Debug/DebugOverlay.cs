using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FallingSand.Debugging
{
    /// <summary>
    /// Core debug overlay manager. Handles rendering all debug sections.
    /// Toggle: F3 = toggle overlay, F4 = toggle gizmos
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        public static DebugOverlay Instance { get; private set; }

        private readonly List<IDebugSection> sections = new List<IDebugSection>();
        private bool overlayEnabled = true;
        private bool gizmosEnabled = true;

        private Keyboard keyboard;

        private GUIStyle boxStyle;
        private GUIStyle labelStyle;
        private GUIStyle headerStyle;
        private Texture2D backgroundTexture;

        private const float Padding = 10f;
        private const float LineHeight = 18f;
        private const float Width = 280f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            keyboard = Keyboard.current;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            if (backgroundTexture != null)
            {
                Destroy(backgroundTexture);
            }
        }

        /// <summary>
        /// Register a debug section. Sections are sorted by priority.
        /// </summary>
        public void RegisterSection(IDebugSection section)
        {
            sections.Add(section);
            sections.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        /// <summary>
        /// Unregister a debug section.
        /// </summary>
        public void UnregisterSection(IDebugSection section)
        {
            sections.Remove(section);
        }

        private void Update()
        {
            if (keyboard == null) keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Toggle overlay with F3
            if (keyboard.f3Key.wasPressedThisFrame)
            {
                overlayEnabled = !overlayEnabled;
            }

            // Toggle gizmos with F4
            if (keyboard.f4Key.wasPressedThisFrame)
            {
                gizmosEnabled = !gizmosEnabled;
                foreach (var section in sections)
                {
                    section.AreGizmosEnabled = gizmosEnabled;
                }
            }

            // Toggle profiling with F5
            if (keyboard.f5Key.wasPressedThisFrame)
            {
                FallingSand.PerformanceProfiler.Enabled = !FallingSand.PerformanceProfiler.Enabled;
            }

            // Update all sections
            float deltaTime = Time.unscaledDeltaTime;
            foreach (var section in sections)
            {
                section.Update(deltaTime);
            }
        }

        private void OnGUI()
        {
            if (!overlayEnabled) return;

            InitializeStyles();

            // Calculate total height
            float totalLines = 2; // Header lines
            foreach (var section in sections)
            {
                if (section.IsGUIEnabled)
                {
                    totalLines += 1; // Section header
                    totalLines += CountSectionLines(section);
                }
            }

            float height = LineHeight * totalLines + Padding * 2;
            Rect boxRect = new Rect(Padding, Padding, Width, height);
            GUI.Box(boxRect, GUIContent.none, boxStyle);

            float y = Padding + 5;
            float x = Padding + 10;

            // Draw header
            GUI.color = Color.cyan;
            GUI.Label(new Rect(x, y, Width - 20, LineHeight), "DEBUG OVERLAY", headerStyle);
            y += LineHeight;

            GUI.color = Color.gray;
            string toggleState = $"F3={OnOffText(overlayEnabled)} | F4=Gizmos | F5=Profile {OnOffText(FallingSand.PerformanceProfiler.Enabled)}";
            GUI.Label(new Rect(x, y, Width - 20, LineHeight), toggleState, labelStyle);
            y += LineHeight;

            GUI.color = Color.white;

            // Draw each section
            foreach (var section in sections)
            {
                if (!section.IsGUIEnabled) continue;

                // Section header
                GUI.color = Color.yellow;
                GUI.Label(new Rect(x, y, Width - 20, LineHeight), $"--- {section.SectionName} ---", labelStyle);
                y += LineHeight;

                GUI.color = Color.white;

                // Section content
                int linesDrawn = section.DrawGUI(labelStyle, x, y, LineHeight);
                y += linesDrawn * LineHeight;
            }
        }

        private void OnDrawGizmos()
        {
            if (!gizmosEnabled) return;

            foreach (var section in sections)
            {
                if (section.AreGizmosEnabled)
                {
                    section.DrawGizmos();
                }
            }
        }

        private void InitializeStyles()
        {
            if (boxStyle != null) return;

            backgroundTexture = MakeTexture(2, 2, new Color(0, 0, 0, 0.75f));

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = backgroundTexture;

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 13;
            labelStyle.normal.textColor = Color.white;

            headerStyle = new GUIStyle(labelStyle);
            headerStyle.fontSize = 14;
            headerStyle.fontStyle = FontStyle.Bold;
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private string OnOffText(bool value) => value ? "ON" : "OFF";

        /// <summary>
        /// Estimate how many lines a section will draw.
        /// This is a rough estimate for layout; actual drawing happens in DrawGUI.
        /// </summary>
        private int CountSectionLines(IDebugSection section)
        {
            // Use a dummy call to count lines
            // Sections should be consistent in their line counts
            return section.DrawGUI(null, 0, 0, 0);
        }
    }
}
