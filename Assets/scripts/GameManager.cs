using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Boid Management")]
    [SerializeField] private GameObject boidPrefab;
    [SerializeField] private int initialAmount = 6;
    [SerializeField] private float respawnDelay = 4f;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 2.5f;

    public List<Boid> ActiveBoids { get; private set; } = new List<Boid>();
    public List<Fruit> ActiveFruits { get; private set; } = new List<Fruit>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        for (int i = 0; i < initialAmount; i++)
        {
            SpawnGroupedBoid();
        }
    }

    public void RegisterBoid(Boid boid) => ActiveBoids.Add(boid);
    public void UnregisterBoid(Boid boid) => ActiveBoids.Remove(boid);

    public void RegisterFruit(Fruit fruit) => ActiveFruits.Add(fruit);
    public void UnregisterFruit(Fruit fruit) => ActiveFruits.Remove(fruit);

    public void NotifyBoidDeath(Boid deadBoid)
    {
        deadBoid.gameObject.SetActive(false);
        StartCoroutine(RespawnBoidRoutine(deadBoid));
    }

    private IEnumerator RespawnBoidRoutine(Boid boid)
    {
        yield return new WaitForSeconds(respawnDelay);
        Vector2 newPos = Random.insideUnitCircle * spawnRadius;
        boid.transform.position = newPos;
        boid.gameObject.SetActive(true);
        boid.Revive();
    }

    private void SpawnGroupedBoid()
    {
        // Spawnea a todos pegados en un radio de 2.5 alrededor del centro
        Vector2 pos = Random.insideUnitCircle * spawnRadius;
        Instantiate(boidPrefab, pos, Quaternion.identity);
    }
}