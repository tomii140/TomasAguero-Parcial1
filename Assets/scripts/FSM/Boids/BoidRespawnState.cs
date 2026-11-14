using UnityEngine;

public class BoidRespawnState : IState
{
    private readonly Boid _boid;
    private float _respawnTimer;
    private float _targetRespawnTime;

    public BoidRespawnState(Boid boid) => _boid = boid;

    public void Enter()
    {
        _boid.ResetVelocity();
        _boid.SetRespawnVisibility(false);

        _respawnTimer = 0f;
        _targetRespawnTime = Random.Range(_boid.MinRespawnTime, _boid.MaxRespawnTime);
    }

    public void Update()
    {
        _respawnTimer += Time.deltaTime;

        if (_respawnTimer >= _targetRespawnTime)
        {
            ExecuteRandomRespawn();
        }
    }

    private void ExecuteRandomRespawn()
    {
        Vector3 randomSpawnPosition = CalculateRandomPosition();

        _boid.transform.position = randomSpawnPosition;
        _boid.SetRespawnVisibility(true);
        _boid.Revive();
    }

    private Vector3 CalculateRandomPosition()
    {
        float posX = WorldBounds.Instance != null ? Random.Range(WorldBounds.Instance.MinX, WorldBounds.Instance.MaxX) : Random.Range(-15f, 15f);
        float posZ = WorldBounds.Instance != null ? Random.Range(WorldBounds.Instance.MinZ, WorldBounds.Instance.MaxZ) : Random.Range(-15f, 15f);

        return new Vector3(posX, _boid.transform.position.y, posZ);
    }

    public void Exit() { }
}