using UnityEngine;

[CreateAssetMenu(fileName = "PatrolData", menuName = "Scriptable Objects/PatrolData")]
public class PatrolData : ScriptableObject
{
    public float speed;
    public float range;
    public float toleranceDistance;
    public int maxAttempts = 30;
}
