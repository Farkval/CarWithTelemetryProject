using UnityEngine;

/// <summary>
/// Контроллер камеры игрока
/// </summary>
public class CarCameraSwitch : MonoBehaviour
{
    public Camera thirdPersonCamera;
    public Camera topDownCamera;

    private CarCameraType _currentCameraMod = CarCameraType.ThirdPerson;

    private void Start()
    {
        SwitchCamera(CarCameraType.ThirdPerson);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            SwitchToNextCameraMod();
        }
    }

    private void SwitchToNextCameraMod()
    {
        if (_currentCameraMod == CarCameraType.ThirdPerson)
        {
            SwitchCamera(CarCameraType.TopDown);
        }
        else if (_currentCameraMod == CarCameraType.TopDown)
        {
            SwitchCamera(CarCameraType.ThirdPerson);
        }
    }

    private void SwitchCamera(CarCameraType currentCameraMod)
    {
        switch (currentCameraMod)
        {
            case CarCameraType.ThirdPerson:
                thirdPersonCamera.enabled = true;
                topDownCamera.enabled = false;
                break;

            case CarCameraType.TopDown:
                topDownCamera.enabled = true;
                thirdPersonCamera.enabled = false;
                break;
        }

        _currentCameraMod = currentCameraMod;
    }
}
