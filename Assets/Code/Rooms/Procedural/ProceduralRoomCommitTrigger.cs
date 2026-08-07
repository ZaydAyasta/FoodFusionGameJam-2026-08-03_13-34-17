using UnityEngine;

public class ProceduralRoomCommitTrigger : MonoBehaviour
{
    private RoomGenerationTestBootstrap generator;
    private ProceduralRoomCandidate candidate;
    private bool committed;

    public void Initialize(RoomGenerationTestBootstrap owner, ProceduralRoomCandidate roomCandidate)
    {
        generator = owner;
        candidate = roomCandidate;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (committed || generator == null || candidate == null)
            return;

        CharacterInput player = other.GetComponentInParent<CharacterInput>();
        if (player == null)
            return;

        committed = true;
        generator.CommitCandidate(candidate, player.transform);
    }
}
