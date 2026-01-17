using UnityEngine;

namespace FallingSand
{
    public class CellRenderer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Shader worldShader;

        private CellWorld world;
        private Texture2D cellTexture;
        private Texture2D paletteTexture;
        private Texture2D variationTexture;
        private Texture2D emissionTexture;
        private Texture2D densityTexture;
        private Material renderMaterial;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;

        // Buffer for texture upload
        private Color32[] textureBuffer;
        private Color32[] densityBuffer;

        public void Initialize(CellWorld world)
        {
            Debug.Log("[CellRenderer] Initialize() called");
            this.world = world;

            CreateTextures();
            CreateMaterial();
            CreateQuad();
            BuildPalette();
            UploadFullTexture();

            Debug.Log("[CellRenderer] Initialize() complete");
        }

        private void CreateTextures()
        {
            Debug.Log($"[CellRenderer] Creating textures for {world.width}x{world.height} world...");

            // Cell texture - R8 format stores material IDs
            // Using RGBA32 for compatibility, only red channel is used
            cellTexture = new Texture2D(
                world.width,
                world.height,
                TextureFormat.RGBA32,
                mipChain: false,
                linear: true
            );
            cellTexture.filterMode = FilterMode.Point;  // Crisp pixels
            cellTexture.wrapMode = TextureWrapMode.Clamp;
            Debug.Log($"[CellRenderer] Cell texture created: {cellTexture.width}x{cellTexture.height}");

            // Palette texture - 256 colours
            paletteTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false, false);
            paletteTexture.filterMode = FilterMode.Point;
            paletteTexture.wrapMode = TextureWrapMode.Clamp;
            Debug.Log("[CellRenderer] Palette texture created");

            // Allocate upload buffer
            textureBuffer = new Color32[world.width * world.height];
            Debug.Log($"[CellRenderer] Upload buffer allocated: {textureBuffer.Length} pixels");
        }

        private void CreateMaterial()
        {
            Debug.Log("[CellRenderer] Creating material...");

            if (worldShader == null)
            {
                Debug.Log("[CellRenderer] Shader not assigned, searching for 'FallingSand/WorldRender'...");
                worldShader = Shader.Find("FallingSand/WorldRender");
            }

            if (worldShader == null)
            {
                Debug.LogError("[CellRenderer] WorldRender shader not found! Trying fallback...");
                worldShader = Shader.Find("Unlit/Color");
                if (worldShader == null)
                {
                    Debug.LogError("[CellRenderer] No fallback shader found either!");
                    return;
                }
                Debug.LogWarning("[CellRenderer] Using fallback Unlit/Color shader");
            }
            else
            {
                Debug.Log($"[CellRenderer] Shader found: {worldShader.name}");
            }

            renderMaterial = new Material(worldShader);
            renderMaterial.SetTexture("_CellTex", cellTexture);
            renderMaterial.SetTexture("_PaletteTex", paletteTexture);
            Debug.Log($"[CellRenderer] Material created: {renderMaterial.name}");
        }

        private void CreateQuad()
        {
            Debug.Log("[CellRenderer] Creating quad mesh...");

            // Add mesh components
            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

            // Create a simple quad mesh
            Mesh mesh = new Mesh();
            mesh.name = "CellWorldQuad";

            // Each cell renders as 2×2 pixels
            const int PixelsPerCell = 2;
            int pixelWidth = world.width * PixelsPerCell;   // 1024 * 2 = 2048 pixels
            int pixelHeight = world.height * PixelsPerCell; // 512 * 2 = 1024 pixels
            float halfWidth = pixelWidth / 2f;   // 1024
            float halfHeight = pixelHeight / 2f; // 512

            Debug.Log($"[CellRenderer] Quad size: {pixelWidth} x {pixelHeight} pixels (cells: {world.width} x {world.height})");

            mesh.vertices = new Vector3[]
            {
                new Vector3(-halfWidth, -halfHeight, 0),
                new Vector3(halfWidth, -halfHeight, 0),
                new Vector3(halfWidth, halfHeight, 0),
                new Vector3(-halfWidth, halfHeight, 0),
            };

            // UVs - flip V to match Y=0 at top coordinate system
            mesh.uv = new Vector2[]
            {
                new Vector2(0, 1),  // Bottom-left of quad = top of texture
                new Vector2(1, 1),  // Bottom-right of quad = top-right of texture
                new Vector2(1, 0),  // Top-right of quad = bottom-right of texture
                new Vector2(0, 0),  // Top-left of quad = bottom-left of texture
            };

            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
            meshRenderer.material = renderMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            Debug.Log($"[CellRenderer] Quad created. Bounds: {mesh.bounds}, Renderer enabled: {meshRenderer.enabled}");
            Debug.Log($"[CellRenderer] Material assigned: {meshRenderer.material?.name ?? "NULL"}, Shader: {meshRenderer.material?.shader?.name ?? "NULL"}");
        }

        private void BuildPalette()
        {
            Debug.Log("[CellRenderer] Building palette...");
            Color32[] colours = new Color32[256];

            for (int i = 0; i < world.materials.Length && i < 256; i++)
            {
                colours[i] = world.materials[i].baseColour;
            }

            // Log first few colors
            Debug.Log($"[CellRenderer] Palette colors - Air[0]: {colours[0]}, Stone[1]: {colours[1]}, Sand[2]: {colours[2]}, Water[3]: {colours[3]}");

            paletteTexture.SetPixels32(colours);
            paletteTexture.Apply();
            Debug.Log("[CellRenderer] Palette built and applied");
        }

        private int uploadCount = 0;

        public void UploadFullTexture()
        {
            // Convert cell material IDs to colours
            // Material ID goes into red channel as 0-255 value
            int nonAirCount = 0;
            for (int i = 0; i < world.cells.Length; i++)
            {
                byte materialId = world.cells[i].materialId;
                if (materialId != Materials.Air) nonAirCount++;
                // Store materialId in red channel, scaled to be read as 0-1 in shader
                textureBuffer[i] = new Color32(materialId, 0, 0, 255);
            }

            cellTexture.SetPixels32(textureBuffer);
            cellTexture.Apply(updateMipmaps: false);

            uploadCount++;
            if (uploadCount <= 3 || uploadCount % 60 == 0)
            {
                Debug.Log($"[CellRenderer] Texture upload #{uploadCount}, non-air cells: {nonAirCount}");
            }
        }

        private void OnDestroy()
        {
            if (cellTexture != null)
            {
                Destroy(cellTexture);
            }
            if (paletteTexture != null)
            {
                Destroy(paletteTexture);
            }
            if (renderMaterial != null)
            {
                Destroy(renderMaterial);
            }
        }
    }
}
