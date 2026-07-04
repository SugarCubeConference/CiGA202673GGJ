using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class DeathAnchorGameManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private DeathAnchorPlayerController player;
    [SerializeField] private GhostReplayController ghost;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform anchorMarker;
    [SerializeField] private Text countdownText;

    [Header("Rules")]
    [SerializeField] private float recordWindowSec = 5f;
    [SerializeField] private float sampleRate = 60f;

    private readonly List<DeathAnchorReplayFrame> recordingFrames = new List<DeathAnchorReplayFrame>();
    private readonly List<KeyPickup> keys = new List<KeyPickup>();

    private bool isRecording;
    private bool levelComplete;
    private float recordingStartedAt;
    private float lastSampleAt;
    private Vector2 activeAnchorFootPosition;
    private Vector2 respawnFootPosition;
    private bool hasRespawnFootPosition;

    public Vector2 ActiveAnchorFootPosition => activeAnchorFootPosition;
    public bool IsRecording => isRecording;

    private void Awake()
    {
        if (player == null)
        {
            player = FindObjectOfType<DeathAnchorPlayerController>();
        }

        if (ghost == null)
        {
            ghost = FindObjectOfType<GhostReplayController>(true);
        }

        keys.AddRange(FindObjectsOfType<KeyPickup>(true));
    }

    private void Start()
    {
        if (spawnPoint != null)
        {
            respawnFootPosition = spawnPoint.position;
            hasRespawnFootPosition = true;
            if (player != null)
            {
                player.SpawnAtFootPosition(respawnFootPosition);
            }
        }

        if (ghost != null)
        {
            ghost.gameObject.SetActive(false);
        }

        if (anchorMarker != null)
        {
            anchorMarker.gameObject.SetActive(false);
        }

        SetCountdownVisible(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            BeginRecording();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (ghost != null && ghost.gameObject.activeSelf && player != null)
        { 
            CheckAndMovePlayerIfOverlapExceeds(player, ghost, 0.8f);
        }
        
        if (!isRecording)
        {
            return;
        }

        if (Time.time - recordingStartedAt > recordWindowSec)
        {
            FinishRecording(false);
            return;
        }

        if (Time.time - lastSampleAt >= 1f / sampleRate)
        {
            SampleRecording();
        }

        UpdateCountdownText();
    }

    public void Configure(float recordWindowSec, DeathAnchorPlayerController player, GhostReplayController ghost, Transform spawnPoint, Transform anchorMarker, Text countdownText)
    {
        this.recordWindowSec = Mathf.Max(1f, recordWindowSec);
        this.player = player;
        this.ghost = ghost;
        this.spawnPoint = spawnPoint;
        this.anchorMarker = anchorMarker;
        this.countdownText = countdownText;
    }

    public void RegisterKey(KeyPickup key)
    {
        if (key != null && !keys.Contains(key))
        {
            keys.Add(key);
        }
    }

    public void KillPlayer()
    {
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

        ResetCarriedKeys();

        Vector2 targetFoot = hasRespawnFootPosition
            ? respawnFootPosition
            : (spawnPoint != null ? (Vector2)spawnPoint.position : player.FootPosition);

        player.SpawnAtFootPosition(targetFoot);
    }

    public bool PlayerHasKey(string keyId)
    {
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] != null && keys[i].Id == keyId && keys[i].IsCarried)
            {
                return true;
            }
        }

        return false;
    }

    public void OnGoalReached()
    {
        Debug.Log("Goal reached. Death Anchor level complete.", this);
        if (levelComplete)
        {
            return;
        }

        levelComplete = true;
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;
        if (nextIndex >= 0 && nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.Log("No next scene in Build Settings. Staying on the final level.", this);
        }
    }

    private void BeginRecording()
    {
        if (player == null)
        {
            return;
        }

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

    private void CancelRecording()
    {
        isRecording = false;
        recordingFrames.Clear();

        if (anchorMarker != null)
        {
            anchorMarker.gameObject.SetActive(false);
        }

        SetCountdownVisible(false);
    }

    private void SampleRecording()
    {
        lastSampleAt = Time.time;
        float elapsed = Time.time - recordingStartedAt;
        Vector2 offset = player.FootPosition - activeAnchorFootPosition;
        recordingFrames.Add(new DeathAnchorReplayFrame(elapsed, offset, player.Facing));
    }

    private void FinishRecording(bool respawnPlayer)
    {
        if (!isRecording)
        {
            return;
        }

        SampleRecording();

        if (recordingFrames.Count >= 2)
        {
            SpawnGhostFromRecording();
            if (respawnPlayer)
            {
                player.SpawnAtFootPosition(respawnFootPosition);
            }
        }
        else
        {
            CancelRecording();
        }
    }

    private void SpawnGhostFromRecording()
    {
        if (ghost == null)
        {
            return;
        }

        isRecording = false;
        ghost.gameObject.SetActive(true);
        ghost.Play(activeAnchorFootPosition, recordingFrames);
        
        recordingFrames.Clear();

        if (anchorMarker != null)
        {
            anchorMarker.gameObject.SetActive(false);
        }

        SetCountdownVisible(false);
    }
    
    /// <summary>
    /// 检测两个collider重叠是否超过指定比例，如果超过则将player移到ghost上方
    /// </summary>
    /// <param name="player">玩家控制器</param>
    /// <param name="ghost">ghost控制器</param>
    /// <param name="threshold">重叠阈值，默认0.8（80%）</param>
    /// <returns>是否检测到超过阈值的重叠</returns>
    public bool CheckAndMovePlayerIfOverlapExceeds(
        DeathAnchorPlayerController player, 
        GhostReplayController ghost, 
        float threshold = 0.8f)
    {
        // 一步到位：获取边界 -> 计算重叠 -> 判断阈值 -> 移动player
        Bounds playerBounds = player.GetComponent<Collider2D>().bounds;
        Bounds ghostBounds = ghost.GetComponent<Collider2D>().bounds;
    
        float intersectMinX = Mathf.Max(playerBounds.min.x, ghostBounds.min.x);
        float intersectMaxX = Mathf.Min(playerBounds.max.x, ghostBounds.max.x);
        float intersectMinY = Mathf.Max(playerBounds.min.y, ghostBounds.min.y);
        float intersectMaxY = Mathf.Min(playerBounds.max.y, ghostBounds.max.y);
    
        if (intersectMinX >= intersectMaxX || intersectMinY >= intersectMaxY)
            return false;
    
        float intersectArea = (intersectMaxX - intersectMinX) * (intersectMaxY - intersectMinY);
        float minArea = Mathf.Min(
            playerBounds.size.x * playerBounds.size.y, 
            ghostBounds.size.x * ghostBounds.size.y
        );
    
        if (intersectArea / minArea >= threshold)
        {
            float newY = ghostBounds.max.y + playerBounds.size.y * 0.5f + 0.01f;
            player.SpawnAtFootPosition(new Vector2(player.FootPosition.x, newY));
            return true;
        }
        
        return false;
    }

    private void ResetCarriedKeys()
    {
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] != null)
            {
                keys[i].ResetToHome();
            }
        }
    }

    private void UpdateCountdownText()
    {
        if (countdownText == null)
        {
            return;
        }

        float remaining = Mathf.Max(0f, recordWindowSec - (Time.time - recordingStartedAt));
        countdownText.text = $"ANCHOR REC {remaining:0.0}s";
    }

    private void SetCountdownVisible(bool visible)
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(visible);
        }
    }
}
