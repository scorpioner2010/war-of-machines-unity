using UnityEngine;

public sealed class PrimitiveTransportVehicle : MonoBehaviour
{
    private float maxSpeed;
    private float reverseSpeed;
    private float acceleration;
    private float braking;
    private float turnSpeed;
    private float currentSpeed;
    private Vector2 input;

    public float CurrentSpeed => currentSpeed;
    public float NormalizedSpeed => maxSpeed <= 0f ? 0f : Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
    public float Throttle01 => Mathf.Clamp01(Mathf.Abs(input.y));
    public float EngineDemand01 => Mathf.Clamp01(Mathf.Max(Throttle01, NormalizedSpeed * 0.72f));

    public void Configure(float configuredMaxSpeed, float configuredAcceleration, float configuredBraking, float configuredTurnSpeed)
    {
        maxSpeed = Mathf.Max(1f, configuredMaxSpeed);
        reverseSpeed = maxSpeed * 0.42f;
        acceleration = Mathf.Max(0.1f, configuredAcceleration);
        braking = Mathf.Max(0.1f, configuredBraking);
        turnSpeed = Mathf.Max(1f, configuredTurnSpeed);
    }

    public void Simulate(Vector2 requestedInput)
    {
        input = Vector2.ClampMagnitude(requestedInput, 1f);

        float targetSpeed = input.y >= 0f ? input.y * maxSpeed : input.y * reverseSpeed;
        float speedChange = Mathf.Abs(input.y) > 0.01f ? acceleration : braking;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChange * Time.deltaTime);

        if (Mathf.Abs(currentSpeed) > 0.05f)
        {
            float steeringSign = Mathf.Sign(currentSpeed);
            float steeringAmount = input.x * turnSpeed * Time.deltaTime * Mathf.Clamp01(Mathf.Abs(currentSpeed) / 2f);
            transform.Rotate(0f, steeringAmount * steeringSign, 0f, Space.Self);
        }

        transform.position += transform.forward * currentSpeed * Time.deltaTime;
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -27f, 27f);
        clampedPosition.z = Mathf.Clamp(clampedPosition.z, -17f, 17f);
        transform.position = clampedPosition;
    }
}
