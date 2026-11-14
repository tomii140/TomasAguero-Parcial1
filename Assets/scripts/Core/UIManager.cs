using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI capturedBoidsText;
    [SerializeField] private TextMeshProUGUI consumedFruitsText;

    private int capturedBoids = 0;
    private int consumedFruits = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void AddCapturedBoid()
    {
        capturedBoids++;
        if (capturedBoidsText != null)
        {
            capturedBoidsText.text = $"Capturados: {capturedBoids}";
        }
    }

    public void AddFruit()
    {
        consumedFruits++;
        if (consumedFruitsText != null)
        {
            consumedFruitsText.text = $"Frutas consumidas: {consumedFruits}";
        }
    }
}