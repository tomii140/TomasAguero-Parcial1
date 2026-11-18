
using UnityEngine;
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

    [Header("Spawn Settings")]
    [SerializeField, Range(0.5f, 25f)] private float spawnRadius = 8f;

    private readonly List<Boid> activeBoids = new List<Boid>();
    private readonly List<Fruit> activeFruits = new List<Fruit>();

    public IReadOnlyList<Boid> ActiveBoids => activeBoids;
    public IReadOnlyList<Fruit> ActiveFruits => activeFruits;

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
        for (int i = 0; i < initialAmount; i++)
            SpawnGroupedBoid();
    }

    public void RegisterBoid(Boid boid)
    {
        if (boid != null && !activeBoids.Contains(boid))
            activeBoids.Add(boid);
    }

    public void UnregisterBoid(Boid boid)
    {
        if (boid != null)
            activeBoids.Remove(boid);
    }

    public void RegisterFruit(Fruit fruit)
    {
        if (fruit != null && !activeFruits.Contains(fruit))
            activeFruits.Add(fruit);
    }

    public void UnregisterFruit(Fruit fruit)
    {
        if (fruit != null)
            activeFruits.Remove(fruit);
    }

    public void NotifyBoidDeath(Boid deadBoid)
    {
        if (deadBoid != null)
            deadBoid.TriggerRespawnFlow();
    }

    private void SpawnGroupedBoid()
    {
        if (boidPrefab == null)
            return;

        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;

        Vector3 spawnPosition = new Vector3(
            randomCircle.x,
            0f,
            randomCircle.y
        );

        Instantiate(boidPrefab, spawnPosition, Quaternion.identity);
    }
}
