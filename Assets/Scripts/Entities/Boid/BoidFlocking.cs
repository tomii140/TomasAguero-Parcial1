
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
        return (GetSeparationForce() * boid.SeparationWeight) +
               (GetAlignmentForce() * boid.AlignmentWeight) +
               (GetCohesionForce() * boid.CohesionWeight);
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

            Vector3 direction;

            if (distance <= boid.DistanceEpsilon)
            {
                direction = GetFallbackSeparationDirection(otherBoid);
                distance = boid.DistanceEpsilon;
            }
            else
            {
                direction = offset / distance;
            }

            // Los vecinos más cercanos tienen mayor influencia en la separación.
            float proximityWeight =
                1f - Mathf.Clamp01(distance / boid.SeparationRadius);

            separationDirection += direction * proximityWeight;
            totalProximityWeight += proximityWeight;
        }

        if (totalProximityWeight <= boid.DirectionEpsilon)
            return Vector3.zero;

        separationDirection /= totalProximityWeight;

        if (separationDirection.sqrMagnitude <= boid.DirectionEpsilon)
            return Vector3.zero;

        Vector3 desiredVelocity =
            separationDirection.normalized * boid.MaxSpeed;

        Vector3 steeringForce = desiredVelocity - boid.Velocity;

        return Vector3.ClampMagnitude(steeringForce, boid.MaxForce);
    }

    private Vector3 GetFallbackSeparationDirection(Boid otherBoid)
    {
        Vector3 direction =
            boid.transform.position - otherBoid.transform.position;

        if (direction.sqrMagnitude > 0f)
            return direction.normalized;

        int firstId = boid.GetInstanceID();
        int secondId = otherBoid.GetInstanceID();

        int lowerId = Mathf.Min(firstId, secondId);
        int higherId = Mathf.Max(firstId, secondId);

        float angle = Mathf.Abs(
            (lowerId * 0.754877666f + higherId * 0.569840296f) * 360f
        );

        // Si coinciden exactamente, usa sus IDs para asignar direcciones opuestas.
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

        Vector3 steeringForce = averageVelocity - boid.Velocity;

        return Vector3.ClampMagnitude(steeringForce, boid.MaxForce);
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
        int fruitCount = 0;

        foreach (Fruit fruit in nearbyFruits)
        {
            if (fruit == null || fruit == ignoredFruit)
                continue;

            Vector3 direction =
                boid.transform.position - fruit.transform.position;

            direction.y = 0f;

            float distance = direction.magnitude;

            if (distance <= boid.DistanceEpsilon ||
                distance > boid.FruitAvoidanceRadius)
                continue;

            avoidanceDirection += direction.normalized / distance;
            fruitCount++;
        }

        if (fruitCount == 0 ||
            avoidanceDirection.sqrMagnitude <= boid.DirectionEpsilon)
            return Vector3.zero;

        Vector3 desiredVelocity =
            avoidanceDirection.normalized * boid.MaxSpeed;

        Vector3 steeringForce = desiredVelocity - boid.Velocity;

        return Vector3.ClampMagnitude(steeringForce, boid.MaxForce);
    }
}
