using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Garage
{
    /// <summary>
    /// Строит горизонтальный список кнопок-иконок машин.
    /// Загружает спрайты из Resources/Vehicles/Icons/{PrefabName}.
    /// </summary>
    public class VehicleListUI : MonoBehaviour
    {
        [Header("Prefabs & Roots")]
        [SerializeField] Button itemButtonPrefab;   // VehicleButton.prefab
        [SerializeField] Transform contentRoot;     // VehicleList/Viewport/Content

        readonly List<GameObject> _prefabs = new();

        /// <param name="onClick">Вызывается кликом по иконке, передаёт исходный GameObject-префаб машины</param>
        public void Build(Action<GameObject> onClick)
        {
            // Очистим старые кнопки (если Build вызывался повторно)
            foreach (Transform ch in contentRoot)
                Destroy(ch.gameObject);

            _prefabs.Clear();

            // Загружаем все префабы машин из Resources/Vehicles
            var all = Resources.LoadAll<GameObject>("Vehicles");
            foreach (var go in all)
            {
                _prefabs.Add(go);

                // Инстанцируем кнопку
                var btn = Instantiate(itemButtonPrefab, contentRoot);
                btn.name = go.name;

                // Пытаемся загрузить спрайт-иконку из Resources/Vehicles/Icons/{go.name}
                var icon = Resources.Load<Sprite>($"Vehicles/Icons/{go.name}");
                if (icon != null)
                {
                    btn.GetComponentInChildren<Image>().sprite = icon;
                }
                btn.GetComponentInChildren<TMP_Text>().text = go.name;

                var vb = btn.gameObject.AddComponent<VehicleButtonUI>();
                vb.OnClick = () => onClick(go);

                // Навешиваем обработчик
                btn.onClick.AddListener(() => onClick(go));
            }
        }
    }
}
