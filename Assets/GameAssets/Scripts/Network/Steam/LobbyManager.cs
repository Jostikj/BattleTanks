using Mirror;
using Steamworks;
using System;
using System.Text;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    [Header("Singleton")]
    public static LobbyManager Instance;

    [Header("Actions")]
    public Action onLobbyEntered;
    public Action OnLobbyExited;
    public Action<JoinLobbyResults> OnJoinLobbyResults;
    public Action<string> OnGameStartedHost;
    public Action<string> OnGameStartedClient;
    public Action<string> OnMapChange;

    [Header("Flags")]
    private bool _isLobbyCreating = false;
    private bool _isGameStarting = false;
    private bool _isLobbyDataLoaded = false;
    private bool _isInLobby;

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
        SteamMatchmaking.CreateLobby(LobbyData.Instance.LobbyType, LobbyData.Instance.MaxPlayersCount);
        _isLobbyCreating = true;
        Debug.Log("Запрос на создание лобби отправлен в Steam...");
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        _isLobbyCreating = false;

        if (callback.m_eResult != EResult.k_EResultOK)
        {
            LobbyData.Instance.DeleteLobbyData();
            Debug.LogError($"Не удалось создать лобби. Код ошибки: {callback.m_eResult}");
            return;
        }

        LobbyData.Instance.SetLobbyID(new CSteamID(callback.m_ulSteamIDLobby));
        Debug.Log($"Лобби успешно создано! ID: {LobbyData.Instance.LobbyID}");

        SteamMatchmaking.SetLobbyData(LobbyData.Instance.LobbyID, "lobbyName", LobbyData.Instance.LobbyName);
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        Debug.Log($"Получен запрос на вступление в лобби {callback.m_steamIDLobby}");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private JoinLobbyResults ConvertJoinResults(EChatRoomEnterResponse response)
    {
        switch (response)
        {
            case EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess:
                return JoinLobbyResults.Success;

            case EChatRoomEnterResponse.k_EChatRoomEnterResponseDoesntExist:
                return JoinLobbyResults.LobbyDoesNotExist;

            case EChatRoomEnterResponse.k_EChatRoomEnterResponseFull:
                return JoinLobbyResults.LobbyIsFull;

            case EChatRoomEnterResponse.k_EChatRoomEnterResponseBanned:
            case EChatRoomEnterResponse.k_EChatRoomEnterResponseNotAllowed:
                return JoinLobbyResults.NoPermission;

            default:
                return JoinLobbyResults.UnknownError;
        }
    }


    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        OnJoinLobbyResults?.Invoke(ConvertJoinResults((EChatRoomEnterResponse)callback.m_EChatRoomEnterResponse));

        if ((EChatRoomEnterResponse)callback.m_EChatRoomEnterResponse != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            Debug.LogWarning($"Не удалось войти в лобби. Причина: {(EChatRoomEnterResponse)callback.m_EChatRoomEnterResponse}");
            return;
        }

        LobbyData.Instance.SetLobbyID((CSteamID)callback.m_ulSteamIDLobby);
        Debug.Log($"Успешно подключён к лобби: {LobbyData.Instance.LobbyID}");
        LobbyData.Instance.SetLobbyID(new CSteamID(callback.m_ulSteamIDLobby));
        LobbyData.Instance.SetHostID(new CSteamID((ulong)SteamMatchmaking.GetLobbyOwner(LobbyData.Instance.LobbyID)));

        SteamMatchmaking.RequestLobbyData(LobbyData.Instance.LobbyID);
        _isInLobby = true;
    }

    private void OnLobbyDataUpdate(LobbyDataUpdate_t callback)
    {
        if (!_isInLobby)
            return;

        if (callback.m_ulSteamIDLobby != LobbyData.Instance.LobbyID.m_SteamID)
            return;

        if (!NetworkServer.active && !_isGameStarting)
            ClientGameStarted();

        if (!_isLobbyDataLoaded)
        {
            LobbyDataLoad();
        }
        UpdateReadyState();

        MapUpdate();
    }

    private void MapUpdate()
    {
        OnMapChange?.Invoke(SteamMatchmaking.GetLobbyData(LobbyData.Instance.LobbyID, "mapName"));
    }

    private void LobbyDataLoad()
    {
        Debug.Log("Загрузка данных лобби");
        LobbyData.Instance.SetMaxPlayersCount(Convert.ToInt32(SteamMatchmaking.GetLobbyMemberLimit(LobbyData.Instance.LobbyID)));
        LobbyData.Instance.SetLobbyName(SteamMatchmaking.GetLobbyData(LobbyData.Instance.LobbyID, "lobbyName"));
        LobbyData.Instance.SetPlayersCount(SteamMatchmaking.GetNumLobbyMembers(LobbyData.Instance.LobbyID));

        for (int i = 0; i < LobbyData.Instance.PlayersCount; i++)
        {
            LobbyData.Instance.AddPlayer(SteamMatchmaking.GetLobbyMemberByIndex(LobbyData.Instance.LobbyID, i));
        }
        UpdateReadyState();
        NetworkManager.singleton.networkAddress = LobbyData.Instance.HostID.ToString();

        _isLobbyDataLoaded = true;
        onLobbyEntered?.Invoke();
    }

    private void UpdateReadyState()
    {
        int readyPlayers = 0;

        for (int i = 0; i < LobbyData.Instance.PlayersCount; i++)
        {
            CSteamID player = SteamMatchmaking.GetLobbyMemberByIndex(
                LobbyData.Instance.LobbyID,
                i);

            bool ready =
                SteamMatchmaking.GetLobbyMemberData(
                    LobbyData.Instance.LobbyID,
                    player,
                    "ready") == "1";

            LobbyData.Instance.SetPlayerReady(player, ready);


            if (ready)
                readyPlayers++;
        }

        LobbyData.Instance.SetReadyPlayersCount(readyPlayers);
    }

    public void InviteFriends()
    {
        if (!LobbyData.Instance.LobbyID.IsValid())
        {
            Debug.LogWarning("Нет активного лобби для приглашения");
            return;
        }
        Debug.Log("Приглашение друзей");
        SteamFriends.ActivateGameOverlayInviteDialog(LobbyData.Instance.LobbyID);
    }

    private void ClientGameStarted()
    {
        if (SteamMatchmaking.GetLobbyData(LobbyData.Instance.LobbyID, "gameStarted") == "1")
        {
            _isGameStarting = true;
            OnGameStartedClient?.Invoke(SteamMatchmaking.GetLobbyData(LobbyData.Instance.LobbyID, "mapName"));
            Debug.Log("Игра запущена");
        }
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        if (!_isInLobby)
            return;

        if (callback.m_ulSteamIDLobby != LobbyData.Instance.LobbyID.m_SteamID)
            return;

        ConnectedAndDisconnectedPlayer(callback);
        UpdateReadyState();
    }

    private void ConnectedAndDisconnectedPlayer(LobbyChatUpdate_t callback)
    {
        var state = (EChatMemberStateChange)callback.m_rgfChatMemberStateChange;
        CSteamID playerSteamID = new CSteamID(callback.m_ulSteamIDUserChanged);

        if ((state & EChatMemberStateChange.k_EChatMemberStateChangeEntered) != 0)
        {
            LobbyData.Instance.AddPlayer(playerSteamID);
            LobbyData.Instance.SetPlayersCount(SteamMatchmaking.GetNumLobbyMembers(LobbyData.Instance.LobbyID));
            Debug.Log($"Игрок {playerSteamID} присоединился!");
        }

        if ((state & EChatMemberStateChange.k_EChatMemberStateChangeLeft) != 0)
        {
            LobbyData.Instance.RemovePlayer(playerSteamID);
            LobbyData.Instance.RemovePlayerReady(playerSteamID);
            LobbyData.Instance.SetPlayersCount(SteamMatchmaking.GetNumLobbyMembers(LobbyData.Instance.LobbyID));
            LobbyData.Instance.SetHostID(SteamMatchmaking.GetLobbyOwner(LobbyData.Instance.LobbyID));
            Debug.Log($"Игрок {playerSteamID} покинул лобби.");
        }

        if ((state & EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) != 0)
        {
            LobbyData.Instance.RemovePlayer(playerSteamID);
            LobbyData.Instance.RemovePlayerReady(playerSteamID);
            LobbyData.Instance.SetPlayersCount(SteamMatchmaking.GetNumLobbyMembers(LobbyData.Instance.LobbyID));
            LobbyData.Instance.SetHostID(SteamMatchmaking.GetLobbyOwner(LobbyData.Instance.LobbyID));
            Debug.Log($"Игрок {playerSteamID} был отключен.");
        }
    }

    public void StartGameHost(string mapName)
    {
        if (LobbyData.Instance.MyData.SteamID != LobbyData.Instance.HostID || _isGameStarting) return;
        _isGameStarting = true;
        MapChange(mapName);
        SteamMatchmaking.SetLobbyData(LobbyData.Instance.LobbyID, "gameStarted", "1");
        OnGameStartedHost?.Invoke(mapName);
        Debug.Log("Игра запущена");
    }

    public void MapChange(string mapName)
    {
        SteamMatchmaking.SetLobbyData(LobbyData.Instance.LobbyID, "mapName", mapName);
    }

    private void OnLobbyChatMsg(LobbyChatMsg_t callback)
    {
        if (callback.m_ulSteamIDLobby != LobbyData.Instance.LobbyID.m_SteamID) return;
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

        if (message.StartsWith("KICK_") && senderID == LobbyData.Instance.HostID.m_SteamID)
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
                Debug.Log($"Вас кикнули из лобби: {LobbyData.Instance.LobbyID}");
                LeaveLobby();
            }
        }
    }

    public void KickPlayer(CSteamID playerSteamID)
    {
        byte[] data = Encoding.UTF8.GetBytes($"KICK_{playerSteamID}");
        SteamMatchmaking.SendLobbyChatMsg(LobbyData.Instance.LobbyID, data, data.Length);
        Debug.Log($"Кик игрока: {playerSteamID}");
    }

    public void LeaveLobby()
    {
        if (LobbyData.Instance.PlayersCount == 1)
            SteamMatchmaking.SetLobbyJoinable(LobbyData.Instance.LobbyID, false);
        SteamMatchmaking.LeaveLobby(LobbyData.Instance.LobbyID);

        _isInLobby = false;
        _isLobbyDataLoaded = false;
        Debug.Log($"Вы вышли из лобби: {LobbyData.Instance.LobbyID}");
        LobbyData.Instance.DeleteLobbyData();
        OnLobbyExited?.Invoke();
    }

    public void JoinLobbyToID(string lobbySteamID)
    {
        if (ulong.TryParse(lobbySteamID, out ulong lobbyIDNumber) && lobbyIDNumber != 0)
        {
            CSteamID lobbyID = new CSteamID(lobbyIDNumber);
            SteamMatchmaking.JoinLobby(lobbyID);
            Debug.Log($"Пытаемся присоединиться к лобби: {lobbyID}");
        }
        else Debug.Log("Неверный формат ID лобби! ID должен состоять только из цифр.");
    }

    public void Ready()
    {
        SteamMatchmaking.SetLobbyMemberData(
            LobbyData.Instance.LobbyID,
            "ready",
            "1");
    }

    public void Unready()
    {
        SteamMatchmaking.SetLobbyMemberData(
            LobbyData.Instance.LobbyID,
            "ready",
            "0");
    }
}