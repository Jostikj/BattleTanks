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
    public PlayerData PlayerData { get; private set; }

    public void PlayerKick()
    {
        if (LobbyData.Instance.HostID != LobbyData.Instance.MyData.SteamID) return;
        LobbyManager.Instance.KickPlayer(PlayerData.SteamID);
    }

    public void InitializePanel(PlayerData player)
    {
        PlayerData = player;
        _avatarImage.texture = player.Avatar;
        _nameText.text = player.Name;
        KickButtonVisible();
        HostCrownVisible();
        ReadyTextUpdate(LobbyData.Instance.IsPlayerReady(player.SteamID));
    }

    public void ResetPanel()
    {
        KickButtonVisible();
        HostCrownVisible();
    }

    private void KickButtonVisible()
    {
        if (LobbyData.Instance.HostID == LobbyData.Instance.MyData.SteamID)
            _kickButton.SetActive(true);
        else _kickButton.SetActive(false);

        if (PlayerData.SteamID == LobbyData.Instance.HostID) _kickButton.SetActive(false);
    }

    private void HostCrownVisible()
    {
        if (PlayerData.SteamID == LobbyData.Instance.HostID)
            _hostCrown.SetActive(true);
        else _hostCrown.SetActive(false);
    }

    public void ReadyTextUpdate(bool ready)
    {
        _readyText.SetActive(ready);
        _unreadyText.SetActive(!ready);
    }
}