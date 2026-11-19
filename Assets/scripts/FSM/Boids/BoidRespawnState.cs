
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
            CompleteRespawn();
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
        {
            // Si no hay espacio libre, espera antes de volver a intentar.
            return;
        }

        boid.transform.position = spawnPosition;

        Physics.SyncTransforms();

        CompleteRespawn();
    }

    private void CompleteRespawn()
    {
        boid.SetRespawnVisibility(true);
        boid.Revive();

        boid.FSM.ChangeState(boid.FlockingState);
    }

    public void Exit() { }
}
