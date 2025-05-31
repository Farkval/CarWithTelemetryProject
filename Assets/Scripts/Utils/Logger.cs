using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    public class Logger : MonoBehaviour
    {
        // Единственный экземпляр
        private static Logger _instance;
        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Попытка найти в сцене
                    _instance = FindFirstObjectByType<Logger>();
                    if (_instance == null)
                    {
                        // Если нет — создаём новый GameObject
                        var go = new GameObject("Logger");
                        _instance = go.AddComponent<Logger>();
                    }
                    _instance.Initialize();
                }
                return _instance;
            }
        }

        // Событие на добавление лога
        public event Action<string> OnLogAdded;

        // Внутренний буфер логов
        private readonly List<string> _logs = new List<string>();

        // Максимальное число строк (необязательно)
        [SerializeField] private int maxLogs = 1000;

        private bool _initialized;

        private void Initialize()
        {
            if (_initialized) 
                return;

            DontDestroyOnLoad(gameObject);
            _initialized = true;
        }

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Добавить свой лог.
        /// </summary>
        public static void Log(string message)
        {
            Instance.AddLog("[LOG] " + message);
        }

        /// <summary>
        /// Добавить свой warning.
        /// </summary>
        public static void Warning(string message)
        {
            Instance.AddLog("[WARN] " + message);
        }

        /// <summary>
        /// Добавить свою ошибку.
        /// </summary>
        public static void Error(string message)
        {
            Instance.AddLog("[ERROR] " + message);
        }

        private void AddLog(string formatted)
        {
            // Ограничиваем длину буфера
            if (_logs.Count >= maxLogs)
                _logs.RemoveAt(0);

#if UNITY_EDITOR
            Debug.Log(formatted);
#endif
            _logs.Add(formatted);
            OnLogAdded?.Invoke(formatted);
        }

        /// <summary>
        /// Получить весь буфер лога (копия).
        /// </summary>
        public string[] GetAllLogs()
        {
            return _logs.ToArray();
        }
    }
}
