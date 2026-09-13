using UnityEngine;

public class Boid : Agent
{
    public bool isDead = false;
    private HunterAI hunter;

    [Header("Radios de Flocking")]
    public float radioVisionBoids = 6f;
    public float radioSeparacion = 2.5f;

    [Header("Radios Fruta")]
    public float radioDeteccionFruta = 7f;
    public float distanciaFrenadoFruta = 2f;

    protected override void Start()
    {
        base.Start();
        BuscarCazadorEnEscena();
    }

    void Update()
    {
        // Si el agente está muerto, se detiene completamente en el lugar
        if (isDead)
        {
            velocity = Vector2.zero;
            aceleracion = Vector2.zero;
            return;
        }

        if (hunter == null) BuscarCazadorEnEscena();

        // Evade Prioritario sobre Flocking si el Cazador entra en su rango de visión
        if (hunter != null)
        {
            float distAlHunter = Vector2.Distance(transform.position, hunter.transform.position);
            if (distAlHunter < hunter.visionRadius)
            {
                Vector2 fuerzaEvadir = Evade(hunter);
                Vector2 escapeLateral = new Vector2(-hunter.velocity.y, hunter.velocity.x).normalized;
                fuerzaEvadir += escapeLateral * fMax * 0.4f;

                MeterFuerza(Vector2.ClampMagnitude(fuerzaEvadir, fMax * 1.5f));
                AplicarFisicas();
                return;
                
            }
        }

        // Si no hay amenaza, calcula Flocking
        Vector2 fuerzasGrupo = CalcularFlocking();
        if (fuerzasGrupo == Vector2.zero && velocity.sqrMagnitude < 0.05f)
        {
            fuerzasGrupo = Random.insideUnitCircle.normalized * 0.5f;
        }
        MeterFuerza(fuerzasGrupo);

        // Búsqueda y consumo de Manzanas utilizando Arrive
        Fruit manzana = BuscarFrutaCercana();
        if (manzana != null)
        {
            float distAlManzana = Vector2.Distance(transform.position, manzana.transform.position);

            if (distAlManzana <= manzana.radioComer)
            {
                velocity = Vector2.zero;
                manzana.SerConsumida(manzana.dmgPorSeg * Time.deltaTime);
            }
            else
            {
                MeterFuerza(Arrive(manzana.transform.position, distanciaFrenadoFruta) * 1.2f);
            }
        }

        AplicarFisicas();
    }

    void BuscarCazadorEnEscena()
    {
        hunter = GameObject.FindAnyObjectByType<HunterAI>();
    }

    Vector2 CalcularFlocking()
    {
        Boid[] todos = Object.FindObjectsByType<Boid>(FindObjectsSortMode.None);
        Vector2 centroMasa = Vector2.zero;
        Vector2 velPromedio = Vector2.zero;
        Vector2 fuerzaSeparacion = Vector2.zero;

        int vecCount = 0;
        int sepCount = 0;

        foreach (var otro in todos)
        {
            if (otro == this || otro.isDead) continue;
            float d = Vector2.Distance(transform.position, otro.transform.position);

            // Cohesión y Alineación
            if (d < radioVisionBoids && d > 0.01f)
            {
                centroMasa += (Vector2)otro.transform.position;
                velPromedio += otro.velocity;
                vecCount++;
            }

            // Separación (Rango menor obligatorio)
            if (d < radioSeparacion && d > 0.01f)
            {
                Vector2 repulsion = (Vector2)transform.position - (Vector2)otro.transform.position;
                fuerzaSeparacion += repulsion.normalized / d;
                sepCount++;
            }
        }

        Vector2 fuerzaResultante = Vector2.zero;

        if (vecCount > 0)
        {
            centroMasa /= vecCount;
            velPromedio /= vecCount;

            Vector2 fuerzaCohesion = Seek(centroMasa);
            Vector2 fuerzaAlineacion = Vector2.ClampMagnitude(velPromedio.normalized * vMax - velocity, fMax);

            fuerzaResultante += fuerzaCohesion * 0.4f;
            fuerzaResultante += fuerzaAlineacion * 0.3f;
        }

        if (sepCount > 0)
        {
            fuerzaSeparacion /= sepCount;
            Vector2 deseadaSep = fuerzaSeparacion.normalized * vMax;
            Vector2 fuerzaSepSteering = Vector2.ClampMagnitude(deseadaSep - velocity, fMax);

            fuerzaResultante += fuerzaSepSteering * 2.5f;
        }

        return fuerzaResultante;
    }

    public void Morir()
    {
        isDead = true;
        velocity = Vector2.zero;
        aceleracion = Vector2.zero;
        if (miRender != null) miRender.color = new Color(0.2f, 0.2f, 0.2f, 1f);
    }

    public void Revivir()
    {
        isDead = false;
        velocity = Random.insideUnitCircle * vMax;
        aceleracion = Vector2.zero;
        if (miRender != null) miRender.color = Color.white;
    }

    Fruit BuscarFrutaCercana()
    {
        Fruit[] frutas = Object.FindObjectsByType<Fruit>(FindObjectsSortMode.None);
        Fruit masCercana = null;
        float dMin = radioDeteccionFruta;

        foreach (var f in frutas)
        {
            if (f == null) continue;
            float d = Vector2.Distance(transform.position, f.transform.position);
            if (d < dMin) { dMin = d; masCercana = f; }
        }
        return masCercana;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white; Gizmos.DrawWireSphere(transform.position, radioVisionBoids);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, radioSeparacion);
        Gizmos.color = Color.green; Gizmos.DrawWireSphere(transform.position, radioDeteccionFruta);
    }
}