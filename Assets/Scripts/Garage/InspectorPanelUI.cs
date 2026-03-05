using Assets.Scripts.Garage.Attributes;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Garage
{
    public class InspectorPanelUI : MonoBehaviour
    {
        [Header("Container")]
        [SerializeField] Transform contentRoot;

        [Header("Field Factory")]
        [SerializeField] PropertyUIFactory factory;

        [Header("Section Header Prefab")]
        [SerializeField] GameObject headerPrefab;
        public void BuildFor(IEnumerable<Component> targets)
        {
            foreach (Transform child in contentRoot)
                Destroy(child.gameObject);

            foreach (var target in targets)
            {
                var headerGO = Instantiate(headerPrefab, contentRoot);
                var label = headerGO.transform.Find("Label")
                                           .GetComponent<TMP_Text>();

                var secAttr = target.GetType()
                    .GetCustomAttribute<SectionNameAttribute>();
                                label.text = secAttr != null
                                    ? secAttr.Name
                                    : NicifyName(target.GetType().Name);

                var toggle = headerGO.GetComponentInChildren<Toggle>();
                toggle.isOn = false;

                var containerGO = new GameObject(
                    target.GetType().Name + "_Fields",
                    typeof(RectTransform));
                containerGO.transform.SetParent(contentRoot, false);

                var vlg = containerGO.AddComponent<VerticalLayoutGroup>();
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.spacing = 6f;
                var fitter = containerGO.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                containerGO.SetActive(false);
                toggle.onValueChanged.AddListener(isOn =>
                    containerGO.SetActive(isOn));

                var fields = target.GetType()
                                   .GetFields(BindingFlags.Instance | BindingFlags.Public);
                foreach (var fi in fields)
                {
                    if (!PropertyUIFactory.CanHandle(fi.FieldType))
                        continue;
                    factory.CreateUIFor(fi, target, containerGO.transform);
                }
            }
        }

        public static string NicifyName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            var nicified = System.Text.RegularExpressions.Regex.Replace(
                name, @"([a-z])([A-Z])", "$1 $2");
            return char.ToUpper(nicified[0]) + nicified.Substring(1);
        }
    }
}
