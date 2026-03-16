using UnityEngine;

public class UIPanelMarker : MonoBehaviour
{
    [SerializeField] private string _panelName;

    public string PanelName => string.IsNullOrWhiteSpace(_panelName) ? gameObject.name : _panelName;

    private void Awake()
    {
        UIManager.Instance.RegisterPanel(PanelName, gameObject);
    }
}
