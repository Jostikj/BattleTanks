using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDataPanelLogic : MonoBehaviour
{
    [SerializeField] private RawImage _avatarImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private GameObject _kickButton;
    [SerializeField] private GameObject _hostCrown;
    [SerializeField] private GameObject _readyText;
    [SerializeField] private GameObject _unreadyText;
    public PlayerSteamData PlayerData { get; private set; }

    public void PlayerKick()
    {
        if (SteamLobbyData.Instance.HostID != SteamLobbyData.Instance.MySteamData.SteamID) return;
        SteamLobbyManager.Instance.KickPlayer(PlayerData.SteamID);
    }

    public void InitializePanel(PlayerSteamData player)
    {
        PlayerData = player;
        _avatarImage.texture = player.Avatar;
        _nameText.text = player.Name;
        KickButtonVisible();
        HostCrownVisible();
        ReadyTextUpdate(SteamLobbyData.Instance.IsPlayerReady(player.SteamID));
    }

    public void ResetPanel()
    {
        KickButtonVisible();
        HostCrownVisible();
    }

    private void KickButtonVisible()
    {
        if (SteamLobbyData.Instance.HostID == SteamLobbyData.Instance.MySteamData.SteamID)
            _kickButton.SetActive(true);
        else _kickButton.SetActive(false);

        if (PlayerData.SteamID == SteamLobbyData.Instance.HostID) _kickButton.SetActive(false);
    }

    private void HostCrownVisible()
    {
        if (PlayerData.SteamID == SteamLobbyData.Instance.HostID)
            _hostCrown.SetActive(true);
        else _hostCrown.SetActive(false);
    }

    public void ReadyTextUpdate(bool ready)
    {
        _readyText.SetActive(ready);
        _unreadyText.SetActive(!ready);
    }
}