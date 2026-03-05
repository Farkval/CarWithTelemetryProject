using Assets.Scripts.Garage.Interfaces;
using Assets.Scripts.Garage.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Assets.Scripts.Garage
{
    public static class VehicleLoader
    {
        public static void SaveSettings(string prefabName, List<Component> components)
        {
            var vsd = new VehicleSaveData
            {
                prefabName = prefabName,
                components = new List<ComponentSaveData>()
            };

            foreach (var comp in components)
            {
                var csd = new ComponentSaveData
                {
                    assemblyQualifiedName = comp.GetType().AssemblyQualifiedName,
                    fields = new List<FieldSaveData>()
                };

                var fields = comp.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
                foreach (var f in fields)
                {
                    if (!PropertyUIFactory.CanHandle(f.FieldType))
                        continue;

                    var val = f.GetValue(comp);
                    csd.fields.Add(new FieldSaveData
                    {
                        fieldName = f.Name,
                        rawValue = val.ToString()
                    });
                }

                vsd.components.Add(csd);
            }

            var json = JsonUtility.ToJson(vsd);
            PlayerPrefs.SetString(Key(prefabName), json);
            PlayerPrefs.Save();
        }

        public static void LoadSettings(string prefabName, List<Component> components)
        {
            var key = Key(prefabName);
            if (!PlayerPrefs.HasKey(key))
            {
                return;
            }

            var vsd = JsonUtility.FromJson<VehicleSaveData>(PlayerPrefs.GetString(key));
            if (vsd?.components == null)
            {
                return;
            }

            int count = Math.Min(components.Count, vsd.components.Count);
            for (int i = 0; i < count; i++)
            {
                var comp = components[i];
                var csd = vsd.components[i];

                if (comp.GetType().AssemblyQualifiedName != csd.assemblyQualifiedName)
                {
                    continue;
                }

                foreach (var fsd in csd.fields)
                {
                    var field = comp.GetType().GetField(fsd.fieldName,
                        BindingFlags.Instance | BindingFlags.Public);
                    if (field == null) 
                        continue;

                    object parsed = ParseStringToFieldType(fsd.rawValue, field.FieldType);
                    field.SetValue(comp, parsed);
                }


                if (comp is IApplySettings applySettingsComponent)
                {
                    applySettingsComponent.ApplySettings();
                }
            }
        }

        static object ParseStringToFieldType(string raw, Type t)
        {
            if (t == typeof(float) && float.TryParse(raw, out var f)) return f;
            if (t == typeof(int) && int.TryParse(raw, out var i)) return i;
            if (t == typeof(bool) && bool.TryParse(raw, out var b)) return b;
            if (t.IsEnum) return Enum.Parse(t, raw);
            return null;
        }

        static string Key(string prefabName) => $"VehicleData_{prefabName}";
    }
}
