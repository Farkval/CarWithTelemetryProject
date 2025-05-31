using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Game.Controllers
{
    public class ConsoleUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform content;              // Content у ScrollRect
        [SerializeField] private ScrollRect scrollRect;              // Сам ScrollRect
        [SerializeField] private TMP_Text linePrefab;                // Префаб ConsoleLinePrefab
        [Header("Settings")]
        [SerializeField] private int maxLines = 1000;                // Лимит строк

        private void OnEnable()
        {
            // Убедимся, что инстанс создан
            var logger = Utils.Logger.Instance;
            logger.OnLogAdded += HandleNewLog;
            // При старте вывести уже имеющиеся логи
            foreach (var line in logger.GetAllLogs())
                AddLineToUI(line);
            // Прокрутить вниз
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        private void OnDisable()
        {
            if (Utils.Logger.Instance != null)
                Utils.Logger.Instance.OnLogAdded -= HandleNewLog;
        }

        private void HandleNewLog(string formatted)
        {
            AddLineToUI(formatted);

            // Поддерживаем лимит
            if (content.childCount > maxLines)
                Destroy(content.GetChild(0).gameObject);

            // Обновляем Layout и скролл вниз
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        private void AddLineToUI(string formatted)
        {
            // Создаём копию
            var go = Instantiate(linePrefab, content);
            go.gameObject.SetActive(true);
            go.text = FormatString(formatted);

            // Определяем цвет по префиксу
            if (formatted.StartsWith("[ERROR]"))
                go.color = Color.red;
            else if (formatted.StartsWith("[WARN]"))
                go.color = new Color(1f, 0.5f, 0f); // оранжевый
            else
                go.color = Color.white;
        }

        private string FormatString(string text)
        {
            if (text.Contains('\\'))
                return text.Replace('\\', '/');
            return text;
        }
    }
}
