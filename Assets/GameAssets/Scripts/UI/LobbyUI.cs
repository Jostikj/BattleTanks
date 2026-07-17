using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Steamworks;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    #region Variables

    private List<PlayerDataPanelLogic> _allPlayerPanels = new List<PlayerDataPanelLogic>();
    [SerializeField] private GameObject _playerPanelPrefab;

    [Header("EnterToLobbyUI")]
    [SerializeField] private TMP_InputField _enterLobbyCode;
    [SerializeField] private TextMeshProUGUI _errorText;

    [Header("LobbyUI")]
    [SerializeField] private TextMeshProUGUI _lobbyNameText;
    [SerializeField] private TextMeshProUGUI _playersCountText;
    [SerializeField] private TMP_InputField _lobbyIDInputField;
    [SerializeField] private GameObject _content;
    [SerializeField] private GameObject _readyButton;
    [SerializeField] private GameObject _unreadyButton;
    [SerializeField] private TextMeshProUGUI _readyText;
    [SerializeField] private GameObject _playStartButton;
    [SerializeField] private TMP_Dropdown _mapChanger;

    [Header("CreateLobbyUI")]
    [SerializeField] private TextMeshProUGUI _maxPlayersCountText;
    [SerializeField] private Slider _maxPlayersSlider;
    [SerializeField] private Toggle _lobbyTypeToggle;
    [SerializeField] private TMP_InputField _lobbyNameInputField;

    #endregion

    #region Awake

    private void Start()
    {
        OnEnable();
    }

    private void OnEnable()
    {
        if (LobbyData.Instance != null && LobbyManager.Instance != null)
        {
            LobbyData.Instance.OnPlayerConnected += PlayerConnected;
            LobbyData.Instance.OnPlayerDisconnected += PlayerDiconnected;
            LobbyData.Instance.OnHostIDChanged += HostIDChanged;
            LobbyData.Instance.OnPlayersCountChanged += PlayersCountChanged;
            LobbyData.Instance.OnPlayersCountChanged += PlayButtonUpdate;
            LobbyData.Instance.OnReadyPlayersCountChanged += ReadyPlayersCountChanged;
            LobbyData.Instance.OnReadyPlayersCountChanged += PlayButtonUpdate;
            LobbyData.Instance.OnHostIDChanged += PlayButtonUpdate;
            LobbyData.Instance.OnPlayerReadyUpdate += ReadyPanelUpdate;
            LobbyManager.Instance.onLobbyEntered += OnLobbyEntered;
            LobbyManager.Instance.OnLobbyExited += OnLobbyExit;
        }
    }

    private void OnDisable()
    {
        LobbyData.Instance.OnPlayerConnected -= PlayerConnected;
        LobbyData.Instance.OnPlayerDisconnected -= PlayerDiconnected;
        LobbyData.Instance.OnHostIDChanged -= HostIDChanged;
        LobbyData.Instance.OnPlayersCountChanged -= PlayersCountChanged;
        LobbyData.Instance.OnPlayersCountChanged -= PlayButtonUpdate;
        LobbyData.Instance.OnReadyPlayersCountChanged -= ReadyPlayersCountChanged;
        LobbyData.Instance.OnReadyPlayersCountChanged -= PlayButtonUpdate;
        LobbyData.Instance.OnHostIDChanged -= PlayButtonUpdate;
        LobbyData.Instance.OnPlayerReadyUpdate -= ReadyPanelUpdate;
        LobbyManager.Instance.onLobbyEntered -= OnLobbyEntered;
        LobbyManager.Instance.OnLobbyExited -= OnLobbyExit;
    }

    #endregion

    #region LobbyUI

    private void InstantiatePlayerPanel(PlayerData player)
    {
        var panelLogic = Instantiate(_playerPanelPrefab).GetComponent<PlayerDataPanelLogic>();
        panelLogic.transform.SetParent(_content.transform, false);
        _allPlayerPanels.Add(panelLogic);
        panelLogic.InitializePanel(player);
    }

    private void DeletePlayerPanel(PlayerData player)
    {
        for (int i = 0; i < _allPlayerPanels.Count; i++)
        {
            if (_allPlayerPanels[i].PlayerData.SteamID == player.SteamID)
            {
                Destroy(_allPlayerPanels[i].gameObject);
                _allPlayerPanels.RemoveAt(i);
                break;
            }
        }
    }

    private void DeleteAllPlayerPanels()
    {
        foreach (var panel in _allPlayerPanels)
        {
            Destroy(panel.gameObject);
        }
        _allPlayerPanels.Clear();
    }

    private void PlayerConnected(PlayerData player)
    {
        InstantiatePlayerPanel(player);
    }

    private void PlayerDiconnected(PlayerData player)
    {
        DeletePlayerPanel(player);
    }

    private void HostIDChanged()
    {
        foreach (var panel in _allPlayerPanels)
        {
            panel.ResetPanel();
        }

        _mapChanger.interactable = LobbyData.Instance.HostID == LobbyData.Instance.MyData.SteamID;
    }

    private void PlayersCountChanged()
    {
        _playersCountText.text = $"{LobbyData.Instance.PlayersCount} / {LobbyData.Instance.MaxPlayersCount}";
        ReadyPlayersCountChanged();
    }

    private void OnLobbyEntered()
    {
        _lobbyNameText.text = LobbyData.Instance.LobbyName;
        _lobbyIDInputField.text = LobbyData.Instance.LobbyID.ToString();
        _playersCountText.text = $"{LobbyData.Instance.PlayersCount} / {LobbyData.Instance.MaxPlayersCount}";
        CreateLobbyDataDelete();
        EnterLobbyDataDelete();
    }

    private void OnLobbyExit()
    {
        _lobbyIDInputField.text = "";
        _lobbyNameText.text = "";
        _playersCountText.text = "";
        _readyButton.SetActive(true);
        _unreadyButton.SetActive(false);
        DeleteAllPlayerPanels();
    }

    public void GameStarted()
    {
        LobbyManager.Instance.StartGame(_mapChanger.options[_mapChanger.value].text);
    }

    public void ExitLobbyButtonPressed()
    {
        LobbyManager.Instance.OnExitLobby();
    }

    public void ReadyButtonPressed()
    {
        _readyButton.SetActive(false);
        _unreadyButton.SetActive(true);
        LobbyManager.Instance.Ready();
    }

    public void UnreadyButtonPressed()
    {
        _unreadyButton.SetActive(false);
        _readyButton.SetActive(true);
        LobbyManager.Instance.Unready();
    }

    public void ReadyPlayersCountChanged()
    {
        _readyText.text = $"{LobbyData.Instance.ReadyPlayersCount} / {LobbyData.Instance.PlayersCount}";
    }

    public void ReadyPanelUpdate(CSteamID playerSteamID, bool ready)
    {
        foreach (var panel in _allPlayerPanels)
        {
            if (panel.PlayerData.SteamID == playerSteamID)
            {
                panel.ReadyTextUpdate(ready);
                return;
            }
        }
    }

    private void PlayButtonUpdate()
    {
        bool isHost =
            LobbyData.Instance.MyData.SteamID ==
            LobbyData.Instance.HostID;

        bool canStart =
            LobbyData.Instance.PlayersCount > 1 &&
            LobbyData.Instance.ReadyPlayersCount ==
            LobbyData.Instance.PlayersCount;

        _playStartButton.SetActive(isHost && canStart);
    }

    public void MapChangerUpdate()
    {

    }

    #endregion


    #region CreateLobbyUI

    public void CreateLobby()
    {
        if (_lobbyTypeToggle.enabled)  //починить
            LobbyData.Instance.SetLobbyType(ELobbyType.k_ELobbyTypeFriendsOnly);
        else LobbyData.Instance.SetLobbyType(ELobbyType.k_ELobbyTypePublic);

        LobbyData.Instance.SetMaxPlayersCount((int)_maxPlayersSlider.value);
        LobbyData.Instance.SetLobbyName(_lobbyNameInputField.text);

        LobbyManager.Instance.CreateLobbyRequest();
    }

    public void CreateLobbyMaxPlayersCount()
    {
        LobbyData.Instance.SetMaxPlayersCount((int)_maxPlayersSlider.value);
        _maxPlayersCountText.text = _maxPlayersSlider.value.ToString();
    }

    private void CreateLobbyDataDelete()
    {
        _maxPlayersCountText.text = "2";
        _maxPlayersSlider.value = 2;
        _lobbyTypeToggle.isOn = false;
        _lobbyNameInputField.text = "New lobby";
    }

    #endregion


    #region EnterToLobbyUI

    public void EnterToLobbyWithCode()
    {
        if (!LobbyManager.Instance.JoinLobbyToID(_enterLobbyCode.text))
        {
            _errorText.text = "Неудалось подключиться к лобби, введённый код лобби не является действительным";
            return;
        }
    }

    private void EnterLobbyDataDelete()
    {
        _enterLobbyCode.text = "";
        _errorText.text = "";
    }

    #endregion
}