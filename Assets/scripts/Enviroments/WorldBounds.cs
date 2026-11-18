
using System.Collections.Generic;
using UnityEngine;

public class WorldBounds : MonoBehaviour
{
    public static WorldBounds Instance { get; private set; }

    [Header("Map Boundaries XZ Plane")]
    [SerializeField] private float minX = -20f;
    [SerializeField] private float maxX = 20f;
    [SerializeField] private float minZ = -20f;
    [SerializeField] private float maxZ = 20f;

    [Header("Wrap Configuration")]
    [SerializeField, Min(0f)] private float wrapOffset = 0.5f;

    [Header("Spawn Validation")]
    [SerializeField, Min(0.1f)] private float spawnClearanceRadius = 2.5f;
    [SerializeField, Min(1)] private int spawnPositionAttempts = 30;
    [SerializeField, Min(1)] private int spawnOverlapBufferCapacity = 64;
    [SerializeField, Min(0.01f)] private float spawnRetryInterval = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool showWrapDebugLogs = false;

    private readonly Dictionary<Boid, int> boidWrapCounts =
        new Dictionary<Boid, int>();

    private Collider[] spawnOverlapResults;

    public float MinX => minX;
    public float MaxX => maxX;
    public float MinZ => minZ;
    public float MaxZ => maxZ;
    public float SpawnRetryInterval => spawnRetryInterval;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        spawnOverlapResults = new Collider[
            Mathf.Max(1, spawnOverlapBufferCapacity)
        ];
    }

    public Vector3 ClampPosition(ref Vector3 position, ref Vector3 velocity)
    {
        if (position.x < minX)
        {
            position.x = minX;
            velocity.x = -velocity.x;
        }
        else if (position.x > maxX)
        {
            position.x = maxX;
            velocity.x = -velocity.x;
        }

        if (position.z < minZ)
        {
            position.z = minZ;
            velocity.z = -velocity.z;
        }
        else if (position.z > maxZ)
        {
            position.z = maxZ;
            velocity.z = -velocity.z;
        }

        return position;
    }

    public Vector3 WrapPosition(Vector3 position, Boid boid)
    {
        Vector3 previousPosition = position;
        bool wrapped = false;

        if (position.x < minX)
        {
            position.x = maxX - wrapOffset;
            wrapped = true;
        }
        else if (position.x > maxX)
        {
            position.x = minX + wrapOffset;
            wrapped = true;
        }

        if (position.z < minZ)
        {
            position.z = maxZ - wrapOffset;
            wrapped = true;
        }
        else if (position.z > maxZ)
        {
            position.z = minZ + wrapOffset;
            wrapped = true;
        }

        if (wrapped)
        {
            RegisterBoidWrap(boid);

            if (showWrapDebugLogs && boid != null)
            {
                Debug.Log(
                    $"[WORLD BOUNDS] {boid.name} salió del mapa y reapareció del otro lado. " +
                    $"Posición anterior: {previousPosition}. Nueva posición: {position}."
                );
            }
        }

        return position;
    }

    public bool TryGetRandomSpawnPosition(
        Vector3 center,
        float searchRadius,
        float y,
        Boid ignoredBoid,
        out Vector3 spawnPosition)
    {
        for (int attempt = 0; attempt < spawnPositionAttempts; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * searchRadius;

            Vector3 candidate = new Vector3(
                center.x + offset.x,
                y,
                center.z + offset.y
            );

            if (!IsInsideBounds(candidate))
                continue;

            if (!IsSpawnPositionAvailable(candidate, ignoredBoid))
                continue;

            spawnPosition = candidate;
            return true;
        }

        spawnPosition = Vector3.zero;
        return false;
    }

    public bool TryGetRandomSpawnPosition(
        Boid ignoredBoid,
        float y,
        out Vector3 spawnPosition)
    {
        for (int attempt = 0; attempt < spawnPositionAttempts; attempt++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(minX, maxX),
                y,
                Random.Range(minZ, maxZ)
            );

            if (!IsSpawnPositionAvailable(candidate, ignoredBoid))
                continue;

            spawnPosition = candidate;
            return true;
        }

        spawnPosition = Vector3.zero;
        return false;
    }

    private bool IsInsideBounds(Vector3 position)
    {
        return position.x >= minX &&
               position.x <= maxX &&
               position.z >= minZ &&
               position.z <= maxZ;
    }

    private bool IsSpawnPositionAvailable(
        Vector3 position,
        Boid ignoredBoid)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            position,
            spawnClearanceRadius,
            spawnOverlapResults,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        // Si el buffer se llena, no se considera segura la posición.
        if (hitCount >= spawnOverlapResults.Length)
            return false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = spawnOverlapResults[i];

            if (hit == null)
                continue;

            Boid otherBoid = hit.GetComponentInParent<Boid>();

            if (otherBoid != null && otherBoid != ignoredBoid)
                return false;

            Fruit fruit = hit.GetComponentInParent<Fruit>();

            if (fruit != null)
                return false;
        }

        return true;
    }

    private void RegisterBoidWrap(Boid boid)
    {
        if (boid == null)
            return;

        boidWrapCounts.TryGetValue(boid, out int currentCount);
        boidWrapCounts[boid] = currentCount + 1;
    }

    public int GetBoidWrapCount(Boid boid)
    {
        if (boid == null)
            return 0;

        return boidWrapCounts.TryGetValue(boid, out int count)
            ? count
            : 0;
    }

    public void UnregisterBoid(Boid boid)
    {
        if (boid != null)
            boidWrapCounts.Remove(boid);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        Vector3 center = new Vector3(
            (minX + maxX) / 2f,
            0f,
            (minZ + maxZ) / 2f
        );

        Vector3 size = new Vector3(
            maxX - minX,
            1f,
            maxZ - minZ
        );

        Gizmos.DrawWireCube(center, size);
    }
}
