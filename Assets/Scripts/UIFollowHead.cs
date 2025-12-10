using UnityEngine;

public class UIFollowHead : MonoBehaviour
{
    [Header("Цель")]
    [SerializeField] private Transform headCamera;

    [Header("Параметры")]
    [Tooltip("Как далеко меню от лица")]
    [SerializeField] private float distance = 2.0f;

    [Tooltip("Скорость плавности (чем меньше, тем медленнее)")]
    [SerializeField] private float smoothSpeed = 5.0f;

    [Header("Смещение")]
    [Tooltip("Сдвиг по вертикали относительно уровня глаз. Поставь 0.2 или 0.3, чтобы поднять над каяком.")]
    [SerializeField] private float heightOffset = 0.0f;

    void LateUpdate()
    {
        if (headCamera == null) return;

        // ШАГ 1: Определяем, куда смотрит игрок, но ИГНОРИРУЕМ наклон вверх/вниз
        Vector3 forwardDirection = headCamera.forward;
        forwardDirection.y = 0; // <-- БЛОКИРОВКА ГОРИЗОНТА (Убиваем наклон)
        forwardDirection.Normalize(); // Делаем вектор снова длиной в 1 метр

        // ШАГ 2: Вычисляем позицию
        // Берем позицию головы + Вектор горизонта * дистанцию
        Vector3 targetPosition = headCamera.position + (forwardDirection * distance);

        // ШАГ 3: Применяем ручное смещение по высоте
        // Меню будет всегда на уровне глаз (headCamera.position.y) + твой отступ
        targetPosition.y = headCamera.position.y + heightOffset;

        // ШАГ 4: Поворот
        // Меню всегда смотрит на игрока, но не наклоняется (так как forwardDirection плоский)
        Quaternion targetRotation = Quaternion.LookRotation(forwardDirection);

        // ШАГ 5: Плавное движение
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }
}