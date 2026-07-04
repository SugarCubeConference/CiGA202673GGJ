using System;
using UnityEngine;

/// <summary>
/// 关卡数据模型——对应对关卡 JSON 文件的完整结构。
/// 由 DeathAnchorLevelBaker 解析 JSON 后使用。
/// </summary>
[Serializable]
public sealed class DeathAnchorLevelData
{
    public string schema;
    public string title;
    public DeathAnchorRules rules;
    public DeathAnchorWorld world;
    public DeathAnchorPlayerSpec player;
    public DeathAnchorLevelObject spawn;
    public DeathAnchorLevelObject[] goals;
    public DeathAnchorLevelObject[] platforms;
    public DeathAnchorLevelObject[] movingPlatforms;
    public DeathAnchorLevelObject[] spikes;
    public DeathAnchorLevelObject[] lasers;
    public DeathAnchorLevelObject[] keys;
    public DeathAnchorLevelObject[] doors;
    public DeathAnchorLevelObject[] buttons;
    public DeathAnchorLevelObject[] bridges;
    public DeathAnchorLevelObject[] anchorZones;
    public DeathAnchorLevelObject[] notes;
}

/// <summary>关卡规则配置</summary>
[Serializable]
public sealed class DeathAnchorRules
{
    public float recordWindowSec = 5f;       // 录制窗口时长（秒）
    public int maxGhosts = 1;                // 最大分身数量
    public bool ghostSolid = true;           // 分身是否有碰撞
    public bool ghostCanStandOnPlayer = true; // 分身能否站在玩家头上
    public bool ghostCanPressButtons = true;  // 分身能否按压按钮
    public bool ghostIgnoresHazards = true;   // 分身是否免疫陷阱
    public bool playerWallSlide = true;       // 玩家能否蹬墙滑行
    public float wallSlideMaxSpeed = 125f;
}

[Serializable]
public sealed class DeathAnchorWorld
{
    public float w = 2400f;
    public float h = 720f;
    public float grid = 20f;
}

[Serializable]
public sealed class DeathAnchorPlayerSpec
{
    public float w = 30f;
    public float h = 42f;
    public DeathAnchorPlayerAbilities abilities;
}

[Serializable]
public sealed class DeathAnchorPlayerAbilities
{
    public bool wallSlide = true;
    public float wallSlideMaxSpeed = 125f;    // 蹬墙滑行最大速度（像素/秒）
}

[Serializable]
public sealed class DeathAnchorLevelObject
{
    public string id;
    public string type;
    public string label;
    public float x;
    public float y;
    public float w;
    public float h;
    public float rotation;
    public string channel;
    public string[] links;
    public string[] tags;
    public string notes;
    public string requiredKey;
    public string requiredButton;
    public string mode;
    public string pressedBy;
    public string affects;
    public string defaultState;
    public string activeState;
    public string motionMode;
    public float moveTargetX;
    public float moveTargetY;
    public float periodSec;
    public float moveSpeed;
    public string attachedTo;
    public float dirX;
    public float dirY;
    public float maxDistance;
    public float[] beamColor;
}

/// <summary>
/// 录制帧——存储每一帧的输入快照，而非位置快照。
/// 这样分身在回放时可以用自己的物理系统重新模拟，从而支持反重力等变体。
/// </summary>
[Serializable]
public struct DeathAnchorReplayFrame
{
    public float time;             // 录制时间（相对于录制开始）
    public Vector2 footOffset;     // 脚底偏移量
    public int facing;             // 朝向 (1=右, -1=左)

    public DeathAnchorReplayFrame(float time, Vector2 footOffset, int facing)
    {
        this.time = time;
        this.footOffset = footOffset;
        this.facing = facing;
    }
}
