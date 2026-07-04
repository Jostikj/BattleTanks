using Edgegap;
using Mirror;
using Steamworks;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SteamLobbyManager : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Map1";

    [Header("Singleton")]
    public static SteamLobbyManager Instance;

    [Header("Components")]
    [SerializeField] private SteamLobbyUI SteamLobbyUI;

    [Header("Actions")]
    public Action OnGameStart;
    public Action onLobbyEntered;
    public Action OnLobbyExited;

    [Header("Flags")]
    private bool _isLobbyCreating = false;
    private bool _isGameStarting = false;
    private bool _isLobbyDataLoaded = false;

    [Header("Callbacks")]
    private Callback<LobbyCreated_t> lobbyCreatedCallback;
    private Callback<GameLobbyJoinRequested_t> joinRequestCallback;
    private Callback<LobbyEnter_t> lobbyEnteredCallback;
    private Callback<LobbyDataUpdate_t> lobbyDataUpdateCallback;
    private Callback<LobbyChatUpdate_t> lobbyChatUpdateCallback;
    private Callback<LobbyChatMsg_t> lobbyChatMsgCallback;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
        Debug.LogWarning("test");

        lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        joinRequestCallback = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        lobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyDataUpdateCallback = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
        lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        lobbyChatMsgCallback = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMsg);
    }

    public void CreateLobbyRequest()
    {
        if (_isLobbyCreating) return;
        SteamMatchmaking.CreateLobby(SteamLobbyData.Instance.LobbyType, SteamLobbyData.Instance.MaxPlayersCount);
        _isLobbyCreating = true;
        Debug.Log("Запрос на создание лобби отправлен в Steam...");
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        _isLobbyCreating = false;

        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"Не удалось создать лобби. Код ошибки: {callback.m_eResult}");
            return;
        }

        SteamLobbyData.Instance.SetLobbyID(new CSteamID(callback.m_ulSteamIDLobby));
        Debug.Log($"Лобби успешно создано! ID: {SteamLobbyData.Instance.LobbyID}");

        SteamMatchmaking.SetLobbyData(SteamLobbyData.Instance.LobbyID, "lobbyName", SteamLobbyData.Instance.LobbyName);
        SteamMatchmaking.SetLobbyData(SteamLobbyData.Instance.LobbyID, "lobbyMaxPlayersCount", SteamLobbyData.Instance.MaxPlayersCount.ToString());
        SteamMatchmaking.SetLobbyData(SteamLobbyData.Instance.LobbyID, "readyPlayers", "0");
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        Debug.Log($"Получен запрос на вступление в лобби {callback.m_steamIDLobby}");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        Debug.Log($"Успешно подключён к лобби: {SteamLobbyData.Instance.LobbyID}");
        SteamLobbyData.Instance.SetLobbyID(new CSteamID(callback.m_ulSteamIDLobby));
        SteamLobbyData.Instance.SetHostID(new CSteamID((ulong)SteamMatchmaking.GetLobbyOwner(SteamLobbyData.Instance.LobbyID)));

        SteamMatchmaking.RequestLobbyData(SteamLobbyData.Instance.LobbyID);
    }

    private void OnLobbyDataUpdate(LobbyDataUpdate_t callback)
    {
        if (callback.m_ulSteamIDLobby != SteamLobbyData.Instance.LobbyID.m_SteamID)
            return;

        if (!NetworkServer.active && !_isGameStarting)
            ClientGameStartedCheck();

        if (!_isLobbyDataLoaded)
            LobbyDataLoad();

        UpdateReadyState();
    }

    private void LobbyDataLoad()
    {
        Debug.Log("Загрузка данных лобби");
        _isLobbyDataLoaded = true;
        SteamLobbyData.Instance.SetMaxPlayersCount(Convert.ToInt32(SteamMatchmaking.GetLobbyData(SteamLobbyData.Instance.LobbyID, "lobbyMaxPlayersCount")));
        SteamLobbyData.Instance.SetLobbyName(SteamMatchmaking.GetLobbyData(SteamLobbyData.Instance.LobbyID, "lobbyName"));
        SteamLobbyData.Instance.SetPlayersCount(SteamMatchmaking.GetNumLobbyMembers(SteamLobbyData.Instance.LobbyID));

        for (int i = 0; i < SteamLobbyData.Instance.PlayersCount; i++)
        {
            SteamLobbyData.Instance.AddPlayer(SteamMatchmaking.GetLobbyMemberByIndex(SteamLobbyData.Instance.LobbyID, i));
        }
        UpdateReadyState();
        NetworkManager.singleton.networkAddress = SteamLobbyData.Instance.HostID.ToString();

        onLobbyEntered?.Invoke();
    }

    private void UpdateReadyState()
    {
        int readyPlayers = 0;

        for (int i = 0; i < SteamLobbyData.Instance.PlayersCount; i++)
        {
            CSteamID player = SteamMatchmaking.GetLobbyMemberByIndex(
                SteamLobbyData.Instance.LobbyID,
                i);

            bool ready =
                SteamMatchmaking.GetLobbyMemberData(
                    SteamLobbyData.Instance.LobbyID,
                    player,
                    "ready") == "1";

            SteamLobbyData.Instance.SetPlayerReady(player, ready);

            SteamLobbyUI.ReadyPanelUpdate(player, ready);

            if (ready)
                readyPlayers++;
        }

        SteamLobbyData.Instance.SetReadyPlayersCount(readyPlayers);
    }

    public void InviteFriends()
    {
        if (!SteamLobbyData.Instance.LobbyID.IsValid())
        {
            Debug.LogWarning("Нет активного лобби для приглашения");
            return;
        }
        Debug.Log("Приглашение друзей");
        SteamFriends.ActivateGameOverlayInviteDialog(SteamLobbyData.Instance.LobbyID);
    }

    private void ClientGameStartedCheck()
    {
        if (SteamMatchmaking.GetLobbyData(SteamLobbyData.Instance.LobbyID, "game_started") == "1")
        {
            Debug.Log("Игра запущена");
            _isGameStarting = true;
            SceneManager.LoadScene(gameSceneName);
            NetworkManager.singleton.StartClient();
        }
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        ConnectedAndDisconnectedPlayer(callback);
    }

    private void ConnectedAndDisconnectedPlayer(LobbyChatUpdate_t callback)
    {
        var state = (EChatMemberStateChange)callback.m_rgfChatMemberStateChange;
        CSteamID playerSteamID = new CSteamID(callback.m_ulSteamIDUserChanged);

        if ((state & EChatMemberStateChange.k_EChatMemberStateChangeEntered) != 0)
        {
            SteamLobbyData.Instance.AddPlayer(playerSteamID);
            SteamLobbyData.Instance.SetPlayersCount(SteamMatchmaking.GetNumLobbyMembers(SteamLobbyData.Instance.LobbyID));
            Debug.Log($"Игрок {playerSteamID} присоединился!");
        }

        if ((state & EChatMemberStateChange.k_EChatMemberStateChangeLeft) != 0)
        {
            SteamLobbyData.Instance.RemovePlayer(playerSteamID);
            SteamLobbyData.Instance.SetPlayersCount(SteamMatchmaking.GetNumLobbyMembers(SteamLobbyData.Instance.LobbyID));
            SteamLobbyData.Instance.SetHostID(SteamMatchmaking.GetLobbyOwner(SteamLobbyData.Instance.LobbyID));
            Debug.Log($"Игрок {playerSteamID} покинул лобби.");
        }

        if ((state & EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) != 0)
        {
            SteamLobbyData.Instance.RemovePlayer(playerSteamID);
            SteamLobbyData.Instance.SetPlayersCount(SteamMatchmaking.GetNumLobbyMembers(SteamLobbyData.Instance.LobbyID));
            SteamLobbyData.Instance.SetHostID(SteamMatchmaking.GetLobbyOwner(SteamLobbyData.Instance.LobbyID));
            Debug.Log($"Игрок {playerSteamID} был отключен.");
        }
    }

    public void StartGame()
    {
        if (SteamLobbyData.Instance.MySteamData.SteamID != SteamLobbyData.Instance.HostID) return;
        if (_isGameStarting) return;
        Debug.Log("Игра запущена");
        _isGameStarting = true;

        SteamMatchmaking.SetLobbyData(SteamLobbyData.Instance.LobbyID, "game_started", "1");

        SceneManager.LoadScene(gameSceneName);
        NetworkManager.singleton.StartHost();
        OnGameStart?.Invoke();
    }

    private void OnLobbyChatMsg(LobbyChatMsg_t callback)
    {
        if (callback.m_ulSteamIDLobby != SteamLobbyData.Instance.LobbyID.m_SteamID) return;
        Debug.Log("Обновление сообщений в чате лобби");

        var senderID = callback.m_ulSteamIDUser;
        byte[] data = new byte[1024];
        int bytesRead = SteamMatchmaking.GetLobbyChatEntry(
            new CSteamID(callback.m_ulSteamIDLobby),
            (int)callback.m_iChatID,
            out CSteamID _,
            data,
            data.Length,
            out EChatEntryType _
        );

        if (bytesRead <= 0) return;

        string message = Encoding.UTF8.GetString(data, 0, bytesRead);

        if (message.StartsWith("KICK_") && senderID == SteamLobbyData.Instance.HostID.m_SteamID)
            KickUpdate(message);
    }

    private void KickUpdate(string message)
    {
        string[] parts = message.Split('_');
        if (parts.Length == 2 && ulong.TryParse(parts[1], out ulong kickedID))
        {
            CSteamID targetID = new CSteamID(kickedID);
            if (targetID == SteamUser.GetSteamID())
            {
                Debug.Log($"Вас кикнули из лобби: {SteamLobbyData.Instance.LobbyID}");
                OnExitLobby();
            }
        }
    }

    public void KickPlayer(CSteamID playerSteamID)
    {
        byte[] data = Encoding.UTF8.GetBytes($"KICK_{playerSteamID}");
        SteamMatchmaking.SendLobbyChatMsg(SteamLobbyData.Instance.LobbyID, data, data.Length);
        Debug.Log($"Кик игрока: {playerSteamID}");
    }

    public void OnExitLobby()
    {
        SteamMatchmaking.LeaveLobby(SteamLobbyData.Instance.LobbyID);
        SteamLobbyData.Instance.DeleteLobbyData();
        _isLobbyDataLoaded = false;

        Debug.Log($"Вы вышли из лобби: {SteamLobbyData.Instance.LobbyID}");
        OnLobbyExited?.Invoke();
    }

    public bool JoinLobbyToID(string lobbySteamID)
    {
        if (ulong.TryParse(lobbySteamID, out ulong lobbyIDNumber) && lobbyIDNumber != 0)
        {
            CSteamID lobbyID = new CSteamID(lobbyIDNumber);
            SteamMatchmaking.JoinLobby(lobbyID);
            Debug.Log($"Пытаемся присоединиться к лобби: {lobbyID}");
            return true;
        }
        else
        {
            Debug.Log("Неверный формат ID лобби! ID должен состоять только из цифр.");
            return false;
        }
    }

    public void Ready()
    {
        SteamMatchmaking.SetLobbyMemberData(
            SteamLobbyData.Instance.LobbyID,
            "ready",
            "1");
    }

    public void Unready()
    {
        SteamMatchmaking.SetLobbyMemberData(
            SteamLobbyData.Instance.LobbyID,
            "ready",
            "0");
    }
}