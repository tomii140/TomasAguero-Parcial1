using UnityEngine;

public class BoidRespawnState : IState
{
    private readonly Boid _boid;
    private float _respawnTimer;
    private float _targetRespawnTime;

    public BoidRespawnState(Boid boid) => _boid = boid;

    public void Enter()
    {
        _boid.SetColorFeedback(Color.clear);
        _boid.ResetVelocity();
        _boid.gameObject.SetActive(false);

        _respawnTimer = 0f;
        // Lee los valores configurados desde el Inspector en Boid.cs (Eliminando el hardcoding)
        _targetRespawnTime = Random.Range(_boid.MinRespawnTime, _boid.MaxRespawnTime);
    }

    public void Update()
    {
        _respawnTimer += Time.deltaTime;

        if (_respawnTimer >= _targetRespawnTime)
        {
            ExecuteRespawn();
        }
    }

    private void ExecuteRespawn()
    {
        float posX = 0f;
        float posZ = 0f;

        if (WorldBounds.Instance != null)
        {
            posX = Random.Range(WorldBounds.Instance.MinX, WorldBounds.Instance.MaxX);
            posZ = Random.Range(WorldBounds.Instance.MinZ, WorldBounds.Instance.MaxZ);
        }
        else
        {
            posX = Random.Range(-15f, 15f);
            posZ = Random.Range(-15f, 15f);
        }

        Vector3 randomPos = new Vector3(posX, _boid.transform.position.y, posZ);
        _boid.transform.position = randomPos;

        _boid.gameObject.SetActive(true);
        _boid.Revive();
    }

    public void Exit() { }
}