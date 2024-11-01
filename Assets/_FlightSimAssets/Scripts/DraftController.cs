using System.Numerics;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody))]
public class DraftController : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField]
     private float mass = 1;
     private UnityEngine.Vector3 airVelocity;
     private float flapAngleLimit = 15f;
     private float flapAngularSpeed = 0.08f;
     private float pitchAngle;
     private float yawAngle;
     private float rollAngle;
     private float maxThrust = 150f;
     private float thrustPercent;
     private float thrustPercentChangeRate = 0.5f;
     private float liftCoefficient = 1.0f;
     private float dragCoefficient = 0.05f;
     private float airDensity = 1.225f;
     private float pitchTorqueCoefficient = 5.0f;
     private float yawTorqueCoefficient = 2.0f;
     private float rollTorqueCoefficient = 3.0f;
    
    
    
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
    }

    void FixedUpdate()
    {
        airVelocity = -rb.linearVelocity;
        HandleInputs();
        rb.AddForce(CalculateForces(pitchAngle, airVelocity));
        rb.AddForce(CalculateTorques(pitchAngle, yawAngle, rollAngle));
    }

    private void HandleInputs()
    {
        //pitch
        if(Input.GetKeyDown(KeyCode.W)) pitchAngle += flapAngularSpeed*Time.deltaTime;
        else if (Input.GetKeyDown(KeyCode.S)) pitchAngle -= flapAngularSpeed*Time.deltaTime;
        else
        {
            if(pitchAngle > 0.1f) pitchAngle -= flapAngularSpeed*Time.deltaTime;
            if(pitchAngle < -0.1f) pitchAngle += flapAngularSpeed*Time.deltaTime; 
        }
        Mathf.Clamp(pitchAngle, -flapAngleLimit, flapAngleLimit);

        //yaw
        if(Input.GetKeyDown(KeyCode.A)) yawAngle += flapAngularSpeed*Time.deltaTime;
        else if (Input.GetKeyDown(KeyCode.D)) yawAngle -= flapAngularSpeed*Time.deltaTime;
        else
        {
            if(yawAngle > 0.1f) yawAngle -= flapAngularSpeed*Time.deltaTime;
            if(yawAngle < -0.1f) yawAngle += flapAngularSpeed*Time.deltaTime; 
        }
        Mathf.Clamp(yawAngle, -flapAngleLimit, flapAngleLimit);

        //roll
        if(Input.GetKeyDown(KeyCode.W)) rollAngle += flapAngularSpeed*Time.deltaTime;
        else if (Input.GetKeyDown(KeyCode.S)) rollAngle -= flapAngularSpeed*Time.deltaTime;
        else
        {
            if(rollAngle > 0.1f) rollAngle -= flapAngularSpeed*Time.deltaTime;
            if(rollAngle < -0.1f) rollAngle += flapAngularSpeed*Time.deltaTime; 
        }
        Mathf.Clamp(rollAngle, -flapAngleLimit, flapAngleLimit);


        //thrust
        if(Input.GetKeyDown(KeyCode.LeftShift)) thrustPercent += thrustPercentChangeRate * Time.deltaTime;
        if(Input.GetKeyDown(KeyCode.LeftControl)) thrustPercent -= thrustPercentChangeRate * Time.deltaTime;
        Mathf.Clamp(thrustPercent, 0f, 100f);
    }

    private UnityEngine.Vector3 CalculateForces(float pitchAngle, UnityEngine.Vector3 airVelocity)
    {
        UnityEngine.Vector3 totalForce = UnityEngine.Vector3.zero;

        float speed = airVelocity.magnitude;
        
        float lift = 0.5f * airDensity * speed * speed * liftCoefficient * Mathf.Cos(pitchAngle);
        UnityEngine.Vector3 liftForce = UnityEngine.Vector3.up * lift;

        float drag = 0.5f * airDensity * speed * speed * dragCoefficient;
        UnityEngine.Vector3 dragForce = -airVelocity.normalized * drag;

        UnityEngine.Vector3 thrust = transform.forward * maxThrust*thrustPercent;

        totalForce = liftForce + dragForce + thrust;
       
        return totalForce;

    }

    public UnityEngine.Vector3 CalculateTorques(float pitch, float yaw, float roll)
    {
        float pitchTorque = pitch * pitchTorqueCoefficient;
        UnityEngine.Vector3 pitchTorqueVector = transform.right * pitchTorque;

        float yawTorque = yaw * yawTorqueCoefficient;
        UnityEngine.Vector3 yawTorqueVector = transform.up * yawTorque;

        float rollTorque = roll * rollTorqueCoefficient;
        UnityEngine.Vector3 rollTorqueVector = transform.forward * rollTorque;

        UnityEngine.Vector3 totalTorque = pitchTorqueVector + yawTorqueVector + rollTorqueVector;
        return totalTorque;
    }
    


}
