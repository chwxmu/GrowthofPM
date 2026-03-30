using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuSceneController : MonoBehaviour
{
    private const string ContinueGameDefaultLabel = "继续游戏";
    private const string ContinueGameNoSaveLabel = "继续游戏（无存档）";

    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _continueGameButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private TMP_Text _continueGameButtonLabel;
    [SerializeField] private Text _continueGameButtonLegacyLabel;

    #region Unity Lifecycle

    private void Awake()
    {
        AutoBindIfNeeded();
    }

    private void OnEnable()
    {
        BindEvents();
    }

    private void Start()
    {
        RefreshContinueButton();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    #endregion

    #region Internal Helpers

    private void BindEvents()
    {
        if (_newGameButton != null)
        {
            _newGameButton.onClick.AddListener(OnClickNewGame);
        }

        if (_continueGameButton != null)
        {
            _continueGameButton.onClick.AddListener(OnClickContinueGame);
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.AddListener(OnClickQuit);
        }
    }

    private void UnbindEvents()
    {
        if (_newGameButton != null)
        {
            _newGameButton.onClick.RemoveListener(OnClickNewGame);
        }

        if (_continueGameButton != null)
        {
            _continueGameButton.onClick.RemoveListener(OnClickContinueGame);
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.RemoveListener(OnClickQuit);
        }
    }

    private void AutoBindIfNeeded()
    {
        if (_newGameButton == null)
        {
            GameObject target = GameObject.Find("NewGameButton");
            if (target != null)
            {
                _newGameButton = target.GetComponent<Button>();
            }
        }

        if (_continueGameButton == null)
        {
            GameObject target = GameObject.Find("ContinueGameButton");
            if (target != null)
            {
                _continueGameButton = target.GetComponent<Button>();
            }
        }

        if (_quitButton == null)
        {
            GameObject target = GameObject.Find("QuitButton");
            if (target != null)
            {
                _quitButton = target.GetComponent<Button>();
            }
        }

        BindContinueButtonLabelIfNeeded();
    }

    private void RefreshContinueButton()
    {
        if (_continueGameButton == null)
        {
            return;
        }

        bool hasSave = DataManager.Instance != null && DataManager.Instance.HasSaveFile();
        _continueGameButton.interactable = hasSave;
        SetContinueButtonLabel(hasSave ? ContinueGameDefaultLabel : ContinueGameNoSaveLabel);
    }

    private void BindContinueButtonLabelIfNeeded()
    {
        if (_continueGameButton == null)
        {
            return;
        }

        if (_continueGameButtonLabel == null)
        {
            _continueGameButtonLabel = _continueGameButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (_continueGameButtonLegacyLabel == null)
        {
            _continueGameButtonLegacyLabel = _continueGameButton.GetComponentInChildren<Text>(true);
        }
    }

    private void SetContinueButtonLabel(string labelText)
    {
        BindContinueButtonLabelIfNeeded();

        if (_continueGameButtonLabel != null)
        {
            _continueGameButtonLabel.text = labelText;
        }

        if (_continueGameButtonLegacyLabel != null)
        {
            _continueGameButtonLegacyLabel.text = labelText;
        }
    }

    private static void OnClickNewGame()
    {
        GameManager.Instance.StartNewGame();
    }

    private static void OnClickContinueGame()
    {
        GameManager.Instance.ContinueGame();
    }

    private static void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion
}
