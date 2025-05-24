using Assets.Scripts.Garage.Attributes;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
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

        /// <summary>
        /// Генерирует foldout-секцию настроек для каждого компонента из списка.
        /// </summary>
        public void BuildFor(IEnumerable<Component> targets)
        {
            // 1. очистить старые
            foreach (Transform child in contentRoot)
                Destroy(child.gameObject);

            // 2. для каждого компонента – заголовок + контейнер полей
            foreach (var target in targets)
            {
                // --- Header ---
                var headerGO = Instantiate(headerPrefab, contentRoot);
                // Надпись
                var label = headerGO.transform.Find("Label")
                                           .GetComponent<TMP_Text>();

                var secAttr = target.GetType()
                    .GetCustomAttribute<SectionNameAttribute>();
                                label.text = secAttr != null
                                    ? secAttr.Name
                                    : ObjectNames.NicifyVariableName(target.GetType().Name);

                // Получаем toggle внутри headerPrefab
                var toggle = headerGO.GetComponentInChildren<Toggle>();
                toggle.isOn = false; // свернут по умолчанию

                // --- Container для полей ---
                var containerGO = new GameObject(
                    target.GetType().Name + "_Fields",
                    typeof(RectTransform));
                containerGO.transform.SetParent(contentRoot, false);

                // Layout + автоподгонка размера
                var vlg = containerGO.AddComponent<VerticalLayoutGroup>();
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.spacing = 6f;
                var fitter = containerGO.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                containerGO.SetActive(false);
                // переключаем видимость при клике на toggle
                toggle.onValueChanged.AddListener(isOn =>
                    containerGO.SetActive(isOn));

                // --- Создаём поля для каждого public-поля компонента ---
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
    }
}
