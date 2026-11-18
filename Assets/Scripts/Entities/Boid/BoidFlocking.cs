
using System.Collections.Generic;
using UnityEngine;

public class BoidFlocking
{
    private readonly Boid boid;

    public BoidFlocking(Boid boid)
    {
        this.boid = boid;
    }

    public Vector3 CalculateFlocking()
    {
        return GetSeparationForce() * boid.SeparationWeight +
               GetAlignmentForce() * boid.AlignmentWeight +
               GetCohesionForce() * boid.CohesionWeight;
    }

    public Vector3 GetSeparationForce()
    {
        List<Boid> nearbyBoids = boid.GetNearbyBoids();

        Vector3 separationDirection = Vector3.zero;
        float totalProximityWeight = 0f;

        foreach (Boid otherBoid in nearbyBoids)
        {
            if (otherBoid == null ||
                otherBoid.IsDead ||
                otherBoid.IsRespawning)
                continue;

            Vector3 offset =
                boid.transform.position - otherBoid.transform.position;

            offset.y = 0f;

            float distance = offset.magnitude;

            if (distance > boid.SeparationRadius)
                continue;

            Vector3 direction = distance <= boid.DistanceEpsilon
                ? GetFallbackSeparationDirection(otherBoid)
                : offset / distance;

            float proximityWeight = 1f -
                Mathf.Clamp01(distance / boid.SeparationRadius);

            separationDirection += direction * proximityWeight;
            totalProximityWeight += proximityWeight;
        }

        if (totalProximityWeight <= boid.DistanceEpsilon)
            return Vector3.zero;

        // La magnitud conserva la influencia de la proximidad.
        separationDirection /= totalProximityWeight;

        float averageProximity = Mathf.Clamp01(
            totalProximityWeight / Mathf.Max(1, nearbyBoids.Count)
        );

        Vector3 desiredVelocity =
            separationDirection.normalized * boid.MaxSpeed * averageProximity;

        Vector3 steeringForce = desiredVelocity - boid.Velocity;

        return Vector3.ClampMagnitude(steeringForce, boid.MaxForce);
    }

    private Vector3 GetFallbackSeparationDirection(Boid otherBoid)
    {
        int firstId = boid.GetInstanceID();
        int secondId = otherBoid.GetInstanceID();

        int lowerId = Mathf.Min(firstId, secondId);
        int higherId = Mathf.Max(firstId, secondId);

        float angle = Mathf.Abs(
            (lowerId * 0.754877666f + higherId * 0.569840296f) * 360f
        );

        Vector3 fallbackDirection =
            Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

        return firstId < secondId
            ? fallbackDirection
            : -fallbackDirection;
    }

    public Vector3 GetAlignmentForce()
    {
        List<Boid> nearbyBoids = boid.GetNearbyBoids();

        Vector3 averageVelocity = Vector3.zero;
        int neighborCount = 0;

        foreach (Boid otherBoid in nearbyBoids)
        {
            if (otherBoid == null ||
                otherBoid.IsDead ||
                otherBoid.IsRespawning)
                continue;

            float distance = Vector3.Distance(
                boid.transform.position,
                otherBoid.transform.position
            );

            if (distance > boid.AlignmentRadius)
                continue;

            averageVelocity += otherBoid.Velocity;
            neighborCount++;
        }

        if (neighborCount == 0)
            return Vector3.zero;

        averageVelocity /= neighborCount;
        averageVelocity = averageVelocity.normalized * boid.MaxSpeed;

        return Vector3.ClampMagnitude(
            averageVelocity - boid.Velocity,
            boid.MaxForce
        );
    }

    public Vector3 GetCohesionForce()
    {
        List<Boid> nearbyBoids = boid.GetNearbyBoids();

        Vector3 averagePosition = Vector3.zero;
        int neighborCount = 0;

        foreach (Boid otherBoid in nearbyBoids)
        {
            if (otherBoid == null ||
                otherBoid.IsDead ||
                otherBoid.IsRespawning)
                continue;

            float distance = Vector3.Distance(
                boid.transform.position,
                otherBoid.transform.position
            );

            if (distance > boid.CohesionRadius)
                continue;

            averagePosition += otherBoid.transform.position;
            neighborCount++;
        }

        if (neighborCount == 0)
            return Vector3.zero;

        averagePosition /= neighborCount;

        return boid.Seek(averagePosition);
    }

    public Vector3 GetFruitAvoidanceForce(Fruit ignoredFruit = null)
    {
        List<Fruit> nearbyFruits = boid.GetNearbyFruits();

        if (nearbyFruits == null || nearbyFruits.Count == 0)
            return Vector3.zero;

        Vector3 avoidanceDirection = Vector3.zero;

        foreach (Fruit fruit in nearbyFruits)
        {
            if (fruit == null || fruit == ignoredFruit)
                continue;

            Vector3 direction =
                boid.transform.position - fruit.transform.position;

            direction.y = 0f;

            float distance = direction.magnitude;

            if (distance > boid.FruitAvoidanceRadius)
                continue;

            if (distance <= boid.DistanceEpsilon)
                direction = Vector3.right;
            else
                direction /= distance;

            float proximityWeight = 1f -
                Mathf.Clamp01(distance / boid.FruitAvoidanceRadius);

            avoidanceDirection += direction * proximityWeight;
        }

        if (avoidanceDirection.sqrMagnitude <= boid.DirectionEpsilon)
            return Vector3.zero;

        Vector3 desiredVelocity =
            avoidanceDirection.normalized * boid.MaxSpeed;

        return Vector3.ClampMagnitude(
            desiredVelocity - boid.Velocity,
            boid.MaxForce
        );
    }
}
