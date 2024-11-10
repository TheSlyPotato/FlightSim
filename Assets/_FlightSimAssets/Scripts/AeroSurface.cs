using System;
using System.Collections.Generic;
using UnityEngine;

public enum ControlInputType { Pitch, Yaw, Roll, Flap }

public class AeroSurface : MonoBehaviour
{
    public float liftSlope = 6.28f;
    public float skinFriction = 0.02f;
    public float zeroLiftAoA = 0;
    public float stallAngleHigh = 15;
    public float stallAngleLow = -15;
    public float chord = 1;
    public float flapFraction = 0;
    public float span = 1;
    public float aspectRatio = 2;
    public bool IsControlSurface;
    public ControlInputType InputType;
    public float InputMultiplyer = 1;
    public Vector3 force;
    public Vector3 torque;
    public Vector3 coeffs;

    private float flapAngle;

    public void SetFlapAngle(float angle)
    {
        flapAngle = Mathf.Clamp(angle, -Mathf.Deg2Rad * 50, Mathf.Deg2Rad * 50);
    }

    public Vector3[] CalculateForces(Vector3 worldAirVelocity, float airDensity, Vector3 relativePosition)
    {
        Vector3[] forceAndTorque = {Vector3.zero, Vector3.zero};
        if (!gameObject.activeInHierarchy) return forceAndTorque;
        
        //nan check
        if (float.IsNaN(worldAirVelocity.x) || float.IsNaN(worldAirVelocity.y) || float.IsNaN(worldAirVelocity.z))
        {
            Debug.LogWarning("NaN detected in worldAirVelocity before CalculateForces is called.");
            worldAirVelocity = Vector3.zero; // or another fallback
        }


        // Accounting for aspect ratio effect on lift coefficient.
        float correctedLiftSlope = liftSlope * aspectRatio /
           (aspectRatio + 2 * (aspectRatio + 4) / (aspectRatio + 2));

        // Calculating flap deflection influence on zero lift angle of attack
        // and angles at which stall happens.
        float theta = Mathf.Acos(2 * flapFraction - 1);
        float flapEffectivness = 1 - (theta - Mathf.Sin(theta)) / Mathf.PI;
        float deltaLift = correctedLiftSlope * flapEffectivness * FlapEffectivnessCorrection(flapAngle) * flapAngle;

        float zeroLiftAoaBase = zeroLiftAoA * Mathf.Deg2Rad;
        float zeroLiftAoACorrected = zeroLiftAoaBase - deltaLift / correctedLiftSlope;

        float stallAngleHighBase = stallAngleHigh * Mathf.Deg2Rad;
        float stallAngleLowBase = stallAngleLow * Mathf.Deg2Rad;

        float clMaxHigh = correctedLiftSlope * (stallAngleHighBase - zeroLiftAoaBase) + deltaLift * LiftCoefficientMaxFraction(flapFraction);
        float clMaxLow = correctedLiftSlope * (stallAngleLowBase - zeroLiftAoaBase) + deltaLift * LiftCoefficientMaxFraction(flapFraction);

        float stallAngleHighCorrected = zeroLiftAoACorrected + clMaxHigh / correctedLiftSlope;
        float stallAngleLowCorrected = zeroLiftAoACorrected + clMaxLow / correctedLiftSlope;

        // Calculating air velocity relative to the surface's coordinate system.
        // Z component of the velocity is discarded. 
        Vector3 airVelocity = transform.InverseTransformDirection(worldAirVelocity);
        airVelocity = new Vector3(airVelocity.x, airVelocity.y);
        Vector3 dragDirection = transform.TransformDirection(airVelocity.normalized);
        Vector3 liftDirection = Vector3.Cross(dragDirection, transform.forward);

        float area = chord * span;
        float dynamicPressure = 0.5f * airDensity * airVelocity.sqrMagnitude;
        float angleOfAttack = Mathf.Atan2(airVelocity.y, -airVelocity.x);
        
        //nan check
        if (dynamicPressure == 0)
        {
            Debug.LogWarning("Dynamic pressure is zero, potential NaN risk in further calculations.");
            
        }

        //nan check
        if (float.IsNaN(angleOfAttack))
        {
            Debug.LogWarning("NaN detected in angleOfAttack calculation.");
            angleOfAttack = 0; 
        }
        
        Vector3 aerodynamicCoefficients = CalculateCoefficients(angleOfAttack,
                                                                correctedLiftSlope,
                                                                zeroLiftAoACorrected,
                                                                stallAngleHighCorrected,
                                                                stallAngleLowCorrected);      

        Vector3 lift = liftDirection * aerodynamicCoefficients.x * dynamicPressure * area;
        Vector3 drag = dragDirection * aerodynamicCoefficients.y * dynamicPressure * area;
        Vector3 torque = -transform.forward * aerodynamicCoefficients.z * dynamicPressure * area * chord;


        /*Debug.Log($"Lift: {lift}, Drag: {drag}, Torque: {torque}");
        if (float.IsNaN(lift.x) || float.IsNaN(drag.x) || float.IsNaN(torque.x))
        {
            Debug.LogError("NaN detected in calculated forces/torques.");
        }*/


        forceAndTorque[0] += lift + drag;
        forceAndTorque[1] += Vector3.Cross(relativePosition, forceAndTorque[0]);
        forceAndTorque[1] += torque;
        

        return forceAndTorque;  
        
    }

    private Vector3 CalculateCoefficients(float angleOfAttack,
                                          float correctedLiftSlope,
                                          float zeroLiftAoA,
                                          float stallAngleHigh, 
                                          float stallAngleLow)
    {
        Vector3 aerodynamicCoefficients;

        // Low angles of attack mode and stall mode curves are stitched together by a line segment. 
        float paddingAngleHigh = Mathf.Deg2Rad * Mathf.Lerp(15, 5, (Mathf.Rad2Deg * flapAngle + 50) / 100);
        float paddingAngleLow = Mathf.Deg2Rad * Mathf.Lerp(15, 5, (-Mathf.Rad2Deg * flapAngle + 50) / 100);
        float paddedStallAngleHigh = stallAngleHigh + paddingAngleHigh;
        float paddedStallAngleLow = stallAngleLow - paddingAngleLow;

        if (angleOfAttack < stallAngleHigh && angleOfAttack > stallAngleLow)
        {
            // Low angle of attack mode.
            aerodynamicCoefficients = CalculateCoefficientsAtLowAoA(angleOfAttack, correctedLiftSlope, zeroLiftAoA);
        }
        else
        {
            if (angleOfAttack > paddedStallAngleHigh || angleOfAttack < paddedStallAngleLow)
            {
                // Stall mode.
                aerodynamicCoefficients = CalculateCoefficientsAtStall(
                    angleOfAttack, correctedLiftSlope, zeroLiftAoA, stallAngleHigh, stallAngleLow);
            }
            else
            {
                // Linear stitching in-between stall and low angles of attack modes.
                Vector3 aerodynamicCoefficientsLow;
                Vector3 aerodynamicCoefficientsStall;
                float lerpParam;

                if (angleOfAttack > stallAngleHigh)
                {
                    aerodynamicCoefficientsLow = CalculateCoefficientsAtLowAoA(stallAngleHigh, correctedLiftSlope, zeroLiftAoA);
                    aerodynamicCoefficientsStall = CalculateCoefficientsAtStall(
                        paddedStallAngleHigh, correctedLiftSlope, zeroLiftAoA, stallAngleHigh, stallAngleLow);
                    lerpParam = (angleOfAttack - stallAngleHigh) / (paddedStallAngleHigh - stallAngleHigh);
                }
                else
                {
                    aerodynamicCoefficientsLow = CalculateCoefficientsAtLowAoA(stallAngleLow, correctedLiftSlope, zeroLiftAoA);
                    aerodynamicCoefficientsStall = CalculateCoefficientsAtStall(
                        paddedStallAngleLow, correctedLiftSlope, zeroLiftAoA, stallAngleHigh, stallAngleLow);
                    lerpParam = (angleOfAttack - stallAngleLow) / (paddedStallAngleLow - stallAngleLow);
                }
                aerodynamicCoefficients = Vector3.Lerp(aerodynamicCoefficientsLow, aerodynamicCoefficientsStall, lerpParam);
            }
        }
        return aerodynamicCoefficients;
    }

    private Vector3 CalculateCoefficientsAtLowAoA(float angleOfAttack,
                                                  float correctedLiftSlope,
                                                  float zeroLiftAoA)
    {
        float liftCoefficient = correctedLiftSlope * (angleOfAttack - zeroLiftAoA);
        float inducedAngle = liftCoefficient / (Mathf.PI * aspectRatio);
        float effectiveAngle = angleOfAttack - zeroLiftAoA - inducedAngle;

        float tangentialCoefficient = skinFriction * Mathf.Cos(effectiveAngle);
        
        float normalCoefficient = (liftCoefficient +
            Mathf.Sin(effectiveAngle) * tangentialCoefficient) / Mathf.Cos(effectiveAngle);
        float dragCoefficient = normalCoefficient * Mathf.Sin(effectiveAngle) + tangentialCoefficient * Mathf.Cos(effectiveAngle);
        float torqueCoefficient = -normalCoefficient * TorqCoefficientProportion(effectiveAngle);

        return new Vector3(liftCoefficient, dragCoefficient, torqueCoefficient);
    }

    private Vector3 CalculateCoefficientsAtStall(float angleOfAttack,
                                                 float correctedLiftSlope,
                                                 float zeroLiftAoA,
                                                 float stallAngleHigh,
                                                 float stallAngleLow)
    {
        float liftCoefficientLowAoA;
        if (angleOfAttack > stallAngleHigh)
        {
            liftCoefficientLowAoA = correctedLiftSlope * (stallAngleHigh - zeroLiftAoA);
        }
        else
        {
            liftCoefficientLowAoA = correctedLiftSlope * (stallAngleLow - zeroLiftAoA);
        }
        float inducedAngle = liftCoefficientLowAoA / (Mathf.PI * aspectRatio);

        float lerpParam;
        if (angleOfAttack > stallAngleHigh)
        {
            lerpParam = (Mathf.PI / 2 - Mathf.Clamp(angleOfAttack, -Mathf.PI / 2, Mathf.PI / 2))
                / (Mathf.PI / 2 - stallAngleHigh);
        }
        else
        {
            lerpParam = (-Mathf.PI / 2 - Mathf.Clamp(angleOfAttack, -Mathf.PI / 2, Mathf.PI / 2))
                / (-Mathf.PI / 2 - stallAngleLow);
        }
        inducedAngle = Mathf.Lerp(0, inducedAngle, lerpParam);
        float effectiveAngle = angleOfAttack - zeroLiftAoA - inducedAngle;

        float normalCoefficient = FrictionAt90Degrees(flapAngle) * Mathf.Sin(effectiveAngle) *
            (1 / (0.56f + 0.44f * Mathf.Abs(Mathf.Sin(effectiveAngle))) -
            0.41f * (1 - Mathf.Exp(-17 / aspectRatio)));
        float tangentialCoefficient = 0.5f * skinFriction * Mathf.Cos(effectiveAngle);

        float liftCoefficient = normalCoefficient * Mathf.Cos(effectiveAngle) - tangentialCoefficient * Mathf.Sin(effectiveAngle);
        float dragCoefficient = normalCoefficient * Mathf.Sin(effectiveAngle) + tangentialCoefficient * Mathf.Cos(effectiveAngle);
        float torqueCoefficient = -normalCoefficient * TorqCoefficientProportion(effectiveAngle);

        return new Vector3(liftCoefficient, dragCoefficient, torqueCoefficient);
    }

    private float TorqCoefficientProportion(float effectiveAngle)
    {
        return 0.25f - 0.175f * (1 - 2 * Mathf.Abs(effectiveAngle) / Mathf.PI);
    }

    private float FrictionAt90Degrees(float flapAngle)
    {
        return 1.98f - 4.26e-2f * flapAngle * flapAngle + 2.1e-1f * flapAngle;
    }

    private float FlapEffectivnessCorrection(float flapAngle)
    {
        return Mathf.Lerp(0.8f, 0.4f, (Mathf.Abs(flapAngle) * Mathf.Rad2Deg - 10) / 50);
    }

    private float LiftCoefficientMaxFraction(float flapFraction)
    {
        return Mathf.Clamp01(1 - 0.5f * (flapFraction - 0.1f) / 0.3f);
    }
}
