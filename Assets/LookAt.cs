    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class LookAt : MonoBehaviour
    {
    // Ссылка на камеру, к которой будет поворачиваться объект
        private Transform mainCamera;

        void Start()
        {
            // Получаем основную камеру
            mainCamera = Camera.main.transform;
        }

        void Update()
        {
            // Получаем направление к камере
            Vector3 directionToCamera = mainCamera.position - transform.position;

            // Обнуляем направление по оси X и Z, чтобы вращаться только по Y
            directionToCamera.y = 0;

            // Если вектор не нулевой — вращаем объект
            if (directionToCamera != Vector3.zero)
            {
                // Поворачиваем объект в сторону камеры только по Y
                Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera);
                transform.rotation = targetRotation;
            }
        }
    }
