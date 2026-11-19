
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Text Object Names")]
    [SerializeField] private string capturedBoidsTextObjectName = "CapturedBoids_txt";
    [SerializeField] private string consumedFruitsTextObjectName = "ConsumedFruits_txt";

    private TMP_Text capturedBoidsText;
    private TMP_Text consumedFruitsText;

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
        FindCounterTexts();

        UpdateCapturedBoidsText();
        UpdateConsumedFruitsText();
    }

    private void FindCounterTexts()
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (TMP_Text text in texts)
        {
            if (text.gameObject.name == capturedBoidsTextObjectName)
                capturedBoidsText = text;

            if (text.gameObject.name == consumedFruitsTextObjectName)
                consumedFruitsText = text;
        }

        if (capturedBoidsText == null)
            Debug.LogWarning($"[UI] No se encontró el texto: {capturedBoidsTextObjectName}");

        if (consumedFruitsText == null)
            Debug.LogWarning($"[UI] No se encontró el texto: {consumedFruitsTextObjectName}");
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
