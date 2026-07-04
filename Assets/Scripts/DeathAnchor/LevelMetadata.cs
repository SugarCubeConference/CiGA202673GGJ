using UnityEngine;

/// <summary>
/// 关卡元数据——存储从 JSON 解析出来的关卡配置信息，
/// 挂载在烘焙场景的 BakedLevelRoot 上供运行时查询。
/// </summary>
public sealed class LevelMetadata : MonoBehaviour
{
    [SerializeField] private string sourceJson;
    [SerializeField] private string title;
    [SerializeField] private Vector2 worldSizePixels;
    [SerializeField] private float recordWindowSec = 5f;
    [SerializeField] private bool ghostCanStandOnPlayer = true;
    [SerializeField] private bool ghostCanPressButtons = true;
    [SerializeField] private bool playerWallSlide = true;

    /// <summary>来源 JSON 文件名</summary>
    public string SourceJson => sourceJson;
    /// <summary>关卡标题</summary>
    public string Title => title;
    /// <summary>世界尺寸（像素）</summary>
    public Vector2 WorldSizePixels => worldSizePixels;
    /// <summary>录制窗时长（秒）</summary>
    public float RecordWindowSec => recordWindowSec;

    /// <summary>由 Baker 调用，从关卡数据初始化元数据</summary>
    public void Initialize(string sourceJson, DeathAnchorLevelData level)
    {
        this.sourceJson = sourceJson;
        title = level != null ? level.title : string.Empty;
        worldSizePixels = level != null && level.world != null ? new Vector2(level.world.w, level.world.h) : Vector2.zero;

        DeathAnchorRules rules = level != null ? level.rules : null;
        recordWindowSec = rules != null ? rules.recordWindowSec : 5f;
        ghostCanStandOnPlayer = rules == null || rules.ghostCanStandOnPlayer;
        ghostCanPressButtons = rules == null || rules.ghostCanPressButtons;
        playerWallSlide = rules == null || rules.playerWallSlide;
    }
}