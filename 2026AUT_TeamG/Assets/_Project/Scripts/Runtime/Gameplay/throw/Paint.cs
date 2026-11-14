using UnityEngine;

namespace TeamG.Gameplay
{
    /// <summary>Static, single-material mesh prototype. A binary UV mask owns the painted region.</summary>
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(MeshCollider))]
    public class Paint : MonoBehaviour
    {
        [SerializeField, Range(32, 1024)] private int resolution = 512;
        [SerializeField] private Color paintColor = new Color(0.05f, 0.55f, 1f, 1f);
        [Tooltip("Optional. Imported textures must have Read/Write enabled. Empty uses the material's base texture.")]
        [SerializeField] private Texture2D baseTextureOverride = null;
        [SerializeField, Min(0.001f)] private float contactProbeDistance = 0.03f;

        private MeshRenderer targetRenderer;
        private MeshCollider meshCollider;
        private Material originalMaterial;
        private Material instanceMaterial;
        private Texture2D mask;
        private Texture2D displayTexture;
        private Color32[] maskPixels;
        private Color32[] basePixels;
        private Color32[] displayPixels;
        private int size;
        private bool dirty;
        // Primitive/imported UVs can have tiny floating-point overshoots at the 0/1 boundary.
        private const float UvTolerance = 0.0001f;

        public Texture2D Mask => mask;
        public int PaintedPixelCount { get; private set; }
        // Includes unused UV space. This is NOT a world-space shirt area / final match score.
        public float PaintedUvRatio => maskPixels == null ? 0f :
            (float)PaintedPixelCount / maskPixels.Length;

        private void Awake()
        {
            Initialize();
        }

        private bool Fail(string message)
        {
            Debug.LogError("Paint [" + name + "]: " + message, this);
            enabled = false;
            return false;
        }

        private bool Initialize()
        {
            targetRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
            Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
            if (mesh == null)
                return Fail("MeshFilter has no mesh. Attach Paint to the rendered mesh object, not an empty parent.");
            if (meshCollider.sharedMesh != mesh)
                return Fail("MeshFilter uses '" + mesh.name + "' but MeshCollider uses '" +
                    (meshCollider.sharedMesh != null ? meshCollider.sharedMesh.name : "None") +
                    "'. Assign the same mesh to both components.");
            if (!mesh.isReadable)
                return Fail("Enable Read/Write on the imported mesh, then Apply.");
            Vector2[] uvs = mesh.uv;
            if (uvs.Length != mesh.vertexCount || mesh.subMeshCount != 1)
                return Fail("Use a single-submesh mesh with UV0 coordinates for every vertex.");
            Vector2 uvMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 uvMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (Vector2 uv in uvs)
            {
                if (float.IsNaN(uv.x) || float.IsNaN(uv.y) ||
                    float.IsInfinity(uv.x) || float.IsInfinity(uv.y))
                    return Fail("Mesh '" + mesh.name + "' contains invalid UV0 coordinates.");
                uvMin = Vector2.Min(uvMin, uv);
                uvMax = Vector2.Max(uvMax, uv);
            }
            if (uvMin.x < -UvTolerance || uvMin.y < -UvTolerance ||
                uvMax.x > 1f + UvTolerance || uvMax.y > 1f + UvTolerance)
                return Fail("Mesh '" + mesh.name + "' UV0 is outside 0..1: U=" +
                    uvMin.x.ToString("G9") + ".." + uvMax.x.ToString("G9") + ", V=" +
                    uvMin.y.ToString("G9") + ".." + uvMax.y.ToString("G9") +
                    ". Unwrap UV0 into 0..1. Repeating UVs are not supported by this paint prototype.");
            if (meshCollider.convex || meshCollider.isTrigger)
                return Fail("MeshCollider must have Convex OFF and Is Trigger OFF.");
            if (meshCollider.attachedRigidbody != null && !meshCollider.attachedRigidbody.isKinematic)
                return Fail("Use a stationary shirt with no dynamic Rigidbody.");
            if (targetRenderer.sharedMaterials.Length != 1 || targetRenderer.sharedMaterial == null)
                return Fail("Assign one material to the shirt.");

            originalMaterial = targetRenderer.sharedMaterial;
            string mapProperty = originalMaterial.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            if (!originalMaterial.HasProperty(mapProperty))
                return Fail("Use URP Lit/Unlit or Built-in Standard with a base texture property.");
            if (originalMaterial.GetTextureScale(mapProperty) != Vector2.one ||
                originalMaterial.GetTextureOffset(mapProperty) != Vector2.zero)
                return Fail("Set base texture Tiling to (1,1) and Offset to (0,0).");

            Texture source = baseTextureOverride != null ? baseTextureOverride : originalMaterial.GetTexture(mapProperty);
            Texture2D sourceTexture = source as Texture2D;
            if (source != null && sourceTexture == null)
                return Fail("The base texture must be a Texture2D.");
            if (sourceTexture != null && !sourceTexture.isReadable)
                return Fail("Enable Read/Write on the base texture, then Apply.");

            string colorProperty = originalMaterial.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            Color tint = originalMaterial.HasProperty(colorProperty) ? originalMaterial.GetColor(colorProperty) : Color.white;
            size = Mathf.Clamp(resolution, 32, 1024);
            basePixels = new Color32[size * size];
            displayPixels = new Color32[basePixels.Length];
            maskPixels = new Color32[basePixels.Length];
            Color32[] sourcePixels = sourceTexture != null ? sourceTexture.GetPixels32() : null;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Color color = Color.white;
                if (sourceTexture != null)
                {
                    int sx = Mathf.Min(sourceTexture.width - 1, x * sourceTexture.width / size);
                    int sy = Mathf.Min(sourceTexture.height - 1, y * sourceTexture.height / size);
                    color = sourcePixels[sy * sourceTexture.width + sx];
                }
                int index = y * size + x;
                basePixels[index] = color * tint;
                displayPixels[index] = basePixels[index];
                maskPixels[index] = new Color32(0, 0, 0, 255);
            }

            // These new textures remain CPU-readable: never Apply(..., true).
            mask = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            mask.name = name + "_paint_mask_runtime";
            mask.wrapMode = TextureWrapMode.Clamp;
            mask.filterMode = FilterMode.Point;
            displayTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            displayTexture.name = name + "_paint_display_runtime";
            displayTexture.wrapMode = TextureWrapMode.Clamp;
            displayTexture.filterMode = FilterMode.Bilinear;

            // Do not alter a shared material or the source texture asset.
            instanceMaterial = new Material(originalMaterial);
            instanceMaterial.name = originalMaterial.name + " (Paint Runtime)";
            instanceMaterial.SetTexture(mapProperty, displayTexture);
            if (instanceMaterial.HasProperty(colorProperty)) instanceMaterial.SetColor(colorProperty, Color.white);
            targetRenderer.sharedMaterial = instanceMaterial;
            UploadTextures();
            return true;
        }

        public bool TryPaintContact(Vector3 contactPoint, Vector3 outwardNormal, float radiusUv)
        {
            if (!isActiveAndEnabled || mask == null || outwardNormal.sqrMagnitude < 0.0001f) return false;
            Vector3 normal = outwardNormal.normalized;
            float distance = Mathf.Max(0.001f, contactProbeDistance);
            Ray ray = new Ray(contactPoint + normal * distance, -normal);
            // Collision ContactPoint does not contain UVs. Query ONLY the collider actually hit.
            if (!meshCollider.Raycast(ray, out RaycastHit hit, distance * 2f)) return false;
            PaintAtUV(hit.textureCoord, radiusUv);
            return true;
        }

        public void PaintAtUV(Vector2 uv, float radiusUv)
        {
            if (!isActiveAndEnabled || mask == null || radiusUv <= 0f ||
                float.IsNaN(uv.x) || float.IsNaN(uv.y) ||
                float.IsNaN(radiusUv) || float.IsInfinity(radiusUv) ||
                uv.x < -UvTolerance || uv.x > 1f + UvTolerance ||
                uv.y < -UvTolerance || uv.y > 1f + UvTolerance) return;
            uv.x = Mathf.Clamp01(uv.x);
            uv.y = Mathf.Clamp01(uv.y);
            // texel centres match normalized texture UVs; UV=1 never indexes outside the array.
            float cx = uv.x * size - 0.5f;
            float cy = uv.y * size - 0.5f;
            float radius = Mathf.Max(0.75f, Mathf.Min(radiusUv, 1f) * size);
            int x0 = Mathf.Clamp(Mathf.FloorToInt(cx - radius), 0, size - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(cx + radius), 0, size - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(cy - radius), 0, size - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(cy + radius), 0, size - 1);
            Color32 ink = paintColor;
            ink.a = 255;
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                if (dx * dx + dy * dy > radius * radius) continue;
                int index = y * size + x;
                if (maskPixels[index].r != 0) continue; // Union: no duplicate area.
                maskPixels[index] = new Color32(255, 255, 255, 255);
                displayPixels[index] = ink;
                PaintedPixelCount++;
                dirty = true;
            }
        }

        [ContextMenu("Clear Paint (Play Mode)")]
        public void ClearPaint()
        {
            if (maskPixels == null) return;
            for (int i = 0; i < maskPixels.Length; i++)
            {
                maskPixels[i] = new Color32(0, 0, 0, 255);
                displayPixels[i] = basePixels[i];
            }
            PaintedPixelCount = 0;
            dirty = true;
        }

        private void LateUpdate()
        {
            if (dirty) UploadTextures(); // At most one upload per shirt per frame.
        }

        private void UploadTextures()
        {
            mask.SetPixels32(maskPixels);
            mask.Apply(false, false);
            displayTexture.SetPixels32(displayPixels);
            displayTexture.Apply(false, false);
            dirty = false;
        }

        private void OnDestroy()
        {
            if (targetRenderer != null && instanceMaterial != null &&
                targetRenderer.sharedMaterial == instanceMaterial)
                targetRenderer.sharedMaterial = originalMaterial;
            if (instanceMaterial != null) Destroy(instanceMaterial);
            if (mask != null) Destroy(mask);
            if (displayTexture != null) Destroy(displayTexture);
        }
    }
}
