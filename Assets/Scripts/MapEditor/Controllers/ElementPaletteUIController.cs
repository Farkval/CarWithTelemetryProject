using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Assets.Scripts.MapEditor.Models;
using Assets.Scripts.MapEditor.Controllers;
using Assets.Scripts.Consts;
using TMPro;

public class ElementPaletteUIController : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private Toggle categoryTogglePrefab;
    [SerializeField] private Button elementButtonPrefab;

    [Header("Scroll Views Content")]
    [SerializeField] private RectTransform categoriesContent;  // Content из CategoriesScroll
    [SerializeField] private RectTransform elementsContent;    // Content из ElementsScroll

    [Header("Toggle Group")]
    [SerializeField] private ToggleGroup categoriesToggleGroup;

    [Header("Data")]
    [Tooltip("Список категорий и их элементов")]
    public List<ElementCategory> categories;

    // Внутренние поля для хранения текущего выделения
    private Button _selectedElementBtn;
    private ElementData _selectedElementData;
    private MapEditorController _mapEditor;
    private List<Toggle> _toolsToggles;

    private void Awake()
    {
        // Находим контроллер редактора на сцене
        _mapEditor = FindFirstObjectByType<MapEditorController>();

        // Собираем ваши тул-тогглы, чтобы сбрасывать их при выборе элемента
        _toolsToggles = FindObjectsByType<Toggle>(FindObjectsSortMode.None)
            .Where(t => GameObjectNameConst.ToolsToggles.Contains(t.name))
            .ToList();
    }

    private void Start()
    {
        BuildCategoryToggles();

        // Автовыбор первой категории (чтобы сразу показать её элементы)
        if (categoriesContent.childCount > 0)
        {
            var first = categoriesContent.GetChild(0).GetComponent<Toggle>();
            if (first != null) first.isOn = true;
        }
    }

    private void BuildCategoryToggles()
    {
        foreach (var cat in categories)
        {
            // 1) Создаём Toggle-категорию
            var tog = Instantiate(categoryTogglePrefab, categoriesContent);
            tog.group = categoriesToggleGroup;
            tog.GetComponentInChildren<Text>().text = cat.name;

            // 2) Подписываемся на onValueChanged
            tog.onValueChanged.AddListener(isOn =>
            {
                if (isOn) 
                    ShowElementsOfCategory(cat);
                else 
                    ClearElements();
            });
        }
    }

    private void ShowElementsOfCategory(ElementCategory cat)
    {
        // Сначала очищаем старые кнопки
        ClearElements();

        // Для каждой модели элемента создаём кнопку
        foreach (var el in cat.elements)
        {
            var btn = Instantiate(elementButtonPrefab, elementsContent);

            // Сразу установим иконку (допустим, у кнопки есть дочерний Image)
            var img = btn.GetComponentInChildren<Image>();
            if (img != null)
            {
                img.sprite = el.icon;
                var arFitter = img.GetComponent<AspectRatioFitter>();
                if (arFitter != null && img.sprite != null)
                    arFitter.aspectRatio =
                        (float)img.sprite.rect.width / img.sprite.rect.height;
            }
            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = el.displayName;
            }

            // Сбросим масштаб на стандартный
            btn.transform.localScale = Vector3.one;

            // Подписка на клик
            btn.onClick.AddListener(() => OnElementClicked(btn, el));

            // Hover-эффект: увеличиваем, если не выбран
            var trigger = btn.gameObject.AddComponent<EventTrigger>();
            var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entryEnter.callback.AddListener(_ =>
            {
                if (btn != _selectedElementBtn)
                    btn.transform.localScale = Vector3.one * 1.1f;
            });
            var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            entryExit.callback.AddListener(_ =>
            {
                if (btn != _selectedElementBtn)
                    btn.transform.localScale = Vector3.one;
            });
            trigger.triggers.Add(entryEnter);
            trigger.triggers.Add(entryExit);
        }
    }

    private void OnElementClicked(Button btn, ElementData data)
    {
        // Если клик по уже выделенному — сбросим
        if (_selectedElementBtn == btn && _selectedElementData == data)
        {
            ClearSelection();
            return;
        }

        // Снимаем масштаб со старого выделения
        if (_selectedElementBtn != null)
        {
            _selectedElementBtn.transform.localScale = Vector3.one;
        }

        // Устанавливаем новое выделение
        _selectedElementBtn = btn;
        _selectedElementData = data;
        btn.transform.localScale = Vector3.one * 1.1f;

        // Передаём выбор в MapEditorController
        _mapEditor.SetActiveElement(data);

        // Сбрасываем все тул-тогглы
        foreach (var t in _toolsToggles)
            t.isOn = false;
    }

    /// <summary>
    /// Полностью очищает список кнопок элементов и сбрасывает выделение.
    /// </summary>
    private void ClearElements()
    {
        // Удаляем все кнопки
        foreach (Transform c in elementsContent)
            Destroy(c.gameObject);

        // Сбрасываем текущее выделение
        ClearSelection();
    }

    /// <summary>
    /// Снимает выделение с последнего активного элемента.
    /// </summary>
    public void ClearSelection()
    {
        if (_selectedElementBtn != null)
        {
            _selectedElementBtn.transform.localScale = Vector3.one;
            _selectedElementBtn = null;
        }

        _selectedElementData = null;
        _mapEditor.SetActiveElement(null);
    }
}
