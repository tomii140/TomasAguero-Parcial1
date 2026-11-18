
using UnityEngine;

public class BoidRespawnState : IState
{
    private readonly Boid boid;

    private float respawnTimer;
    private float targetRespawnTime;
    private float spawnRetryTimer;

    public BoidRespawnState(Boid boid)
    {
        this.boid = boid;
    }

    public void Enter()
    {
        boid.ResetVelocity();
        boid.SetRespawnVisibility(false);

        respawnTimer = 0f;
        spawnRetryTimer = 0f;

        targetRespawnTime = Random.Range(
            boid.MinRespawnTime,
            boid.MaxRespawnTime
        );
    }

    public void Update()
    {
        respawnTimer += Time.deltaTime;

        if (respawnTimer < targetRespawnTime)
            return;

        TryExecuteRespawn();
    }

    private void TryExecuteRespawn()
    {
        WorldBounds worldBounds = WorldBounds.Instance;

        if (worldBounds == null)
        {
            boid.SetRespawnVisibility(true);
            boid.Revive();
            return;
        }

        spawnRetryTimer += Time.deltaTime;

        if (spawnRetryTimer < worldBounds.SpawnRetryInterval)
            return;

        spawnRetryTimer = 0f;

        bool foundPosition = worldBounds.TryGetRandomSpawnPosition(
            boid,
            boid.transform.position.y,
            out Vector3 spawnPosition
        );

        if (!foundPosition)
            return;

        boid.transform.position = spawnPosition;

        Physics.SyncTransforms();

        boid.SetRespawnVisibility(true);
        boid.Revive();
    }

    public void Exit() { }
}
