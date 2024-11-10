using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody))]
public class Simulator : MonoBehaviour
{
    [SerializeField] 
    float maxThrust = 0;
    [SerializeField] List<AeroSurface> aeroSurfaces = null;
    [SerializeField] List<AeroSurface> controlSurfaces = null;

    [SerializeField] float rollControlSensitivity = 0.2f;
    [SerializeField] float pitchControlSensitivity = 0.2f;
    [SerializeField] float yawControlSensitivity = 0.2f;

    public float wind = 20f;

    public float thrust;
    float thrustPercent;
    Rigidbody rb;
    float flap;
    Vector3[] currentForceAndTorque = {Vector3.zero, Vector3.zero};


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        foreach(var surface in aeroSurfaces)
        {
            if(surface.IsControlSurface) controlSurfaces.Add(surface);
        }
    }
    private void Update()
    {
        if (GameInput.instance.flapDeployed)
        {
            flap = flap > 0 ? 0 : 0.3f;
        }

        thrust = SetThrust();
    }

    private void FixedUpdate()
    {
       
        SetControlSurfecesAngles(GameInput.instance.pitch, GameInput.instance.roll, GameInput.instance.yaw, flap);
        Vector3[] forceAndTorqueThisFrame = CalculateAerodynamicForces(rb.linearVelocity, rb.angularVelocity, -transform.forward * wind, 1.2f, rb.worldCenterOfMass);

        Vector3 velocityPrediction = HalfFrameVelocity(forceAndTorqueThisFrame[0] + transform.forward * thrust * thrustPercent + Physics.gravity * rb.mass);
        Vector3 angularVelocityPrediction = HalfFrameAngularVelocity(forceAndTorqueThisFrame[1]);

        Vector3[] forceAndTorquePrediction = CalculateAerodynamicForces(velocityPrediction, angularVelocityPrediction, -transform.forward * wind, 1.2f, rb.worldCenterOfMass);

        currentForceAndTorque[0] = (forceAndTorqueThisFrame[0] + forceAndTorquePrediction[0]) * 0.5f;
        currentForceAndTorque[1] = (forceAndTorqueThisFrame[1] + forceAndTorquePrediction[1]) * 0.5f;

        Debug.Log(currentForceAndTorque[0]);
        Debug.Log(currentForceAndTorque[1]);

        rb.AddForce(currentForceAndTorque[0]);
        rb.AddTorque(currentForceAndTorque[1]);
        rb.AddForce(transform.forward * thrust);
        
    }

    private Vector3[] CalculateAerodynamicForces(Vector3 velocity, Vector3 angularVelocity, Vector3 wind, float airDensity, Vector3 centerOfMass)
    {
        Vector3[] forceAndTorque = {Vector3.zero, Vector3.zero};
        foreach (var surface in aeroSurfaces)
        {
            Vector3 relativePosition = surface.transform.position - centerOfMass;
            surface.force = surface.CalculateForces(-velocity + wind -Vector3.Cross(angularVelocity, relativePosition), airDensity, relativePosition)[0];
            surface.torque = surface.CalculateForces(-velocity + wind - Vector3.Cross(angularVelocity, relativePosition), airDensity, relativePosition)[1];
            forceAndTorque[0] += surface.force;
            forceAndTorque[1] += surface.torque;
        }
        return forceAndTorque;
    }

    private Vector3 HalfFrameVelocity(Vector3 force)
    {
        return rb.linearVelocity + Time.fixedDeltaTime * 0.5f * force / rb.mass;
    }

    private Vector3 HalfFrameAngularVelocity(Vector3 torque)
    {        
        Quaternion inertiaTensorWorldRotation = rb.rotation * rb.inertiaTensorRotation;
        if (inertiaTensorWorldRotation != Quaternion.identity) {
            inertiaTensorWorldRotation = Quaternion.Normalize(inertiaTensorWorldRotation);
        }
        Vector3 torqueInDiagonalSpace = Quaternion.Inverse(inertiaTensorWorldRotation) * torque;
        Vector3 angularVelocityChangeInDiagonalSpace;
        
        angularVelocityChangeInDiagonalSpace.x = rb.inertiaTensor.x != 0 ? torqueInDiagonalSpace.x / rb.inertiaTensor.x : 0;
        angularVelocityChangeInDiagonalSpace.y = rb.inertiaTensor.y != 0 ? torqueInDiagonalSpace.y / rb.inertiaTensor.y : 0;
        angularVelocityChangeInDiagonalSpace.z = rb.inertiaTensor.z != 0 ? torqueInDiagonalSpace.z / rb.inertiaTensor.z : 0;

        Vector3 angularVelocityChange = inertiaTensorWorldRotation * angularVelocityChangeInDiagonalSpace;
        Vector3 newAngularVelocity = rb.angularVelocity + Time.fixedDeltaTime * 0.5f * angularVelocityChange;
    
        if (float.IsNaN(newAngularVelocity.x) || float.IsNaN(newAngularVelocity.y) || float.IsNaN(newAngularVelocity.z)) {
            Debug.LogWarning("NaN detected in angular velocity calculation; retaining previous angular velocity.");
            return rb.angularVelocity;
        }

        return newAngularVelocity;
    }

    private float SetThrust()
    {
        thrustPercent += GameInput.instance.isThrottleUpPressed ? 0.01f : 0f;
        thrustPercent += GameInput.instance.isThrottleDownPressed ? -0.01f : 0f;

        if (thrustPercent >= 1f)
        {
            thrustPercent = 1f;
        }
        if(thrustPercent <= 0f)
        {
            thrustPercent = 0f;
        }

        return maxThrust * thrustPercent;
    }

    void SetControlSurfecesAngles(float pitch, float roll, float yaw, float flap)
    {
        foreach (var surface in controlSurfaces)
        {
            if (surface == null || !surface.IsControlSurface) continue;
            switch (surface.InputType)
            {
                case ControlInputType.Pitch:
                    surface.SetFlapAngle(pitch * pitchControlSensitivity * surface.InputMultiplyer);
                    break;
                case ControlInputType.Roll:
                    surface.SetFlapAngle(roll * rollControlSensitivity * surface.InputMultiplyer);
                    break;
                case ControlInputType.Yaw:
                    surface.SetFlapAngle(yaw * yawControlSensitivity * surface.InputMultiplyer);
                    break;
                case ControlInputType.Flap:
                    surface.SetFlapAngle(flap * surface.InputMultiplyer);
                    break;
            }
        }
    }
}
