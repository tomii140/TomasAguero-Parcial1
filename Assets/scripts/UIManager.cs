using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public TextMeshProUGUI fruitText;
    public TextMeshProUGUI boidText;

    private int fruitCount= 0;
    private int boidCount= 0;

    void Awake()
    {
        if (Instance== null) Instance= this;
        else Destroy(gameObject);
    }

    public void AddFruit()
    {
        fruitCount++;
        if (fruitText != null) fruitText.text= "Fruits: " + fruitCount;
    }

    public void AddCapturedBoid()
    {
        boidCount++;
        if (boidText != null) boidText.text= "Captured Boids: " + boidCount;
    }
}