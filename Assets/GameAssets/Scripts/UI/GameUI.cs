using UnityEngine;
using UnityEngine.InputSystem;

public class GameUI : MonoBehaviour, IUIWindow
{
    #region Variables

    [Header("PauseUI")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _settingsPanel;

    [Header("GameUI")]

    [Header("HUD")]
    [SerializeField] private GameObject _HUD;

    [Header("Components")]
    private InputSystem _inputSystem;

    #endregion

    private void Awake()
    {
        _inputSystem = new InputSystem();
        _HUD.SetActive(true);
        _pausePanel.SetActive(false);
        _settingsPanel.SetActive(false);
    }

    private void OnEnable()
    {
        _inputSystem.GameUI.PauseUI.performed += OnPause;
        _inputSystem.Enable();
    }

    private void OnDisable()
    {
        _inputSystem.GameUI.PauseUI.performed -= OnPause;
        _inputSystem.Disable();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void HideAllPanels()
    {
        _pausePanel.SetActive(false);
    }

    #region EscUI

    private void OnPause(InputAction.CallbackContext context)
    {
        if (_settingsPanel.activeSelf)
        {
            _settingsPanel.SetActive(false);
        }
        else
        {
            _pausePanel.SetActive(!_pausePanel.activeSelf);
            _HUD.SetActive(!_HUD.activeSelf);
        }
    }

    public void OpenHUD()
    {
        _HUD.SetActive(true);
        HideAllPanels();
    }

    public void OnExitToMenu()
    {
        
    }

    public void OnExitApplication()
    {
        Application.Quit();
    }

    #endregion

    #region GameUI

    #endregion
}