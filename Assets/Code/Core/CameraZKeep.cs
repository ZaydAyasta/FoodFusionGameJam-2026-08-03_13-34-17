using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineBrain))]
public class LockMainCameraZ : MonoBehaviour
{
    private CinemachineBrain brain;
    private float fixedZ;

    private void Awake()
    {
        brain = GetComponent<CinemachineBrain>();
        fixedZ = transform.position.z;
    }

    private void OnEnable()
    {
        CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
    }

    private void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
    }

    private void OnCameraUpdated(CinemachineBrain updatedBrain)
    {
        if (updatedBrain != brain)
            return;

        Vector3 pos = transform.position;
        pos.z = fixedZ;
        transform.position = pos;
    }
}