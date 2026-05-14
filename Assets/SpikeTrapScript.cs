using UnityEngine;

public class SpikeTrapScript : MonoBehaviour
{
    [SerializeField] private Animator trapAnimator;
    
    [SerializeField] private float activationTime = 4f; // Tiempo que tardan en salir los pinchos
    [SerializeField] private float activeTime = 2f; // Tiempo que están los pinchos subidos

    [SerializeField] private AudioClip sfx_Ready;
    [SerializeField] private AudioClip sfx_Out;
    [SerializeField] private AudioClip sfx_In;

    private float currentActivationTime;
    private float currentActiveTime;

    private bool spikesOut = false;

    private void Start()
    {
        currentActivationTime = activationTime;
        currentActiveTime = activeTime;
    }

    private void Update()
    {
        switch (spikesOut)
        {
            case true:
                
                currentActiveTime -= Time.deltaTime;

                if (currentActiveTime <= 0)
                {
                    trapAnimator.SetTrigger("SpikeIn");
                    currentActiveTime = activeTime;
                    spikesOut = false;
                }

                break;

            case false:

                currentActivationTime -= Time.deltaTime;

                if (currentActivationTime <= 0)
                {
                    trapAnimator.SetTrigger("SpikeOut");
                    currentActivationTime = activationTime;
                    spikesOut = true;
                }

                break;
        }
    }

    public void PlayTrapReady()
    {
        AudioSource.PlayClipAtPoint(sfx_Ready, transform.position, 1);
    }

    public void PlayeTrapSpikeOut()
    {
        AudioSource.PlayClipAtPoint(sfx_Out, transform.position, 1);
    }

    public void PlayeTrapSpikeIn()
    {
        AudioSource.PlayClipAtPoint(sfx_In, transform.position, 1);
    }
}
