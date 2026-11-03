using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WolfAudioController : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip ambientLoopClip;  // Sonido ambiental en bucle
    [SerializeField] private AudioClip howlClip;         // Aullido de alerta

    private AudioSource sfxSource;
    private AudioSource ambientSource;

    private void Awake()
    {
        // Obtener o configurar las fuentes de audio en el mismo lobo
        AudioSource[] sources = GetComponents<AudioSource>();

        sfxSource = sources[0];

        if (sources.Length < 2)
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            ambientSource = sources[1];
        }

        // Configurar audio ambiental constante en bucle
        if (ambientLoopClip != null)
        {
            ambientSource.clip = ambientLoopClip;
            ambientSource.loop = true;
            ambientSource.spatialBlend = 1f; // 3D posicional en el mundo
            ambientSource.minDistance = 2f;
            ambientSource.maxDistance = 30f;
            ambientSource.Play();
        }
    }

    public void PlayHowl()
    {
        if (sfxSource != null && howlClip != null)
        {
            sfxSource.spatialBlend = 1f; // Asegurar 3D
            sfxSource.PlayOneShot(howlClip);
        }
    }
}