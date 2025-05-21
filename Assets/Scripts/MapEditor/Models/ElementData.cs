using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.MapEditor.Models
{
    /// <summary>
    /// Определение элемента карты. Создаётся через «Create/Map Editor/Element Data».
    /// </summary>
    [CreateAssetMenu(menuName = "Map Editor/Element Data", fileName = "NewElementData")]
    public class ElementData : ScriptableObject
    {
        public string displayName;
        public Sprite icon;
        public GameObject prefab;
        public float minScale = 0.5f;
        public float maxScale = 2f;

        [Tooltip("Дополнительные пользовательские параметры. Ключ – название, значение – строковое представление.")]
        public List<CustomProperty> customProperties = new();
    }
}
