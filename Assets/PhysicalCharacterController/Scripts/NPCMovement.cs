using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCMovement : MonoBehaviour
{
    interface IState {
        bool IsMoving();
        bool IsWaiting();
        bool IsRunning();

        float Speed();

        string ToString();
    }

    class MovingState : IState {
        public bool IsMoving() => true;
        public bool IsWaiting() => false;
        public bool IsRunning() => false;

        public float Speed() {
            return 5;
        }

        public string ToString() {
            return "Moving";
        }
    }

    class WaitingState : IState {
        public bool IsMoving() => false;
        public bool IsWaiting() => true;
        public bool IsRunning() => false;

        public GameObject vehicle = null;
        public float waitTime = 0.0f;

        public float Speed() {
            return 0;
        }

        public string ToString() {
            return "Waiting";
        }
    }

    class RunningState : IState {
        public bool IsMoving() => false;
        public bool IsWaiting() => false;
        public bool IsRunning() => true;

        public float Speed() {
            return 5 * 2;
        }

        public Vector3 Direction = Vector3.zero;
        public float waitTime = 0.0f;

        public string ToString() {
            return "Running";
        }
    }

    public Transform[] waypoints;    // Точки пути
    public float initialSpeed = 5f;         // Скорость передвижения
    public float turnSpeed = 5f;     // Скорость поворота
    public float detectionDistance = 4; // Дистанция для Raycast'ов
    public LayerMask obstacleLayer;  // Слой для проверки препятствий

    private int currentWaypointIndex = 0;
    private Vector3 cachedDirection; // Кэшируемый результат Raycast
    private IState state = new MovingState();

    void Update()
    {
        if (state.IsWaiting()) {
            var waitingState = (WaitingState)state;
            waitingState.waitTime += Time.deltaTime;

            Vector3 direction = GetCurrentDirection();
            Vector3 leftRayDirection = Quaternion.Euler(0, -90, 0) * direction;
            Vector3 rightRayDirection = Quaternion.Euler(0, 90, 0) * direction;
            Vector3 backRayDirection = Quaternion.Euler(0, 180, 0) * direction;

            //  проверим не пропал ли объект ожидания
            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, detectionDistance, obstacleLayer)) {
                Debug.DrawRay(transform.position, direction * hit.distance, Color.red);
            } else {    //   нет препятствий
                state = new MovingState();
                Debug.Log("Waiting -> Moving");
                return;
            }

            //   проверим наличие опасности слева
            if (Physics.Raycast(transform.position, leftRayDirection, out hit, detectionDistance * 2, obstacleLayer)) {
                Debug.DrawRay(transform.position, leftRayDirection * hit.distance, Color.red);
                state = new RunningState() {
                    Direction = backRayDirection,
                };
                Debug.Log("Waiting -> Running, backRayDirection =" + backRayDirection.ToString());
                return;
            }

            //   проверим наличие опасности справа
            if (Physics.Raycast(transform.position, rightRayDirection, out hit, detectionDistance * 2, obstacleLayer)) {
                Debug.DrawRay(transform.position, rightRayDirection * hit.distance, Color.red);
                state = new RunningState() {
                    Direction = backRayDirection,
                };
                Debug.Log("Waiting -> Running, backRayDirection =" + backRayDirection.ToString());
                return;
            }

            if (waitingState.waitTime > 2.0f) {
                state = new MovingState();
                Debug.Log("Waiting -> Moving");
                return;
            }
        } else if (state.IsMoving()) {
            MoveTowardsWaypoint(GetCurrentDirection());
        } else if (state.IsRunning()) {
            var runningState = (RunningState)state;
            Vector3 direction = runningState.Direction;
            MoveTowardsWaypoint(direction);
            runningState.waitTime += Time.deltaTime;
            if (runningState.waitTime > 1.0f) {
                state = new MovingState();
                Debug.Log("Running -> Moving");
                return;
            }
        }
    }

    void OnGUI() {
        // string labelText = "Hello, World!";  // Текст метки
        Vector2 labelPosition = new Vector2(10, 10);  // Позиция метки на экране
        int labelWidth = 200;
        int labelHeight = 50;
        GUI.Label(new Rect(labelPosition.x, labelPosition.y, labelWidth, labelHeight), state.ToString());
    }

    private Vector3 GetCurrentDirection() {
        return (waypoints[currentWaypointIndex].position - transform.position).normalized;
    }

    // Функция движения к текущей точке
    private void MoveTowardsWaypoint(Vector3 direction)
    {        
        // Проверяем на препятствия и корректируем направление
        direction = DetectObstacles(direction);
        
        // Плавный поворот в направлении движения
        if (direction != Vector3.zero) {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // Двигаемся вперед с постоянной скоростью
        var originalSpeed = state.Speed();
        var affectedSpeed = Random.Range(originalSpeed - 1, originalSpeed + 1);

        transform.Translate(Vector3.forward * affectedSpeed * Time.deltaTime);
        // Если достигли текущего waypoint'а, переходим к следующему
        if (Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position) < 1f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    // Функция проверки препятствий с помощью Raycast
    private Vector3 DetectObstacles(Vector3 direction)
    {
        // Если закэшированный результат еще валиден, используем его
        if (cachedDirection != Vector3.zero)
        {
            return cachedDirection;
        }

        Vector3 leftRayDirection = Quaternion.Euler(0, -45, 0) * direction;
        Vector3 rightRayDirection = Quaternion.Euler(0, 45, 0) * direction;
        Vector3 left90RayDirection = Quaternion.Euler(0, -90, 0) * direction;
        Vector3 right90RayDirection = Quaternion.Euler(0, 90, 0) * direction;
        Vector3 backRayDirection = Quaternion.Euler(0, 180, 0) * direction;

        RaycastHit hit;

        if (Physics.Raycast(transform.position, left90RayDirection, out hit, detectionDistance * 2, obstacleLayer)) {
            Debug.DrawRay(transform.position, left90RayDirection * hit.distance, Color.red);
            var tag = hit.collider.tag;
            if (tag == "Vehicle") {
                Debug.Log("Danger from left: " + tag);
                state = new RunningState();
                Debug.Log("Moving -> Running");
                return backRayDirection;
            }
        }

        if (Physics.Raycast(transform.position, right90RayDirection, out hit, detectionDistance * 2, obstacleLayer)) {
            Debug.DrawRay(transform.position, right90RayDirection * hit.distance, Color.red);
            var tag = hit.collider.tag;
            if (tag == "Vehicle") {
                Debug.Log("Danger from right: " + tag);
                state = new RunningState();
                Debug.Log("Moving -> Running");
                return backRayDirection;
            }
        }

        // Прямой Raycast
        if (Physics.Raycast(transform.position, direction, out hit, detectionDistance, obstacleLayer))
        {
            Debug.DrawRay(transform.position, direction * hit.distance, Color.red);

            var collider = hit.collider;
            var tag = collider.tag;
            // Debug.Log("Collider: " + collider.name + ", tag = " + tag);

            if (tag == "Vehicle") {
                // speed = 0;
                state = new WaitingState();
                Debug.Log("Moving -> Waiting");
                return direction;
            } else {
                Debug.Log("Front building found");
            }
            // Если есть препятствие, меняем направление
            direction += hit.normal * 5f;
        }
        else
        {
            Debug.DrawRay(transform.position, direction * detectionDistance, Color.green);
        }

        // Случайно решаем, проверять сначала левый или правый Raycast
        if (Random.value > 0.5f)
        {
            // Сначала проверяем левый, потом правый
            direction = CheckRaycast(leftRayDirection, rightRayDirection, direction);
        }
        else
        {
            // Сначала проверяем правый, потом левый
            direction = CheckRaycast(rightRayDirection, leftRayDirection, direction);
        }

        // Кэшируем результат направления
        cachedDirection = direction.normalized;
        StartCoroutine(ResetCachedDirection());

        return cachedDirection;
    }

    private Vector3 CheckRaycast(Vector3 firstDirection, Vector3 secondDirection, Vector3 direction)
    {
        RaycastHit hit;
        
        // Первый Raycast
        if (Physics.Raycast(transform.position, firstDirection, out hit, detectionDistance, obstacleLayer))
        {
            Debug.DrawRay(transform.position, firstDirection * hit.distance, Color.red);
            direction += hit.normal * 5f;
        }
        else
        {
            Debug.DrawRay(transform.position, firstDirection * detectionDistance, Color.green);
        }

        // Второй Raycast
        if (Physics.Raycast(transform.position, secondDirection, out hit, detectionDistance, obstacleLayer))
        {
            Debug.DrawRay(transform.position, secondDirection * hit.distance, Color.red);
            direction += hit.normal * 5f;
        }
        else
        {
            Debug.DrawRay(transform.position, secondDirection * detectionDistance, Color.green);
        }

        return direction;
    }

    // Функция сброса кэшированного направления для оптимизации
    IEnumerator ResetCachedDirection()
    {
        yield return new WaitForSeconds(0.1f); // Полсекунды перед сбросом
        cachedDirection = Vector3.zero;
    }
}


/*[RequireComponent(typeof(CharacterController))]
public class NPCMovement : MonoBehaviour
{
    public CharacterController cc { get; private set; }

    [Header("Ground Check")]
    public bool isGround;
    public float groundAngle;
    public Vector3 groundNormal { get; private set; }

    [Header("Movement")]
    public float speed = 3f;  // Скорость NPC
    private Vector3 moveVelocity;

    [Header("Obstacle Avoidance")]
    public float detectionDistance = 2f; // Дистанция для проверки на препятствие
    public LayerMask obstacleLayer;      // Слой препятствий
    public float avoidanceSpeed = 2f;    // Скорость обхода препятствий

    [Header("Waypoints")]
    public Transform[] waypoints;  // Массив объектов-целей
    private int currentWaypointIndex = 0;  // Индекс текущей цели

    private void Start()
    {
        cc = GetComponent<CharacterController>();

        // Устанавливаем начальную цель — первый элемент массива waypoints
        if (waypoints.Length > 0)
        {
            currentWaypointIndex = 0;
        }
    }

    private void Update()
    {
        GroundCheck();

        if (isGround)
        {
            MoveTowardsWaypoint();
        }

        GravityUpdate();
        
        cc.Move(moveVelocity * Time.deltaTime); // Движение с использованием CharacterController

        // Проверка достижения текущей цели
        if (waypoints.Length > 0 && Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position) < 0.5f)
        {
            // Переход к следующей цели
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    private void MoveTowardsWaypoint()
    {
        if (waypoints.Length == 0) return;

        // Debug.Log("MoveTowardsWaypoint");

        // Получаем текущую точку назначения
        Vector3 targetPosition = waypoints[currentWaypointIndex].position;
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;

        // Проверяем наличие препятствий
        if (IsObstacleInFront(directionToTarget))
        {
            // Если есть препятствие, изменяем направление
            Vector3 avoidanceDirection = GetAvoidanceDirection(directionToTarget);
            moveVelocity = avoidanceDirection * avoidanceSpeed;
        }
        else
        {
            // Если препятствий нет, движемся к цели
            moveVelocity = directionToTarget * speed;
        }

        Debug.Log("MoveVelocity: " + moveVelocity.magnitude);
    }

    private bool IsObstacleInFront(Vector3 direction)
    {
        // Проверка на наличие препятствия прямо перед NPC
        return Physics.Raycast(transform.position, direction, detectionDistance, obstacleLayer);
    }

    private Vector3 GetAvoidanceDirection(Vector3 originalDirection)
    {
        // Пытаемся найти обходное направление, используя несколько проверок
        Vector3 left = Quaternion.Euler(0, -90, 0) * originalDirection; // Налево
        Vector3 right = Quaternion.Euler(0, 90, 0) * originalDirection; // Направо
        Vector3 forward = originalDirection; // Вперед

        // Проверяем, есть ли препятствия слева, справа и впереди
        bool frontClear = !IsObstacleInFront(forward);
        bool leftClear = !IsObstacleInFront(left);
        bool rightClear = !IsObstacleInFront(right);

        Debug.Log("Front: " + frontClear + ", Left: " + leftClear + ", Right: " + rightClear);

        // Определяем направление обхода
        if (frontClear && (leftClear || rightClear))
        {
            // Если впереди свободно и одно из боковых направлений свободно, идем вперед
            var result = forward * speed;
            Debug.Log("Going forward for " + result.magnitude + " units");            
            return result;
        }
        else if (leftClear)
        {
            var result = left * avoidanceSpeed;
            Debug.Log("Going left for " + result.magnitude + " units");
            return result; // Если слева свободно, идем налево
        }
        else if (rightClear)
        {
            var result = right * avoidanceSpeed;
            Debug.Log("Going right for " + result.magnitude + " units");
            return result; // Если справа свободно, идем направо
        }
        else
        {
            Debug.Log("No way to go");
            return -originalDirection * avoidanceSpeed;
        }
    }

    private void GravityUpdate()
    {
        if (!isGround)
        {
            moveVelocity.y -= 9.81f * Time.deltaTime; // Гравитация, когда NPC не на земле
        }
    }

    private void GroundCheck()
    {
        if (Physics.SphereCast(transform.position, cc.radius, Vector3.down, out RaycastHit hit, cc.height / 2 - cc.radius + 0.01f))
        {
            isGround = true;
            groundAngle = Vector3.Angle(Vector3.up, hit.normal);
            groundNormal = hit.normal;
        }
        else
        {
            isGround = false;
        }
    }
}*/
