using UnityEngine;

public class Prop_SpringScript : MonoBehaviour
{
    [SerializeField] private Animator propAnimator;
    [SerializeField] private float pushForce = 35f;
    [Header("Sound")]
    [SerializeField] private AudioClip touchSound;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.2f;
    [SerializeField] private float volume = 1f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            propAnimator.SetTrigger("Active");

            Rigidbody playerRb = other.GetComponent<Rigidbody>();

            MovementBooster.BounceUp(playerRb, pushForce);
            PlayCollectSound();
        }
    }

    private void PlayCollectSound()
        {
            if (touchSound == null) return;

            GameObject soundObject = new GameObject("Spring Sound");
            AudioSource audioSource = soundObject.AddComponent<AudioSource>();

            audioSource.clip = touchSound;
            audioSource.volume = volume;
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.Play();

            Destroy(soundObject, touchSound.length / audioSource.pitch);
        }
}