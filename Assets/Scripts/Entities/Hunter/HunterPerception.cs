
using UnityEngine;

public class HunterPerception
{
    private readonly HunterAI hunter;
    private readonly float updateInterval;

    private readonly Collider[] overlapResults = new Collider[64];

    private float updateTimer;

    private Boid nearestLiveBoid;
    private Boid nearestDeadBoid;

    public HunterPerception(HunterAI hunter, float updateInterval)
    {
        this.hunter = hunter;
        this.updateInterval = Mathf.Max(0.02f, updateInterval);
    }

    public void Refresh()
    {
        nearestLiveBoid = null;
        nearestDeadBoid = null;

        int hitCount = Physics.OverlapSphereNonAlloc(
            hunter.transform.position,
            hunter.VisionRadius,
            overlapResults,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        float nearestLiveDistance = float.MaxValue;
        float nearestDeadDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapResults[i];

            if (hit == null)
                continue;

            Boid boid = hit.GetComponent<Boid>();

            if (boid == null || boid.IsRespawning)
                continue;

            float distance = Vector3.Distance(
                hunter.transform.position,
                boid.transform.position
            );

            if (distance > hunter.VisionRadius)
                continue;

            if (boid.IsDead)
            {
                if (distance < nearestDeadDistance)
                {
                    nearestDeadDistance = distance;
                    nearestDeadBoid = boid;
                }
            }
            else if (distance < nearestLiveDistance)
            {
                nearestLiveDistance = distance;
                nearestLiveBoid = boid;
            }
        }
    }

    public void Update(float deltaTime)
    {
        updateTimer += deltaTime;

        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            Refresh();
        }
    }

    public Boid FindBoidInVision(bool lookForDead)
    {
        if (!lookForDead)
        {
            Boid currentTarget = hunter.GetTargetBoid();

            if (currentTarget != null &&
                !currentTarget.IsDead &&
                !currentTarget.IsRespawning)
            {
                float targetDistance = Vector3.Distance(
                    hunter.transform.position,
                    currentTarget.transform.position
                );

                if (targetDistance <= hunter.VisionRadius)
                    return currentTarget;
            }
        }

        Boid candidate = lookForDead
            ? nearestDeadBoid
            : nearestLiveBoid;

        if (candidate == null ||
            candidate.IsRespawning ||
            candidate.IsDead != lookForDead)
        {
            return null;
        }

        float distance = Vector3.Distance(
            hunter.transform.position,
            candidate.transform.position
        );

        return distance <= hunter.VisionRadius
            ? candidate
            : null;
    }
}
