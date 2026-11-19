
using UnityEngine;

public class HunterPerception
{
    private readonly HunterAI hunter;
    private Collider[] overlapResults;

    private readonly float updateInterval;

    private float updateTimer;

    private Boid nearestLiveBoid;
    private Boid nearestDeadBoid;

    public HunterPerception(HunterAI hunter)
    {
        this.hunter = hunter;

        updateInterval = Mathf.Max(
            hunter.MinimumPerceptionInterval,
            hunter.PerceptionUpdateInterval
        );

        overlapResults = new Collider[
            Mathf.Max(1, hunter.PerceptionBufferCapacity)
        ];
    }

    public void Refresh()
    {
        nearestLiveBoid = null;
        nearestDeadBoid = null;

        int hitCount = GetOverlapResults();

        float nearestLiveDistance = float.MaxValue;
        float nearestDeadDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider detectedCollider = overlapResults[i];

            if (detectedCollider == null)
                continue;

            Boid detectedBoid =
                detectedCollider.GetComponentInParent<Boid>();

            if (detectedBoid == null || detectedBoid.IsRespawning)
                continue;

            float distanceToBoid = Vector3.Distance(
                hunter.transform.position,
                detectedBoid.transform.position
            );

            if (distanceToBoid > hunter.VisionRadius)
                continue;

            if (detectedBoid.IsDead)
            {
                if (distanceToBoid < nearestDeadDistance)
                {
                    nearestDeadDistance = distanceToBoid;
                    nearestDeadBoid = detectedBoid;
                }
            }
            else if (distanceToBoid < nearestLiveDistance)
            {
                nearestLiveDistance = distanceToBoid;
                nearestLiveBoid = detectedBoid;
            }
        }
    }

    private int GetOverlapResults()
    {
        int hitCount;

        do
        {
            hitCount = Physics.OverlapSphereNonAlloc(
                hunter.transform.position,
                hunter.VisionRadius,
                overlapResults,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide
            );

            if (hitCount < overlapResults.Length)
                return hitCount;

            // Si se llena el buffer, se amplía para no perder detecciones.
            System.Array.Resize(
                ref overlapResults,
                overlapResults.Length * 2
            );

        } while (true);
    }

    public void Update(float deltaTime)
    {
        updateTimer += deltaTime;

        if (updateTimer < updateInterval)
            return;

        updateTimer = 0f;
        Refresh();
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

        float distanceToCandidate = Vector3.Distance(
            hunter.transform.position,
            candidate.transform.position
        );

        return distanceToCandidate <= hunter.VisionRadius
            ? candidate
            : null;
    }
}
