using UnityEngine;

public class Fruit : MonoBehaviour
{
    [Header("Fruit Health Settings")]
    [SerializeField, Range(10f, 500f)] private float maxHealth = 100f;
    [SerializeField, Range(1f, 100f)] private float damagePerSecond = 25f;
    [SerializeField, Range(0.5f, 5f)] private float consumeRadius = 1.3f;

    public float CurrentHealth { get; private set; }
    public float DamagePerSecond => damagePerSecond;
    public float ConsumeRadius => consumeRadius;

    private void Start()
    {
        CurrentHealth = maxHealth;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterFruit(this);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterFruit(this);
        }
    }

    public void Consume(float amount)
    {
        CurrentHealth -= amount;
        transform.localScale = Vector3.one * Mathf.Clamp01(CurrentHealth / maxHealth);

        if (CurrentHealth <= 0f)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.AddFruit();
            }
            Destroy(gameObject);
        }
    }
}