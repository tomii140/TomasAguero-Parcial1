
using System.Collections.Generic;
using UnityEngine;

public class BoidPerception
{
    private readonly Boid boid;
    private readonly float updateInterval;

    private readonly Collider[] overlapResults = new Collider[64];

    private float updateTimer;

    private HunterAI detectedHunter;
    private Fruit nearestFruit;

    private readonly List<Boid> nearbyBoids = new List<Boid>();

    public BoidPerception(Boid boid, float updateInterval)
    {
        this.boid = boid;
        this.updateInterval = Mathf.Max(0.02f, updateInterval);
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

    public void Refresh()
    {
        detectedHunter = null;
        nearestFruit = null;
        nearbyBoids.Clear();

        float maxRadius = Mathf.Max(
            boid.HunterPerceptionRadius,
            boid.FruitPerceptionRadius,
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

            HunterAI hunter = hit.GetComponent<HunterAI>();

            if (hunter != null)
            {
                float distance = Vector3.Distance(
                    boid.transform.position,
                    hunter.transform.position
                );

                if (distance <= boid.HunterPerceptionRadius &&
                    distance < nearestHunterDistance)
                {
                    nearestHunterDistance = distance;
                    detectedHunter = hunter;
                }
            }

            Fruit fruit = hit.GetComponent<Fruit>();

            if (fruit != null)
            {
                float distance = Vector3.Distance(
                    boid.transform.position,
                    fruit.transform.position
                );

                if (distance <= boid.FruitPerceptionRadius &&
                    distance < nearestFruitDistance)
                {
                    nearestFruitDistance = distance;
                    nearestFruit = fruit;
                }
            }

            Boid otherBoid = hit.GetComponent<Boid>();

            if (otherBoid != null &&
                otherBoid != boid &&
                !otherBoid.IsDead &&
                !otherBoid.IsRespawning &&
                !nearbyBoids.Contains(otherBoid))
            {
                float distance = Vector3.Distance(
                    boid.transform.position,
                    otherBoid.transform.position
                );

                if (distance <= maxRadius)
                    nearbyBoids.Add(otherBoid);
            }
        }
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
}
