
using UnityEngine;

public class BoidRespawnState : IState
{
    private readonly Boid _boid;

    private float _respawnTimer;
    private float _targetRespawnTime;

    public BoidRespawnState(Boid boid)
    {
        _boid = boid;
    }

    public void Enter()
    {
        _boid.ResetVelocity();
        _boid.SetRespawnVisibility(false);

        _respawnTimer = 0f;
        _targetRespawnTime = Random.Range(
            _boid.MinRespawnTime,
            _boid.MaxRespawnTime
        );
    }

    public void Update()
    {
        _respawnTimer += Time.deltaTime;

        if (_respawnTimer >= _targetRespawnTime)
            ExecuteRandomRespawn();
    }

    private void ExecuteRandomRespawn()
    {
        _boid.transform.position = CalculateRandomPosition();

        _boid.SetRespawnVisibility(true);
        _boid.Revive();
    }

    private Vector3 CalculateRandomPosition()
    {
        if (WorldBounds.Instance == null)
            return _boid.transform.position;

        float posX = Random.Range(
            WorldBounds.Instance.MinX,
            WorldBounds.Instance.MaxX
        );

        float posZ = Random.Range(
            WorldBounds.Instance.MinZ,
            WorldBounds.Instance.MaxZ
        );

        return new Vector3(
            posX,
            _boid.transform.position.y,
            posZ
        );
    }

    public void Exit() { }
}
