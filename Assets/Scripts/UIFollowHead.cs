using UnityEngine;

public class UIFollowHead : MonoBehaviour
{
    [Header("Цель")]
    [Tooltip("Перетащи сюда Main Camera")]
    [SerializeField] private Transform headCamera;

    [Header("Параметры")]
    [SerializeField] private float distance = 2.0f; // Расстояние от глаз
    [SerializeField] private float smoothSpeed = 5.0f; // Скорость доводки

    void LateUpdate()
    {
        if (headCamera == null) return;

        // 1. Вычисляем целевую позицию перед лицом
        // Берем позицию головы + вектор взгляда * дистанцию
        Vector3 targetPosition = headCamera.position + (headCamera.forward * distance);

        // Можно зафиксировать высоту (Y), чтобы меню не улетало в небо, когда смотришь вверх
        // targetPosition.y = headCamera.position.y; // Раскомментируй, если хочешь залочить горизонт

        // 2. Вычисляем поворот (чтобы меню смотрело на игрока)
        Quaternion targetRotation = Quaternion.LookRotation(transform.position - headCamera.position);

        // 3. Плавно перемещаем (Lerp / Slerp)
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }
}
