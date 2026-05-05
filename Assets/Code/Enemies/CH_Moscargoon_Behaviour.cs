using System.Collections;
using Unity.VisualScripting;
using UnityEditorInternal.VersionControl;
using UnityEngine;

public class CH_Moscargoon_Behaviour : MonoBehaviour
{
    // =========================
    // REFERENCES
    // =========================
    [Header("References")]
    [SerializeField] private EnemyMovement e_movement;
    [SerializeField] private EnemyAnimController e_animController;
    [SerializeField] private Rigidbody e_rb;
    [SerializeField] private MovementBooster e_moveBoost;
    [SerializeField] private BoxCollider e_headCollider;

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

    private void Update()
    {
        HeadCheck();
    }

    private void HeadCheck()
    {
        Collider[] colliders = new Collider[1];

        Physics.OverlapBoxNonAlloc(e_headCollider.center, e_headCollider.size / 2, colliders);

        if (colliders.Length >= 1 && colliders[1].CompareTag("Player"))
        {
            Stomped(colliders[1].GetComponent<GameObject>());
        }
    }

    private void Stomped(GameObject stomper)
    {
        Rigidbody stompRb = stomper.GetComponent<Rigidbody>();

        e_animController.StompedAnim();
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
            e_moveBoost.Push(stompRb, stomper.transform.up, 50);
        }
        else
        {
            Debug.LogError("Could not apply hurt force, actor is not dynamic");
        }
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
