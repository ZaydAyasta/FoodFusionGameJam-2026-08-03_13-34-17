using UnityEngine;

public class FactionMember : MonoBehaviour
{
    [SerializeField] private CombatFaction faction;

    public CombatFaction Faction => faction;

    public void SetFaction(CombatFaction value)
    {
        faction = value;
    }
}
