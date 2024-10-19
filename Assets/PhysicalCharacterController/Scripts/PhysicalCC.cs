using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PhysicalCC : MonoBehaviour
{
    public CharacterController cc { get; private set; }

    [Header("Ground Check")]
    public bool isGround;
    public float groundAngle;
    public Vector3 groundNormal { get; private set; }

    [Header("Movement")]
    public bool ProjectMoveOnGround;
    public Vector3 moveInput;
    private Vector3 moveVelocity;

    [Header("Slope and inertia")]
    public float slopeLimit = 45;
    public float inertiaDampingTime = 0.1f;
    public float slopeStartForce = 3f;
    public float slopeAcceleration = 3f;
    public Vector3 inertiaVelocity;

    [Header("interaction with the platform")]
    public bool platformAction;
    public Vector3 platformVelocity;

    [Header("Collision")]
    public bool applyCollision = true;
    public float pushForce = 55f;

    [Header("Waypoints")]
    public Vector3 pointA;  // Координата А
    public Vector3 pointB;  // Координата Б
    private Vector3 target;  // Текущая цель
    public float speed = 3f;  // Скорость NPC

    private void Start()
    {
        cc = GetComponent<CharacterController>();

        // Устанавливаем первую цель движения — точку А
        target = pointB;
    }

    private void Update()
    {
        GroundCheck();

        if (isGround)
        {
            // Направление к цели
            moveInput = (target - transform.position).normalized * speed;

            moveVelocity = ProjectMoveOnGround ? Vector3.ProjectOnPlane(moveInput, groundNormal) : moveInput;

            if (groundAngle < slopeLimit && inertiaVelocity != Vector3.zero)
                InertiaDamping();
        }

        GravityUpdate();

        Vector3 moveDirection = (moveVelocity + inertiaVelocity + platformVelocity);

        cc.Move(moveDirection * Time.deltaTime);

        // Проверка достижения цели
        if (Vector3.Distance(transform.position, target) < 0.5f)
        {
            // Меняем цель на противоположную
            target = (target == pointA) ? pointB : pointA;
        }
    }

    private void GravityUpdate()
    {
        if (isGround && groundAngle > slopeLimit)
        {
            inertiaVelocity += Vector3.ProjectOnPlane(groundNormal.normalized + (Vector3.down * (groundAngle / 30)).normalized * Mathf.Pow(slopeStartForce, slopeAcceleration), groundNormal) * Time.deltaTime;
        }
        else if (!isGround)
        {
            inertiaVelocity.y -= Mathf.Pow(3f, 3) * Time.deltaTime;
        }
    }

    private void InertiaDamping()
    {
        var a = Vector3.zero;

        // Торможение инерции при движении
        var resistanceAngle = Vector3.Angle(Vector3.ProjectOnPlane(inertiaVelocity, groundNormal),
        Vector3.ProjectOnPlane(moveVelocity, groundNormal));

        resistanceAngle = resistanceAngle == 0 ? 90 : resistanceAngle;

        inertiaVelocity = (inertiaVelocity + moveVelocity).magnitude <= 0.1f ? Vector3.zero : Vector3.SmoothDamp(inertiaVelocity, Vector3.zero, ref a, inertiaDampingTime / (3 / (180 / resistanceAngle)));
    }

    private void GroundCheck()
    {
        if (Physics.SphereCast(transform.position, cc.radius, Vector3.down, out RaycastHit hit, cc.height / 2 - cc.radius + 0.01f))
        {
            isGround = true;
            groundAngle = Vector3.Angle(Vector3.up, hit.normal);
            groundNormal = hit.normal;

            if (hit.transform.tag == "Platform")
                platformVelocity = hit.collider.attachedRigidbody == null || !platformAction ? Vector3.zero : hit.collider.attachedRigidbody.linearVelocity;

            if (Physics.BoxCast(transform.position, new Vector3(cc.radius / 2.5f, cc.radius / 3f, cc.radius / 2.5f),
                        Vector3.down, out RaycastHit helpHit, transform.rotation, cc.height / 2 - cc.radius / 2))
            {
                groundAngle = Vector3.Angle(Vector3.up, helpHit.normal);
            }
        }
        else
        {
            platformVelocity = Vector3.zero;
            isGround = false;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!applyCollision) return;

        Rigidbody body = hit.collider.attachedRigidbody;

        if (body == null || body.isKinematic) return;

        Vector3 pushDir = hit.point - (hit.point + hit.moveDirection.normalized);

        body.AddForce(pushDir * pushForce, ForceMode.Force);
    }
}
