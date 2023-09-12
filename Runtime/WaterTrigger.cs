using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CHM.ChocoWater
{
    /// <summary>
    /// Makes a WaterVolume able to react to external rigidbodies,<br/>
    /// causing waves automatically.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(WaterVolume))]
    sealed class WaterTrigger : MonoBehaviour
    {
        [Header("Runtime Settings")]
        [SerializeField]
        [Min(0)]
        [Tooltip("The base force used to push objects vertically along waves.")]
        private float waveDisplacementForce = 20;
        [SerializeField]
        [Min(0)]
        [Tooltip("The minimum impulse caused by a Trigger Enter event.")]
        private float minImpulse = 1;
        [SerializeField]
        [Range(0, 1)]
        [Tooltip("A greater ratio means bigger impact radius upon surface impact.")]
        private float impulseRadiusRatio = 0.01f;
        [SerializeField]
        [Range(0, 0.1f)]
        [Tooltip("A greater half-life means the upward force caused by waves will be stronger.")]
        private float depthDecayDistanceHalfLife = 0.001f;
        [SerializeField]
        [Min(0.001f)]
        [Tooltip("Wave heights below this value won't cause any upward force.")]
        private float wavePushMinimumHeight = 0.1f;
        [SerializeField]
        [Range(-1, 0)]
        [Tooltip("Objects lower than the minimum depth won't be pushed by waves.")]
        private float wavePushMinimumDepth = -0.5f;
        private BoxCollider2D box;
        private WaterVolume volume;
#region Unity Events
        void Awake() 
        {
            TryGetComponent(out box);
            TryGetComponent(out volume);
        }
        void Start()
        {
            SetupComponents();
        }
        void OnTriggerEnter2D(Collider2D other) 
        {
            Vector2 impactPoint = box.ClosestPoint(other.transform.position);
            float impulse = 1;
            if(other.attachedRigidbody) 
                impulse = (
                    other.attachedRigidbody.velocity.magnitude 
                    * other.attachedRigidbody.mass);
            impulse = Mathf.Max(impulse, minImpulse);
            float radius = impulse * impulseRadiusRatio;
            volume.SurfaceImpact(
                impactPoint, 
                radius, 
                impulse);
        }
        void OnTriggerStay2D(Collider2D other) 
        {
            if(other.TryGetComponent<Rigidbody2D>(out var rb))
            {
                Vector2 impactPoint = box.ClosestPoint(other.transform.position);
                float distanceToSurface = volume.GetDistanceToSurface(impactPoint);
                float displacement = volume.GetDisplacement(impactPoint);
                if(distanceToSurface >= wavePushMinimumDepth 
                && displacement > wavePushMinimumHeight)
                {
                    distanceToSurface = Mathf.Max(0, distanceToSurface);
                    float halfLife = depthDecayDistanceHalfLife;
                    float decay = halfLife / (halfLife + distanceToSurface);
                    float waveForce = decay * waveDisplacementForce;
                    rb.AddForce(transform.up * waveForce);
                }
            }
        }
#endregion
        /// <summary>
        /// Use this to make the box collider and buoyancy effector look correctly in Edit Mode.
        /// Does not affect runtime behavior.
        /// </summary>
        [ContextMenu(nameof(SyncBoxCollider))]
        private void SyncBoxCollider()
        {
            if(TryGetComponent(out box) && TryGetComponent(out volume))
            {
                SetupComponents();
            }
        }
        private void SetupComponents()
        {
            box.size = volume.Size;
            box.isTrigger = true;
            if(TryGetComponent<BuoyancyEffector2D>(out var buoyancyEffector))
                buoyancyEffector.surfaceLevel = volume.Extents.y;
        }
    }
}
