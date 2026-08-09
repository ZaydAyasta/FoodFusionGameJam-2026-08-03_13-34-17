using UnityEngine;

public class ProceduralRoomCandidate : MonoBehaviour
{
    [SerializeField] private string theme;
    [SerializeField] private RoomDirection exitDirectionFromPreviousRoom;
    [SerializeField] private RoomDirection entryDirection;
    [SerializeField] private ProceduralRoomLayout layout;
    [SerializeField] private ProceduralRoomCommitTrigger commitTrigger;
    [SerializeField] private IngredientData promisedReward;

    public string Theme => theme;
    public RoomDirection ExitDirectionFromPreviousRoom => exitDirectionFromPreviousRoom;
    public RoomDirection EntryDirection => entryDirection;
    public ProceduralRoomLayout Layout => layout;
    public ProceduralRoomCommitTrigger CommitTrigger => commitTrigger;
    public IngredientData PromisedReward => promisedReward;

    public void Initialize(
        string candidateTheme,
        RoomDirection candidateExitDirectionFromPreviousRoom,
        RoomDirection candidateEntryDirection,
        ProceduralRoomLayout candidateLayout,
        ProceduralRoomCommitTrigger candidateCommitTrigger,
        IngredientData candidatePromisedReward)
    {
        theme = candidateTheme;
        exitDirectionFromPreviousRoom = candidateExitDirectionFromPreviousRoom;
        entryDirection = candidateEntryDirection;
        layout = candidateLayout;
        commitTrigger = candidateCommitTrigger;
        promisedReward = candidatePromisedReward;
    }
}
