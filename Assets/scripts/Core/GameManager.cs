using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Hunter Configuration")]
    [SerializeField] private HunterAI hunter;
    public HunterAI Hunter => hunter;

    [Header("Boid Management")]
    [SerializeField] private GameObject boidPrefab;
    [SerializeField, Range(1, 50)] private int initialAmount = 6;
    [SerializeField, Range(0.5f, 10f)] private float respawnDelay = 4f;

    [Header("Spawn Settings")]
    [SerializeField, Range(0.5f, 20f)] private float spawnRadius = 5f;

    public List<Boid> ActiveBoids { get; private set; } = new List<Boid>();
    public List<Fruit> ActiveFruits { get; private set; } = new List<Fruit>();

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
        if (deadBoid != null)
        {
            deadBoid.TriggerRespawnFlow();
            StartCoroutine(RespawnBoidRoutine(deadBoid, respawnDelay, spawnRadius));
        }
    }

    private IEnumerator RespawnBoidRoutine(Boid boid, float delay, float radius)
    {
        boid.gameObject.SetActive(false);
        yield return new WaitForSeconds(delay);

        Vector2 randomCircle = Random.insideUnitCircle * radius;
        boid.transform.position = new Vector3(randomCircle.x, 0f, randomCircle.y);

        boid.gameObject.SetActive(true);
        boid.Revive();
    }

    private void SpawnGroupedBoid()
    {
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = new Vector3(randomCircle.x, 0f, randomCircle.y);

        Instantiate(boidPrefab, pos, Quaternion.identity);
    }
}