using System;
using UnityEngine;

namespace TeamG.Gameplay
{
    public class FallOffRodeo : MonoBehaviour
    {
        [Header("Base of Raycast")]
        [SerializeField] private Transform rayPosition;

        [SerializeField] private float rayDistance = 10f;

        RaycastHit hit;
        void Start()
        {
            if (rayPosition == null)
            {
                Debug.LogError("Base of Raycast is not assigned.");
            }
        }

        private void Update()
        {
            Physics.Raycast(rayPosition.position, rayPosition.forward, out hit, rayDistance);
            Debug.DrawRay(rayPosition.position, rayPosition.forward * rayDistance, Color.red);
        }
    }
}
