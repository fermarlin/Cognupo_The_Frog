using UnityEngine;

public static class MovementBooster
{
    public static void Push(Rigidbody targetRb, Vector3 pushDirection, float pushForce, ForceMode forceMode = ForceMode.VelocityChange)
    {
        if (targetRb == null)
            return;
        targetRb.linearVelocity = Vector3.zero;
        targetRb.AddForce(pushDirection * pushForce, forceMode);
    }

    public static void KnockbackFrom(Rigidbody targetRb, Transform targetTransform, Transform originTransform, float horizontalForce, float upForce)
    {
        if (targetRb == null || targetTransform == null || originTransform == null)
            return;

        Vector3 knockbackDirection = targetTransform.position - originTransform.position;

        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude < 0.001f)
        {
            knockbackDirection = -targetTransform.forward;
        }

        knockbackDirection.Normalize();


        targetRb.linearVelocity = Vector3.zero;


        Vector3 finalKnockbackDirection = knockbackDirection * horizontalForce;
        finalKnockbackDirection += Vector3.up * upForce;


        Push(targetRb, finalKnockbackDirection, 1f, ForceMode.VelocityChange);
    }

    public static void BounceUp(Rigidbody targetRb, float bounceForce)
    {
        if (targetRb == null)
            return;

        PlayerMovement playerMovement = targetRb.GetComponent<PlayerMovement>();

        // Si el player ya acaba de rebotar, no dejamos que el spring se dispare otra vez
        if (playerMovement != null && !playerMovement.CanReceiveExternalBounce())
            return;

        targetRb.linearVelocity = new Vector3(
            targetRb.linearVelocity.x,
            0f,
            targetRb.linearVelocity.z
        );

        Push(targetRb, Vector3.up, bounceForce, ForceMode.VelocityChange);

        // Avisamos al PlayerMovement de que esta subida viene de un spring
        if (playerMovement != null)
        {
            playerMovement.RegisterExternalBounce();
        }
    }
}