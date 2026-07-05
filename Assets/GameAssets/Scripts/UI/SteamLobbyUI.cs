using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Steamworks;
using UnityEngine.UI;

public class SteamLobbyUI : MonoBehaviour
{
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

    [Header("CreateLobbyUI")]
    [SerializeField] private TextMeshProUGUI _maxPlayersCountText;
    [SerializeField] private Slider _maxPlayersSlider;
    [SerializeField] private Toggle _lobbyTypeToggle;
    [SerializeField] private TMP_InputField _lobbyNameInputField;

    private void Start()
    {
        SteamLobbyData.Instance.OnPlayerConnected += PlayerConnected;
        SteamLobbyData.Instance.OnPlayerDisconnected += PlayerDiconnected;
        SteamLobbyData.Instance.OnHostIDChanged += HostIDChanged;
        SteamLobbyData.Instance.OnPlayersCountChanged += PlayersCountChanged;
        SteamLobbyData.Instance.OnPlayersCountChanged += PlayButtonUpdate;
        SteamLobbyData.Instance.OnReadyPlayersCountChanged += ReadyPlayersCountChanged;
        SteamLobbyData.Instance.OnReadyPlayersCountChanged += PlayButtonUpdate;
        SteamLobbyData.Instance.OnHostIDChanged += PlayButtonUpdate;
        SteamLobbyManager.Instance.onLobbyEntered += LobbyEntered;
        SteamLobbyManager.Instance.OnLobbyExited += LobbyExited;
    }


    #region LobbyUI

    private void InstantiatePlayerPanel(PlayerSteamData player)
    {
        var panelLogic = Instantiate(_playerPanelPrefab).GetComponent<PlayerDataPanelLogic>();
        panelLogic.transform.SetParent(_content.transform, false);
        _allPlayerPanels.Add(panelLogic);
        panelLogic.InitializePanel(player);
    }

    private void DeletePlayerPanel(PlayerSteamData player)
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

    private void PlayerConnected(PlayerSteamData player)
    {
        InstantiatePlayerPanel(player);
    }

    private void PlayerDiconnected(PlayerSteamData player)
    {
        DeletePlayerPanel(player);
    }

    private void HostIDChanged()
    {
        foreach (var panel in _allPlayerPanels)
        {
            panel.ResetPanel();
        }
    }

    private void PlayersCountChanged()
    {
        _playersCountText.text = $"{SteamLobbyData.Instance.PlayersCount} / {SteamLobbyData.Instance.MaxPlayersCount}";
        ReadyPlayersCountChanged();
    }

    private void LobbyEntered()
    {
        _lobbyNameText.text = SteamLobbyData.Instance.LobbyName;
        _lobbyIDInputField.text = SteamLobbyData.Instance.LobbyID.ToString();
        _playersCountText.text = $"{SteamLobbyData.Instance.PlayersCount} / {SteamLobbyData.Instance.MaxPlayersCount}";
    }

    private void LobbyExited()
    {
        _lobbyIDInputField.text = "";
        _lobbyNameText.text = "";
        _playersCountText.text = "";
        DeleteAllPlayerPanels();
    }

    public void GameStarted()
    {
        SteamLobbyManager.Instance.StartGame();
    }

    public void ReadyButtonPressed()
    {
        _readyButton.SetActive(false);
        _unreadyButton.SetActive(true);
        SteamLobbyManager.Instance.Ready();
    }

    public void UnreadyButtonPressed()
    {
        _unreadyButton.SetActive(false);
        _readyButton.SetActive(true);
        SteamLobbyManager.Instance.Unready();
    }

    public void ReadyPlayersCountChanged()
    {
        _readyText.text = $"{SteamLobbyData.Instance.ReadyPlayersCount} / {SteamLobbyData.Instance.PlayersCount}";
    }

    public void ReadyPanelUpdate(CSteamID playerSteamID, bool ready)
    {
        foreach (var panel in _allPlayerPanels)
        {
            if(panel.PlayerData.SteamID == playerSteamID)
            {
                panel.ReadyTextUpdate(ready);
                return;
            }
        }
    }

    private void PlayButtonUpdate()
    {
        bool isHost =
            SteamLobbyData.Instance.MySteamData.SteamID ==
            SteamLobbyData.Instance.HostID;

        bool canStart =
            SteamLobbyData.Instance.PlayersCount > 1 &&
            SteamLobbyData.Instance.ReadyPlayersCount ==
            SteamLobbyData.Instance.PlayersCount;

        _playStartButton.SetActive(isHost && canStart);
    }

    #endregion


    #region CreateLobbyUI

    public void CreateLobby()
    {
        if (_lobbyTypeToggle.enabled)  //починить
            SteamLobbyData.Instance.SetLobbyType(ELobbyType.k_ELobbyTypeFriendsOnly);
        else SteamLobbyData.Instance.SetLobbyType(ELobbyType.k_ELobbyTypePublic);

        SteamLobbyData.Instance.SetMaxPlayersCount((int)_maxPlayersSlider.value);
        SteamLobbyData.Instance.SetLobbyName(_lobbyNameInputField.text);

        SteamLobbyManager.Instance.CreateLobbyRequest();
    }

    public void CreateLobbyMaxPlayersCount()
    {
        SteamLobbyData.Instance.SetMaxPlayersCount((int)_maxPlayersSlider.value);
        _maxPlayersCountText.text = _maxPlayersSlider.value.ToString();
    }

    #endregion


    #region EnterToLobbyUI

    public void EnterToLobbyWithCode()
    {
        if (!SteamLobbyManager.Instance.JoinLobbyToID(_enterLobbyCode.text))
        {
            _errorText.text = "Неудалось подключиться к лобби, введённый код лобби не является действительным";
        }
    }

    #endregion
}