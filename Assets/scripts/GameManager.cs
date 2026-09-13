using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Configuración de Boids")]
    public GameObject prefabBoid;
    public int cantidadInicial = 6;
    public float tiempoRespawn = 4f;

    [Header("Límites de Spawn")]
    public float minX = -8f;
    public float maxX = 8f;
    public float minY = -5f;
    public float maxY = 5f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        for (int i = 0; i < cantidadInicial; i++)
        {
            SpawnBoidAleatorio();
        }
    }

    public void NotificarMuerteBoidExistente(Boid boidMuerto)
    {
        boidMuerto.gameObject.SetActive(false);
        StartCoroutine(RutinaRespawnBoidExistente(boidMuerto));
    }

    private IEnumerator RutinaRespawnBoidExistente(Boid boid)
    {
        yield return new WaitForSeconds(tiempoRespawn);
        Vector2 nuevaPos = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
        boid.transform.position = nuevaPos;
        boid.gameObject.SetActive(true);
        boid.Revivir();
    }

    private void SpawnBoidAleatorio()
    {
        Vector2 posicion = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
        Instantiate(prefabBoid, posicion, Quaternion.identity);
    }
}