using UnityEngine;

public class BoidConsumeState : IState
{
    private readonly Boid _boid;
    private AudioSource _audioSource;
    private AudioClip _eatSound;
    private float _audioTimer;
    private float _audioInterval = 0.5f;

    public BoidConsumeState(Boid boid)
    {
        _boid = boid;
        _audioSource = _boid.GetComponent<AudioSource>();
        _eatSound = _boid.EatAudioClip;
    }

    public void Enter()
    {
        _boid.SetColorFeedback(new Color(1f, 0.5f, 0f));
        _boid.ResetVelocity();
        _audioTimer = 0f;
        PlayEatSound();
    }

    public void Update()
    {
        if (_boid.IsDead)
        {
            _boid.FSM.ChangeState(_boid.DeadState);
            return;
        }

        if (_boid.DetectHunterInRange() != null)
        {
            _boid.FSM.ChangeState(_boid.EvadeState);
            return;
        }

        Fruit targetFruit = _boid.FindNearestFruit();
        if (!IsFruitValid(targetFruit)) return;

        ProcessConsumption(targetFruit);
    }

    private bool IsFruitValid(Fruit fruit)
    {
        if (fruit == null)
        {
            _boid.RestoreForwardMomentum();
            _boid.FSM.ChangeState(_boid.FlockingState);
            return false;
        }

        float distToFruit = Vector3.Distance(_boid.transform.position, fruit.transform.position);
        if (distToFruit > fruit.ConsumeRadius)
        {
            _boid.FSM.ChangeState(_boid.SeekObjectState);
            return false;
        }
        return true;
    }

    private void ProcessConsumption(Fruit fruit)
    {
        // Reducción continua de la vida de la fruta basada en el tiempo transcurrido.
        fruit.Consume(fruit.DamagePerSecond * Time.deltaTime);

        _audioTimer += Time.deltaTime;
        if (_audioTimer >= _audioInterval)
        {
            PlayEatSound();
            _audioTimer = 0f;
        }
    }

    public void Exit()
    {
        if (_audioSource != null && _audioSource.isPlaying)
            _audioSource.Stop();
    }

    private void PlayEatSound()
    {
        if (_audioSource != null && _eatSound != null)
            _audioSource.PlayOneShot(_eatSound);
    }
}