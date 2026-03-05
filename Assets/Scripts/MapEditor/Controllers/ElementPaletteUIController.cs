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
    [SerializeField] private RectTransform categoriesContent;
    [SerializeField] private RectTransform elementsContent;

    [Header("Toggle Group")]
    [SerializeField] private ToggleGroup categoriesToggleGroup;

    [Header("Data")]
    [Tooltip("Список категорий и их элементов")]
    public List<ElementCategory> categories;

    private Button _selectedElementBtn;
    private ElementData _selectedElementData;
    private MapEditorController _mapEditor;
    private List<Toggle> _toolsToggles;

    private void Awake()
    {
        _mapEditor = FindFirstObjectByType<MapEditorController>();

        _toolsToggles = FindObjectsByType<Toggle>(FindObjectsSortMode.None)
            .Where(t => GameObjectNameConst.ToolsToggles.Contains(t.name))
            .ToList();
    }

    private void Start()
    {
        BuildCategoryToggles();

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
            var tog = Instantiate(categoryTogglePrefab, categoriesContent);
            tog.group = categoriesToggleGroup;
            tog.GetComponentInChildren<Text>().text = cat.name;

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
        ClearElements();

        foreach (var el in cat.elements)
        {
            var btn = Instantiate(elementButtonPrefab, elementsContent);

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

            btn.transform.localScale = Vector3.one;

            btn.onClick.AddListener(() => OnElementClicked(btn, el));

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
        if (_selectedElementBtn == btn && _selectedElementData == data)
        {
            ClearSelection();
            return;
        }

        if (_selectedElementBtn != null)
        {
            _selectedElementBtn.transform.localScale = Vector3.one;
        }

        _selectedElementBtn = btn;
        _selectedElementData = data;
        btn.transform.localScale = Vector3.one * 1.1f;

        _mapEditor.SetActiveElement(data);

        foreach (var t in _toolsToggles)
            t.isOn = false;
    }
    private void ClearElements()
    {
        foreach (Transform c in elementsContent)
            Destroy(c.gameObject);

        ClearSelection();
    }
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
