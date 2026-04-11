using System.Collections;
using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    // =========================
    // REFERENCES
    // =========================
    [Header("References")]
    [SerializeField] private EnemyMovement e_movement;
    [SerializeField] private EnemyAnimController e_animController;
    [SerializeField] private Rigidbody e_rb;

    // =========================
    // CONFIG
    // =========================
    [Header("Config")]
    [SerializeField] private float e_stunTime = 0;

    // =========================
    // PRIVATES/UTILITY
    // =========================
    private bool e_hurt = false;

    private void Start()
    {
        e_movement = GetComponent<EnemyMovement>();
        e_animController = GetComponent<EnemyAnimController>();
        e_rb = GetComponent<Rigidbody>();

        e_rb.isKinematic = true;
    }

    private void Hurt()
    {
        e_animController.HurtAnim();
        e_stunTime = 0.3f;
        if (!e_hurt)
        {
            e_hurt = true;
            e_movement.enabled = false;
            e_rb.isKinematic = false;

            StartCoroutine(ChangeRbMode());
        }

        if (!e_rb.isKinematic)
        {
            e_rb.AddForce(Vector3.forward * -15, ForceMode.Impulse);
        }
        else
        {
            Debug.LogError("Could not apply hurt force, actor is not dynamic");
        }
    }

    IEnumerator ChangeRbMode()
    {
        yield return new WaitForSeconds(e_stunTime);

        e_hurt = false;
        e_movement.enabled = true;
        e_rb.isKinematic = true;

        e_stunTime = 0;
        yield return null;
    }
}
