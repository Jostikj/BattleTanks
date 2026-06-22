using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Steamworks;
using UnityEngine.UI;

public class SteamLobbyUI : MonoBehaviour
{
    private List<PlayerDataPanelLogic> _allPlayerPanels = new List<PlayerDataPanelLogic>();
    [SerializeField] private GameObject PlayerPanelPrefab;

    [Header("EnterToLobbyUI")]
    [SerializeField] private TMP_InputField EnterLobbyCode;
    [SerializeField] private TextMeshProUGUI ErrorText;

    [Header("LobbyUI")]
    [SerializeField] private TextMeshProUGUI _lobbyNameText;
    [SerializeField] private TextMeshProUGUI _playersCountText;
    [SerializeField] private TMP_InputField _lobbyIDInputField;
    [SerializeField] private GameObject Content;

    [Header("CreateLobbyUI")]
    [SerializeField] private TextMeshProUGUI _maxPlayersCountText;
    [SerializeField] private Slider _maxPlayersSlider;
    [SerializeField] private Toggle _lobbyTypeToggle;
    [SerializeField] private TMP_InputField _lobbyNameInputField;

    private void Awake()
    {
        SteamLobbyData.Instance.OnPlayerConnected += PlayerConnected;
        SteamLobbyData.Instance.OnPlayerDisconnected += PlayerDiconnected;
        SteamLobbyData.Instance.OnHostIDChanged += HostIDChanged;
        SteamLobbyData.Instance.OnPlayersCountChanged += PlayersCountChanged;
        SteamLobbyManager.Instance.onLobbyEntered += LobbyEntered;
    }


    #region LobbyUI

    private void InstantiatePlayersPanels(PlayerSteamData player)
    {
        var panelLogic = Instantiate(PlayerPanelPrefab).GetComponent<PlayerDataPanelLogic>();
        panelLogic.transform.SetParent(Content.transform, false);
        _allPlayerPanels.Add(panelLogic);
        panelLogic.InitializePanel(player);
    }

    private void DeletePlayerPanel(PlayerSteamData player)
    {
        for (int i = 0; i < _allPlayerPanels.Count; i++)
        {
            if (_allPlayerPanels[i].PlayerData == player)
            {
                Destroy(_allPlayerPanels[i].gameObject);
                _allPlayerPanels.Remove(_allPlayerPanels[i]);
                break;
            }
        }
    }

    private void DeleteAllPlayerPanels()
    {
        for (int i = 0; i < _allPlayerPanels.Count; i++)
        {
            Destroy(_allPlayerPanels[i].gameObject);
            _allPlayerPanels.Remove(_allPlayerPanels[i]);
        }
    }

    private void PlayerConnected(PlayerSteamData player)
    {
        InstantiatePlayersPanels(player);
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
        _playersCountText.text = $"»гроков в лобби: {SteamLobbyData.Instance.PlayersCount}/{SteamLobbyData.Instance.MaxPlayersCount}";
    }

    private void LobbyEntered()
    {
        _lobbyNameText.text = SteamLobbyData.Instance.LobbyName;
        _lobbyIDInputField.text = SteamLobbyData.Instance.LobbyID.ToString();
        _maxPlayersCountText.text = $"{SteamLobbyData.Instance.PlayersCount} / {SteamLobbyData.Instance.MaxPlayersCount}";
    }

    public void LobbyExited()
    {
        _lobbyIDInputField.text = "";
        _lobbyNameText.text = "";
        _playersCountText.text = "";
        DeleteAllPlayerPanels();
        SteamLobbyManager.Instance.OnExitLobby();
    }

    public void GameStarted()
    {
        SteamLobbyManager.Instance.StartGame();
    }

    #endregion


    #region CreateLobbyUI

    public void CreateLobby()
    {
        if (_lobbyTypeToggle.enabled)
            SteamLobbyData.Instance.SetLobbyType(ELobbyType.k_ELobbyTypeFriendsOnly);
        else SteamLobbyData.Instance.SetLobbyType(ELobbyType.k_ELobbyTypePublic);

        SteamLobbyData.Instance.SetLobbyName(_lobbyNameInputField.text);

        SteamLobbyManager.Instance.CreateGameLobby();
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
        if (!SteamLobbyManager.Instance.JoinLobbyToID(EnterLobbyCode.text))
        {
            ErrorText.text = "¬ведЄн неверный формат кода лобби";
        }
    }

    #endregion
}