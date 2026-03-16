using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    private const float FadeDuration = 0.2f;

    private readonly Dictionary<string, GameObject> _panels = new Dictionary<string, GameObject>();

    #region Public API

    public void RegisterPanel(string name, GameObject panel)
    {
        if (string.IsNullOrWhiteSpace(name) || panel == null)
        {
            return;
        }

        _panels[name] = panel;
    }

    public void ShowPanel(string name)
    {
        if (!_panels.TryGetValue(name, out GameObject panel) || panel == null)
        {
            Debug.LogWarning($"[UIManager] 未找到面板: {name}");
            return;
        }

        panel.SetActive(true);
        CanvasGroup group = EnsureCanvasGroup(panel);
        group.DOKill();
        group.alpha = 0f;
        group.DOFade(1f, FadeDuration);
    }

    public void HidePanel(string name)
    {
        if (!_panels.TryGetValue(name, out GameObject panel) || panel == null)
        {
            return;
        }

        HidePanelInternal(panel);
    }

    public void HideAllPanels()
    {
        foreach (KeyValuePair<string, GameObject> pair in _panels)
        {
            HidePanelInternal(pair.Value);
        }
    }

    public void RebuildPanelRegistry()
    {
        _panels.Clear();

        UIPanelMarker[] markers = Resources.FindObjectsOfTypeAll<UIPanelMarker>();
        foreach (UIPanelMarker marker in markers)
        {
            if (marker == null || !marker.gameObject.scene.IsValid() || !marker.gameObject.scene.isLoaded)
            {
                continue;
            }

            RegisterPanel(marker.PanelName, marker.gameObject);
        }
    }

    #endregion

    #region Internal Helpers

    private static void HidePanelInternal(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        CanvasGroup group = EnsureCanvasGroup(panel);
        group.DOKill();

        if (!panel.activeSelf)
        {
            return;
        }

        group.DOFade(0f, FadeDuration).OnComplete(() =>
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        });
    }

    private static CanvasGroup EnsureCanvasGroup(GameObject panel)
    {
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = panel.AddComponent<CanvasGroup>();
        }

        return group;
    }

    #endregion
}
