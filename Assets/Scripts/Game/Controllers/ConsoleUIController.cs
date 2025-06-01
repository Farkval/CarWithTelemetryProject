using System.Collections.Generic;    // Для использования Queue<T>
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Game.Controllers
{
    /// <summary>
    /// ConsoleUIController реализует «консоль» внутри Canvas, используя пул строк на основе TMP_Text.
    /// </summary>
    public class ConsoleUIController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("RectTransform, внутри которого лежат все строки (VerticalLayoutGroup / ContentSizeFitter).")]
        [SerializeField] private RectTransform content;

        [Tooltip("Компонент ScrollRect, чтобы программно прокручивать вниз.")]
        [SerializeField] private ScrollRect scrollRect;

        [Tooltip("Префаб одной строки консоли (должен содержать TMP_Text/TextMeshProUGUI).")]
        [SerializeField] private TMP_Text linePrefab;

        [Header("Settings")]
        [Tooltip("Максимальное число одновременно отображаемых строк в консоли.")]
        [SerializeField] private int maxLines = 1000;

        // Очередь свободных (неактивных) строк из пула
        private Queue<TMP_Text> availableLines;

        // Очередь уже активных строк; порядок отражает появление: 
        // в начале — самый старый, в конце — самый новый.
        private Queue<TMP_Text> activeLines;

        private void Awake()
        {
            // Проверяем, что все ссылки назначены в инспекторе
            if (content == null)
            {
                Debug.LogError($"[{nameof(ConsoleUIController)}] Поле content не задано в инспекторе!");
                return;
            }
            if (scrollRect == null)
            {
                Debug.LogError($"[{nameof(ConsoleUIController)}] Поле scrollRect не задано в инспекторе!");
                return;
            }
            if (linePrefab == null)
            {
                Debug.LogError($"[{nameof(ConsoleUIController)}] Поле linePrefab не задано в инспекторе!");
                return;
            }
            if (maxLines <= 0)
            {
                Debug.LogWarning($"[{nameof(ConsoleUIController)}] maxLines задано ≤ 0; будет установлено значение 1.");
                maxLines = 1;
            }

            // Инициализируем наши очереди
            availableLines = new Queue<TMP_Text>(maxLines);
            activeLines = new Queue<TMP_Text>(maxLines);

            // Предварительно создаём пул из maxLines строк
            // Все они создаются как дочерние для content и сразу выключаются.
            for (int i = 0; i < maxLines; i++)
            {
                // Создаём копию префаба внутри content
                TMP_Text newLine = Instantiate(linePrefab, content);

                // Сразу выключаем – пока строка пуста и не должна отображаться
                newLine.gameObject.SetActive(false);

                // Переименуем для удобства в иерархии (необязательно)
                newLine.gameObject.name = $"ConsoleLine_{i}";

                // Кладём в очередь свободных
                availableLines.Enqueue(newLine);
            }
        }

        private void OnEnable()
        {
            // Подписываемся на событие добавления нового лога
            var logger = Utils.Logger.Instance;
            if (logger != null)
            {
                logger.OnLogAdded += HandleNewLog;

                // Выводим все уже накопленные логи при старте
                foreach (var line in logger.GetAllLogs())
                {
                    AddLogToUI(line);
                }

                // Обновляем Layout и скроллим вниз
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void OnDisable()
        {
            // Отписываемся от события
            if (Utils.Logger.Instance != null)
            {
                Utils.Logger.Instance.OnLogAdded -= HandleNewLog;
            }
        }

        /// <summary>
        /// Обработчик нового лога. Просто вызывает основную функцию добавления.
        /// </summary>
        private void HandleNewLog(string formatted)
        {
            AddLogToUI(formatted);

            // После добавления принудительно пересчитываем Layout и скроллим вниз
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        /// <summary>
        /// Основная логика: берём строку из пула (или перезаписываем старейшую), 
        /// заполняем текстом и цветом, делаем активной и ставим в конец списка.
        /// </summary>
        private void AddLogToUI(string formatted)
        {
            TMP_Text lineToUse;

            if (availableLines.Count > 0)
            {
                // Если есть свободный объект — берём его
                lineToUse = availableLines.Dequeue();

                // Делаем активным, так как он был выключен ранее
                lineToUse.gameObject.SetActive(true);
            }
            else
            {
                // Пула нет — все строки уже отображаются. Перезаписываем самую старую.
                lineToUse = activeLines.Dequeue(); // Берём первую (старейшую) по очереди

                // Опционально: убедимся, что он активен (скорее всего, он уже активен)
                if (!lineToUse.gameObject.activeSelf)
                {
                    lineToUse.gameObject.SetActive(true);
                }
            }

            // Настраиваем текст и цвет
            lineToUse.text = FormatString(formatted);
            lineToUse.color = DetermineColor(formatted);

            // Ставим эту строку в конец контента (чтобы она отображалась самой последней)
            lineToUse.transform.SetAsLastSibling();

            // После этого добавляем/перезаписываем её в очередь активных
            activeLines.Enqueue(lineToUse);
        }

        /// <summary>
        /// Определяет цвет строки по префиксу: [ERROR] → красный, [WARN] → оранжевый, иначе — белый.
        /// </summary>
        private Color DetermineColor(string formatted)
        {
            if (formatted.StartsWith("[ERROR]"))
                return Color.red;
            else if (formatted.StartsWith("[WARN]"))
                return new Color(1f, 0.5f, 0f);
            else
                return Color.white;
        }

        /// <summary>
        /// Вспомогательная функция: меняет обратные слэши на прямые (\\ → /).
        /// </summary>
        private string FormatString(string text)
        {
            if (text.Contains("\\"))
                return text.Replace('\\', '/');
            return text;
        }
    }
}
