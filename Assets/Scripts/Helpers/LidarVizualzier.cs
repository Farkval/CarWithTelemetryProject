using Assets.Scripts.Robot.Sensors;
using Assets.Scripts.Sensors.Models;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Helpers
{
    /// <summary>
    /// Примерный скрипт визуализации данных лидара в небольшом окне HUD.
    /// </summary>
    public class LidarVisualizer : MonoBehaviour
    {
        [Header("Lidar Reference")]
        // Ссылка на объект лидара. Можно назначить в инспекторе объект с MechanicalLidar / FlashLidar / MemsLidar.
        public MonoBehaviour lidarComponent;

        // Примечание: т.к. все наши лидары реализуют ILidarSensor,
        // мы можем получить интерфейс и работать с ним универсально.
        private ILidarSensor lidarSensor;

        [Header("UI Settings")]
        // Ссылка на RawImage в Canvas, где мы будем отображать нашу картинку
        public RawImage lidarImage;

        [Tooltip("Размер текстуры в пикселях (ширина и высота).")]
        public int textureSize = 128;

        [Tooltip("Максимальная дистанция лидара (должна совпадать или быть чуть больше, чем maxDistance в самом лидаре).")]
        public float maxDistance = 10f;

        [Tooltip("Какое расстояние будет \"центром\" миникарты (одна половина текстуры).")]
        public float mapExtent = 10f;

        // Приватные поля
        private Texture2D tex;
        private Color[] pixels;

        private void Start()
        {
            // Получаем интерфейс ILidarSensor, если lidarComponent указывает на скрипт, реализующий его
            lidarSensor = lidarComponent as ILidarSensor;
            if (lidarSensor == null)
            {
                Debug.LogError("LidarVisualizer: указан объект, не реализующий ILidarSensor!");
                enabled = false;
                return;
            }

            // Создаём текстуру
            tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            // Массовый массив пикселей (для быстрой заливки)
            pixels = new Color[textureSize * textureSize];

            // Назначаем текстуру нашему UI-объекту
            if (lidarImage != null)
            {
                lidarImage.texture = tex;
            }
        }

        private void Update()
        {
            if (lidarSensor == null) return;

            // Очищаем картинку в чёрный / тёмный
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.black;
            }

            // Получаем облако точек из лидара
            List<LidarPoint> cloud = lidarSensor.PointCloud;

            // Позиция и ориентация робота (или самого лидара)
            // Обычно это transform.position, transform.forward и т.д. (если скрипт висит на том же объекте, где сам лидар).
            // Но можно брать напрямую из lidarComponent.transform
            Transform lidarTransform = (lidarComponent as MonoBehaviour).transform;

            foreach (var p in cloud)
            {
                // Мировые координаты точки
                Vector3 worldPos = p.WorldPosition;
                // Переведём в локальное пространство робота/лидара, чтобы получить x,z относительно центра
                Vector3 localPos = lidarTransform.InverseTransformPoint(worldPos);

                // Мы хотим рисовать "вид сверху": (x, z)
                float px = localPos.x;
                float pz = localPos.z;

                // Расстояние до точки
                float dist = p.Distance;
                // Или можно считать dist = new Vector2(px, pz).magnitude, если хотим визуализировать именно локальный радиус

                // Обрежем точки, выходящие за пределы нашей миникарты
                // Если px или pz слишком велики, пропускаем
                if (Mathf.Abs(px) > mapExtent || Mathf.Abs(pz) > mapExtent)
                    continue;

                // Преобразуем (px, pz) к координатам в текстуре [0..textureSize-1]
                // Допустим, центр текстуры = (textureSize/2, textureSize/2)
                // и mapExtent по сути половина стороны области
                float halfSize = textureSize / 2f;
                float scale = (textureSize / 2f) / mapExtent;  // масштаб: реальное число метров -> пиксели

                int texX = Mathf.RoundToInt(halfSize + px * scale);
                int texY = Mathf.RoundToInt(halfSize + pz * scale);

                if (texX < 0 || texX >= textureSize || texY < 0 || texY >= textureSize)
                    continue; // за границами текстуры

                // Цвет точки: от красного (близко) к зелёному (далеко)
                // Допустим 0м = красный, maxDistance = зелёный
                float t = Mathf.InverseLerp(0f, maxDistance, dist);
                // Хотим небольшой градиент через желтый:
                //  t=0 => красный
                //  t=0.5 => желтый
                //  t=1 => зеленый
                Color colorNear = Color.red;
                Color colorMid = Color.yellow;
                Color colorFar = Color.green;

                // Два варианта:
                // 1) Лерп через два этапа (0..0.5, 0.5..1)
                // 2) Лерп в три этапа вручную
                // Для простоты: если t < 0.5, берем красный->желтый, иначе желтый->зелёный
                Color c;
                if (t < 0.5f)
                {
                    float localT = t / 0.5f;  // нормируем на [0..1]
                    c = Color.Lerp(colorNear, colorMid, localT);
                }
                else
                {
                    float localT = (t - 0.5f) / 0.5f;
                    c = Color.Lerp(colorMid, colorFar, localT);
                }

                // Запишем пиксель
                int index = texY * textureSize + texX;
                pixels[index] = c;
            }

            // Применяем изменения
            tex.SetPixels(pixels);
            tex.Apply(false);
        }
    }
}
