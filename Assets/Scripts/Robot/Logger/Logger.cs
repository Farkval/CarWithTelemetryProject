using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Robot.Logger
{
    public class Logger : MonoBehaviour
    {
        public static Logger Instance { get; private set; }

        [Tooltip("UI Text для вывода логов")]
        public TMP_Text consoleText;

        // Если вы используете TextMeshPro:
        // public TMPro.TMP_Text consoleText;

        private readonly Queue<string> _lines = new Queue<string>();
        private const int MaxLines = 1000; // сколько строк хранить

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            // необязательно: не уничтожать при переходе сцен
            // DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Добавляет строку в консоль.
        /// </summary>
        public void Log(string message)
        {
            Debug.Log(message);
            // разбиваем по переводу строки
            foreach (var line in message.Split('\n'))
            {
                _lines.Enqueue(line);
                if (_lines.Count > MaxLines)
                    _lines.Dequeue();
            }
            RefreshText();
        }

        private void RefreshText()
        {
            if (consoleText == null)
                return;

            consoleText.text = string.Join("\n", _lines);

            // автоскролл вниз:
            Canvas.ForceUpdateCanvases();
            var sv = consoleText.GetComponentInParent<ScrollRect>();
            if (sv != null)
                sv.verticalNormalizedPosition = 0;
        }
    }
}
