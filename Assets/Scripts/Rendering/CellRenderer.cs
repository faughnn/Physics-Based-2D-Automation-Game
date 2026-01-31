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

        // Buffer for texture upload (full world)
        private Color32[] textureBuffer;
        private Color32[] densityBuffer;

        // Chunk upload buffers (64x64 = 4096 cells)
        private Color32[] chunkBuffer;
        private Color32[] chunkDensityBuffer;
        private const int ChunkBufferSize = CellWorld.ChunkSize * CellWorld.ChunkSize;

        // Track if we need a full upload (first frame, terrain load, etc.)
        private bool needsFullUpload = true;

        public void Initialize(CellWorld world)
        {
            this.world = world;

            CreateTextures();
            CreateMaterial();
            CreateQuad();
            BuildPalette();
            UploadFullTexture();
        }

        private void CreateTextures()
        {
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

            // Density texture - per-cell density for lighting calculations
            densityTexture = new Texture2D(
                world.width,
                world.height,
                TextureFormat.RGBA32,
                mipChain: false,
                linear: true
            );
            densityTexture.filterMode = FilterMode.Point;
            densityTexture.wrapMode = TextureWrapMode.Clamp;

            // Palette texture - 256 colours
            paletteTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false, false);
            paletteTexture.filterMode = FilterMode.Point;
            paletteTexture.wrapMode = TextureWrapMode.Clamp;

            // Variation texture - per-material colour variation amount (256x1 lookup)
            variationTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false, true);
            variationTexture.filterMode = FilterMode.Point;
            variationTexture.wrapMode = TextureWrapMode.Clamp;

            // Emission texture - per-material glow intensity (256x1 lookup)
            emissionTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false, true);
            emissionTexture.filterMode = FilterMode.Point;
            emissionTexture.wrapMode = TextureWrapMode.Clamp;

            // Allocate upload buffers
            textureBuffer = new Color32[world.width * world.height];
            densityBuffer = new Color32[world.width * world.height];

            // Allocate chunk buffers (for partial uploads)
            chunkBuffer = new Color32[ChunkBufferSize];
            chunkDensityBuffer = new Color32[ChunkBufferSize];
        }

        private void CreateMaterial()
        {
            if (worldShader == null)
            {
                worldShader = Resources.Load<Shader>("Shaders/WorldRender");
            }

            if (worldShader == null)
            {
                Debug.LogError("[CellRenderer] WorldRender shader not found! Trying fallback...");
                worldShader = Shader.Find("Sprites/Default");
                if (worldShader == null)
                {
                    Debug.LogError("[CellRenderer] No fallback shader found either!");
                    return;
                }
                Debug.LogWarning("[CellRenderer] Using fallback Sprites/Default shader");
            }

            renderMaterial = new Material(worldShader);
            renderMaterial.SetTexture("_CellTex", cellTexture);
            renderMaterial.SetTexture("_PaletteTex", paletteTexture);
            renderMaterial.SetTexture("_DensityTex", densityTexture);
            renderMaterial.SetTexture("_VariationTex", variationTexture);
            renderMaterial.SetTexture("_EmissionTex", emissionTexture);
        }

        private void CreateQuad()
        {
            // Add mesh components
            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

            // Create a simple quad mesh
            Mesh mesh = new Mesh();
            mesh.name = "CellWorldQuad";

            // Each cell renders as PixelsPerCell×PixelsPerCell pixels
            int pixelWidth = world.width * CoordinateUtils.PixelsPerCell;   // 1024 * 2 = 2048 pixels
            int pixelHeight = world.height * CoordinateUtils.PixelsPerCell; // 512 * 2 = 1024 pixels
            float halfWidth = pixelWidth / 2f;   // 1024
            float halfHeight = pixelHeight / 2f; // 512

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
        }

        private void BuildPalette()
        {
            Color32[] colours = new Color32[256];
            Color32[] variations = new Color32[256];
            Color32[] emissions = new Color32[256];

            for (int i = 0; i < world.materials.Length && i < 256; i++)
            {
                colours[i] = world.materials[i].baseColour;
                variations[i] = new Color32(world.materials[i].colourVariation, 0, 0, 255);
                emissions[i] = new Color32(world.materials[i].emission, 0, 0, 255);
            }

            paletteTexture.SetPixels32(colours);
            paletteTexture.Apply();

            variationTexture.SetPixels32(variations);
            variationTexture.Apply();

            emissionTexture.SetPixels32(emissions);
            emissionTexture.Apply();
        }

        public void UploadFullTexture()
        {
            // Convert cell material IDs to colours
            // Material ID goes into red channel as 0-255 value
            for (int i = 0; i < world.cells.Length; i++)
            {
                byte materialId = world.cells[i].materialId;
                // Store materialId in red channel, scaled to be read as 0-1 in shader
                textureBuffer[i] = new Color32(materialId, 0, 0, 255);
                // Density: 0 for air, 255 for solid materials (used for lighting normals)
                byte density = materialId == Materials.Air ? (byte)0 : (byte)255;
                densityBuffer[i] = new Color32(density, 0, 0, 255);
            }

            cellTexture.SetPixels32(textureBuffer);
            cellTexture.Apply(updateMipmaps: false);

            densityTexture.SetPixels32(densityBuffer);
            densityTexture.Apply(updateMipmaps: false);
        }

        /// <summary>
        /// Upload only chunks that changed since last frame.
        /// Uses activeLastFrame and IsDirty flags to determine which chunks need upload.
        /// </summary>
        public void UploadDirtyChunks()
        {
            if (needsFullUpload)
            {
                UploadFullTexture();
                needsFullUpload = false;
                return;
            }

            bool anyUploaded = false;

            for (int chunkIndex = 0; chunkIndex < world.chunks.Length; chunkIndex++)
            {
                ChunkState chunk = world.chunks[chunkIndex];

                // Upload if chunk was active during simulation OR was dirtied by belts after simulation
                bool needsRender = chunk.activeLastFrame != 0
                                || (chunk.flags & ChunkFlags.IsDirty) != 0;

                if (!needsRender)
                    continue;

                int chunkX = chunkIndex % world.chunksX;
                int chunkY = chunkIndex / world.chunksX;

                UploadChunk(chunkX, chunkY);
                anyUploaded = true;
            }

            if (anyUploaded)
            {
                cellTexture.Apply(updateMipmaps: false);
                densityTexture.Apply(updateMipmaps: false);
            }
        }

        /// <summary>
        /// Upload a single chunk region to the textures.
        /// </summary>
        private void UploadChunk(int chunkX, int chunkY)
        {
            int startX = chunkX * CellWorld.ChunkSize;
            int startY = chunkY * CellWorld.ChunkSize;

            // Clamp to world bounds (edge chunks may be partial)
            int endX = Mathf.Min(startX + CellWorld.ChunkSize, world.width);
            int endY = Mathf.Min(startY + CellWorld.ChunkSize, world.height);
            int chunkWidth = endX - startX;
            int chunkHeight = endY - startY;

            // Fill chunk buffer
            int bufferIdx = 0;
            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    int cellIndex = y * world.width + x;
                    byte materialId = world.cells[cellIndex].materialId;

                    chunkBuffer[bufferIdx] = new Color32(materialId, 0, 0, 255);
                    byte density = materialId == Materials.Air ? (byte)0 : (byte)255;
                    chunkDensityBuffer[bufferIdx] = new Color32(density, 0, 0, 255);
                    bufferIdx++;
                }
            }

            // Upload chunk region to textures
            cellTexture.SetPixels32(startX, startY, chunkWidth, chunkHeight, chunkBuffer);
            densityTexture.SetPixels32(startX, startY, chunkWidth, chunkHeight, chunkDensityBuffer);
        }

        /// <summary>
        /// Force a full texture upload on the next render.
        /// Call this after bulk terrain changes, level loading, etc.
        /// </summary>
        public void ForceFullUpload()
        {
            needsFullUpload = true;
        }

        private void OnDestroy()
        {
            if (cellTexture != null)
                Destroy(cellTexture);
            if (paletteTexture != null)
                Destroy(paletteTexture);
            if (densityTexture != null)
                Destroy(densityTexture);
            if (variationTexture != null)
                Destroy(variationTexture);
            if (emissionTexture != null)
                Destroy(emissionTexture);
            if (renderMaterial != null)
                Destroy(renderMaterial);
        }
    }
}
