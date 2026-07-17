using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public LobbyUI LobbyUI;
    public MenuUI MenuUI;
    public GameUI GameUI;
    public SettingsUI SettingsUI;

    private IUIWindow _currentScreen;
    private Stack<IUIWindow> _screenHistory = new();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(gameObject);
    }

    public void OpenScreen(IUIWindow screen)
    {
        if (_currentScreen != null)
        {
            _currentScreen.Hide();
            _screenHistory.Push(_currentScreen);
        }

        _currentScreen = screen;
        _currentScreen.Show();
    }

    public void Back()
    {
        if(_screenHistory.Count == 0) return;

        _currentScreen.Hide();

        _currentScreen = _screenHistory.Pop();
        _currentScreen.Show();
    }

    public void OpenSettingsUI()
    {
        OpenScreen(SettingsUI);
    }

    public void OpenMenuUI()
    {
        OpenScreen(MenuUI);
    }

    public void OpenGameUI()
    {
        OpenScreen(GameUI);
    }
}

public interface IUIWindow
{
    void Show();
    void Hide();
}