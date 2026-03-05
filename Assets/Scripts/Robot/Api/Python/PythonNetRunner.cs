using System;
using System.IO;
using UnityEngine;
using Python.Runtime;
using Assets.Scripts.Robot.Api.Interfaces;

namespace Assets.Scripts.Robot.Api.Python
{
    public class PythonNetRunner : MonoBehaviour
    {
        private string _scriptPath;

        private string _moduleName; 
        private dynamic _updateFunc;
        private bool _running;      
        private IRobotAPI _robot;   

        private static bool _pythonInitDone;
        private static readonly object _pyInitLock = new();

        private static void EnsurePythonEngine()
        {
            if (_pythonInitDone) return;

            lock (_pyInitLock)
            {
                if (_pythonInitDone) return;

                Debug.Log(Application.dataPath);
                Debug.Log(Directory.GetCurrentDirectory());
                string dll = Path.Combine(Application.dataPath, "Plugins", "x86_64", "python311.dll");
                Runtime.PythonDLL = dll;
                PythonEngine.Initialize();

                using (Py.GIL())
                {
                    dynamic sys = Py.Import("sys");
                    var pyStdout = new PyStdout();

                    sys.stdout = pyStdout;
                    sys.stderr = pyStdout;
                }

                AppDomain.CurrentDomain.ProcessExit += (_, __) => PythonEngine.Shutdown();
                _pythonInitDone = true;
            }
        }

        private void Awake()
        {
            EnsurePythonEngine();
            _robot = GetComponentInParent<IRobotAPI>();
            if (_robot == null)
                Debug.LogError($"{nameof(PythonNetRunner)}: рядом не найден компонент IRobotAPI");
        }

        public void Initialize(string scriptPath)
        {
            if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
            {
                Debug.LogError($"{nameof(PythonNetRunner)}: неверный путь '{scriptPath}'");
                return;
            }

            _scriptPath = scriptPath;
            _moduleName = Path.GetFileNameWithoutExtension(scriptPath);
            _updateFunc = null;
            _running = false;
        }

        public void StartScript()
        {
            if (string.IsNullOrEmpty(_scriptPath))
            {
                Debug.LogError($"{nameof(PythonNetRunner)}: скрипт не инициализирован.");
                return;
            }

            try
            {
                LoadModuleFresh();
                _running = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{nameof(PythonNetRunner)}: ошибка при старте — {ex}");
                _running = false;
            }
        }

        public void StopScript() => _running = false;


        private float _hb;
        private void Update()
        {
            _hb += Time.unscaledDeltaTime;
            if (_hb >= 1f)
            {
                _hb = 0f;
                Debug.Log($"HB: enabled={enabled}, goActive={gameObject.activeInHierarchy}, running={_running}, hasUpdate={_updateFunc != null}, timeScale={Time.timeScale}");
            }

            if (!_running || _updateFunc is null) return;

            try
            {
                using (Py.GIL())
                {
                    _updateFunc(_robot, (double)Time.deltaTime);
                }
            }
            catch (PythonException pex)
            {
                using (Py.GIL())
                {
                    Debug.LogError($"PythonNetRunner :: Python error\n{pex.Format()}");
                }
                Debug.LogError($"Stopping python after exception. running={_running}, module={_moduleName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{nameof(PythonNetRunner)}: ошибка вызова update() — {ex}");
            }
        }

        private void LoadModuleFresh()
        {
            using (Py.GIL())
            {
                dynamic sys = Py.Import("sys");
                string dir = Path.GetDirectoryName(_scriptPath);
                if (!sys.path.__contains__(dir)) sys.path.append(dir);

                dynamic machinery = Py.Import("importlib.machinery");
                dynamic loader = machinery.SourceFileLoader(_moduleName, _scriptPath);
                dynamic module = loader.load_module();

                if (!module.HasAttr("update"))
                    throw new InvalidDataException($"В скрипте '{_scriptPath}' нет функции update(robot, dt)");

                _updateFunc = module.update;
            }
        }
    }
}
