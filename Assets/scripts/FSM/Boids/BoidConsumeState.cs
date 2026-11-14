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
    
        if (_boid.DetectHunterInRange() != null)
        {
            _boid.FSM.ChangeState(_boid.EvadeState);
            return;
        }

        Fruit targetFruit = _boid.FindNearestFruit();
        if (targetFruit == null)
        {
            _boid.RestoreForwardMomentum();
            _boid.FSM.ChangeState(_boid.FlockingState);
            return;
        }

        float distToFruit = Vector3.Distance(_boid.transform.position, targetFruit.transform.position);
        if (distToFruit > targetFruit.ConsumeRadius)
        {
            _boid.FSM.ChangeState(_boid.SeekObjectState);
            return;
        }

        targetFruit.Consume(targetFruit.DamagePerSecond * Time.deltaTime);

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