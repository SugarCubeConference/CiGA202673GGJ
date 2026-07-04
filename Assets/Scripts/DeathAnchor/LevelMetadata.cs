using UnityEngine;

public sealed class LevelMetadata : MonoBehaviour
{
    [SerializeField] private string sourceJson;
    [SerializeField] private string title;
    [SerializeField] private string ruleNotes;
    [SerializeField] private string experience;
    [SerializeField] private Vector2 worldSizePixels;
    [SerializeField] private float recordWindowSec = 5f;
    [SerializeField] private bool ghostCanStandOnPlayer = true;
    [SerializeField] private bool ghostCanPressButtons = true;
    [SerializeField] private bool playerWallSlide = true;

    public string SourceJson => sourceJson;
    public string Title => title;
    public string RuleNotes => ruleNotes;
    public string Experience => experience;
    public Vector2 WorldSizePixels => worldSizePixels;
    public float RecordWindowSec => recordWindowSec;

    public void Initialize(string sourceJson, DeathAnchorLevelData level)
    {
        this.sourceJson = sourceJson;
        title = level != null ? level.title : string.Empty;
        ruleNotes = level != null ? level.ruleNotes : string.Empty;
        experience = level != null ? level.experience : string.Empty;
        worldSizePixels = level != null && level.world != null ? new Vector2(level.world.w, level.world.h) : Vector2.zero;

        DeathAnchorRules rules = level != null ? level.rules : null;
        recordWindowSec = rules != null ? rules.recordWindowSec : 5f;
        ghostCanStandOnPlayer = rules == null || rules.ghostCanStandOnPlayer;
        ghostCanPressButtons = rules == null || rules.ghostCanPressButtons;
        playerWallSlide = rules == null || rules.playerWallSlide;
    }
}
