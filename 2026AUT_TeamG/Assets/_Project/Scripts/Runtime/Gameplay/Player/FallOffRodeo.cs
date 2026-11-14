using System;
using UnityEngine;

namespace TeamG.Gameplay
{
    public class FallOffRodeo : MonoBehaviour
    {
        [Header("Base of Raycast")]
        [SerializeField] private Transform rayPosition;

        [SerializeField] private float rayDistance = 10f;

        [SerializeField] private float elapsedTime = 0f;

        [SerializeField] private float timeToFall = 3f;

        [SerializeField] private string targetTag = "Test";

        [SerializeField] private LayerMask targetLayer;

        [SerializeField] private bool isHit = false;

        [SerializeField] private bool isFalling = false;

        RaycastHit hit;
        Rigidbody rb;

        void Start()
        {
            if (rayPosition == null)
            {
                Debug.LogError("Base of Raycast is not assigned.");
            }

            rb = GetComponent<Rigidbody>();
            rb.useGravity = false; //重力を無効にする
            rb.isKinematic = true; //物理演算を無効にする
        }

        private void Update()
        {
            isHit = Physics.Raycast(rayPosition.position, rayPosition.forward, out hit, rayDistance, targetLayer);
            Debug.DrawRay(rayPosition.position, rayPosition.forward * rayDistance, Color.red);

            if (isHit && hit.collider.CompareTag(targetTag)) //指定タグに当たった場合の処理
            {
                elapsedTime = 0f; //時間をリセットする
            }
            else
            {
                elapsedTime += Time.deltaTime; //時間を加算する
                if(elapsedTime >= timeToFall && !isFalling) //指定時間以上当たらなかった場合の処理
                {
                    Fall();
                }
            }

            //落ちる処理
            void Fall()
            {
                isFalling = true; //落ちる処理を一度だけ実行するためにフラグをリセット
                rb.useGravity = true; //重力を有効にする
                rb.isKinematic = false; //物理演算を有効にする
                Debug.Log("落ちる処理を実行");
            }
        }
    }
}
