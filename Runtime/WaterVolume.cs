using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace CHM.ChocoWater
{
    /// <summary>
    /// The core of ChocoWater. Maintains a simulation of the surface water dynamics<br/>
    /// using springs, and synchronizes data with the GPU.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    sealed class WaterVolume : MonoBehaviour
    {
        [Header("Initial Settings")]
        [SerializeField, Min(8)]
        [Tooltip("How many vertices should be used on the water's surface when rendering.\n"
        + "More vertices makes the water look smoother, but this also depends on the "
        + "number of springs used in the simulation.")]
        private int renderResolution = 128;
        [SerializeField, Min(1)]
        [Tooltip("The number of springs in one side of the water, "
        + "not counting the one in the middle.")]
        private int springExtent = 1;
        [SerializeField, Min(0.001f)]
        [Tooltip("The space between each spring, in world units.\n"
        + "It is recommended that the same spring gap is used across every WaterVolume, "
        + "so that all water simulations look similar.")]
        private float springGap = 0.1f;
        [SerializeField, Min(0)]
        [Tooltip("The vertical height of the water volume in local/object space.\n"
        + "This is used to create the water mesh.")]
        private float waterHeight = 5;
        [Header("Runtime Settings")]
        [Min(0)]
        [Tooltip("Stiffness. A greater spring constant makes waves bounce more fiercely "
        + "and take longer to stabilize."
        + "Setting the stiffness too high may cause the simulation to become unstable "
        + "and diverge.")]
        public float springConstant = 50f;
        [Min(0)]
        [Tooltip("Velocity damping. A higher value makes each spring "
        + "return to the water surface more quickly, making the water feel thicker.")]
        public float damping = 6f;
        [Min(0)]
        [Tooltip("How fast waves should spread horizontally.")]
        public float spreadSpeed = 600;
        [Min(0)]
        [Tooltip("An upper bound for how long the simulation should continue after "
        + "invoking SurfaceImpact.\n"
        + "Adjust this number according to your needs.")]
        public float stabilizingTime = 5.0f;
        [SerializeField]
        [Tooltip("When enabled, the simulated water surface is drawn in the scene viewport.")]
        private bool enableDrawGizmo = false;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private Material material;
        private Texture2D displacementMap;
        private NativeArray<float> displacements;
        private NativeArray<float> velocities;
        private readonly int DisplacementShaderID = Shader.PropertyToID("_DisplacementMap");
        private readonly int ObjectSizeShaderID = Shader.PropertyToID("_ObjectSize");
        // Number of springs in the simulation. Cached in Awake.
        private int simResolution = 64; 
        // Used to track if the simulation is in a stable state.
        private float unstableUntil = 0;
        public Vector2 Extents => new Vector2(springExtent * springGap, waterHeight / 2);
        public Vector2 Size => new Vector2(springExtent * springGap * 2, waterHeight);
        public bool IsStable => Time.time >= unstableUntil;
        /// <summary>
        /// Apply an impact with a uniform radius around the given point.
        /// <br/>
        /// Affected springs will have their velocities set to the impulse value.
        /// </summary>
        public void SurfaceImpact(Vector2 worldPoint, float radius, float impulse)
        {
            Vector2 localPoint = transform.InverseTransformPoint(worldPoint);
            // We use the circle's bounding box to find the index range
            // that actually needs to be tested. This skips a lot of springs potentially.
            Vector2 leftBoundPoint = localPoint;
            leftBoundPoint.x -= radius;
            float u = GetSampleIndex(leftBoundPoint);
            int start = Mathf.FloorToInt(u);
            Vector2 rightBoundPoint = localPoint;
            rightBoundPoint.x += radius;
            u = GetSampleIndex(rightBoundPoint);
            int end = Mathf.Min(Mathf.CeilToInt(u), simResolution);
            // Then we go into narrow phase checks.
            for(int i = start; i < end; ++i)
            {
                Vector2 springPosition = GetSpringPosition(i);
                if (Vector2.Distance(localPoint, springPosition) < radius)
                {
                    velocities[i] = impulse;
                }
            }
            // Update unstableUntil to denote the unstable time.
            unstableUntil = Time.time + stabilizingTime;
        }
        /// <summary>
        /// Find the wave displacement above the given point.
        /// </summary>
        public float GetDisplacement(Vector2 worldPoint)
        {
            Vector2 localPoint = transform.InverseTransformPoint(worldPoint);
            float u = GetSampleIndex(localPoint);
            int lowerId = Mathf.FloorToInt(u);
            int higherId = Mathf.Min(lowerId + 1, simResolution - 1);
            float t = u - lowerId;
            return Mathf.Lerp(displacements[lowerId], displacements[higherId], t);
        }
        /// <summary>
        /// Find the vertical distance between the given point and the water surface,
        /// in local space.
        /// </summary>
        public float GetDistanceToSurface(Vector2 worldPoint)
        {
            Vector2 localPoint = transform.InverseTransformPoint(worldPoint);
            float localDistanceToSurface = localPoint.y - (waterHeight / 2 + GetDisplacement(worldPoint));
            return localDistanceToSurface * transform.lossyScale.y;
        }
        /// <summary>
        /// Immediately reset the simulation, filling all buffers with zeroes.
        /// </summary>
        public void ResetSimulation()
        {
            unstableUntil = 0;
            for(int i = 0; i < simResolution; ++i)
            {
                displacements[i] = 0;
                velocities[i] = 0;
            }
            SyncRenderWithSimulation();
        }
#region Unity Events
        void Awake()
        {
            // Setup components.
            TryGetComponent(out meshRenderer);
            TryGetComponent(out meshFilter);
            // Initialize buffers and textures.
            SetupSimulation();
            // Setup material instance.
            material = meshRenderer.material;
            if(material.HasTexture(DisplacementShaderID))
                material.SetTexture(DisplacementShaderID, displacementMap);
            if(material.HasVector(ObjectSizeShaderID))
                material.SetVector(ObjectSizeShaderID, Extents);
            // Finally, the mesh.
            SetupMesh();
        }
        void Start()
        {
            SyncRenderWithSimulation();
        }
        void FixedUpdate()
        {
            Step(Time.fixedDeltaTime);
        }
        void OnDestroy() 
        {
            Destroy(material);
            Destroy(displacementMap);
            displacements.Dispose();
            velocities.Dispose();
            material = null;
            displacementMap = null;
        }
        void OnDrawGizmos() 
        {
            if(!enableDrawGizmo) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            if(Application.isPlaying)
            {
                Gizmos.color = Color.blue;
                for(int i = 1; i < simResolution; ++i)
                {
                    Gizmos.DrawLine(GetSpringPosition(i - 1), GetSpringPosition(i));
                }
            }
            Gizmos.color = Color.green;
            for(int i = 0; i < springExtent * 2 + 1; ++i)
            {
                var pos = GetSpringPosition(i);
                Gizmos.DrawLine(pos, pos + Vector2.up * 0.25f);
            }
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.zero, Size);
        }
#endregion
        private void SetupSimulation()
        {
            // Cache the simulation buffer size.
            simResolution = 1 + springExtent * 2;
            displacementMap = new Texture2D(simResolution, 1, TextureFormat.RFloat, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            displacements = new NativeArray<float>(simResolution, Allocator.Persistent);
            velocities = new NativeArray<float>(simResolution, Allocator.Persistent);
        }
        private void SetupMesh()
        {
            // Initialize mesh.
            Vector3[] vertices = new Vector3[renderResolution * 2];
            int[] tris = new int[(renderResolution - 1) * 2 * 3];
            // Setup vertex positions in object space.
            for (int i = 0; i < renderResolution; ++i)
            {
                // Distribute every vertex evenly across the water's size.
                float x = Mathf.Lerp(-Extents.x, Extents.x, (float) i / (renderResolution - 1));
                // Top, then bottom vertex.
                vertices[i * 2] = new Vector3(x, waterHeight / 2, 0);
                vertices[i * 2 + 1] = new Vector3(x, -waterHeight / 2, 0);
            }
            // Setup triangles.
            // You can also compute triId from i, but it's not any faster.
            int triId = 0;
            for (int i = 1; i < renderResolution; ++i)
            {
                int a = (i - 1) * 2; // Top left
                int b = a + 1; // Bottom left
                int c = i * 2; // Top right
                int d = c + 1; // Bottom right
                // Vertices are laid out clockwise.
                // First triangle in the quad.
                tris[triId++] = c;
                tris[triId++] = b;
                tris[triId++] = a;
                // Second triangle in the quad.
                tris[triId++] = d;
                tris[triId++] = b;
                tris[triId++] = c;
            }
            var mesh = new Mesh
            {
                vertices = vertices,
                triangles = tris
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshFilter.mesh = mesh;
        }
        private void Step(float dt)
        {
            // Already stable, so no need to simulate.
            if (IsStable) return;
            // Update the springs in the Y axis.
            // We also keep track of the maximum displacement 
            // so we can update the renderer's bounding box below.
            float maxDY = displacements[0];
            for (int i = 0; i < simResolution; ++i)
            {
                float force = springConstant * displacements[i] + damping * velocities[i];
                float delta = velocities[i] * dt;
                displacements[i] += delta;
                velocities[i] -= force * dt;
                maxDY = Mathf.Max(displacements[i], maxDY);
            }
            // Update bounds so frustum culling works correctly.
            meshRenderer.localBounds = new Bounds(
                new Vector3(0, maxDY / 2),
                new Vector3(Size.x, maxDY + waterHeight));
            // Calculate spread. This is what propagates waves horizontally.
            SpreadRight(0, dt);
            for (int i = 1; i < simResolution - 1; ++i)
            {
                SpreadLeft(i, dt);
                SpreadRight(i, dt);
            }
            SpreadLeft(simResolution - 1, dt);
            // Finally, upload simulation data to GPU.
            SyncRenderWithSimulation();
        }
        private void SpreadLeft(int i, float deltaTime)
        {
            velocities[i - 1] += spreadSpeed * deltaTime * (displacements[i] - displacements[i - 1]);
        }
        private void SpreadRight(int i, float deltaTime)
        {
            velocities[i + 1] += spreadSpeed * deltaTime * (displacements[i] - displacements[i + 1]);
        }
        /// <summary>
        /// Uploads the displacement buffer to the GPU.
        /// </summary>
        private void SyncRenderWithSimulation()
        {
            displacementMap.SetPixelData(displacements, 0);
            displacementMap.Apply();
        }
        /// <summary>
        /// Gets the object space position of the spring at index i.
        /// </summary>
        private Vector2 GetSpringPosition(int i)
        {
            float x = springGap * (i - springExtent);
            #if UNITY_EDITOR
                float y = Application.isPlaying ? (waterHeight / 2 + displacements[i] / transform.lossyScale.y) : waterHeight / 2;
            #else
                float y = waterHeight / 2 + displacements[i] / transform.lossyScale.y;
            #endif
            Vector2 springPosition = new(x, y);
            return springPosition;
        }
        /// <summary>
        /// Fetches the float index that can be used to sample the simulation buffers.
        /// </summary>
        private float GetSampleIndex(Vector2 localPoint)
        {
            float u = (localPoint.x / Extents.x + 1.0f) * 0.5f;
            u = Mathf.Clamp01(u);
            u *= simResolution - 1;
            return u;
        }
        /// <summary>
        /// Use this to test surface impacts.
        /// </summary>
        [ContextMenu(nameof(TestSurfaceImpact))]
        private void TestSurfaceImpact()
        {
            if(!Application.isPlaying) return;
            SurfaceImpact(transform.TransformPoint(Extents.y * Vector3.up), Extents.x / 20, 30);
        }
    }
}