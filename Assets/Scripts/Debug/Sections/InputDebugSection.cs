using UnityEngine;

namespace FallingSand.Debugging
{
    /// <summary>
    /// Debug section for input/control hints.
    /// Shows current material, brush info, and control hints.
    /// </summary>
    public class InputDebugSection : DebugSectionBase
    {
        public override string SectionName => "Input";
        public override int Priority => 40;

        private readonly SandboxController sandbox;

        public InputDebugSection(SandboxController sandbox)
        {
            this.sandbox = sandbox;
            // Input section doesn't need throttled updates
            updateInterval = 0f;
        }

        public override int DrawGUI(GUIStyle labelStyle, float x, float y, float lineHeight)
        {
            if (labelStyle == null) return 4;

            float width = 260f;
            int lines = 0;

            if (sandbox != null)
            {
                // Current material
                DrawLabel($"Material: {sandbox.CurrentMaterialName}", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.white);
                lines++;

                // Control hints
                DrawLabel("1-6: Material | LMB/RMB: Paint", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.gray);
                lines++;

                DrawLabel("7/8/9: Spawn Clusters | [/]: Size", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.gray);
                lines++;

                DrawLabel("Numpad +/-: Sim Speed | Scroll: Brush", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.gray);
                lines++;
            }
            else
            {
                DrawLabel("No sandbox reference", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.red);
                lines++;
            }

            return lines;
        }
    }
}
