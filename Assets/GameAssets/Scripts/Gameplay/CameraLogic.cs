using Mirror;
using UnityEngine;

public class CameraLogic : NetworkBehaviour
{
    [SerializeField] private GameObject _camera;

    private void Start()
    {
        if (isLocalPlayer)
            _camera.SetActive(true);
    }
}