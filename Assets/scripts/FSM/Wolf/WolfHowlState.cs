using UnityEngine;

public class WolfHowlState : IWolfState
{
    private WolfAI wolf;
    private Vector3 hunterPosition;
    private float howlDuration = 1.5f; // Duración configurable del aullido
    private float howlTimer = 0f;

    public WolfHowlState(WolfAI wolf, Vector3 hunterPos)
    {
        this.wolf = wolf;
        this.hunterPosition = hunterPos;
    }

    public void Enter()
    {
        howlTimer = 0f;
        wolf.StopAgent(); // El lobo se detiene a aullar

        // Reproducir animación de aullido si existe
        if (wolf.Animator != null)
        {
            wolf.Animator.SetFloat("Speed", 0f);
            wolf.Animator.SetTrigger("Howl"); // Asegúrate de tener este Trigger o usa un Bool si prefieres
        }

        Debug.Log($"[{wolf.gameObject.name}] ¡Aullando de alerta!");
    }

    public void UpdateState()
    {
        howlTimer += Time.deltaTime;

        // Mantener detenido mientras aúlla
        wolf.StopAgent();

        // Al terminar el tiempo de aullido, pasa a persecución (Chase)
        if (howlTimer >= howlDuration)
        {
            if (wolf.ChaseState is WolfChaseState chaseState)
            {
                chaseState.SetLastKnownPosition(hunterPosition);
            }
            wolf.ChangeState(wolf.ChaseState);
        }
    }

    public void Exit()
    {
        // Limpieza si es necesaria
    }
}