using Assets.Scripts.Robot.Api.Interfaces;
using Assets.Scripts.Robot.Python;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System;
using System.IO;
using System.Text;
using UnityEngine;
using Logger = Assets.Scripts.Utils.Logger;

[RequireComponent(typeof(MonoBehaviour))]
public class PythonScriptRunner : MonoBehaviour
{
    [Tooltip("Путь к исполняемому файлу")]
    public string scriptFile = @"C:\Users\Student\Desktop\Unity projects\CarWithTelemetryProject\Assets\UserScripts\robot.py";

    [Tooltip("Запустить автоматически при старте сцены")]
    public bool autoRun = true;

    private IRobotAPI _robot;
    private ScriptEngine _engine;
    private ScriptScope _scope;
    private dynamic _updateFunc;
    private bool _running;

    public void Initizalize()
    {
        _robot = GetComponent<IRobotAPI>();
        if (_robot == null)
        {
            Logger.Error("PythonScriptRunner: IRobotAPI component not found");
            enabled = false;
            return;
        }

        _engine = Python.CreateEngine();

        var stream = new LogOutputStream();
        _engine.Runtime.IO.SetOutput(stream, Encoding.UTF8);
        _engine.Runtime.IO.SetErrorOutput(stream, Encoding.UTF8);

        var paths = _engine.GetSearchPaths();
        if (!string.IsNullOrEmpty(scriptFile))
            paths.Add(Path.GetDirectoryName(scriptFile));
        _engine.SetSearchPaths(paths);

        _scope = _engine.CreateScope();
        _scope.SetVariable("robot", _robot);
        // чтобы в sys.modules появился stub, если нужен
        _engine.Execute("import robot", _scope);
    }

    private void Start()
    {
        if (autoRun) 
            Launch();
    }

    private void Update()
    {
        if (_running && _updateFunc != null)
        {
            try
            {
                _updateFunc(_robot, Time.deltaTime);
            }
            catch (Exception ex)
            {
                Logger.Error($"Python runtime error: {ex}");
                Stop();
            }
        }
    }

    public void Launch()
    {
        if (!File.Exists(scriptFile))
        {
            Logger.Error($"Python script '{scriptFile}' not found");
            return;
        }

        try
        {
            // сбросим предыдущий модуль для перезагрузки
            string moduleName = Path.GetFileNameWithoutExtension(scriptFile);
            _engine.Execute($"import sys; sys.modules.pop('{moduleName}', None)", _scope);

            var src = _engine.CreateScriptSourceFromFile(scriptFile);
            src.Execute(_scope);

            if (!_scope.TryGetVariable("update", out _updateFunc))
            {
                Logger.Warning($"'{scriptFile}' не содержит функцию update(robot, dt)");
                return;
            }

            _running = true;
            _robot.ManualControl = false;
            Logger.Log($"Python script '{scriptFile}' launched");
        }
        catch (Exception ex)
        {
            Logger.Error($"Python launch error: {ex}");
        }
    }

    public void Stop()
    {
        _running = false;
        _updateFunc = null;
        _robot.ManualControl = true;
        Logger.Log("Python script stopped (manual control ON)");
    }

    private void OnDestroy()
    {
        _engine?.Runtime.Shutdown();
    }
}