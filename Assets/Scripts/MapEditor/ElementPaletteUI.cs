using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.MapEditor
{
    /// <summary>
    /// Отвечает за нижнюю панель выбора элементов.
    /// </summary>
    public class ElementPaletteUI : MonoBehaviour
    {
        [SerializeField] private RectTransform categoryButtonPrefab;
        [SerializeField] private RectTransform elementButtonPrefab;
        [SerializeField] private Transform categoriesRoot;
        [SerializeField] private ToggleGroup categoriesToggleGroup;

        [Tooltip("Категории и их элементы (заполняется в инспекторе)")]
        public List<ElementCategory> categories;

        private readonly Dictionary<ElementData, Button> _elementButtons = new();
        private MapEditorController _controller; 
        private Button _currentButton;
        private ElementData _activeElement;
        private readonly List<GameObject> _allGrids = new();

        private void Awake()
        {
            _controller = FindFirstObjectByType<MapEditorController>();
        }

        private void Start()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            foreach (var category in categories)
            {
                // Создаём кнопку категории
                var catBtn = Instantiate(categoryButtonPrefab, categoriesRoot);
                catBtn.GetComponentInChildren<Text>().text = category.name;
                var catToggle = catBtn.GetComponent<Toggle>();
                catToggle.group = categoriesToggleGroup;

                // Контейнер сетки элементов внутри категории
                var gridRootGO = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
                var gridRoot = gridRootGO.GetComponent<RectTransform>();

                // настройки сетки
                var grid = gridRootGO.GetComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(80, 80);     // под ваши кнопки 80×80
                grid.spacing = new Vector2(20, 20);
                grid.childAlignment = TextAnchor.UpperCenter;  // ← иконки по центру
                grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                grid.constraintCount = 1; // одна строка

                var fitter = gridRootGO.GetComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                gridRoot.SetParent(catBtn, false);
                gridRoot.anchorMin = new Vector2(0, 0);
                gridRoot.anchorMax = new Vector2(1, 0);
                gridRoot.pivot = new Vector2(0.5f, 1);
                gridRoot.anchoredPosition = new Vector2(0, -40);
                gridRoot.gameObject.SetActive(false);

                _allGrids.Add(gridRootGO);

                // раскрытие ─ одновременно только одна
                catToggle.onValueChanged.AddListener(isOn =>
                {
                    foreach (var g in _allGrids) g.SetActive(false);
                    if (isOn) gridRootGO.SetActive(true);
                });

                gridRootGO.SetActive(false);

                // Создаём кнопки элементов
                foreach (var element in category.elements)
                {
                    var elemBtn = Instantiate(elementButtonPrefab, gridRoot);
                    elemBtn.GetComponentInChildren<Image>().sprite = element.icon;
                    var btn = elemBtn.GetComponent<Button>();
                    btn.onClick.AddListener(() => OnElementSelected(element, btn));
                    _elementButtons[element] = btn;

                    // Hover‑эффект через EventTrigger
                    var trigger = elemBtn.gameObject.AddComponent<EventTrigger>();
                    var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                    entryEnter.callback.AddListener(_ => elemBtn.localScale = Vector3.one * 1.1f);
                    var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                    entryExit.callback.AddListener(_ => elemBtn.localScale = Vector3.one);
                    trigger.triggers.Add(entryEnter);
                    trigger.triggers.Add(entryExit);
                }
            }
        }

        private void OnElementSelected(ElementData data, Button btn)
        {
            // 1) Сброс подсветки у предыдущего
            if (_currentButton != null)
                SetButtonColor(_currentButton, Color.white);

            // 2) Если кликнули по уже выбранной – снимаем выбор
            if (_activeElement == data)
            {
                _controller.SetActiveElement(null);
                _activeElement = null;
                _currentButton = null;
                return;
            }

            // 3) Новый выбор
            _currentButton = btn;
            _activeElement = data;
            _controller.SetActiveElement(data);
            SetButtonColor(btn, new Color(0.4f, 0.9f, 0.4f)); // нежно-зелёный

            GameObject.Find("RaiseToggle").GetComponent<Toggle>().isOn = false;
            GameObject.Find("PitToggle").GetComponent<Toggle>().isOn = false;
        }

        private static void SetButtonColor(Button b, Color c)
        {
            var colors = b.colors;
            colors.normalColor = c;
            colors.selectedColor = c;
            b.colors = colors;
        }
    }
}
