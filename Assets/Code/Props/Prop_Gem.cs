using UnityEngine;

public class Prop_Gem : MonoBehaviour
{
    [SerializeField] private int scoreValue = 1;

    [Header("Spawn On Collect")]
    [SerializeField] private GameObject collectSpawnPrefab;

    [Header("Sound")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.2f;
    [SerializeField] private float volume = 1f;

    private Material materialInstance;


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (ScoreCounter.Instance != null)
        {
            ScoreCounter.Instance.AddScore(scoreValue);
        }

        SpawnCollectObject();
        PlayCollectSound();

        Destroy(gameObject);
    }


    private void SpawnCollectObject()
    {
        if (collectSpawnPrefab == null) return;

        Instantiate(
            collectSpawnPrefab,
            transform.position,
            Quaternion.identity
        );
    }

    private void PlayCollectSound()
    {
        if (collectSound == null) return;

        GameObject soundObject = new GameObject("Gem Collect Sound");
        AudioSource audioSource = soundObject.AddComponent<AudioSource>();

        audioSource.clip = collectSound;
        audioSource.volume = volume;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.Play();

        Destroy(soundObject, collectSound.length / audioSource.pitch);
    }
}