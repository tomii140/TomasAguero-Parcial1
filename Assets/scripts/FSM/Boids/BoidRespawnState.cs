using UnityEngine;

public class BoidRespawnState : IState
{
    private readonly Boid _boid;
    private float _respawnTimer;
    private float _targetRespawnTime;

    public BoidRespawnState(Boid boid) => _boid = boid;

    public void Enter()
    {
        _boid.SetColorFeedback(Color.clear); // Invisible o desactivado durante el respawn
        _boid.ResetVelocity();
        _boid.gameObject.SetActive(false); // Se oculta de la escena

        _respawnTimer = 0f;
        _targetRespawnTime = Random.Range(3f, 6f); // Tiempo configurable de reaparición
    }

    public void Update()
    {
        _respawnTimer += Time.deltaTime;

        if (_respawnTimer >= _targetRespawnTime)
        {
            // Reposicionar aleatoriamente dentro de los límites del mapa (ajustar según tus bounds)
            Vector3 randomPos = new Vector3(Random.Range(-15f, 15f), _boid.transform.position.y, Random.Range(-15f, 15f));
            _boid.transform.position = randomPos;

            _boid.gameObject.SetActive(true);
            _boid.Revive(); // Restaura vida y vuelve a Flocking
        }
    }

    public void Exit() { }
}