using System;
using System.IO;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using UnityEngine;
using Assets.Scripts.Robot.Api.Interfaces;
using Assets.Scripts.Robot.Logger;
using System.Text;

[RequireComponent(typeof(MonoBehaviour))]
public class PythonScriptRunner : MonoBehaviour
{
    [Header("Script file (relative to Assets/UserScripts)")]
    [Tooltip("Напр.:  my_bot.py")]
    public string scriptFile = "my_bot.py";

    [Tooltip("Запустить автоматически при старте сцены")]
    public bool autoRun = true;

    private IRobotAPI _robot;
    private ScriptEngine _engine;
    private ScriptScope _scope;
    private dynamic _updateFunc;
    private bool _running;

    private void Awake()
    {
        _robot = GetComponent<IRobotAPI>();
        if (_robot == null)
        {
            Debug.LogError("PythonScriptRunner: IRobotAPI component not found");
            enabled = false;
            return;
        }

        _engine = Python.CreateEngine();

        var stream = new LogOutputStream();
        _engine.Runtime.IO.SetOutput(stream, Encoding.UTF8);
        _engine.Runtime.IO.SetErrorOutput(stream, Encoding.UTF8);

        var paths = _engine.GetSearchPaths();
        string userScriptsPath = Path.Combine(Application.dataPath, "UserScripts");
        paths.Add(userScriptsPath);
        // Добавим все поддиректории для удобства
        foreach (var dir in Directory.GetDirectories(userScriptsPath))
            paths.Add(dir);
        _engine.SetSearchPaths(paths);

        _scope = _engine.CreateScope();
        _scope.SetVariable("robot", _robot);
        // чтобы в sys.modules появился stub, если нужен
        _engine.Execute("import robot", _scope);
    }

    private void Start()
    {
        if (autoRun) Launch();
    }

    private void Update()
    {
        // горячая клавиша [I] – старт/стоп
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (_running) Stop(); else Launch();
        }

        if (_running && _updateFunc != null)
        {
            try
            {
                _updateFunc(_robot, Time.deltaTime);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Python runtime error:\n{ex}");
                Stop();
            }
        }
    }

    private void Launch()
    {
        string full = Path.Combine(Application.dataPath, "UserScripts", scriptFile);
        if (!File.Exists(full))
        {
            Debug.LogError($"Python script '{full}' not found");
            return;
        }

        try
        {
            // сбросим предыдущий модуль для перезагрузки
            string moduleName = Path.GetFileNameWithoutExtension(scriptFile);
            _engine.Execute($"import sys; sys.modules.pop('{moduleName}', None)", _scope);

            var src = _engine.CreateScriptSourceFromFile(full);
            src.Execute(_scope);

            if (!_scope.TryGetVariable("update", out _updateFunc))
            {
                Debug.LogWarning($"'{scriptFile}' не содержит функцию update(robot, dt)");
                return;
            }

            _running = true;
            _robot.ManualControl = false;
            Debug.Log($"Python script '{scriptFile}' launched");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Python launch error:\n{ex}");
        }
    }

    private void Stop()
    {
        _running = false;
        _updateFunc = null;
        _robot.ManualControl = true;
        Debug.Log("Python script stopped (manual control ON)");
    }

    private void OnDestroy()
    {
        _engine?.Runtime.Shutdown();
    }
}