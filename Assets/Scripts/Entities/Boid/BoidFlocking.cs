
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

        Vector3 force = Vector3.zero;
        int count = 0;

        foreach (Boid other in nearbyBoids)
        {
            if (other == null || other.IsDead || other.IsRespawning)
                continue;

            float distance = Vector3.Distance(
                boid.transform.position,
                other.transform.position
            );

            if (distance <= boid.SeparationRadius && distance > 0f)
            {
                Vector3 repulse =
                    boid.transform.position - other.transform.position;

                repulse /= distance * distance;

                force += repulse;
                count++;
            }
        }

        if (count > 0)
        {
            force /= count;
            force = force.normalized * boid.MaxSpeed;
            force -= boid.Velocity;

            force = Vector3.ClampMagnitude(force, boid.MaxForce);
        }

        return force;
    }

    public Vector3 GetAlignmentForce()
    {
        List<Boid> nearbyBoids = boid.GetNearbyBoids();

        Vector3 sumVelocity = Vector3.zero;
        int count = 0;

        foreach (Boid other in nearbyBoids)
        {
            if (other == null || other.IsDead || other.IsRespawning)
                continue;

            float distance = Vector3.Distance(
                boid.transform.position,
                other.transform.position
            );

            if (distance > boid.AlignmentRadius)
                continue;

            sumVelocity += other.Velocity;
            count++;
        }

        if (count > 0)
        {
            sumVelocity /= count;
            sumVelocity = sumVelocity.normalized * boid.MaxSpeed;

            Vector3 steer = sumVelocity - boid.Velocity;

            return Vector3.ClampMagnitude(steer, boid.MaxForce);
        }

        return Vector3.zero;
    }

    public Vector3 GetCohesionForce()
    {
        List<Boid> nearbyBoids = boid.GetNearbyBoids();

        Vector3 sumPosition = Vector3.zero;
        int count = 0;

        foreach (Boid other in nearbyBoids)
        {
            if (other == null || other.IsDead || other.IsRespawning)
                continue;

            float distance = Vector3.Distance(
                boid.transform.position,
                other.transform.position
            );

            if (distance > boid.CohesionRadius)
                continue;

            sumPosition += other.transform.position;
            count++;
        }

        if (count > 0)
        {
            sumPosition /= count;
            return boid.Seek(sumPosition);
        }

        return Vector3.zero;
    }

    public Vector3 GetFruitAvoidanceForce(Fruit ignoredFruit = null)
    {
        List<Fruit> nearbyFruits = boid.GetNearbyFruits();

        if (nearbyFruits == null || nearbyFruits.Count == 0)
            return Vector3.zero;

        Vector3 avoidanceDirection = Vector3.zero;
        int count = 0;

        foreach (Fruit fruit in nearbyFruits)
        {
            if (fruit == null || fruit == ignoredFruit)
                continue;

            Vector3 direction = boid.transform.position - fruit.transform.position;
            direction.y = 0f;

            float distance = direction.magnitude;

            if (distance <= 0.001f || distance > boid.FruitAvoidanceRadius)
                continue;

            avoidanceDirection += direction.normalized / distance;
            count++;
        }

        if (count == 0 || avoidanceDirection.sqrMagnitude <= 0.001f)
            return Vector3.zero;

        Vector3 desiredVelocity =
            avoidanceDirection.normalized * boid.MaxSpeed;

        Vector3 steeringForce = desiredVelocity - boid.Velocity;

        return Vector3.ClampMagnitude(steeringForce, boid.MaxForce);
    }
}
