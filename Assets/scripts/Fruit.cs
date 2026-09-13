using UnityEngine;

public class Fruit : MonoBehaviour
{
    public float maxHealth= 100f;
    public float currentHealth;
    public float damagePerSecond= 25f;
    public float consumeRadius= 1.3f;

    void Start() => currentHealth= maxHealth;

    public void Consume(float amount)
    {
        currentHealth -= amount;
        transform.localScale= Vector3.one* (currentHealth / maxHealth);

        if (currentHealth<= 0f)
        {
            if (UIManager.Instance) UIManager.Instance.AddFruit();
            Destroy(gameObject);
        }
    }
}