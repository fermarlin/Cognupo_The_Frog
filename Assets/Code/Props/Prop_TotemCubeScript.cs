using UnityEngine;

public class Prop_TotemCubeScript : MonoBehaviour
{
    private GameObject playerRef;


    [SerializeField] private GameObject cubeModel;
    [SerializeField] private GameObject vfxAppear;
    [SerializeField] private GameObject vfxDisappear;

    [SerializeField] private float cubeSpeed;
    [SerializeField] private float cubeDampQuotient = 2f;
    [SerializeField] private float cubeRotationSpeed;

    private void Start()
    {
        playerRef = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        cubeModel.transform.Rotate(transform.up * cubeRotationSpeed * Time.deltaTime, Space.World);

        if (playerRef != null)
        {
            MoveToPlayer();
        }
    }

    private void MoveToPlayer()
    {
        var lookPos = ((playerRef.transform.position + Vector3.up) - transform.position).normalized;
        var rotation = Quaternion.LookRotation(lookPos);

        var dampening = (playerRef.transform.position - transform.position).magnitude;
        var cubeSpeedFinal = Mathf.Clamp((dampening / cubeSpeed)*50, 5, 50);


        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * (dampening/cubeDampQuotient));

        transform.position += transform.forward * Time.deltaTime * cubeSpeedFinal;
    }
}
