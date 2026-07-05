using System.Collections;
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
    [SerializeField] private AnchorCountdownHud countdownHud;
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
    private bool hasFixedAnchor;
    private bool respawnInProgress;

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
                player.ResetDeath();
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

        if (countdownHud != null)
        {
            countdownHud.Configure(recordWindowSec);
        }

        SetCountdownVisible(false);
        UpdateCountdownUi(recordWindowSec);
    }
    private void FixedUpdate()
    {
        if (ghost != null && ghost.gameObject.activeSelf && player != null)
        {
            MovePlayerAboveGhostIfMostlyOverlapping(player, ghost, 0.8f);
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            BeginRecording();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ClearFixedAnchor();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            CRTTransition.Ensure().RestartScene();
        }

        

        if (!isRecording)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            CancelRecording();
            return;
        }

        if (Time.time - recordingStartedAt > recordWindowSec)
        {
            CancelRecording();
            return;
        }

        if (Time.time - lastSampleAt >= 1f / sampleRate)
        {
            SampleRecording();
        }

        UpdateCountdownUi(Mathf.Max(0f, recordWindowSec - (Time.time - recordingStartedAt)));
    }

    public void Configure(float recordWindowSec, DeathAnchorPlayerController player, GhostReplayController ghost, Transform spawnPoint, Transform anchorMarker, AnchorCountdownHud countdownHud, Text countdownText)
    {
        this.recordWindowSec = Mathf.Max(1f, recordWindowSec);
        this.player = player;
        this.ghost = ghost;
        this.spawnPoint = spawnPoint;
        this.anchorMarker = anchorMarker;
        this.countdownHud = countdownHud;
        this.countdownText = countdownText;
        if (this.countdownHud != null)
        {
            this.countdownHud.Configure(this.recordWindowSec);
        }
        UpdateCountdownUi(this.recordWindowSec);
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
        if (player == null || respawnInProgress)
        {
            return;
        }

        bool savedGhost = isRecording && recordingFrames.Count >= 2 && Time.time - recordingStartedAt <= recordWindowSec;
        if (savedGhost)
        {
            SampleRecording();
            CommitActiveAnchor();
            SpawnGhostFromRecording();
        }
        else if (isRecording)
        {
            CancelRecording();
        }

        ResetCarriedKeys();

        DeathAnchorWwiseAudio.Post(player != null ? player.gameObject : gameObject, DeathAnchorWwiseEvents.PlayerDeath);

        Vector2 targetFoot = hasRespawnFootPosition
            ? respawnFootPosition
            : (spawnPoint != null ? (Vector2)spawnPoint.position : player.FootPosition);

        StartCoroutine(RespawnPlayerAfterDeath(targetFoot));
    }

    public void KillPlayer(Vector3 hazardPosition)
    {
        KillPlayer();
    }

    public void ReachGoal()
    {
        OnGoalReached();
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
        DeathAnchorWwiseAudio.Post(gameObject, DeathAnchorWwiseEvents.Goal);

        string currentSceneName = SceneManager.GetActiveScene().name;
        int currentLevelIndex = LevelProgressSave.GetLevelIndexForScene(currentSceneName);
        if (currentLevelIndex < 0)
        {
            Debug.LogWarning("Current scene is not registered as a Death Anchor level: " + currentSceneName, this);
            return;
        }

        LevelProgressSave.UnlockLevelAndNext(currentSceneName);

        int nextLevelIndex = currentLevelIndex + 1;
        string nextSceneName;
        string nextScenePath;
        if (LevelProgressSave.TryGetLevelScene(nextLevelIndex, out nextSceneName, out nextScenePath))
        {
            CRTTransition.Ensure().TransitionToScene(nextSceneName, nextScenePath);
        }
        else
        {
            Debug.Log("No next level registered. Staying on the final level.", this);
        }
    }

    private void BeginRecording()
    {
        if (player == null || respawnInProgress)
        {
            return;
        }

        isRecording = true;        DeathAnchorWwiseAudio.Post(gameObject, DeathAnchorWwiseEvents.AnchorStart);

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
        UpdateCountdownUi(recordWindowSec);
    }

    private IEnumerator RespawnPlayerAfterDeath(Vector2 targetFoot)
    {
        respawnInProgress = true;
        player.TriggerDeath();

        float waitSeconds = Mathf.Max(0f, player.DeathAnimationSeconds);
        if (waitSeconds > 0f)
        {
            yield return new WaitForSeconds(waitSeconds);
        }

        if (player != null)
        {
            player.SpawnAtFootPosition(targetFoot);
            player.ResetDeath();
        }

        respawnInProgress = false;
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

    private void CommitActiveAnchor()
    {
        hasRespawnFootPosition = true;
        respawnFootPosition = activeAnchorFootPosition;
    }

    private void SpawnGhostFromRecording()
    {
        if (ghost == null)
        {
            return;
        }

        isRecording = false;
        hasFixedAnchor = true;
        respawnFootPosition = activeAnchorFootPosition;
        hasRespawnFootPosition = true;
        ghost.Play(activeAnchorFootPosition, recordingFrames);
        recordingFrames.Clear();

        if (anchorMarker != null)
        {
            anchorMarker.gameObject.SetActive(false);
        }

        SetCountdownVisible(false);
    }

    private bool MovePlayerAboveGhostIfMostlyOverlapping(DeathAnchorPlayerController player, GhostReplayController ghost, float threshold)
    {
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        Collider2D ghostCollider = ghost.GetComponent<Collider2D>();
        if (playerCollider == null || ghostCollider == null)
        {
            return false;
        }

        Bounds playerBounds = playerCollider.bounds;
        Bounds ghostBounds = ghostCollider.bounds;

        float intersectMinX = Mathf.Max(playerBounds.min.x, ghostBounds.min.x);
        float intersectMaxX = Mathf.Min(playerBounds.max.x, ghostBounds.max.x);
        float intersectMinY = Mathf.Max(playerBounds.min.y, ghostBounds.min.y);
        float intersectMaxY = Mathf.Min(playerBounds.max.y, ghostBounds.max.y);
        if (intersectMinX >= intersectMaxX || intersectMinY >= intersectMaxY)
        {
            return false;
        }

        float intersectArea = (intersectMaxX - intersectMinX) * (intersectMaxY - intersectMinY);
        float playerArea = playerBounds.size.x * playerBounds.size.y;
        float ghostArea = ghostBounds.size.x * ghostBounds.size.y;
        float minArea = Mathf.Min(playerArea, ghostArea);
        if (minArea <= 0f || intersectArea / minArea < threshold)
        {
            return false;
        }

        Vector2 footPosition = player.FootPosition;
        footPosition.y = ghostBounds.max.y + 0.01f;

        // 安全检测：目标位置是否有 Ground 层墙体阻挡
        Vector2 bodyCenter = footPosition + Vector2.up * playerCollider.bounds.size.y * 0.5f;
        Collider2D wall = Physics2D.OverlapBox(bodyCenter, playerCollider.bounds.size, 0f, LayerMask.GetMask("Ground"));
        if (wall != null && wall.GetComponent<GhostReplayController>() == null)
        {
            return false;
        }

        player.SpawnAtFootPosition(footPosition);
        return true;
    }

    private void ClearFixedAnchor()
    {
        if (!hasFixedAnchor && (ghost == null || !ghost.gameObject.activeSelf))
        {
            return;
        }

        hasFixedAnchor = false;
        if (spawnPoint != null)
        {
            respawnFootPosition = spawnPoint.position;
            hasRespawnFootPosition = true;
        }
        else
        {
            hasRespawnFootPosition = false;
        }

        if (ghost != null)
        {
            ghost.StopReplay();
        }
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

    private void UpdateCountdownUi(float remaining)
    {
        if (countdownHud != null)
        {
            countdownHud.SetRemaining(remaining);
        }

        if (countdownText == null)
        {
            return;
        }

        countdownText.text = "ANCHOR REC";
    }

    private void SetCountdownVisible(bool visible)
    {
        if (countdownHud != null)
        {
            countdownHud.SetVisible(visible);
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(visible);
        }
    }
}
