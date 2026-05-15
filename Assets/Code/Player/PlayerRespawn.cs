using System.Collections;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn")]
    [SerializeField] private Transform startCheckpoint;
    [SerializeField] private float respawnDelay = 0.5f;
    [SerializeField] private float extraHeight = 0.2f;

    private Vector3 currentCheckpointPosition;
    private Quaternion currentCheckpointRotation;

    private Health health;
    private Rigidbody rb;
    private PlayerMovement playerMovement;

    private bool isRespawning = false;

    private void Awake()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();

        if (startCheckpoint != null)
        {
            currentCheckpointPosition = startCheckpoint.position;
            currentCheckpointRotation = startCheckpoint.rotation;
        }
        else
        {
            currentCheckpointPosition = transform.position;
            currentCheckpointRotation = transform.rotation;
        }
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDeath += Respawn;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDeath -= Respawn;
        }
    }

    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        currentCheckpointPosition = position;
        currentCheckpointRotation = rotation;
    }

    private void Respawn()
    {
        if (isRespawning) return;

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        yield return new WaitForSeconds(respawnDelay);

        Vector3 respawnPosition = currentCheckpointPosition + Vector3.up * extraHeight;

        if (rb != null)
        {
            rb.position = respawnPosition;
            rb.rotation = currentCheckpointRotation;
        }
        else
        {
            transform.position = respawnPosition;
            transform.rotation = currentCheckpointRotation;
        }

        Physics.SyncTransforms();

        if (health != null)
            health.RespawnFullHealth();

        yield return new WaitForFixedUpdate();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        yield return new WaitForFixedUpdate();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (playerMovement != null)
            playerMovement.enabled = true;

        isRespawning = false;
    }
}