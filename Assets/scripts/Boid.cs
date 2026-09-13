using UnityEngine;

public class Boid : Agent
{
    public bool isDead= false;
    private HunterAI hunter;

    [Header("Flocking Radii")]
    public float boidVisionRadius= 6f;
    public float separationRadius= 2.5f;

    [Header("Fruit Radii")]
    public float fruitDetectionRadius= 7f;
    public float fruitBrakeDistance= 2f;

    protected override void Start()
    {
        base.Start();
        FindHunterInScene();
    }

    void Update()
    {
        if (isDead)
        {
            velocity= Vector2.zero;
            acceleration= Vector2.zero;
            return;
        }

        if (hunter== null) FindHunterInScene();

        if (hunter != null)
        {
            float distToHunter= Vector2.Distance(transform.position, hunter.transform.position);
            if (distToHunter< hunter.visionRadius)
            {
                Vector2 evadeForce= Evade(hunter);
                Vector2 sideEscape= new Vector2(-hunter.velocity.y, hunter.velocity.x).normalized;
                evadeForce += sideEscape* fMax* 0.4f;

                AddForce(Vector2.ClampMagnitude(evadeForce, fMax* 1.5f));
                ApplyPhysics();
                return;
            }
        }

        Vector2 groupForces= CalculateFlocking();
        if (groupForces== Vector2.zero && velocity.sqrMagnitude< 0.05f)
        {
            groupForces= Random.insideUnitCircle.normalized* 0.5f;
        }
        AddForce(groupForces);

        Fruit fruit= FindNearestFruit();
        if (fruit!= null)
        {
            float distToFruit= Vector2.Distance(transform.position, fruit.transform.position);

            if (distToFruit <= fruit.consumeRadius)
            {
                velocity= Vector2.zero;
                fruit.Consume(fruit.damagePerSecond* Time.deltaTime);
            }
            else
            {
                AddForce(Arrive(fruit.transform.position, fruitBrakeDistance)* 1.2f);
            }
        }

        ApplyPhysics();
    }

    void FindHunterInScene()
    {
        hunter= GameObject.FindFirstObjectByType<HunterAI>();
    }

    Vector2 CalculateFlocking()
    {
        Boid[] allBoids= Object.FindObjectsByType<Boid>(FindObjectsSortMode.None);
        Vector2 centerOfMass= Vector2.zero;
        Vector2 avgVelocity= Vector2.zero;
        Vector2 separationForce= Vector2.zero;

        int neighborCount= 0;
        int sepCount= 0;

        foreach (var other in allBoids)
        {
            if (other== this || other.isDead) continue;
            float d= Vector2.Distance(transform.position, other.transform.position);

            if (d< boidVisionRadius && d> 0.01f)
            {
                centerOfMass += (Vector2)other.transform.position;
                avgVelocity += other.velocity;
                neighborCount++;
            }

            if (d< separationRadius && d> 0.01f)
            {
                Vector2 push= (Vector2)transform.position - (Vector2)other.transform.position;
                separationForce += push.normalized / d;
                sepCount++;
            }
        }

        Vector2 resultForce= Vector2.zero;

        if (neighborCount> 0)
        {
            centerOfMass /= neighborCount;
            avgVelocity /= neighborCount;

            Vector2 cohesionForce= Seek(centerOfMass);
            Vector2 alignForce= Vector2.ClampMagnitude(avgVelocity.normalized* vMax - velocity, fMax);

            resultForce += cohesionForce* 0.4f;
            resultForce += alignForce* 0.3f;
        }

        if (sepCount> 0)
        {
            separationForce /= sepCount;
            Vector2 desiredSep= separationForce.normalized* vMax;
            Vector2 sepSteering= Vector2.ClampMagnitude(desiredSep - velocity, fMax);

            resultForce += sepSteering* 2.5f;
        }

        return resultForce;
    }

    public void Die()
    {
        isDead= true;
        velocity= Vector2.zero;
        acceleration= Vector2.zero;
        if (myRenderer!= null) myRenderer.color= new Color(0.2f, 0.2f, 0.2f, 1f);
    }

    public void Revive()
    {
        isDead= false;
        velocity= Random.insideUnitCircle* vMax;
        acceleration= Vector2.zero;
        if (myRenderer!= null) myRenderer.color= Color.white;
    }

    Fruit FindNearestFruit()
    {
        Fruit[] fruits= Object.FindObjectsByType<Fruit>(FindObjectsSortMode.None);
        Fruit nearest= null;
        float minDistance= fruitDetectionRadius;

        foreach (var f in fruits)
        {
            if (f== null) continue;
            float d= Vector2.Distance(transform.position, f.transform.position);
            if (d< minDistance) { minDistance= d; nearest= f; }
        }
        return nearest;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color= Color.white; Gizmos.DrawWireSphere(transform.position, boidVisionRadius);
        Gizmos.color= Color.red; Gizmos.DrawWireSphere(transform.position, separationRadius);
        Gizmos.color= Color.green; Gizmos.DrawWireSphere(transform.position, fruitDetectionRadius);
    }
}