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
    private bool hasFixedAnchor;

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

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ClearFixedAnchor();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (ghost != null && ghost.gameObject.activeSelf && player != null)
        {
            MovePlayerAboveGhostIfMostlyOverlapping(player, ghost, 0.8f);
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
            CommitActiveAnchor();
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
