using UnityEngine;

public sealed class TransportDemoCamera : MonoBehaviour
{
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 8.5f, -12f);
    [SerializeField] private float positionSmoothTime = 0.22f;
    [SerializeField] private float rotationSmoothSpeed = 7f;

    private Transform target;
    private Vector3 positionVelocity;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + target.TransformDirection(followOffset);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);
        Vector3 lookDirection = target.position + Vector3.up * 0.8f - transform.position;
        if (lookDirection.sqrMagnitude > 0.01f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed * Time.deltaTime);
        }
    }
}
