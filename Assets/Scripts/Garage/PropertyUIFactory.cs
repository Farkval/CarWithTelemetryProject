using Assets.Scripts.Garage.Attributes;
using Assets.Scripts.Garage.Interfaces;
using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Garage
{
    [System.Serializable]
    public class PropertyUIFactory
    {
        [Header("Prefabs")]
        [SerializeField] GameObject floatFieldPrefab;
        [SerializeField] GameObject intFieldPrefab;
        [SerializeField] GameObject boolFieldPrefab;
        [SerializeField] GameObject enumFieldPrefab;

        public static bool CanHandle(Type t) =>
            t == typeof(float) || t == typeof(int) ||
            t == typeof(bool) || t.IsEnum;

        public void CreateUIFor(FieldInfo fi, Component target, Transform parent)
        {
            GameObject ui = null;

            if (fi.FieldType == typeof(float))
            {
                ui = GameObject.Instantiate(floatFieldPrefab, parent);
                var slider = ui.GetComponentInChildren<Slider>();
                var input = ui.GetComponentInChildren<TMP_InputField>();
                float val = (float)fi.GetValue(target);

                RangeAttribute rng = fi.GetCustomAttribute<RangeAttribute>();
                slider.minValue = rng != null ? rng.min : val * 0.2f - 10;
                slider.maxValue = rng != null ? rng.max : val * 5f + 10;

                slider.value = val;
                input.text = val.ToString("0.##");

                slider.onValueChanged.AddListener(v =>
                {
                    fi.SetValue(target, v);
                    input.text = v.ToString("0.##");
                    ApplySettings(target);
                });
                input.onEndEdit.AddListener(s =>
                {
                    if (float.TryParse(s, out var v))
                    {
                        v = Mathf.Clamp(v, slider.minValue, slider.maxValue);
                        fi.SetValue(target, v);
                        slider.SetValueWithoutNotify(v);
                        ApplySettings(target);
                    }
                });
            }
            else if (fi.FieldType == typeof(int))
            {
                ui = GameObject.Instantiate(intFieldPrefab, parent);
                var input = ui.GetComponentInChildren<TMP_InputField>();
                int val = (int)fi.GetValue(target);
                input.text = val.ToString();
                input.onEndEdit.AddListener(s =>
                {
                    if (int.TryParse(s, out var v))
                    {
                        fi.SetValue(target, v);
                        ApplySettings(target);
                    }
                });
            }
            else if (fi.FieldType == typeof(bool))
            {
                ui = GameObject.Instantiate(boolFieldPrefab, parent);
                var toggle = ui.GetComponentInChildren<Toggle>();
                toggle.isOn = (bool)fi.GetValue(target);
                toggle.onValueChanged.AddListener(v =>
                {
                    fi.SetValue(target, v);
                    ApplySettings(target);
                });
            }
            else if (fi.FieldType.IsEnum)
            {
                ui = GameObject.Instantiate(enumFieldPrefab, parent);
                var dd = ui.GetComponentInChildren<TMP_Dropdown>();

                dd.ClearOptions();
                var names = Enum.GetNames(fi.FieldType);
                dd.AddOptions(new System.Collections.Generic.List<string>(names));

                dd.value = Array.IndexOf(names, fi.GetValue(target).ToString());
                dd.onValueChanged.AddListener(i =>
                {
                    fi.SetValue(target, Enum.Parse(fi.FieldType, names[i]));
                    ApplySettings(target);
                });
            }

            var dispAttr = fi.GetCustomAttribute<DisplayNameAttribute>();
            var labelText = dispAttr != null
                ? dispAttr.Name
                : ObjectNames.NicifyVariableName(fi.Name);

            ui.transform.Find("Label")
              .GetComponent<TMP_Text>().text = labelText;
        }

        private void ApplySettings(Component c)
        {
            if (c is IApplySettings applySettings)
                applySettings.ApplySettings();
        }
    }
}
