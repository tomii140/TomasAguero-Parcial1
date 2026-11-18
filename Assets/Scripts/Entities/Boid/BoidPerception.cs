
using System.Collections.Generic;
using UnityEngine;

public class BoidPerception
{
    private readonly Boid boid;
    private readonly float updateInterval;
    private readonly Collider[] overlapResults;

    private float updateTimer;

    private HunterAI detectedHunter;
    private Fruit nearestFruit;

    private readonly List<Boid> nearbyBoids = new List<Boid>();
    private readonly List<Fruit> nearbyFruits = new List<Fruit>();

    public BoidPerception(
        Boid boid,
        float updateInterval,
        int bufferCapacity)
    {
        this.boid = boid;

        this.updateInterval = Mathf.Max(
            boid.MinimumPerceptionInterval,
            updateInterval
        );

        overlapResults = new Collider[Mathf.Max(1, bufferCapacity)];
    }

    public void Update(float deltaTime)
    {
        updateTimer += deltaTime;

        if (updateTimer < updateInterval)
            return;

        updateTimer = 0f;
        Refresh();
    }

    public void Refresh()
    {
        detectedHunter = null;
        nearestFruit = null;

        nearbyBoids.Clear();
        nearbyFruits.Clear();

        float maxRadius = Mathf.Max(
            boid.HunterPerceptionRadius,
            boid.FruitPerceptionRadius,
            boid.FruitAvoidanceRadius,
            boid.SeparationRadius,
            boid.AlignmentRadius,
            boid.CohesionRadius
        );

        int hitCount = Physics.OverlapSphereNonAlloc(
            boid.transform.position,
            maxRadius,
            overlapResults,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        float nearestHunterDistance = float.MaxValue;
        float nearestFruitDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapResults[i];

            if (hit == null)
                continue;

            DetectHunter(hit, ref nearestHunterDistance);
            DetectFruit(hit, ref nearestFruitDistance);
            DetectBoid(hit, maxRadius);
        }
    }

    private void DetectHunter(Collider hit, ref float nearestDistance)
    {
        HunterAI hunter = hit.GetComponentInParent<HunterAI>();

        if (hunter == null)
            return;

        float distance = Vector3.Distance(
            boid.transform.position,
            hunter.transform.position
        );

        if (distance > boid.HunterPerceptionRadius ||
            distance >= nearestDistance)
            return;

        nearestDistance = distance;
        detectedHunter = hunter;
    }

    private void DetectFruit(Collider hit, ref float nearestDistance)
    {
        Fruit fruit = hit.GetComponentInParent<Fruit>();

        if (fruit == null)
            return;

        float distance = Vector3.Distance(
            boid.transform.position,
            fruit.transform.position
        );

        if (distance <= boid.FruitAvoidanceRadius &&
            !nearbyFruits.Contains(fruit))
        {
            nearbyFruits.Add(fruit);
        }

        if (distance > boid.FruitPerceptionRadius ||
            distance >= nearestDistance)
            return;

        nearestDistance = distance;
        nearestFruit = fruit;
    }

    private void DetectBoid(Collider hit, float maxRadius)
    {
        Boid otherBoid = hit.GetComponentInParent<Boid>();

        if (otherBoid == null ||
            otherBoid == boid ||
            otherBoid.IsRespawning ||
            nearbyBoids.Contains(otherBoid))
            return;

        float distance = Vector3.Distance(
            boid.transform.position,
            otherBoid.transform.position
        );

        if (distance <= maxRadius)
            nearbyBoids.Add(otherBoid);
    }

    public HunterAI DetectHunterInRange()
    {
        if (detectedHunter == null)
            return null;

        float distance = Vector3.Distance(
            boid.transform.position,
            detectedHunter.transform.position
        );

        return distance <= boid.HunterPerceptionRadius
            ? detectedHunter
            : null;
    }

    public Fruit FindNearestFruit()
    {
        if (nearestFruit == null)
            return null;

        float distance = Vector3.Distance(
            boid.transform.position,
            nearestFruit.transform.position
        );

        return distance <= boid.FruitPerceptionRadius
            ? nearestFruit
            : null;
    }

    public List<Boid> GetNearbyBoids()
    {
        return nearbyBoids;
    }

    public List<Fruit> GetNearbyFruits()
    {
        return nearbyFruits;
    }
}
