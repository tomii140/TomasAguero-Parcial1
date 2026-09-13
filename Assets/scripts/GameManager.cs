using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Boid Configuration")]
    public GameObject boidPrefab;
    public int initialAmount= 6;
    public float respawnDelay= 4f;

    [Header("Spawn Limits")]
    public float minX= -8f;
    public float maxX= 8f;
    public float minY= -5f;
    public float maxY= 5f;

    void Awake()
    {
        if (Instance== null) Instance= this;
        else Destroy(gameObject);
    }

    void Start()
    {
        for (int i= 0; i< initialAmount; i++)
        {
            SpawnRandomBoid();
        }
    }

    public void NotifyBoidDeath(Boid deadBoid)
    {
        deadBoid.gameObject.SetActive(false);
        StartCoroutine(RespawnBoidRoutine(deadBoid));
    }

    private IEnumerator RespawnBoidRoutine(Boid boid)
    {
        yield return new WaitForSeconds(respawnDelay);
        Vector2 newPos= new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
        boid.transform.position= newPos;
        boid.gameObject.SetActive(true);
        boid.Revive();
    }

    private void SpawnRandomBoid()
    {
        Vector2 pos= new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
        Instantiate(boidPrefab, pos, Quaternion.identity);
    }
}