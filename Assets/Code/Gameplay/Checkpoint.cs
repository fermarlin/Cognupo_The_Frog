using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private bool useCheckpointRotation = true;
    [Header("Spawn On Collect")]
    [SerializeField] private GameObject collectSpawnPrefab;

    private void Awake()
    {
        if (respawnPoint == null)
        {
            respawnPoint = transform;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerRespawn playerRespawn = other.GetComponent<PlayerRespawn>();

        if (playerRespawn == null) return;

        Quaternion rotationToSave = useCheckpointRotation 
            ? respawnPoint.rotation 
            : other.transform.rotation;

        playerRespawn.SetCheckpoint(respawnPoint.position, rotationToSave);
        Vector3 particlespawn = new Vector3(transform.position.x,other.transform.position.y,transform.position.z);
        Instantiate(
            collectSpawnPrefab,
            particlespawn,
            Quaternion.identity
        );

    }
}