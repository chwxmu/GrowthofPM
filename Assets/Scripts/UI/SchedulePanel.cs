using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SchedulePanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _energyText;
    [SerializeField] private Slider _energySlider;
    [SerializeField] private VerticalLayoutGroup _availableTaskLayout;
    [SerializeField] private VerticalLayoutGroup _selectedTaskLayout;
    [SerializeField] private Button _confirmButton;

    private readonly List<Button> _availableButtons = new List<Button>();
    private readonly List<Button> _removeButtons = new List<Button>();
    private readonly List<TMP_Text> _selectedTaskTexts = new List<TMP_Text>();
    private readonly List<DailyTaskData> _availableTasks = new List<DailyTaskData>();
    private readonly List<DailyTaskData> _selectedTasks = new List<DailyTaskData>();

    private Action<List<DailyTaskData>> _onConfirm;
    private int _maxEnergy;
    private int _remainingEnergy;
    private Tween _energyTween;

    private void Awake()
    {
        EnsureLayout();
        if (_confirmButton != null)
        {
            _confirmButton.onClick.AddListener(OnClickConfirm);
        }
    }

    private void OnDestroy()
    {
        _energyTween?.Kill();
        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveListener(OnClickConfirm);
        }
    }

    public void ShowSchedule(List<DailyTaskData> tasks, int availableEnergy, Action<List<DailyTaskData>> onConfirm)
    {
        EnsureLayout();

        _availableTasks.Clear();
        if (tasks != null)
        {
            _availableTasks.AddRange(tasks);
        }

        _selectedTasks.Clear();
        _onConfirm = onConfirm;
        _maxEnergy = Mathf.Max(0, availableEnergy);
        _remainingEnergy = _maxEnergy;
        gameObject.SetActive(true);

        RebuildAvailableList();
        RebuildSelectedList();
        RefreshEnergyDisplay(true);
    }

    private void OnClickConfirm()
    {
        _onConfirm?.Invoke(new List<DailyTaskData>(_selectedTasks));
    }

    private void OnClickAddTask(int index)
    {
        if (index < 0 || index >= _availableTasks.Count)
        {
            return;
        }

        DailyTaskData task = _availableTasks[index];
        if (task == null || task.energyCost > _remainingEnergy)
        {
            return;
        }

        _selectedTasks.Add(task);
        _remainingEnergy -= Mathf.Max(0, task.energyCost);
        RebuildAvailableList();
        RebuildSelectedList();
        RefreshEnergyDisplay(false);
    }

    private void OnClickRemoveTask(int index)
    {
        if (index < 0 || index >= _selectedTasks.Count)
        {
            return;
        }

        DailyTaskData task = _selectedTasks[index];
        _selectedTasks.RemoveAt(index);
        _remainingEnergy = Mathf.Min(_maxEnergy, _remainingEnergy + (task != null ? Mathf.Max(0, task.energyCost) : 0));
        RebuildAvailableList();
        RebuildSelectedList();
        RefreshEnergyDisplay(false);
    }

    private void RebuildAvailableList()
    {
        EnsureAvailableItemCount(_availableTasks.Count);

        for (int i = 0; i < _availableButtons.Count; i += 1)
        {
            Button button = _availableButtons[i];
            if (button == null)
            {
                continue;
            }

            bool hasTask = i < _availableTasks.Count && _availableTasks[i] != null;
            button.gameObject.SetActive(hasTask);
            button.onClick.RemoveAllListeners();

            if (!hasTask)
            {
                continue;
            }

            DailyTaskData task = _availableTasks[i];
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = BuildAvailableTaskLabel(task);
            }

            bool canAdd = task.energyCost <= _remainingEnergy;
            button.interactable = canAdd;
            int capturedIndex = i;
            if (canAdd)
            {
                button.onClick.AddListener(() => OnClickAddTask(capturedIndex));
            }
        }
    }

    private void RebuildSelectedList()
    {
        EnsureSelectedItemCount(_selectedTasks.Count);

        for (int i = 0; i < _selectedTaskTexts.Count; i += 1)
        {
            bool hasTask = i < _selectedTasks.Count && _selectedTasks[i] != null;
            TMP_Text text = _selectedTaskTexts[i];
            Button removeButton = _removeButtons[i];

            if (text != null)
            {
                text.transform.parent.gameObject.SetActive(hasTask);
            }

            if (!hasTask)
            {
                if (removeButton != null)
                {
                    removeButton.onClick.RemoveAllListeners();
                }
                continue;
            }

            DailyTaskData task = _selectedTasks[i];
            if (text != null)
            {
                text.text = task.name + "  (-" + task.energyCost + ")";
            }

            if (removeButton != null)
            {
                removeButton.onClick.RemoveAllListeners();
                int capturedIndex = i;
                removeButton.onClick.AddListener(() => OnClickRemoveTask(capturedIndex));
            }
        }

        if (_confirmButton != null)
        {
            _confirmButton.interactable = _selectedTasks.Count > 0;
        }
    }

    private void RefreshEnergyDisplay(bool immediate)
    {
        if (_energyText != null)
        {
            _energyText.text = "剩余精力：" + _remainingEnergy + " / " + _maxEnergy;
        }

        if (_energySlider == null)
        {
            return;
        }

        _energySlider.minValue = 0;
        _energySlider.maxValue = Mathf.Max(1, _maxEnergy);

        _energyTween?.Kill();
        if (immediate)
        {
            _energySlider.value = _remainingEnergy;
            return;
        }

        float startValue = _energySlider.value;
        _energyTween = DOTween.To(() => startValue, value =>
        {
            startValue = value;
            _energySlider.value = value;
        }, _remainingEnergy, 0.2f);
    }

    private static string BuildAvailableTaskLabel(DailyTaskData task)
    {
        if (task == null)
        {
            return string.Empty;
        }

        return task.name + "  (-" + task.energyCost + ")\n" + BuildEffectPreview(task.effects);
    }

    private static string BuildEffectPreview(StatEffects effects)
    {
        if (effects == null)
        {
            return "无属性变化";
        }

        List<string> entries = new List<string>();
        AppendEffect(entries, "技术", effects.techPower);
        AppendEffect(entries, "沟通", effects.commPower);
        AppendEffect(entries, "管理", effects.managePower);
        AppendEffect(entries, "抗压", effects.stressPower);
        return entries.Count > 0 ? string.Join("  ", entries) : "无属性变化";
    }

    private static void AppendEffect(List<string> entries, string name, int value)
    {
        if (value == 0)
        {
            return;
        }

        string sign = value > 0 ? "+" : string.Empty;
        entries.Add(name + sign + value);
    }

    private void EnsureAvailableItemCount(int targetCount)
    {
        if (_availableTaskLayout == null)
        {
            return;
        }

        while (_availableButtons.Count < targetCount)
        {
            _availableButtons.Add(CreateAvailableTaskButton(_availableButtons.Count));
        }
    }

    private void EnsureSelectedItemCount(int targetCount)
    {
        if (_selectedTaskLayout == null)
        {
            return;
        }

        while (_selectedTaskTexts.Count < targetCount)
        {
            CreateSelectedTaskItem();
        }
    }

    private Button CreateAvailableTaskButton(int index)
    {
        GameObject item = new GameObject("AvailableTask" + (index + 1), typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        item.transform.SetParent(_availableTaskLayout.transform, false);

        Image image = item.GetComponent<Image>();
        image.color = new Color32(41, 52, 74, 220);

        LayoutElement layoutElement = item.GetComponent<LayoutElement>();
        layoutElement.minHeight = 88f;
        layoutElement.preferredHeight = 110f;

        Button button = item.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(236, 242, 255, 255);
        colors.pressedColor = new Color32(216, 228, 244, 255);
        colors.disabledColor = new Color32(150, 150, 150, 180);
        button.colors = colors;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(item.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(20f, 12f);
        labelRect.offsetMax = new Vector2(-20f, -12f);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = FindSharedFont();
        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Left;
        label.enableWordWrapping = true;
        label.color = Color.white;
        return button;
    }

    private void CreateSelectedTaskItem()
    {
        GameObject item = new GameObject("SelectedTask" + (_selectedTaskTexts.Count + 1), typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        item.transform.SetParent(_selectedTaskLayout.transform, false);

        Image image = item.GetComponent<Image>();
        image.color = new Color32(34, 64, 58, 220);

        LayoutElement layoutElement = item.GetComponent<LayoutElement>();
        layoutElement.minHeight = 70f;
        layoutElement.preferredHeight = 76f;

        GameObject textObject = new GameObject("Label", typeof(RectTransform));
        textObject.transform.SetParent(item.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(16f, 10f);
        textRect.offsetMax = new Vector2(-110f, -10f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = FindSharedFont();
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = true;
        text.color = Color.white;

        GameObject buttonObject = new GameObject("RemoveButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(item.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.sizeDelta = new Vector2(84f, 44f);
        buttonRect.anchoredPosition = new Vector2(-52f, 0f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color32(123, 52, 52, 255);

        Button button = buttonObject.GetComponent<Button>();

        GameObject buttonTextObject = new GameObject("Label", typeof(RectTransform));
        buttonTextObject.transform.SetParent(buttonObject.transform, false);
        RectTransform buttonTextRect = buttonTextObject.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = buttonTextObject.AddComponent<TextMeshProUGUI>();
        buttonText.font = FindSharedFont();
        buttonText.fontSize = 22f;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;
        buttonText.text = "移除";

        _selectedTaskTexts.Add(text);
        _removeButtons.Add(button);
    }

    private void EnsureLayout()
    {
        if (_energyText != null && _energySlider != null && _availableTaskLayout != null && _selectedTaskLayout != null && _confirmButton != null)
        {
            return;
        }

        TMP_FontAsset sharedFont = FindSharedFont();
        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }

        Image background = GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }
        background.color = new Color32(10, 18, 28, 215);

        GameObject contentRoot = FindOrCreateChild(gameObject, "PanelContent");
        RectTransform contentRect = EnsureRectTransform(contentRoot);
        contentRect.anchorMin = new Vector2(0.08f, 0.08f);
        contentRect.anchorMax = new Vector2(0.92f, 0.92f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup rootLayout = contentRoot.GetComponent<HorizontalLayoutGroup>();
        if (rootLayout == null)
        {
            rootLayout = contentRoot.AddComponent<HorizontalLayoutGroup>();
        }
        rootLayout.padding = new RectOffset(24, 24, 24, 24);
        rootLayout.spacing = 20f;
        rootLayout.childAlignment = TextAnchor.UpperLeft;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = true;

        GameObject leftColumn = FindOrCreateChild(contentRoot, "AvailableColumn");
        ConfigureColumn(leftColumn, 2f);
        GameObject rightColumn = FindOrCreateChild(contentRoot, "SelectedColumn");
        ConfigureColumn(rightColumn, 1f);

        _energyText = EnsureText(leftColumn.transform, "EnergyText", sharedFont, 28f, FontStyles.Bold, TextAlignmentOptions.Left, 48f);
        _energySlider = EnsureSlider(leftColumn.transform, "EnergySlider", 32f);
        _availableTaskLayout = EnsureList(leftColumn.transform, "AvailableTaskList");

        TMP_Text selectedTitle = EnsureText(rightColumn.transform, "SelectedTitle", sharedFont, 28f, FontStyles.Bold, TextAlignmentOptions.Left, 48f);
        selectedTitle.text = "本周安排";
        _selectedTaskLayout = EnsureList(rightColumn.transform, "SelectedTaskList");
        _confirmButton = EnsureButton(rightColumn.transform, "ConfirmButton", sharedFont, "确认安排", 56f);
    }

    private static void ConfigureColumn(GameObject column, float flexibleWidth)
    {
        Image image = column.GetComponent<Image>();
        if (image == null)
        {
            image = column.AddComponent<Image>();
        }
        image.color = new Color32(28, 38, 58, 240);

        LayoutElement layoutElement = column.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = column.AddComponent<LayoutElement>();
        }
        layoutElement.flexibleWidth = flexibleWidth;

        VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = column.AddComponent<VerticalLayoutGroup>();
        }
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static TMP_Text EnsureText(Transform parent, string name, TMP_FontAsset font, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, float preferredHeight)
    {
        Transform existing = parent.Find(name);
        GameObject textObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        if (existing == null)
        {
            textObject.transform.SetParent(parent, false);
        }

        LayoutElement layoutElement = textObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = textObject.AddComponent<LayoutElement>();
        }
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.minHeight = preferredHeight;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = textObject.AddComponent<TextMeshProUGUI>();
        }

        if (font != null)
        {
            text.font = font;
        }

        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.color = Color.white;
        return text;
    }

    private static Slider EnsureSlider(Transform parent, string name, float preferredHeight)
    {
        Transform existing = parent.Find(name);
        GameObject sliderObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Slider));
        if (existing == null)
        {
            sliderObject.transform.SetParent(parent, false);
        }

        LayoutElement layoutElement = sliderObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = sliderObject.AddComponent<LayoutElement>();
        }
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.minHeight = preferredHeight;

        Slider slider = sliderObject.GetComponent<Slider>();
        EnsureSliderVisuals(sliderObject, slider);
        slider.interactable = false;
        return slider;
    }

    private static void EnsureSliderVisuals(GameObject sliderObject, Slider slider)
    {
        RectTransform root = EnsureRectTransform(sliderObject);
        root.sizeDelta = new Vector2(0f, 32f);

        GameObject background = FindOrCreateChild(sliderObject, "Background");
        RectTransform backgroundRect = EnsureRectTransform(background);
        backgroundRect.anchorMin = new Vector2(0f, 0.25f);
        backgroundRect.anchorMax = new Vector2(1f, 0.75f);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        Image backgroundImage = background.GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = background.AddComponent<Image>();
        }
        backgroundImage.color = new Color32(56, 66, 84, 255);

        GameObject fillArea = FindOrCreateChild(sliderObject, "Fill Area");
        RectTransform fillAreaRect = EnsureRectTransform(fillArea);
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = new Vector2(6f, 6f);
        fillAreaRect.offsetMax = new Vector2(-6f, -6f);

        GameObject fill = FindOrCreateChild(fillArea, "Fill");
        RectTransform fillRect = EnsureRectTransform(fill);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fill.GetComponent<Image>();
        if (fillImage == null)
        {
            fillImage = fill.AddComponent<Image>();
        }
        fillImage.color = new Color32(99, 194, 122, 255);

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        slider.direction = Slider.Direction.LeftToRight;
    }

    private static VerticalLayoutGroup EnsureList(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        GameObject listObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
        if (existing == null)
        {
            listObject.transform.SetParent(parent, false);
        }

        LayoutElement layoutElement = listObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = listObject.AddComponent<LayoutElement>();
        }
        layoutElement.flexibleHeight = 1f;
        layoutElement.minHeight = 220f;

        VerticalLayoutGroup layout = listObject.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = listObject.AddComponent<VerticalLayoutGroup>();
        }
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    private static Button EnsureButton(Transform parent, string name, TMP_FontAsset font, string labelText, float preferredHeight)
    {
        Transform existing = parent.Find(name);
        GameObject buttonObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        if (existing == null)
        {
            buttonObject.transform.SetParent(parent, false);
        }

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color32(69, 114, 206, 255);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = buttonObject.AddComponent<LayoutElement>();
        }
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.minHeight = preferredHeight;

        Button button = buttonObject.GetComponent<Button>();

        GameObject labelObject = FindOrCreateChild(buttonObject, "Label");
        RectTransform labelRect = EnsureRectTransform(labelObject);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        if (label == null)
        {
            label = labelObject.AddComponent<TextMeshProUGUI>();
        }
        if (font != null)
        {
            label.font = font;
        }
        label.fontSize = 28f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.text = labelText;

        return button;
    }

    private static GameObject FindOrCreateChild(GameObject parent, string childName)
    {
        Transform existing = parent.transform.Find(childName);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject child = new GameObject(childName, typeof(RectTransform));
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static RectTransform EnsureRectTransform(GameObject target)
    {
        RectTransform rectTransform = target.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = target.AddComponent<RectTransform>();
        }

        return rectTransform;
    }

    private static TMP_FontAsset FindSharedFont()
    {
        TextMeshProUGUI existingText = FindObjectOfType<TextMeshProUGUI>(true);
        return existingText != null ? existingText.font : TMP_Settings.defaultFontAsset;
    }
}
