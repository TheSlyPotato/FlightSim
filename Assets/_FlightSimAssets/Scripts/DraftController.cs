using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody))]
public class DraftController : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float mass = 1;
    [SerializeField] private GameObject model;

    [SerializeField] private Vector3 airVelocity;
    [SerializeField] private float flapAngleLimit = 15f;
    [SerializeField] private float flapAngularSpeed = 0.08f;
    [SerializeField] private float pitchAngle;
    [SerializeField] private float yawAngle;
    [SerializeField] private float rollAngle;
    [SerializeField] private float maxThrust = 150f;
    [SerializeField] private float thrustPercent;
    [SerializeField] private float thrustPercentChangeRate = 0.5f;
    [SerializeField] private float liftCoefficient = 1.0f;
    [SerializeField] private float dragCoefficient = 0.05f;
    [SerializeField] private float airDensity = 1.225f;
    [SerializeField] private float pitchTorqueCoefficient = 5.0f;
    [SerializeField] private float yawTorqueCoefficient = 2.0f;
    [SerializeField] private float rollTorqueCoefficient = 3.0f;
    [SerializeField] private float epsilon;

    private float pitchInput;
    private float yawInput;
    private float rollInput;  
    
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
    }

    private void Update()
    {
        RotateModelAlongVelocity();

        airVelocity = rb.linearVelocity;
        rb.AddForce(CalculateForces(pitchAngle, airVelocity), ForceMode.Force);
    }

    void FixedUpdate()
    {
        HandleInputs();
        HandleThrust();
        HandleAngles();
        //rb.AddForce(CalculateTorques(pitchAngle, yawAngle, rollAngle) * Time.fixedDeltaTime);
    }

    private void HandleInputs()
    {
        pitchInput = GameInput.instance.pitch;
        yawInput = GameInput.instance.yaw;
        rollInput = GameInput.instance.roll;
    }

    private void HandleThrust()
    {
        //thrust
        if (GameInput.instance.isThrottlePressed)
        {
            thrustPercent += thrustPercentChangeRate * Time.fixedDeltaTime;
        }
        if (GameInput.instance.isBrakePressed)
        {
            thrustPercent -= thrustPercentChangeRate * Time.fixedDeltaTime;
        }
        thrustPercent = Mathf.Clamp(thrustPercent, 0f, 100f);
    }

    private void HandleAngles()
    {
        //pitch
        if (pitchInput != 0)
            pitchAngle += pitchInput * flapAngularSpeed * Time.fixedDeltaTime;
        else if (pitchAngle > epsilon)
            pitchAngle -= flapAngularSpeed * Time.fixedDeltaTime;
        else if (pitchAngle < -epsilon)
            pitchAngle += flapAngularSpeed * Time.fixedDeltaTime;
        else
        {
            pitchAngle = 0;
        }
        pitchAngle = Mathf.Clamp(pitchAngle, -flapAngleLimit, flapAngleLimit);

        //yaw
        if (yawInput != 0)
            yawAngle += yawInput * flapAngularSpeed * Time.fixedDeltaTime;
        else if (yawAngle > epsilon)
            yawAngle -= flapAngularSpeed * Time.fixedDeltaTime;
        else if (yawAngle < -epsilon)
            yawAngle += flapAngularSpeed * Time.fixedDeltaTime;
        else
        {
            yawAngle = 0;
        }
        yawAngle = Mathf.Clamp(yawAngle, -flapAngleLimit, flapAngleLimit);

        //roll
        if (rollInput != 0)
            rollAngle += rollInput * flapAngularSpeed * Time.fixedDeltaTime;
        else if (rollAngle > epsilon)
            rollAngle -= flapAngularSpeed * Time.fixedDeltaTime;
        else if (rollAngle < -epsilon)
            rollAngle += flapAngularSpeed * Time.fixedDeltaTime;
        else
        {
            rollAngle = 0;
        }
        rollAngle = Mathf.Clamp(rollAngle, -flapAngleLimit, flapAngleLimit);
    }

    private Vector3 CalculateForces(float pitchAngle, Vector3 airVelocity)
    {
        Vector3 totalForce = Vector3.zero;

        float speed = airVelocity.magnitude;
        
        float lift = 0.5f * airDensity * speed * speed * liftCoefficient * Mathf.Cos(pitchAngle * Mathf.Deg2Rad);
        Vector3 liftForce = Vector3.up * lift;

        float drag = 0.5f * airDensity * speed * speed * dragCoefficient;
        Vector3 dragForce = -airVelocity * drag;

        Vector3 thrust = transform.forward * maxThrust * thrustPercent;

        totalForce = liftForce + dragForce + thrust + 9.81f * mass * Vector3.down;
       
        return totalForce;

    }

    public Vector3 CalculateTorques(float pitch, float yaw, float roll)
    {
        float pitchTorque = pitch * pitchTorqueCoefficient;
        Vector3 pitchTorqueVector = transform.right * pitchTorque;

        float yawTorque = yaw * yawTorqueCoefficient;
        Vector3 yawTorqueVector = transform.up * yawTorque;

        float rollTorque = roll * rollTorqueCoefficient;
        Vector3 rollTorqueVector = transform.forward * rollTorque;

        Vector3 totalTorque = pitchTorqueVector + yawTorqueVector + rollTorqueVector;
        return totalTorque;
    }

    private void RotateModelAlongVelocity()
    {
        model.transform.forward = rb.linearVelocity.normalized;
    }
}
