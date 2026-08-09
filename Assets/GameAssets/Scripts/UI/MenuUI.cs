using TMPro;
using UnityEngine;

public class MenuUI : MonoBehaviour, IUIWindow
{
    [Header("UIElements")]
    [SerializeField] private GameObject _menuPanel;
    [SerializeField] private GameObject _autorsPanel;
    [SerializeField] private GameObject _playMenuPanel;
    [SerializeField] private GameObject _multiplayerMenuPanel;
    [SerializeField] private GameObject _lobbyCreatorPanel;
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _joinLobbyPanel;

    [SerializeField] private TextMeshProUGUI _versionText;

    private void Start()
    {
        OpenMainMenu();
        OnEnable();
        _versionText.text = Application.version;
    }

    private void OnEnable()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.onLobbyEntered += OnEnteredLobby;
            LobbyManager.Instance.OnLobbyExited += OnExitedLobby;
        }
    }

    private void OnDisable()
    {
        LobbyManager.Instance.onLobbyEntered -= OnEnteredLobby;
        LobbyManager.Instance.OnLobbyExited -= OnExitedLobby;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    #region MenuButton

    public void OpenMainMenu()
    {
        HideAllPanels();
        _menuPanel.SetActive(true);
    }

    public void OpenAutors()
    {
        HideAllPanels();
        _autorsPanel.SetActive(true);
    }

    public void OpenPlayMenu()
    {
        HideAllPanels();
        _playMenuPanel.SetActive(true);
    }

    public void OpenMultiplayerMenu()
    {
        HideAllPanels();
        _multiplayerMenuPanel.SetActive(true);
    }

    public void OpenLobbyCreator()
    {
        HideAllPanels();
        _lobbyCreatorPanel.SetActive(true);
    }

    public void OpenJoinLobby()
    {
        HideAllPanels();
        _joinLobbyPanel.SetActive(true);
    }

    public void OnEnteredLobby()
    {
        HideAllPanels();
        _lobbyPanel.SetActive(true);
    }

    public void OnExitedLobby()
    {
        OpenMainMenu();
    }

    public void ExitButton()
    {
        Application.Quit();
    }
    #endregion


    private void HideAllPanels()
    {
        _playMenuPanel.SetActive(false);
        _multiplayerMenuPanel.SetActive(false);
        _lobbyCreatorPanel.SetActive(false);
        _lobbyPanel.SetActive(false);
        _joinLobbyPanel.SetActive(false);
        _menuPanel.SetActive(false);
        _autorsPanel.SetActive(false);
    }
}