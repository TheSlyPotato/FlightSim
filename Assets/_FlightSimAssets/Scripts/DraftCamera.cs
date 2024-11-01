using UnityEngine;

public class DraftCamera : MonoBehaviour
{
    public Transform target;           // Aircraft to follow
    public Vector3 offset = new Vector3(0, 5, -10); // Offset position relative to the target
    public float positionSmoothTime = 0.3f;  // Time for position smoothing
    public float rotationSmoothTime = 0.1f;  // Time for rotation smoothing
    public float maxRotationSpeed = 100f;    // Maximum rotation speed

    private Vector3 positionVelocity;        // Velocity reference for SmoothDamp
    private Vector3 rotationVelocity;        // Velocity reference for rotation SmoothDamp

    void LateUpdate()
    {
        if (target == null) return;

        // Calculate the desired position
        Vector3 desiredPosition = target.position + target.TransformDirection(offset);

        // Smoothly move the camera to the desired position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);

        // Calculate the desired rotation to look at the target's position
        Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position, target.up);

        // Smoothly rotate the camera towards the desired rotation
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, maxRotationSpeed * Time.deltaTime);
    }
}
