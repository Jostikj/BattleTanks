using Edgegap;
using Mirror;
using Steamworks;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SteamLobbyManager : MonoBehaviour
{
    #region Переписал
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

    public void CreateGameLobby()
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
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        Debug.Log($"Получен запрос на вступление в лобби {callback.m_steamIDLobby}");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        SteamLobbyData.Instance.SetLobbyID(new CSteamID(callback.m_ulSteamIDLobby));
        SteamLobbyData.Instance.SetHostID(new CSteamID((ulong)SteamMatchmaking.GetLobbyOwner(SteamLobbyData.Instance.LobbyID)));

        SteamMatchmaking.RequestLobbyData(SteamLobbyData.Instance.LobbyID);
    }

    private void OnLobbyDataUpdate(LobbyDataUpdate_t callback)
    {
        if (callback.m_ulSteamIDLobby != SteamLobbyData.Instance.LobbyID.m_SteamID) return;

        if (!NetworkServer.active && !_isGameStarting)
            ClientGameStartedCheck();

        if (!_isLobbyDataLoaded)
            LobbyDataLoad();
    }

    private void LobbyDataLoad()
    {
        _isLobbyDataLoaded = true;
        SteamLobbyData.Instance.SetMaxPlayersCount(Convert.ToInt32(SteamMatchmaking.GetLobbyData(SteamLobbyData.Instance.LobbyID, "lobbyMaxPlayersCount")));
        SteamLobbyData.Instance.SetLobbyName(SteamMatchmaking.GetLobbyData(SteamLobbyData.Instance.LobbyID, "lobbyName"));
        SteamLobbyData.Instance.SetPlayersCount(SteamMatchmaking.GetNumLobbyMembers(SteamLobbyData.Instance.LobbyID));

        for (int i = 0; i < SteamLobbyData.Instance.PlayersCount; i++)
        {
            SteamLobbyData.Instance.AddPlayer(SteamMatchmaking.GetLobbyMemberByIndex(SteamLobbyData.Instance.LobbyID, i));
        }

        NetworkManager.singleton.networkAddress = SteamLobbyData.Instance.HostID.ToString();

        onLobbyEntered?.Invoke();
    }

    public void InviteFriends()
    {
        if (!SteamLobbyData.Instance.LobbyID.IsValid())
        {
            Debug.LogWarning("Нет активного лобби для приглашения");
            return;
        }

        SteamFriends.ActivateGameOverlayInviteDialog(SteamLobbyData.Instance.LobbyID);
    }

    private void ClientGameStartedCheck()
    {
        if (SteamMatchmaking.GetLobbyData(SteamLobbyData.Instance.LobbyID, "game_started") == "1")
        {
            _isGameStarting = true;
            SceneManager.LoadScene(gameSceneName);
            NetworkManager.singleton.StartClient();
        }
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        Debug.Log("LobbyChatUpdate");
        ConnectedAndDisconnectedPlayer(callback);
    }

    private void ConnectedAndDisconnectedPlayer(LobbyChatUpdate_t callback)
    {
        Debug.Log(
            $"ChatUpdate: " +
            $"Changed={callback.m_ulSteamIDUserChanged}, " +
            $"MakingChange={callback.m_ulSteamIDMakingChange}, " +
            $"State={(EChatMemberStateChange)callback.m_rgfChatMemberStateChange}"
            );

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
        _isGameStarting = true;

        SteamMatchmaking.SetLobbyData(SteamLobbyData.Instance.LobbyID, "game_started", "1");

        SceneManager.LoadScene(gameSceneName);
        NetworkManager.singleton.StartHost();
        OnGameStart?.Invoke();
    }

    private void OnLobbyChatMsg(LobbyChatMsg_t callback)
    {
        if (callback.m_ulSteamIDLobby != SteamLobbyData.Instance.LobbyID.m_SteamID) return;
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

        string message = System.Text.Encoding.UTF8.GetString(data, 0, bytesRead);

        if (message.StartsWith("KiCK_") && senderID == SteamLobbyData.Instance.HostID.m_SteamID)
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
                Debug.Log("Вас кикнули из лобби!");
                OnExitLobby();
            }
        }
    }

    public void KickPlayer(CSteamID steamID)
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes($"KICK_{steamID}");
        SteamMatchmaking.SendLobbyChatMsg(SteamLobbyData.Instance.LobbyID, data, data.Length);
    }

    public void OnExitLobby()
    {
        SteamMatchmaking.LeaveLobby(SteamLobbyData.Instance.LobbyID);
        SteamLobbyData.Instance.DeleteLobbyData();
        _isLobbyDataLoaded = false;
        Debug.Log("Вы вышли из лобби...");
        OnLobbyExited?.Invoke();
    }

    public bool JoinLobbyToID(string lobbySteamID)
    {
        if (ulong.TryParse(lobbySteamID, out ulong lobbyIDNumber) && lobbyIDNumber != 0)
        {
            CSteamID lobbyID = new CSteamID(lobbyIDNumber);
            SteamMatchmaking.JoinLobby(lobbyID);
            Debug.Log($"Пытаемся присоединиться к лобби с ID: {lobbyID}");
            return true;
        }
        else
        {
            Debug.Log("Неверный формат ID лобби! ID должен состоять только из цифр.");
            return false;
        }
    }
    #endregion
}