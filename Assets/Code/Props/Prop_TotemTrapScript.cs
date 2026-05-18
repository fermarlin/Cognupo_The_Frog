using UnityEngine;

public class Prop_TotemTrapScript : MonoBehaviour
{
    private GameObject playerRef;
    
    [SerializeField] private GameObject totemCube;
    [SerializeField] private Transform cubeStartPoint;
    [SerializeField] private GameObject vfxAppear;
    [SerializeField] private GameObject vfxDisappear;

    [SerializeField] [Range(1, 100)] private float detectRadius = 10f;

    private bool playerSpotted = false;

    private void Start()
    {
        playerRef = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        if (totemCube != null)
        {
            playerSpotted = PlayerInRange();

            if (playerSpotted)
            {
                if (!totemCube.activeSelf)
                {
                    ActivateCube();
                }
            }
            else
            {
                if (totemCube.activeSelf)
                {
                    DeactivateCube();
                }
            }
        }
        else
        {
            GetComponent<Prop_TotemTrapScript>().enabled = false;
        }
        
    }

    private bool PlayerInRange()
    {
        float playerDistance = (playerRef.transform.position - transform.position).sqrMagnitude;

        if (playerDistance <= (Mathf.Pow(detectRadius, 2)))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void DeactivateCube()
    {
        Instantiate(vfxDisappear, totemCube.transform.position, Quaternion.identity);
        totemCube.transform.position = cubeStartPoint.position;
        totemCube.SetActive(false);
    }

    private void ActivateCube()
    {
        totemCube.SetActive(true);
        Instantiate(vfxAppear, totemCube.transform.position, Quaternion.identity);
    }

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }

#endif
}
