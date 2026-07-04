using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Death Anchor 游戏核心管理器。
/// 负责录制玩家的操作输入、生成分身幽灵、处理玩家死亡/复活/过关逻辑。
/// 
/// 按键操作：
///   E  — 开始录制（放下锚点）
///   R  — 重新加载场景
///   WASD/方向键 — 移动和跳跃
/// 
/// 核心循环：
///   1. 按 E 开始录制玩家输入（录制窗口期内）
///   2. 录制结束或玩家死亡 → 生成分身幽灵在锚点位置
///   3. 分身回放玩家的操作，玩家可利用分身协作解谜
///   4. 触碰陷阱死亡 → 复活在上一个锚点位置
///   5. 触碰终点 → 加载下一关
/// </summary>
public sealed class DeathAnchorGameManager : MonoBehaviour
{
    [Header("场景引用")]
    [SerializeField] private DeathAnchorPlayerController player;
    [SerializeField] private GhostReplayController ghost;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform anchorMarker;
    [SerializeField] private Text countdownText;

    [Header("规则")]
    [SerializeField] private float recordWindowSec = 5f;
    [SerializeField] private float sampleRate = 60f;

    /// <summary>录制帧列表</summary>
    private readonly List<DeathAnchorReplayFrame> recordingFrames = new List<DeathAnchorReplayFrame>();
    /// <summary>关卡中所有钥匙</summary>
    private readonly List<KeyPickup> keys = new List<KeyPickup>();

    private bool isRecording;
    private bool levelComplete;
    private float recordingStartedAt;
    private float lastSampleAt;
    private Vector2 activeAnchorFootPosition;  // 当前锚点脚底位置
    private Vector2 respawnFootPosition;       // 复活点脚底位置
    private bool hasRespawnFootPosition;       // 是否已设置复活点

    /// <summary>当前锚点位置（脚底坐标）</summary>
    public Vector2 ActiveAnchorFootPosition => activeAnchorFootPosition;
    /// <summary>是否正在录制</summary>
    public bool IsRecording => isRecording;

    private void Awake()
    {
        if (player == null) player = FindObjectOfType<DeathAnchorPlayerController>();
        if (ghost == null) ghost = FindObjectOfType<GhostReplayController>(true);
        keys.AddRange(FindObjectsOfType<KeyPickup>(true));
    }

    private void Start()
    {
        if (spawnPoint != null)
        {
            respawnFootPosition = spawnPoint.position;
            hasRespawnFootPosition = true;
            player?.SpawnAtFootPosition(respawnFootPosition);
        }

        ghost?.gameObject.SetActive(false);

        if (anchorMarker != null) anchorMarker.gameObject.SetActive(false);
        SetCountdownVisible(false);
    }

    private void Update()
    {
        // E 键：开始录制
        if (Input.GetKeyDown(KeyCode.E)) BeginRecording();
        // R 键：重新加载场景
        if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        if (!isRecording) return;

        // 录制窗口到期 → 自然结束
        if (Time.time - recordingStartedAt > recordWindowSec)
        {
            FinishRecording(false);
            return;
        }

        // 按采样率记录输入
        if (Time.time - lastSampleAt >= 1f / sampleRate) SampleRecording();
        UpdateCountdownText();
    }

    /// <summary>由 Baker 调用，注入场景引用</summary>
    public void Configure(float recordWindowSec, DeathAnchorPlayerController player, GhostReplayController ghost,
        Transform spawnPoint, Transform anchorMarker, Text countdownText)
    {
        this.recordWindowSec = Mathf.Max(1f, recordWindowSec);
        this.player = player;
        this.ghost = ghost;
        this.spawnPoint = spawnPoint;
        this.anchorMarker = anchorMarker;
        this.countdownText = countdownText;
    }

    /// <summary>注册钥匙供管理器追踪</summary>
    public void RegisterKey(KeyPickup key)
    {
        if (key != null && !keys.Contains(key)) keys.Add(key);
    }

    /// <summary>玩家死亡——由 HazardTrigger 调用</summary>
    public void KillPlayer()
    {
        // 录制中且有足够帧 → 保存幽灵并设置复活点
        bool savedGhost = isRecording && recordingFrames.Count >= 2 && Time.time - recordingStartedAt <= recordWindowSec;
        if (savedGhost)
        {
            SampleRecording();
            SpawnGhostFromRecording();
        }
        else if (isRecording)
        {
            CancelRecording();
        }

        // 重置所有钥匙位置
        ResetCarriedKeys();

        // 确定复活位置
        Vector2 targetFoot = hasRespawnFootPosition
            ? respawnFootPosition
            : (spawnPoint != null ? (Vector2)spawnPoint.position : player.FootPosition);

        player.SpawnAtFootPosition(targetFoot);
    }

    /// <summary>检查玩家是否携带指定 ID 的钥匙</summary>
    public bool PlayerHasKey(string keyId)
    {
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] != null && keys[i].Id == keyId && keys[i].IsCarried) return true;
        }
        return false;
    }

    /// <summary>玩家到达终点——加载下一关</summary>
    public void OnGoalReached()
    {
        if (levelComplete) return;
        levelComplete = true;

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;
        if (nextIndex >= 0 && nextIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(nextIndex);
        else
            Debug.Log("No next scene in Build Settings. Staying on the final level.", this);
    }

    /// <summary>开始录制：记录锚点位置，清除旧帧</summary>
    private void BeginRecording()
    {
        if (player == null) return;

        isRecording = true;
        recordingStartedAt = Time.time;
        lastSampleAt = -999f;
        activeAnchorFootPosition = player.FootPosition;
        recordingFrames.Clear();

        if (anchorMarker != null)
        {
            anchorMarker.position = activeAnchorFootPosition;
            anchorMarker.gameObject.SetActive(true);
        }

        SampleRecording();
        SetCountdownVisible(true);
        UpdateCountdownText();
    }

    /// <summary>取消录制（无幽灵生成）</summary>
    private void CancelRecording()
    {
        isRecording = false;
        recordingFrames.Clear();
        if (anchorMarker != null) anchorMarker.gameObject.SetActive(false);
        SetCountdownVisible(false);
    }

    /// <summary>采样一帧录制数据：记录当前输入而非位置</summary>
    private void SampleRecording()
    {
        lastSampleAt = Time.time;
        float elapsed = Time.time - recordingStartedAt;
        float input = Input.GetAxisRaw("Horizontal");
        bool jump = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow);
        recordingFrames.Add(new DeathAnchorReplayFrame(elapsed, input, jump, player.Facing));
    }

    /// <summary>录制结束：生成幽灵，更新复活点</summary>
    private void FinishRecording(bool respawnPlayer)
    {
        if (!isRecording) return;

        SampleRecording();

        if (recordingFrames.Count >= 2)
        {
            SpawnGhostFromRecording();
            // 始终更新复活点为本次锚点（修复：之前只在 respawnPlayer=true 时更新）
            respawnFootPosition = activeAnchorFootPosition;
            hasRespawnFootPosition = true;
            if (respawnPlayer) player.SpawnAtFootPosition(respawnFootPosition);
        }
        else
        {
            CancelRecording();
        }
    }

    /// <summary>从录制帧生成幽灵分身</summary>
    private void SpawnGhostFromRecording()
    {
        if (ghost == null) return;

        isRecording = false;
        ghost.gameObject.SetActive(true);
        ghost.Play(activeAnchorFootPosition, recordingFrames);
        recordingFrames.Clear();

        if (anchorMarker != null) anchorMarker.gameObject.SetActive(false);
        SetCountdownVisible(false);
    }

    /// <summary>将所有钥匙放回初始位置</summary>
    private void ResetCarriedKeys()
    {
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] != null) keys[i].ResetToHome();
        }
    }

    private void UpdateCountdownText()
    {
        if (countdownText == null) return;
        float remaining = Mathf.Max(0f, recordWindowSec - (Time.time - recordingStartedAt));
        countdownText.text = $"ANCHOR REC {remaining:0.0}s";
    }

    private void SetCountdownVisible(bool visible)
    {
        if (countdownText != null) countdownText.gameObject.SetActive(visible);
    }
}