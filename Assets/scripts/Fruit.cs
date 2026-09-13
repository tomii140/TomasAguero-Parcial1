using UnityEngine;

public class Fruit : MonoBehaviour
{
    public float vidaMax = 100f;
    public float vidaAct;
    public float dmgPorSeg = 25f;
    public float radioComer = 1.3f;

    void Start() => vidaAct = vidaMax;

    public void SerConsumida(float daño)
    {
        vidaAct -= daño;
        transform.localScale = Vector3.one * (vidaAct / vidaMax);

        if (vidaAct <= 0f)
        {
            if (UIManager.Instance) UIManager.Instance.SumarFruta();
            Destroy(gameObject);
        }
    }
}