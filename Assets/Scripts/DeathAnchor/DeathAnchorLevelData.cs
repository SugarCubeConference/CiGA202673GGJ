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
    public float wallSlideMaxSpeed = 125f;    // 蹬墙滑行最大速度（像素/秒）
}

/// <summary>关卡世界参数</summary>
[Serializable]
public sealed class DeathAnchorWorld
{
    public float w = 2400f;    // 世界宽度（像素）
    public float h = 720f;     // 世界高度（像素）
    public float grid = 20f;   // 网格大小
}

/// <summary>玩家规格</summary>
[Serializable]
public sealed class DeathAnchorPlayerSpec
{
    public float w = 30f;                         // 玩家宽度（像素）
    public float h = 42f;                         // 玩家高度（像素）
    public DeathAnchorPlayerAbilities abilities;   // 玩家能力
}

/// <summary>玩家能力配置</summary>
[Serializable]
public sealed class DeathAnchorPlayerAbilities
{
    public bool wallSlide = true;        // 是否允许蹬墙
    public float wallSlideMaxSpeed = 125f; // 蹬墙最大速度
}

/// <summary>关卡中的单个物体（平台、陷阱等通用结构）</summary>
[Serializable]
public sealed class DeathAnchorLevelObject
{
    public string id;
    public string type;
    public string label;
    public float x, y, w, h;       // 位置和尺寸（像素坐标）
    public float rotation;         // 旋转角度
    public string channel;
    public string[] links;         // 关联的其他物体 ID（按钮→桥梁）
    public string[] tags;
    public string notes;
    public string requiredKey;    // 钥匙门需要的钥匙 ID
    public string requiredButton;
    public string mode;
    public string pressedBy;      // 按钮按压者："player" / "ghost" / "both"
    public string affects;
    public string defaultState;   // 桥梁默认状态："solid" / 其他
    public string activeState;    // 桥梁激活状态
    public float moveTargetX, moveTargetY; // 移动平台目标位置
    public float periodSec;       // 移动平台周期（秒）
}

/// <summary>
/// 录制帧——存储每一帧的输入快照，而非位置快照。
/// 这样分身在回放时可以用自己的物理系统重新模拟，从而支持反重力等变体。
/// </summary>
[Serializable]
public struct DeathAnchorReplayFrame
{
    public float time;             // 录制时间（相对于录制开始）
    public float horizontalInput;  // 水平输入 (-1/0/+1)
    public bool jumpPressed;       // 跳跃是否按下
    public int facing;             // 朝向 (1=右, -1=左)

    public DeathAnchorReplayFrame(float time, float horizontalInput, bool jumpPressed, int facing)
    {
        this.time = time;
        this.horizontalInput = horizontalInput;
        this.jumpPressed = jumpPressed;
        this.facing = facing;
    }
}