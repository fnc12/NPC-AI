using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VikingCrew.Tools.UI; 

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
    private float heyCooldown = 0;
    struct Message {
        public string text;
        public SpeechBubbleManager.SpeechbubbleType type;
    }
    private Message[] heyMessages = new Message[] {
        new Message { text = "Hey!", type = SpeechBubbleManager.SpeechbubbleType.ANGRY },
        new Message { text = "Hi!", type = SpeechBubbleManager.SpeechbubbleType.ANGRY },
        new Message { text = "Hey, what's up?", type = SpeechBubbleManager.SpeechbubbleType.ANGRY },
        new Message { text = "Hey, how are you?", type = SpeechBubbleManager.SpeechbubbleType.ANGRY },
        new Message { text = "Hey, how's it going?", type = SpeechBubbleManager.SpeechbubbleType.ANGRY },
        new Message { text = "Hey, what's up?", type = SpeechBubbleManager.SpeechbubbleType.ANGRY },
        new Message { text = "Hey, how are you?", type = SpeechBubbleManager.SpeechbubbleType.ANGRY },
        new Message { text = "Hey, how's it going?", type = SpeechBubbleManager.SpeechbubbleType.ANGRY },
        new Message { text = "Cutie!", type = SpeechBubbleManager.SpeechbubbleType.NORMAL }, 
        new Message { text = "I know her!", type = SpeechBubbleManager.SpeechbubbleType.NORMAL }, 
        new Message { text = "I know him!", type = SpeechBubbleManager.SpeechbubbleType.NORMAL }, 
        new Message { text = "Cool hair!", type = SpeechBubbleManager.SpeechbubbleType.NORMAL }, 
        // new Message { text = "Cutie!", type = SpeechBubbleManager.SpeechbubbleType.NORMAL }, 
    };

    void Start() {
        
    }

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

            if (heyCooldown <= 0) {
                var allPlayers = GameObject.FindGameObjectsWithTag("Player");
                var targetSquaredDistance = 1.5f * 1.5f;
                foreach (var player in allPlayers) {
                    if (player == gameObject) continue;
                    var otherTransform = player.transform;
                    var squaredDistance = (transform.position - otherTransform.position).sqrMagnitude;
                    if (squaredDistance < targetSquaredDistance) {
                        if (Random.value < 0.5f) {
                            var timeToLive = 1.5f;
                            var heyMessage = heyMessages[Random.Range(0, heyMessages.Length)];
                            Say(heyMessage.text, timeToLive, heyMessage.type);
                            heyCooldown = timeToLive;
                        }
                        break;
                    }
                }
            }
        } else if (state.IsRunning()) {
            var runningState = (RunningState)state;
            Vector3 direction = runningState.Direction;
            MoveTowardsWaypoint(direction);
            runningState.waitTime += Time.deltaTime;
            if (runningState.waitTime > 1.0f) {
                state = new MovingState();
                Debug.Log("Running -> Moving");
                if (Random.value < 0.5f) {
                    Say("Phew!", 1.5f);
                }
                return;
            }
        }

        if (heyCooldown > 0) {
            heyCooldown -= Time.deltaTime;
            if (heyCooldown < 0) {
                heyCooldown = 0;
            }
        }
    }

    private void Say(string text, float timeToLive = 1.5f, SpeechBubbleManager.SpeechbubbleType type = SpeechBubbleManager.SpeechbubbleType.NORMAL) {
        SpeechBubbleManager.Instance.AddSpeechBubble(transform, text, type, timeToLive);
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
        if ((transform.position - waypoints[currentWaypointIndex].position).sqrMagnitude < 1f)
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
