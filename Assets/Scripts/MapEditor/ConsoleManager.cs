using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.MapEditor
{
    public class ConsoleManager : MonoBehaviour
    {
        public static ConsoleManager Instance { get; private set; }

        [Header("UI link")]
        [SerializeField] TextMeshProUGUI consoleText;
        [SerializeField] int maxLines = 300;
        [SerializeField] KeyCode toggleKey = KeyCode.BackQuote;     // «`» под Esc

        readonly Queue<string> _lines = new();

        void Awake()
        {
            if (Instance) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.logMessageReceived += AddUnityLog;
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                consoleText.transform.parent.gameObject.SetActive(!consoleText.transform.parent.gameObject.activeSelf);
        }

        /*––––– API –––––*/
        public static void Log(string msg) => Instance?.AddLine(msg);
        public static void LogObj(object o) => Log(o?.ToString() ?? "null");

        /*––––– internal –––––*/
        void AddUnityLog(string condition, string stackTrace, LogType type)
            => AddLine($"[{type}] {condition}");

        void AddLine(string txt)
        {
            _lines.Enqueue(txt);
            while (_lines.Count > maxLines) _lines.Dequeue();
            consoleText.text = string.Join("\n", _lines);
        }
    }
}
