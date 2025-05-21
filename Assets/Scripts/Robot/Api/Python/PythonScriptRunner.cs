using Assets.Scripts.MapEditor;
using Assets.Scripts.Robot.Api.Interfaces;
using Microsoft.Scripting.Hosting;
using System.IO;
using UnityEngine;
using IronPython.Hosting;
using Assets.Scripts.Robot.Api.Python;
using Assets.Scripts.MapEditor.Controllers;

[RequireComponent(typeof(MonoBehaviour))]
public class PythonScriptRunner : MonoBehaviour
{
    [Tooltip("Имя файла в папке Assets/UserScripts (без пути).")]
    public string scriptFile = "my_bot.py";

    [Tooltip("Стартовать автоматически при запуске сцены.")]
    public bool autoRun = false;

    IRobotAPI robot;
    ScriptEngine engine;
    ScriptScope scope;
    dynamic updateFunc;      // python-функция update(robot, dt)
    bool isRunning;

    void Awake()
    {
        RobotPythonStubGenerator.EnsureStub();
        robot = GetComponent<IRobotAPI>();
        engine = Python.CreateEngine();

        // чтобы import работал:
        var paths = engine.GetSearchPaths();
        paths.Add(Path.Combine(Application.dataPath, "UserScripts"));
        engine.SetSearchPaths(paths);

        scope = engine.CreateScope();
        scope.SetVariable("robot", robot);
        engine.Execute("import robot as _r\n_r.robot = robot", scope);
    }

    void Start()
    {
        if (autoRun) LoadAndRun();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (!isRunning) LoadAndRun();        // запуск
            else StopScript();       // стоп/перезапуск
        }

        if (isRunning && updateFunc != null)
        {
            try { updateFunc(robot, Time.deltaTime); }
            catch (System.Exception e) { Debug.LogError(e); StopScript(); }
        }
    }

    void LoadAndRun()
    {
        string path = Path.Combine(Application.dataPath, "UserScripts", scriptFile);
        if (!File.Exists(path)) { Debug.LogError($"Script {path} not found"); return; }

        try
        {
            var source = engine.CreateScriptSourceFromFile(path);
            // позволяем log() из Python
            scope.SetVariable("log", (System.Action<object>)ConsoleManager.LogObj);
            // переопределяем стандартный print
            engine.Execute("import builtins\nbuiltins.print=lambda *a,**k: log(' '.join(map(str,a)))", scope);
            source.Execute(scope);                       // выполняем модуль
            scope.TryGetVariable("update", out updateFunc);
            isRunning = true;
            robot.ManualControl = false;                // переключаемся на скрипт
            Debug.Log($"Python: {scriptFile} started");
        }
        catch (System.Exception e) { Debug.LogError(e); }
    }

    void StopScript()
    {
        isRunning = false;
        updateFunc = null;
        robot.ManualControl = true;                     // вернуться к WASD
        Debug.Log("Python stopped; manual control ON");
    }
}
