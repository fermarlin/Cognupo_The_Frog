using UnityEngine;

// Este script es el cerebro concreto del Moscardeux.
public class CH_Moscardeux_Behaviour : MonoBehaviour
{
    [Header("Modules")]
    [SerializeField] private FlyingHeightController heightController;
    [SerializeField] private PlayerDetector playerDetector;
    [SerializeField] private RangedEnemyMovement rangedMovement;
    [SerializeField] private PatrolBetweenPoints patrol;
    [SerializeField] private KnockbackRecovery knockbackRecovery;

    private Transform player;

    private void Awake()
    {
        // Si no estan puestos por Inspector, los buscamos en este GameObject.
        if (heightController == null)
            heightController = GetComponent<FlyingHeightController>();

        if (playerDetector == null)
            playerDetector = GetComponent<PlayerDetector>();

        if (rangedMovement == null)
            rangedMovement = GetComponent<RangedEnemyMovement>();

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

        // Siempre intenta detectar al player.
        if (playerDetector != null)
        {
            player = playerDetector.UpdateDetection(player);
        }

        // Si esta recuperandose de un knockback dejamos que el modulo de recuperacion controle ese momento.
        if (knockbackRecovery != null && knockbackRecovery.IsRecovering)
        {
            HandleKnockbackState();
            return;
        }

        // Si tengo player, hago comportamiento de enemigo a distancia.
        if (player != null && rangedMovement != null)
        {
            rangedMovement.HandleTarget(player);
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

        if (!canMoveAgain)
            return;

        //Mientras se recupera, puede volver a posicionarse, ero no dispara.
        if (player != null && rangedMovement != null)
        {
            rangedMovement.HandleTarget(
                player,
                knockbackRecovery.RecoveryMoveMultiplier,
                false
            );
            return;
        }

    }
}