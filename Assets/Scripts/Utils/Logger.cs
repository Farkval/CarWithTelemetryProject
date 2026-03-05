using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    public class Logger : MonoBehaviour
    {
        private static Logger _instance;
        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<Logger>();
                    if (_instance == null)
                    {
                        var go = new GameObject("Logger");
                        _instance = go.AddComponent<Logger>();
                    }
                    _instance.Initialize();
                }
                return _instance;
            }
        }

        public event Action<string> OnLogAdded;
        public event Action<string> OnGameLogAdded;

        private readonly List<string> _logs = new List<string>();
        private readonly List<string> _gameLogs = new List<string>();

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

        public static void Log(string message, bool isGameLog = false)
        {
            Instance.AddLog("[LOG] " + message, isGameLog);
        }

        public static void Warning(string message, bool isGameLog = false)
        {
            Instance.AddLog("[WARN] " + message, isGameLog);
        }

        public static void Error(string message, bool isGameLog = false)
        {
            Instance.AddLog("[ERROR] " + message, isGameLog);
        }

        private void AddLog(string formatted, bool isGameLog = false)
        {
#if UNITY_EDITOR
            Debug.Log(formatted);
#endif
            
            if (isGameLog)
            {
                if (_gameLogs.LastOrDefault()?.Equals(formatted) == true)
                    return;

                if (_gameLogs.Count >= maxLogs)
                    _gameLogs.RemoveAt(0);
                _gameLogs.Add(formatted);
                OnGameLogAdded?.Invoke(formatted);
            }
            else
            {
                if (_logs.Count >= maxLogs)
                    _logs.RemoveAt(0);
                _logs.Add(formatted);
                OnLogAdded?.Invoke(formatted);
            }
        }

        public string[] GetAllLogs(bool isGameLog = false)
        {
            return isGameLog 
                ? _gameLogs.ToArray() 
                : _logs.ToArray();
        }
    }
}
