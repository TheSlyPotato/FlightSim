using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody))]
public class DraftController : MonoBehaviour
{
    private Rigidbody rb;

    [SerializeField] private float mass;
    [SerializeField] private float wingArea;
    [SerializeField] private float liftCoefficient;
    [SerializeField] private float dragCoefficient;
    [SerializeField] private float airDensity;
    [SerializeField] private float pitchTorqueCoefficient;
    [SerializeField] private float yawTorqueCoefficient;
    [SerializeField] private float rollTorqueCoefficient;
    [SerializeField] private float dampingFactor;
    [SerializeField] private float flapAngleLimit;
    [SerializeField] private float flapAngularSpeed;
    [SerializeField] private float maxThrust;
    [SerializeField] private float thrustPercentChangeRate;
    [SerializeField] private float epsilon;
    public Vector3 relVelocity;
    private float pitchAngle;
    private float yawAngle;
    private float rollAngle;
    private float thrustPercent;

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
        HandleInputs();
        HandleThrust();
        HandleAngles();

        relVelocity = rb.linearVelocity;
        rb.AddForce(CalculateForces(pitchAngle, relVelocity), ForceMode.Force);
        rb.AddTorque(CalculateTorques(pitchAngle, yawAngle, rollAngle), ForceMode.Force);
    }

    void FixedUpdate()
    {
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
            thrustPercent += thrustPercentChangeRate * Time.deltaTime;
        }
        if (GameInput.instance.isBrakePressed)
        {
            thrustPercent -= thrustPercentChangeRate * Time.deltaTime;
        }
        thrustPercent = Mathf.Clamp(thrustPercent, 0f, 100f);
    }

    private void HandleAngles()
    {
        //pitch
        if (pitchInput != 0)
            pitchAngle += pitchInput * flapAngularSpeed * Time.deltaTime;
        else if (pitchAngle > epsilon)
            pitchAngle -= flapAngularSpeed * Time.deltaTime;
        else if (pitchAngle < -epsilon)
            pitchAngle += flapAngularSpeed * Time.deltaTime;
        else
        {
            pitchAngle = 0;
        }
        pitchAngle = Mathf.Clamp(pitchAngle, -flapAngleLimit, flapAngleLimit);

        //yaw
        if (yawInput != 0)
            yawAngle += yawInput * flapAngularSpeed * Time.deltaTime;
        else if (yawAngle > epsilon)
            yawAngle -= flapAngularSpeed * Time.deltaTime;
        else if (yawAngle < -epsilon)
            yawAngle += flapAngularSpeed * Time.deltaTime;
        else
        {
            yawAngle = 0;
        }
        yawAngle = Mathf.Clamp(yawAngle, -flapAngleLimit, flapAngleLimit);

        //roll
        if (rollInput != 0)
            rollAngle += rollInput * flapAngularSpeed * Time.deltaTime;
        else if (rollAngle > epsilon)
            rollAngle -= flapAngularSpeed * Time.deltaTime;
        else if (rollAngle < -epsilon)
            rollAngle += flapAngularSpeed * Time.deltaTime;
        else
        {
            rollAngle = 0;
        }
        rollAngle = Mathf.Clamp(rollAngle, -flapAngleLimit, flapAngleLimit);
    }

    private Vector3 CalculateForces(float pitchAngle, Vector3 relVelocity)
    {
        Vector3 totalForce = Vector3.zero;

        float speed = relVelocity.magnitude;
        
        float lift = 0.5f * airDensity * speed * speed * liftCoefficient 
            * Mathf.Cos(Vector3.Angle(new Vector3(transform.forward.x, 0f, transform.forward.z), relVelocity)
            * Mathf.Deg2Rad);
        Vector3 liftForce = Vector3.up * lift;

        float drag = 0.5f * airDensity * speed * speed * dragCoefficient;
        Vector3 dragForce = -relVelocity * drag;

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

        float rollTorque = -roll * rollTorqueCoefficient;
        Vector3 rollTorqueVector = transform.forward * rollTorque; 
        
        Vector3 counterTorque = -rb.angularVelocity * dampingFactor;
        rb.AddTorque(counterTorque, ForceMode.Force);

        Vector3 totalTorque = pitchTorqueVector + yawTorqueVector + rollTorqueVector + counterTorque;
        return totalTorque;
    }
}
