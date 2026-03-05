using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Game.Controllers
{
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
        [Tooltip("Это внутренние логи симулятора.")]
        [SerializeField] private bool isGameLog = false;

        private Queue<TMP_Text> availableLines;

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

            availableLines = new Queue<TMP_Text>(maxLines);
            activeLines = new Queue<TMP_Text>(maxLines);

            for (int i = 0; i < maxLines; i++)
            {
                TMP_Text newLine = Instantiate(linePrefab, content);

                newLine.gameObject.SetActive(false);

                newLine.gameObject.name = $"ConsoleLine_{i}";

                availableLines.Enqueue(newLine);
            }
        }

        private void OnEnable()
        {
            var logger = Utils.Logger.Instance;
            if (logger != null)
            {
                if (!isGameLog)
                    logger.OnLogAdded += HandleNewLog;
                else
                    logger.OnGameLogAdded += HandleNewLog;

                ClearConsoleUI();

                foreach (var line in logger.GetAllLogs(isGameLog))
                {
                    AddLogToUI(line, true);
                }

                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void OnDisable()
        {
            if (Utils.Logger.Instance != null)
            {
                if (!isGameLog)
                    Utils.Logger.Instance.OnLogAdded -= HandleNewLog;
                else
                    Utils.Logger.Instance.OnGameLogAdded -= HandleNewLog;
            }
        }

        private void HandleNewLog(string formatted)
        {
            AddLogToUI(formatted);

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        private void AddLogToUI(string formatted, bool clear = false)
        {
            if (clear)
            {
                while (activeLines.Count > 0)
                {
                    var line = activeLines.Dequeue();
                    line.gameObject.SetActive(false);
                    availableLines.Enqueue(line);
                }
            }

            TMP_Text lineToUse;

            if (availableLines.Count > 0)
            {
                lineToUse = availableLines.Dequeue();

                lineToUse.gameObject.SetActive(true);
            }
            else
            {
                if (activeLines.Count == 0)
                    return;
                lineToUse = activeLines.Dequeue();

                if (!lineToUse.gameObject.activeSelf)
                {
                    lineToUse.gameObject.SetActive(true);
                }
            }

            lineToUse.text = FormatString(formatted);
            lineToUse.color = DetermineColor(formatted);

            lineToUse.transform.SetAsLastSibling();

            activeLines.Enqueue(lineToUse);
        }
        private Color DetermineColor(string formatted)
        {
            if (formatted.StartsWith("[ERROR]"))
                return Color.red;
            else if (formatted.StartsWith("[WARN]"))
                return new Color(1f, 0.5f, 0f);
            else
                return Color.white;
        }

        private string FormatString(string text)
        {
            if (text.Contains("\\"))
                return text.Replace('\\', '/');
            return text;
        }

        private void ClearConsoleUI()
        {
            while (activeLines.Count > 0)
            {
                var line = activeLines.Dequeue();
                line.gameObject.SetActive(false);
                availableLines.Enqueue(line);
            }
        }
    }
}
