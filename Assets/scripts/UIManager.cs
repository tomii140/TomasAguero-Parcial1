using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI capturedBoidsText;
    [SerializeField] private TextMeshProUGUI consumedFruitsText;

    private int capturedBoids = 0;
    private int consumedFruits = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddCapturedBoid()
    {
        capturedBoids++;
        if (capturedBoidsText) capturedBoidsText.text = $"Capturados: {capturedBoids}";
    }

    public void AddFruit()
    {
        consumedFruits++;
        if (consumedFruitsText) consumedFruitsText.text = $"Frutas consumidas: {consumedFruits}";
    }
}