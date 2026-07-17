using UnityEngine;

public class SettingsUI : MonoBehaviour, IUIWindow
{
    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}