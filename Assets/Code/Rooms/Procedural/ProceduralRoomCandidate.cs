using UnityEngine;

public class ProceduralRoomCandidate : MonoBehaviour
{
    [SerializeField] private string theme;
    [SerializeField] private RoomDirection exitDirectionFromPreviousRoom;
    [SerializeField] private RoomDirection entryDirection;
    [SerializeField] private ProceduralRoomLayout layout;
    [SerializeField] private ProceduralRoomCommitTrigger commitTrigger;

    public string Theme => theme;
    public RoomDirection ExitDirectionFromPreviousRoom => exitDirectionFromPreviousRoom;
    public RoomDirection EntryDirection => entryDirection;
    public ProceduralRoomLayout Layout => layout;
    public ProceduralRoomCommitTrigger CommitTrigger => commitTrigger;

    public void Initialize(
        string candidateTheme,
        RoomDirection candidateExitDirectionFromPreviousRoom,
        RoomDirection candidateEntryDirection,
        ProceduralRoomLayout candidateLayout,
        ProceduralRoomCommitTrigger candidateCommitTrigger)
    {
        theme = candidateTheme;
        exitDirectionFromPreviousRoom = candidateExitDirectionFromPreviousRoom;
        entryDirection = candidateEntryDirection;
        layout = candidateLayout;
        commitTrigger = candidateCommitTrigger;
    }
}
