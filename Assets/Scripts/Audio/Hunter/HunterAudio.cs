
using UnityEngine;

public class HunterAudio : MonoBehaviour
{
    [Header("Attack Sounds")]
    [SerializeField] private AudioClip rangedAttackSound;
    [SerializeField] private AudioClip meleeAttackSound;

    [Header("Behavior Sounds")]
    [SerializeField] private AudioClip detectBoidSound;
    [SerializeField] private AudioClip restSound;

    private AudioSource audioSource;

    public AudioClip DetectBoidSound => detectBoidSound;
    public AudioClip RestSound => restSound;

    private void Awake()
    {
        InitializeAudioSource();
    }

    // Obtiene el componente encargado de reproducir los sonidos.
    private void InitializeAudioSource()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
        }
    }

    public void PlayAttackSound(bool isMelee)
    {
        AudioClip clipToPlay = isMelee
            ? meleeAttackSound
            : rangedAttackSound;

        PlaySound(clipToPlay);
    }

    public void PlayDetectionSound()
    {
        PlaySound(detectBoidSound);
    }

    public void PlayRestSound()
    {
        PlaySound(restSound);
    }

    public void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
