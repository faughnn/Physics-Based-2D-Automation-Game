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
            if (labelStyle == null) return 5;

            float width = 280f;
            int lines = 0;

            if (sandbox != null)
            {
                // Mode display
                if (sandbox.BeltMode)
                {
                    string dir = sandbox.BeltDirection > 0 ? "RIGHT" : "LEFT";
                    DrawLabel($"[BELT MODE] Direction: {dir}", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.cyan);
                    lines++;

                    int tileCount = sandbox.BeltManager?.TileCount ?? 0;
                    int beltCount = sandbox.BeltManager?.BeltCount ?? 0;
                    DrawLabel($"Belts: {beltCount} | Tiles: {tileCount}", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.white);
                    lines++;
                }
                else if (sandbox.LiftMode)
                {
                    DrawLabel("[LIFT MODE] Hollow force zone", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.green);
                    lines++;

                    int liftCount = sandbox.LiftManager?.LiftCount ?? 0;
                    DrawLabel($"Lifts: {liftCount}", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.white);
                    lines++;
                }
                else if (sandbox.WallMode)
                {
                    DrawLabel("[WALL MODE] Solid 8x8 blocks", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.gray);
                    lines++;
                }
                else
                {
                    // Current material
                    DrawLabel($"Material: {sandbox.CurrentMaterialName}", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.white);
                    lines++;
                }

                // Control hints
                DrawLabel("B: Belt | L: Lift | W: Wall | Q/E: Dir", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.gray);
                lines++;

                DrawLabel("1-6: Material | LMB/RMB: Paint", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.gray);
                lines++;

                DrawLabel("7/8/9: Spawn Clusters | Scroll: Brush", x, y + lines * lineHeight, width, lineHeight, labelStyle, Color.gray);
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
