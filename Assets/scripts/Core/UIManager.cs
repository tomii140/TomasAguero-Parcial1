
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Counter Text References")]
    [SerializeField] private TMP_Text capturedBoidsText;
    [SerializeField] private TMP_Text consumedFruitsText;

    private int capturedBoids = 0;
    private int consumedFruits = 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        UpdateCapturedBoidsText();
        UpdateConsumedFruitsText();

        if (capturedBoidsText == null)
            Debug.LogWarning("[UI] Falta asignar el texto de Boids capturados.");

        if (consumedFruitsText == null)
            Debug.LogWarning("[UI] Falta asignar el texto de frutas consumidas.");
    }

    public void AddCapturedBoid()
    {
        capturedBoids++;
        UpdateCapturedBoidsText();
    }

    public void AddFruit()
    {
        consumedFruits++;
        UpdateConsumedFruitsText();
    }

    private void UpdateCapturedBoidsText()
    {
        if (capturedBoidsText != null)
            capturedBoidsText.text = $"Capturados: {capturedBoids}";
    }

    private void UpdateConsumedFruitsText()
    {
        if (consumedFruitsText != null)
            consumedFruitsText.text = $"Frutas consumidas: {consumedFruits}";
    }
}
