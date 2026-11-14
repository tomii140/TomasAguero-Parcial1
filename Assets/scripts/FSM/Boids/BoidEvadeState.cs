using UnityEngine;

public class BoidEvadeState : IState
{
    private readonly Boid _boid;

    public BoidEvadeState(Boid boid) => _boid = boid;

    public void Enter() => _boid.SetColorFeedback(Color.red);

    public void Update()
    {
        HunterAI hunter = GameManager.Instance != null ? GameManager.Instance.Hunter : null;
        if (hunter != null)
        {
            Vector3 evadeForce = _boid.Evade(hunter);
            Vector3 separationForce = _boid.GetSeparationForce() * _boid.SeparationWeight;
            _boid.AddForce(evadeForce + separationForce);
        }
    }

    public void Exit() { }
}