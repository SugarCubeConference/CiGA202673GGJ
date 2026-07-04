using UnityEngine;

/// <summary>
/// 角色身份枚举：Player（玩家）或 Ghost（分身/幽灵）
/// </summary>
public enum DeathAnchorActorKind
{
    Player,
    Ghost
}

/// <summary>
/// 标记 GameObject 的角色身份。
/// 其他组件通过查询此组件判断该对象是玩家还是分身，
/// 用于按钮按压、陷阱触发、钥匙拾取等交互逻辑。
/// </summary>
public sealed class ActorIdentity : MonoBehaviour
{
    [SerializeField] private DeathAnchorActorKind kind;

    /// <summary>当前角色的身份类型</summary>
    public DeathAnchorActorKind Kind => kind;
    /// <summary>是否为玩家</summary>
    public bool IsPlayer => kind == DeathAnchorActorKind.Player;
    /// <summary>是否为分身/幽灵</summary>
    public bool IsGhost => kind == DeathAnchorActorKind.Ghost;

    /// <summary>运行时动态修改角色身份（Baker 创建时调用）</summary>
    public void SetKind(DeathAnchorActorKind nextKind)
    {
        kind = nextKind;
    }
}