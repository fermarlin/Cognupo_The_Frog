using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    // =========================
    // REFERENCES
    // =========================
    [Header("References")]
    [SerializeField] private NavMeshAgent navAgent;

    // =========================
    // CONFIG
    // =========================
    [Header("Patrol----------------------------------")]
    public Transform[] waypoints; // Puntos de la patrulla que seguira el enemigo
    [Tooltip("0 - Ping Pong \n 1 - Loop")]
    [Range(0, 1)] public int patrolType = 0; // Forma en la que patrulla el enemigo
                                             // Ping-Pong: Vuelve por los puntos que ha recorrido
                                             // Loop: Vuelve al primer punto del array de patrulla

    [Header("Stats----------------------------------")]
    public float agentVelocity = 5;

    // =========================
    // PRIVATES/UTILITY
    // =========================
    private bool pingPong = false;
    private int navPathIndex = 0;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        navAgent.speed = agentVelocity;
    }

    private void Update()
    {
        // Si el agente ha llegado al final del trayecto y no hay un trayecto pendiente
        if (navAgent.remainingDistance < navAgent.stoppingDistance && !navAgent.pathPending)
        {
            GoToNextPoint();
        }
    }

    private void GoToNextPoint()
    {
        switch (patrolType)
        {
            case 0: // Ping-Pong

                if (navPathIndex >= (waypoints.Length - 1)) // Si el enemigo ha llegado al final de su patrulla
                {
                    pingPong = true;
                }

                if (pingPong)
                {
                    navAgent.SetDestination(waypoints[navPathIndex--].position); // Pasar por el array al revés

                    if (navPathIndex == 0)
                    {
                        pingPong = false;
                    }
                }
                else
                {
                    navAgent.SetDestination(waypoints[navPathIndex++].position); // Pasar por el array de arriba a abajo
                }

                break;

            case 1:

                if (navPathIndex >= waypoints.Length)
                {
                    navPathIndex = 0; // Volver al primer punto del array
                }

                navAgent.SetDestination(waypoints[navPathIndex++].position);

                break;
        }
    }

    private void OnDisable()
    {
        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;
    }

    private void OnEnable()
    {
        navAgent.isStopped = false;
    }
}
