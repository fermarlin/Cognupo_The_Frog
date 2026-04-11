using UnityEngine;

public class EnemyAnimController : MonoBehaviour
{
    // =========================
    // REFERENCES
    // =========================
    [SerializeField] private Animator e_animator;

    private void Awake()
    {
        e_animator = GetComponent<Animator>();
    }

    public void HurtAnim()
    {
        e_animator.SetTrigger("GotHurt");
        e_animator.SetInteger("hurt", 1);
    }
}
