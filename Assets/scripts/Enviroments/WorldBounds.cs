
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
    [SerializeField, Min(1)] private int spawnPositionAttempts = 60;
    [SerializeField, Min(1)] private int spawnOverlapBufferCapacity = 128;
    [SerializeField, Min(0.01f)] private float spawnRetryInterval = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool showWrapDebugLogs;

    private readonly Dictionary<Boid, int> boidWrapCounts =
        new Dictionary<Boid, int>();

    private Collider[] overlapResults;

    public float MinX => minX;
    public float MaxX => maxX;
    public float MinZ => minZ;
    public float MaxZ => maxZ;
    public float SpawnRetryInterval => spawnRetryInterval;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        overlapResults = new Collider[Mathf.Max(1, spawnOverlapBufferCapacity)];
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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

        if (!wrapped)
            return position;

        float clearance = boid != null
            ? boid.HorizontalCollisionRadius
            : 0f;

        if (!IsPositionAvailable(position, clearance, boid))
        {
            if (!TryGetRandomSpawnPosition(
                position,
                Mathf.Max(maxX - minX, maxZ - minZ),
                position.y,
                clearance,
                boid,
                out Vector3 safePosition))
            {
                return previousPosition;
            }

            position = safePosition;
        }

        RegisterBoidWrap(boid);

        if (showWrapDebugLogs && boid != null)
        {
            Debug.Log(
                $"[WORLD BOUNDS] {boid.name} realizó Wrap. " +
                $"Anterior: {previousPosition}. Nueva: {position}."
            );
        }

        return position;
    }

    public bool TryGetRandomSpawnPosition(
        Vector3 center,
        float searchRadius,
        float y,
        float clearanceRadius,
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

            if (!IsInsideBounds(candidate, clearanceRadius))
                continue;

            if (!IsPositionAvailable(candidate, clearanceRadius, ignoredBoid))
                continue;

            spawnPosition = candidate;
            return true;
        }

        spawnPosition = Vector3.zero;
        return false;
    }

    public bool TryGetRandomSpawnPosition(
        float y,
        float clearanceRadius,
        Boid ignoredBoid,
        out Vector3 spawnPosition)
    {
        for (int attempt = 0; attempt < spawnPositionAttempts; attempt++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(minX + clearanceRadius, maxX - clearanceRadius),
                y,
                Random.Range(minZ + clearanceRadius, maxZ - clearanceRadius)
            );

            if (!IsPositionAvailable(candidate, clearanceRadius, ignoredBoid))
                continue;

            spawnPosition = candidate;
            return true;
        }

        spawnPosition = Vector3.zero;
        return false;
    }

    // Compatibilidad con BoidRespawnState.
    public bool TryGetRandomSpawnPosition(
        Boid ignoredBoid,
        float y,
        out Vector3 spawnPosition)
    {
        float clearance = ignoredBoid != null
            ? ignoredBoid.HorizontalCollisionRadius
            : 0f;

        return TryGetRandomSpawnPosition(
            y,
            clearance,
            ignoredBoid,
            out spawnPosition
        );
    }

    public bool IsPositionAvailable(
        Vector3 position,
        float clearanceRadius,
        Boid ignoredBoid = null)
    {
        if (overlapResults == null ||
            overlapResults.Length != Mathf.Max(1, spawnOverlapBufferCapacity))
        {
            overlapResults = new Collider[
                Mathf.Max(1, spawnOverlapBufferCapacity)
            ];
        }

        int hitCount = Physics.OverlapSphereNonAlloc(
            position,
            Mathf.Max(0.01f, clearanceRadius),
            overlapResults,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        if (hitCount >= overlapResults.Length)
            return false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapResults[i];

            if (hit == null || !hit.enabled)
                continue;

            Boid otherBoid = hit.GetComponentInParent<Boid>();

            if (otherBoid != null && otherBoid != ignoredBoid)
            {
                if (!otherBoid.IsDead && !otherBoid.IsRespawning)
                    return false;

                continue;
            }

            Fruit fruit = hit.GetComponentInParent<Fruit>();

            if (fruit != null)
                return false;
        }

        return true;
    }

    private bool IsInsideBounds(Vector3 position, float margin)
    {
        return position.x >= minX + margin &&
               position.x <= maxX - margin &&
               position.z >= minZ + margin &&
               position.z <= maxZ - margin;
    }

    private void RegisterBoidWrap(Boid boid)
    {
        if (boid == null)
            return;

        boidWrapCounts.TryGetValue(boid, out int count);
        boidWrapCounts[boid] = count + 1;
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
            (minX + maxX) * 0.5f,
            0f,
            (minZ + maxZ) * 0.5f
        );

        Vector3 size = new Vector3(
            maxX - minX,
            1f,
            maxZ - minZ
        );

        Gizmos.DrawWireCube(center, size);
    }
}
