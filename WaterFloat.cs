using UnityEngine;
using System.Collections.Generic;

public class WaterFloat : MonoBehaviour
{
    [Header("Water Settings")]
    [Tooltip("The collider representing the water volume (its top is the water surface).")]
    public Collider waterCollider;
    [Tooltip("Density of water in kg/m^3 (typically 1000 for fresh water).")]
    public float waterDensity = 1000f;
    [Tooltip("Multiplier for drag applied to submerged objects.")]
    public float waterDragMultiplier = 1f;
    [Tooltip("Optional water flow (in world space) that affects drag.")]
    public Vector3 waterFlow = Vector3.zero;

    [Header("Buoyancy Spring Settings")]
    [Tooltip("Spring constant for the buoyancy force. Lower for a softer vertical response.")]
    public float springConstant = 20f;
    [Tooltip("Damping constant to smooth vertical oscillations.")]
    public float dampingConstant = 0.1f;

    [Header("Rotational Oscillation Settings (Pitch & Roll Only)")]
    [Tooltip("Spring constant for aligning pitch and roll to level (preserving yaw).")]
    public float rotationalSpringConstant = 5f;
    [Tooltip("Damping coefficient for rotational oscillations (pitch and roll).")]
    public float rotationalDampingCoefficient = 0.2f;


    private readonly Dictionary<Rigidbody, BuoyancyBody> buoyantBodies = new Dictionary<Rigidbody, BuoyancyBody>();

    private float waterSurface;

    void Start()
    {
        if (waterCollider != null)
            waterSurface = waterCollider.bounds.max.y;
        else
            waterSurface = transform.position.y;
    }

    void OnTriggerEnter(Collider col)
    {
        Rigidbody rb = col.attachedRigidbody;
        if (rb == null)
            return;

        if (!buoyantBodies.ContainsKey(rb))
            buoyantBodies.Add(rb, new BuoyancyBody(rb, col));
    }

    void OnTriggerExit(Collider col)
    {
        Rigidbody rb = col.attachedRigidbody;
        if (rb == null)
            return;

        if (buoyantBodies.TryGetValue(rb, out BuoyancyBody body))
        {
            body.Reset();
            buoyantBodies.Remove(rb);
        }
    }

    void FixedUpdate()
    {
        foreach (var kvp in buoyantBodies)
        {
            kvp.Value.ApplyBuoyancy(
                waterSurface,
                waterDensity,
                waterFlow,
                waterDragMultiplier,
                springConstant,
                dampingConstant,
                rotationalSpringConstant,
                rotationalDampingCoefficient
            );
        }
    }
}

public class BuoyancyBody
{
    private readonly Rigidbody rb;
    private readonly Collider col;
    private readonly float originalDrag;
    private readonly float originalAngularDrag;

    public BuoyancyBody(Rigidbody rb, Collider col)
    {
        this.rb = rb;
        this.col = col;
        originalDrag = rb.drag;
        originalAngularDrag = rb.angularDrag;
    }

    public void Reset()
    {
        rb.drag = originalDrag;
        rb.angularDrag = originalAngularDrag;
    }

    public void ApplyBuoyancy(
        float waterSurface,
        float waterDensity,
        Vector3 waterFlow,
        float waterDragMultiplier,
        float springConstant,
        float dampingConstant,
        float rotationalSpringConstant,
        float rotationalDampingCoefficient)
    {
        Bounds bounds = col.bounds;
        float objectHeight = bounds.size.y;
        float effectiveVolume = bounds.size.x * bounds.size.y * bounds.size.z;

        float submersion = Mathf.Clamp01((waterSurface - bounds.min.y) / objectHeight);
        if (submersion <= 0f)
            return;

        float equilibriumSubmersion = rb.mass / (waterDensity * effectiveVolume);

        float error = submersion - equilibriumSubmersion;
        float springForce = springConstant * error;
        float dampingForce = -dampingConstant * rb.velocity.y;
        float totalBuoyantForce = springForce + dampingForce;

        rb.AddForce(Vector3.up * totalBuoyantForce, ForceMode.Force);


        Vector3 relativeVelocity = rb.velocity - waterFlow;
        Vector3 dragForce = -relativeVelocity * waterDragMultiplier * submersion;
        rb.AddForce(dragForce, ForceMode.Force);


        Quaternion currentRotation = rb.rotation;

        Vector3 currentForward = rb.transform.forward;
        Vector3 projectedForward = Vector3.ProjectOnPlane(currentForward, Vector3.up).normalized;
        if (projectedForward.sqrMagnitude < 0.0001f)
            projectedForward = rb.transform.forward;


        Quaternion targetRotation = Quaternion.LookRotation(projectedForward, Vector3.up);

        Quaternion deltaRotation = targetRotation * Quaternion.Inverse(currentRotation);
        deltaRotation.ToAngleAxis(out float angleInDegrees, out Vector3 axis);
        if (angleInDegrees > 180f)
            angleInDegrees = 360f - angleInDegrees;
        if (angleInDegrees > 0.01f)
        {
            float angleInRadians = angleInDegrees * Mathf.Deg2Rad;

            Vector3 correctiveTorque = axis.normalized * angleInRadians * rotationalSpringConstant;

            Vector3 horizontalAngularVelocity = Vector3.ProjectOnPlane(rb.angularVelocity, Vector3.up);
            Vector3 dampingTorque = -horizontalAngularVelocity * rotationalDampingCoefficient;
            rb.AddTorque(correctiveTorque + dampingTorque, ForceMode.Force);
        }
    }
}
