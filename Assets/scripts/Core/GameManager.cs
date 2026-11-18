
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField, Min(0.1f)] private float spawnRetryInterval = 0.5f;

    private readonly List<Boid> activeBoids = new List<Boid>();
    private readonly List<Fruit> activeFruits = new List<Fruit>();

    public IReadOnlyList<Boid> ActiveBoids => activeBoids;
    public IReadOnlyList<Fruit> ActiveFruits => activeFruits;

    private int spawnedBoids;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(SpawnInitialBoids());
    }

    private IEnumerator SpawnInitialBoids()
    {
        if (boidPrefab == null)
        {
            Debug.LogError("[GAME MANAGER] No hay Boid Prefab asignado.");
            yield break;
        }

        while (spawnedBoids < initialAmount)
        {
            if (TrySpawnGroupedBoid())
            {
                spawnedBoids++;
                yield return null;
            }
            else
            {
                yield return new WaitForSeconds(spawnRetryInterval);
            }
        }
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

    private bool TrySpawnGroupedBoid()
    {
        WorldBounds bounds = WorldBounds.Instance;

        Vector3 spawnPosition = Vector3.zero;

        if (bounds != null)
        {
            float clearanceRadius = GetPrefabClearanceRadius();

            bool foundPosition = bounds.TryGetRandomSpawnPosition(
                Vector3.zero,
                spawnRadius,
                0f,
                clearanceRadius,
                null,
                out spawnPosition
            );

            if (!foundPosition)
            {
                foundPosition = bounds.TryGetRandomSpawnPosition(
                    0f,
                    clearanceRadius,
                    null,
                    out spawnPosition
                );
            }

            if (!foundPosition)
                return false;
        }
        else
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            spawnPosition = new Vector3(offset.x, 0f, offset.y);
        }

        Instantiate(boidPrefab, spawnPosition, Quaternion.identity);
        Physics.SyncTransforms();

        return true;
    }

    private float GetPrefabClearanceRadius()
    {
        Collider[] prefabColliders =
            boidPrefab.GetComponentsInChildren<Collider>(true);

        float radius = 0.5f;

        foreach (Collider prefabCollider in prefabColliders)
        {
            if (prefabCollider == null)
                continue;

            Vector3 scale = prefabCollider.transform.lossyScale;

            if (prefabCollider is SphereCollider sphere)
            {
                float scaledRadius = sphere.radius *
                    Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));

                radius = Mathf.Max(radius, scaledRadius);
            }
            else if (prefabCollider is CapsuleCollider capsule)
            {
                float scaledRadius = capsule.radius *
                    Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));

                radius = Mathf.Max(radius, scaledRadius);
            }
            else if (prefabCollider is BoxCollider box)
            {
                Vector3 halfSize = Vector3.Scale(
                    box.size * 0.5f,
                    new Vector3(
                        Mathf.Abs(scale.x),
                        Mathf.Abs(scale.y),
                        Mathf.Abs(scale.z)
                    )
                );

                radius = Mathf.Max(
                    radius,
                    new Vector2(halfSize.x, halfSize.z).magnitude
                );
            }
        }

        return radius;
    }
}
