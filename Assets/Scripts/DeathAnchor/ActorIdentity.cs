using UnityEngine;

public enum DeathAnchorActorKind
{
    Player,
    Ghost
}

public sealed class ActorIdentity : MonoBehaviour
{
    [SerializeField] private DeathAnchorActorKind kind;

    public DeathAnchorActorKind Kind => kind;
    public bool IsPlayer => kind == DeathAnchorActorKind.Player;
    public bool IsGhost => kind == DeathAnchorActorKind.Ghost;

    public void SetKind(DeathAnchorActorKind nextKind)
    {
        kind = nextKind;
    }
}
