using UnityEngine;

// Este script es el cerebro concreto del Moscargoon.
public class CH_Moscargoon_Behaviour : MonoBehaviour
{
    [Header("Modules")]
    [SerializeField] private FlyingHeightController heightController;
    [SerializeField] private PatrolBetweenPoints patrol;
    [SerializeField] private KnockbackRecovery knockbackRecovery;

    private Transform player;

    private void Awake()
    {
        // Si no estan puestos por Inspector, los buscamos en este GameObject.
        if (heightController == null)
            heightController = GetComponent<FlyingHeightController>();

        if (patrol == null)
            patrol = GetComponent<PatrolBetweenPoints>();

        if (knockbackRecovery == null)
            knockbackRecovery = GetComponent<KnockbackRecovery>();
    }

    private void Update()
    {
        // Siempre intenta mantener su altura.
        if (heightController != null)
        {
            heightController.KeepHeightFromGround();
        }


        // Si esta recuperandose de un knockback dejamos que el modulo de recuperacion controle ese momento.
        if (knockbackRecovery != null && knockbackRecovery.IsRecovering)
        {
            HandleKnockbackState();
            return;
        }


        // Si no tengo player, patrullo.
        if (patrol != null)
        {
            patrol.Patrol();
        }
    }

    private void HandleKnockbackState()
    {
        // Actualiza la recuperacion.
        bool canMoveAgain = knockbackRecovery.UpdateRecovery();

    }
}