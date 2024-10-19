using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CharacterMover : MonoBehaviour
{
    public Transform pointA; // Точка A
    public Transform pointB; // Точка B
    private NavMeshAgent agent; // NavMeshAgent

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        MoveToNextPoint(); // Начинаем движение к первой точке
    }

    void MoveToNextPoint()
    {
        // Выбираем следующую точку
        if (agent.destination == pointA.position)
        {
            agent.SetDestination(pointB.position);
        }
        else
        {
            agent.SetDestination(pointA.position);
        }
    }

    void Update()
    {
        // Проверяем, достиг ли агент цели
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            MoveToNextPoint(); // Переключаемся на следующую точку
        }
    }
}
