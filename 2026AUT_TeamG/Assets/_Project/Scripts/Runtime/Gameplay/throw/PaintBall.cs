using UnityEngine;

namespace TeamG.Gameplay
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class PaintBall : MonoBehaviour
    {
        [SerializeField, Range(0.001f, 0.5f)] private float brushRadiusUv = 0.065f;
        [SerializeField, Min(0.1f)] private float lifetimeSeconds = 8f;

        private Rigidbody body;
        private SphereCollider ballCollider;
        private bool launched;
        private bool consumed;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ballCollider = GetComponent<SphereCollider>();
            body.isKinematic = true;
            body.useGravity = false;
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
            launched = true;
            transform.SetParent(null, true);
            ballCollider.isTrigger = false;
            ballCollider.enabled = true;
            if (owner != null)
            {
                foreach (Collider ownCollider in owner.GetComponentsInChildren<Collider>(true))
                    if (ownCollider != ballCollider)
                        Physics.IgnoreCollision(ballCollider, ownCollider, true);
            }
            body.isKinematic = false;
            body.useGravity = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = Vector3.zero;
#else
            body.velocity = Vector3.zero;
#endif
            body.angularVelocity = Vector3.zero;
            // A speed in m/s, independent of the prefab's mass.
            body.AddForce(velocity, ForceMode.VelocityChange);
            Destroy(gameObject, lifetimeSeconds);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!launched || consumed || collision.contactCount == 0) return;
            // Destroy is deferred. Mark consumed before painting to prevent duplicate callbacks.
            consumed = true;
            ballCollider.enabled = false;
            Paint surface = collision.collider.GetComponent<Paint>();
            if (surface != null && surface.isActiveAndEnabled)
            {
                ContactPoint contact = collision.GetContact(0);
                Vector3 normal = contact.normal;
                if (Vector3.Dot(normal, body.worldCenterOfMass - contact.point) < 0f)
                    normal = -normal;
                if (!surface.TryPaintContact(contact.point, normal, brushRadiusUv))
                    Debug.LogWarning("PaintBall: could not get a UV from the hit MeshCollider. Check the mesh and collider setup.", surface);
            }
            // Walls and floors also consume the ball; they never paint a shirt behind them.
            Destroy(gameObject);
        }
    }
}
