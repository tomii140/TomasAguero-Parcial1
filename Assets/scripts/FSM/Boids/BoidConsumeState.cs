
using UnityEngine;

public class BoidConsumeState : IState
{
    private readonly Boid boid;
    private readonly AudioSource audioSource;
    private readonly AudioClip eatSound;

    private float audioTimer;

    public BoidConsumeState(Boid boid)
    {
        this.boid = boid;
        audioSource = boid.GetComponent<AudioSource>();
        eatSound = boid.EatAudioClip;
    }

    public void Enter()
    {
        boid.SetColorFeedback(new Color(1f, 0.5f, 0f));
        boid.ResetVelocity();

        audioTimer = 0f;
        PlayEatSound();
    }

    public void Update()
    {
        if (boid.IsDead)
        {
            boid.FSM.ChangeState(boid.DeadState);
            return;
        }

        if (boid.DetectHunterInRange() != null)
        {
            boid.FSM.ChangeState(boid.EvadeState);
            return;
        }

        Fruit targetFruit = boid.FindNearestFruit();

        if (!IsFruitValid(targetFruit))
            return;

        ProcessConsumption(targetFruit);
    }

    private bool IsFruitValid(Fruit fruit)
    {
        if (fruit == null)
        {
            boid.RestoreForwardMomentum();
            boid.FSM.ChangeState(boid.FlockingState);
            return false;
        }

        float distanceToFruit = Vector3.Distance(
            boid.transform.position,
            fruit.transform.position
        );

        if (distanceToFruit > fruit.ConsumeRadius)
        {
            boid.FSM.ChangeState(boid.SeekObjectState);
            return false;
        }

        return true;
    }

    private void ProcessConsumption(Fruit fruit)
    {
        fruit.Consume(fruit.DamagePerSecond * Time.deltaTime);

        audioTimer += Time.deltaTime;

        if (audioTimer >= boid.EatSoundInterval)
        {
            PlayEatSound();
            audioTimer = 0f;
        }
    }

    private void PlayEatSound()
    {
        if (audioSource != null && eatSound != null)
            audioSource.PlayOneShot(eatSound);
    }

    public void Exit()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }
}
