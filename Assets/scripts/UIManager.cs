using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public TextMeshProUGUI textoFrutas;
    public TextMeshProUGUI textoBoids;

    private int contadorFrutas = 0;
    private int contadorBoids = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SumarFruta()
    {
        contadorFrutas++;
        if (textoFrutas != null) textoFrutas.text = "Apples: " + contadorFrutas;
    }

    public void SumarBoidAtrapado()
    {
        contadorBoids++;
        if (textoBoids != null) textoBoids.text = "Boids Atrapados: " + contadorBoids;
    }
}