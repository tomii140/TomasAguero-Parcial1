using UnityEngine;

public class BoidConsumeState : IState
{
    private readonly Boid _boid;
    private AudioSource _audioSource;
    private AudioClip _eatSound;
    private float _audioTimer;
    private float _audioInterval = 0.5f;//intervalo cuando mastica el boid.

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
        Fruit targetFruit = _boid.FindNearestFruit();
        if (targetFruit != null)
        {
            targetFruit.Consume(targetFruit.DamagePerSecond * Time.deltaTime);

            // Reproducir sonido de forma ritmica mientras mastica
            _audioTimer += Time.deltaTime;
            if (_audioTimer >= _audioInterval)
            {
                PlayEatSound();
                _audioTimer = 0f;
            }
        }
    }

    public void Exit()
    {
        // Se corta el sonido inmediatamente cuando al salir del estado de consumo
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }

    private void PlayEatSound()
    {
        if (_audioSource != null && _eatSound != null)
        {
            _audioSource.PlayOneShot(_eatSound);
        }
    }
}