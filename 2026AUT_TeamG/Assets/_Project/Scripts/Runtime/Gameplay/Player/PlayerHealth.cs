using UnityEngine;
using UnityEngine.UI;

namespace TeamG.Gameplay
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;
        [SerializeField] private int damageAmount = 10;

        [Header("UI Elements")]
        [SerializeField] private Slider healthSlider;

        //ボールが当たっての処理
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ball"))
            {
                TakeDamage(damageAmount);
                Debug.Log("Player took damage.");
            }
        }

        private void TakeDamage(int amount)
        {
            currentHealth -= amount;
            healthSlider.value = (float)currentHealth / maxHealth;
            if (currentHealth <= 0)
            {
                Debug.Log("Player is dead.");
            }
        }
    }
}
