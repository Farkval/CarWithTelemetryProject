using Assets.Scripts.Robot.Api.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Robot.Python;

namespace Assets.Scripts.Robot.Api.Python
{
    public static class RobotPythonStubGenerator
    {
        private const string FILE_NAME = "robot.py";

        public static void EnsureStub()
        {
            string dir = Path.Combine(Application.dataPath, "UserScripts");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, FILE_NAME);
            File.WriteAllText(path, BuildStub());
        }

        private static string BuildStub()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\"\"\"");
            sb.AppendLine("Auto-generated stub for Unity IRobotAPI.");
            sb.AppendLine("Edit your own scripts, but **не правьте этот файл вручную**.");
            sb.AppendLine("\"\"\"");
            sb.AppendLine("from typing import List, Any");
            sb.AppendLine();
            sb.AppendLine("class Robot:");
            sb.AppendLine();

            // Собираем все интерфейсы из IRobotAPI и помеченные атрибутом
            var root = typeof(IRobotAPI);
            var allIfcs = new[] { root }
                .Concat(root.GetInterfaces())
                .Concat(Assembly.GetAssembly(root)
                    .GetTypes()
                    .Where(t => t.GetCustomAttribute<PythonStubExportAttribute>() != null
                                && t.IsInterface))
                .Distinct();

            // Свойства
            var seenProps = new HashSet<string>();
            foreach (var iface in allIfcs)
            {
                foreach (var prop in iface.GetProperties())
                {
                    var attr = prop.GetCustomAttribute<PythonStubExportAttribute>();
                    if (attr != null && !attr.Include) continue;
                    if (!seenProps.Add(prop.Name)) continue;

                    // Докстринг
                    if (attr?.Doc != null)
                    {
                        sb.AppendLine($"    # {attr.Doc}");
                    }

                    sb.AppendLine($"    {prop.Name}: {PyType(prop.PropertyType)}  #: {prop.PropertyType.Name}");
                    sb.AppendLine();
                }
            }

            // Методы
            var seenMethods = new HashSet<string>();
            foreach (var iface in allIfcs)
            {
                foreach (var m in iface.GetMethods())
                {
                    if (m.IsSpecialName || m.DeclaringType == typeof(object)) continue;
                    var attr = m.GetCustomAttribute<PythonStubExportAttribute>();
                    if (attr != null && !attr.Include) continue;

                    string sig = m.Name + "(" +
                        string.Join(",", m.GetParameters().Select(p => p.ParameterType.FullName)) + ")";
                    if (!seenMethods.Add(sig)) continue;

                    if (attr?.Doc != null)
                    {
                        sb.AppendLine($"    # {attr.Doc}");
                    }

                    sb.Append($"    def {m.Name}(");
                    sb.Append("self");
                    foreach (var p in m.GetParameters())
                        sb.Append($", {p.Name}: {PyType(p.ParameterType)}");
                    sb.AppendLine(") -> Any: ...");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("# runtime instance (Unity заменит его настоящим объектом)");
            sb.AppendLine("robot = Robot()");
            return sb.ToString();
        }

        private static string PyType(Type t)
        {
            if (t == typeof(void)) return "None";
            if (t == typeof(float) || t == typeof(double) || t == typeof(int))
                return "float";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(string)) return "str";
            if (t.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(t))
                return "List[Any]";
            return "Any";
        }
    }
}
