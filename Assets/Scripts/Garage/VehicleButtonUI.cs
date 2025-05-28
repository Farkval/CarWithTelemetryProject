using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Garage
{
    [RequireComponent(typeof(RectTransform))]
    public class VehicleButtonUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        // Скейл относительно исходного
        [Header("Scales")]
        [SerializeField] float hoverScale = 1.1f;
        [SerializeField] float selectedScale = 1.2f;

        // Колбэк на клик
        public Action OnClick;

        // Внутреннее состояние
        RectTransform _rt;
        Vector3 _baseScale;
        bool _isSelected;

        // Статика для отслеживания текущей выделенной
        static VehicleButtonUI _currentSelected;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _baseScale = _rt.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isSelected)
                _rt.localScale = _baseScale * hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isSelected)
                _rt.localScale = _baseScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 1) снять выделение с предыдущей
            if (_currentSelected != null && _currentSelected != this)
                _currentSelected.Deselect();

            // 2) отметить себя
            _isSelected = true;
            _rt.localScale = _baseScale * selectedScale;
            _currentSelected = this;

            // 3) вызвать внешний обработчик
            OnClick?.Invoke();
        }

        public void Deselect()
        {
            _isSelected = false;
            _rt.localScale = _baseScale;
        }
    }
}
