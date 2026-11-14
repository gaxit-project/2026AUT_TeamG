using System.Collections.Generic;
using UnityEngine;

namespace TeamG.Gameplay
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class PaintBall : MonoBehaviour
    {
        [SerializeField, Range(0.001f, 0.5f)] private float brushRadiusUv = 0.065f;
        [SerializeField, Min(0.1f)] private float lifetimeSeconds = 8f;
        [Tooltip("Turn OFF for straight shots. This setting is applied when the ball is launched.")]
        [SerializeField] private bool useGravity = true;
        [Tooltip("Layers that consume this ball. Include shirts, opponents, walls and floor.")]
        [SerializeField] private LayerMask hitMask = ~0;

        private readonly HashSet<Collider> ownerColliders = new HashSet<Collider>();
        private Rigidbody body;
        private SphereCollider ballCollider;
        private Transform ownerRoot;
        private Vector3 previousCenter;
        private Vector3 launchDirection;
        private bool launched;
        private bool consumed;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ballCollider = GetComponent<SphereCollider>();
            body.isKinematic = true;
            body.useGravity = false;
            ballCollider.isTrigger = true;
            ballCollider.enabled = false;
        }

        public void Prepare(Transform holdPoint)
        {
            transform.SetParent(holdPoint, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public void Launch(Vector3 velocity, Transform owner)
        {
            if (launched || consumed) return;
            ownerRoot = owner;
            transform.SetParent(null, true);
            ballCollider.isTrigger = true;
            ballCollider.enabled = true;
            ownerColliders.Clear();
            if (ownerRoot != null)
            {
                foreach (Collider ownCollider in ownerRoot.GetComponentsInChildren<Collider>(true))
                {
                    if (ownCollider == ballCollider) continue;
                    ownerColliders.Add(ownCollider);
                    if (ownCollider.enabled && ownCollider.gameObject.activeInHierarchy)
                        Physics.IgnoreCollision(ballCollider, ownCollider, true);
                }
            }

            body.isKinematic = false;
            body.useGravity = useGravity;
            // Fast motion is checked explicitly below; trigger detection does not rely on CCD.
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.interpolation = RigidbodyInterpolation.Interpolate;
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = Vector3.zero;
#else
            body.velocity = Vector3.zero;
#endif
            body.angularVelocity = Vector3.zero;
            previousCenter = WorldCenter();
            launchDirection = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : transform.forward;
            launched = true;
            body.AddForce(velocity, ForceMode.VelocityChange);
            Destroy(gameObject, lifetimeSeconds);
        }

        private Vector3 WorldCenter()
        {
            // Read the physics pose rather than an interpolated render pose.
            return body.position + body.rotation * Vector3.Scale(ballCollider.center, transform.lossyScale);
        }

        private float WorldRadius()
        {
            Vector3 scale = transform.lossyScale;
            float largest = Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            return Mathf.Max(0.0001f, ballCollider.radius * largest);
        }

        private bool IsIgnored(Collider other)
        {
            if (other == null || other == ballCollider || other.isTrigger) return true;
            if ((hitMask.value & (1 << other.gameObject.layer)) == 0) return true;
            if (ownerColliders.Contains(other)) return true;
            if (ownerRoot != null && other.transform.IsChildOf(ownerRoot)) return true;
            if (Physics.GetIgnoreLayerCollision(gameObject.layer, other.gameObject.layer)) return true;
            return Physics.GetIgnoreCollision(ballCollider, other);
        }

        private bool TrySweep(Vector3 from, Vector3 to, out RaycastHit nearest)
        {
            nearest = default;
            Vector3 movement = to - from;
            float distance = movement.magnitude;
            if (distance <= 0.00001f) return false;

            // Sweep the full sphere, so a thin surface crossed between physics steps can be hit.
            RaycastHit[] hits = Physics.SphereCastAll(from, WorldRadius(), movement / distance,
                distance, hitMask, QueryTriggerInteraction.Ignore);
            bool found = false;
            float closest = float.PositiveInfinity;
            foreach (RaycastHit hit in hits)
            {
                if (IsIgnored(hit.collider) || hit.distance >= closest) continue;
                found = true;
                closest = hit.distance;
                nearest = hit;
            }
            return found;
        }

        private void FixedUpdate()
        {
            if (!launched || consumed) return;
            Vector3 currentCenter = WorldCenter();
            if (TrySweep(previousCenter, currentCenter, out RaycastHit hit))
            {
                ConsumeSweepHit(hit, previousCenter, currentCenter);
                return;
            }
            previousCenter = currentCenter;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!launched || consumed || IsIgnored(other)) return;
            Vector3 currentCenter = WorldCenter();
            // Callback order is unspecified. Select the nearest obstacle in this motion segment.
            if (TrySweep(previousCenter, currentCenter, out RaycastHit hit))
            {
                ConsumeSweepHit(hit, previousCenter, currentCenter);
                return;
            }

            // Covers a ball initially overlapping a surface, or a newly enabled collider.
            bool hasSurface = TrySurfaceRay(other, previousCenter, currentCenter, out RaycastHit surfaceHit);
            Consume(other, surfaceHit.point, surfaceHit.normal, hasSurface);
        }

        private void ConsumeSweepHit(RaycastHit hit, Vector3 from, Vector3 to)
        {
            // Sphere casts starting in an overlap can report a zero-distance hit with no valid point.
            if (hit.distance > 0.00001f && hit.normal.sqrMagnitude > 0.0001f)
            {
                Consume(hit.collider, hit.point, hit.normal, true);
                return;
            }
            bool hasSurface = TrySurfaceRay(hit.collider, from, to, out RaycastHit surfaceHit);
            Consume(hit.collider, surfaceHit.point, surfaceHit.normal, hasSurface);
        }

        private bool TrySurfaceRay(Collider other, Vector3 from, Vector3 to, out RaycastHit hit)
        {
            Vector3 movement = to - from;
            Vector3 direction = movement.sqrMagnitude > 0.00000001f ? movement.normalized : launchDirection;
            float padding = WorldRadius() + 0.03f;
            return other.Raycast(new Ray(from - direction * padding, direction), out hit,
                movement.magnitude + padding * 2f);
        }

        private void Consume(Collider other, Vector3 point, Vector3 normal, bool hasSurface)
        {
            if (consumed || IsIgnored(other)) return;
            // Destroy is deferred: prevent trigger callbacks and the sweep from consuming twice.
            consumed = true;
            ballCollider.enabled = false;
            body.isKinematic = true;
            body.useGravity = false;

            Paint surface = other.GetComponent<Paint>();
            if (surface != null && surface.isActiveAndEnabled)
            {
                // SphereCast.textureCoord is not used. Paint raycasts the actual MeshCollider for UVs.
                if (!hasSurface || !surface.TryPaintContact(point, normal, brushRadiusUv))
                    Debug.LogWarning("PaintBall: no paint UV at this hit. Check the MeshCollider and avoid spawning inside the surface.", surface);
            }
            Destroy(gameObject);
        }
    }
}
