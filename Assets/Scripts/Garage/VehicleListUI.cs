using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Garage
{
    public class VehicleListUI : MonoBehaviour
    {
        [Header("Prefabs & Roots")]
        [SerializeField] Button itemButtonPrefab;
        [SerializeField] Transform contentRoot;

        readonly List<GameObject> _prefabs = new();

        public void Build(Action<GameObject> onClick)
        {
            foreach (Transform ch in contentRoot)
                Destroy(ch.gameObject);

            _prefabs.Clear();

            var all = Resources.LoadAll<GameObject>("Vehicles");
            foreach (var go in all)
            {
                _prefabs.Add(go);

                var btn = Instantiate(itemButtonPrefab, contentRoot);
                btn.name = go.name;

                var icon = Resources.Load<Sprite>($"Vehicles/Icons/{go.name}");
                if (icon != null)
                {
                    btn.GetComponentInChildren<Image>().sprite = icon;
                }
                btn.GetComponentInChildren<TMP_Text>().text = go.name;

                var vb = btn.gameObject.AddComponent<VehicleButtonUI>();
                vb.OnClick = () => onClick(go);

                btn.onClick.AddListener(() => onClick(go));
            }
        }
    }
}
