using UnityEngine;

public class CognupoSoundEvent : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip footstepClip;
    [SerializeField] private AudioClip hurtClip;

    public void PlayFootstep()
    {
        if (audioSource != null && footstepClip != null)
            audioSource.PlayOneShot(footstepClip);
    }
    public void PlayHurt()
    {
        if (audioSource != null && hurtClip != null)
            audioSource.PlayOneShot(hurtClip);
    }
}