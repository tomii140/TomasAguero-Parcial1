
using UnityEngine;
using UnityEngine.UI;

public class Fruit : MonoBehaviour
{
    [Header("Fruit Health Settings")]
    [SerializeField, Range(10f, 500f)] private float maxHealth = 100f;
    [SerializeField, Range(1f, 100f)] private float damagePerSecond = 25f;
    [SerializeField, Range(0.5f, 5f)] private float consumeRadius = 1.3f;

    [Header("Health Bar")]
    [SerializeField] private Slider healthBar;

    public float CurrentHealth { get; private set; }
    public float DamagePerSecond => damagePerSecond;
    public float ConsumeRadius => consumeRadius;

    private void Start()
    {
        CurrentHealth = maxHealth;
        UpdateHealthBar();

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterFruit(this);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterFruit(this);
    }

    public void Consume(float amount)
    {
        if (CurrentHealth <= 0f)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        UpdateHealthBar();

        if (CurrentHealth <= 0f)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.AddFruit();

            Destroy(gameObject);
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar == null)
            return;

        healthBar.maxValue = maxHealth;
        healthBar.value = CurrentHealth;
    }
}
