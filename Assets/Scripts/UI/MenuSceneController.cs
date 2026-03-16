using UnityEngine;
using UnityEngine.UI;

public class MenuSceneController : MonoBehaviour
{
    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _continueGameButton;
    [SerializeField] private Button _quitButton;

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
    }

    private void RefreshContinueButton()
    {
        if (_continueGameButton == null)
        {
            return;
        }

        _continueGameButton.interactable = DataManager.Instance.HasSaveFile();
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
