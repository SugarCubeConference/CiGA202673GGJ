using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    public Button startButton;
    public Button exitButton;
    public GameObject buttonContainer;
    public GameObject levelGrid;
    public Image levelGridBackground;

    [Header("Fade Settings")]
    public float fadeDuration = 0.3f;

    private CanvasGroup _btnCG;
    private CanvasGroup _gridCG;
    private bool _isGridOpen;

    void Awake()
    {
        _btnCG = GetOrAdd(buttonContainer);
        _gridCG = GetOrAdd(levelGrid);

        levelGrid.SetActive(false);
        _btnCG.alpha = 1f;
        _gridCG.alpha = 0f;

        // Bind Start / Exit buttons
        startButton.onClick.AddListener(OnStartGame);
        exitButton.onClick.AddListener(OnQuit);

        // Bind level buttons
        for (int i = 0; i < levelGrid.transform.childCount; i++)
        {
            var btn = levelGrid.transform.GetChild(i).GetComponent<Button>();
            if (btn == null) continue;
            int idx = i;
            btn.onClick.AddListener(() => OnLevelClick(idx));
        }

        RefreshLevelButtons();

        // Bind background click to close grid
        if (levelGridBackground != null)
        {
            var bgBtn = levelGridBackground.GetComponent<Button>();
            if (bgBtn == null) bgBtn = levelGridBackground.gameObject.AddComponent<Button>();
            bgBtn.transition = Selectable.Transition.None;
            bgBtn.onClick.AddListener(OnBackgroundClick);
        }
    }

    public void OnStartGame()
    {
        if (_isGridOpen) return;
        _isGridOpen = true;
        RefreshLevelButtons();
        DeathAnchorWwiseAudio.Post(startButton.gameObject, DeathAnchorWwiseEvents.UiSelect);
        StartCoroutine(FadeTransition(false, true));
    }

    public void OnBackgroundClick()
    {
        if (!_isGridOpen) return;
        _isGridOpen = false;
        StartCoroutine(FadeTransition(true, false));
    }

    public void OnLevelClick(int levelIndex)
    {
        DeathAnchorWwiseAudio.Post(gameObject, DeathAnchorWwiseEvents.UiSelect);
        if (!LevelProgressSave.IsLevelUnlocked(levelIndex))
        {
            Debug.LogWarning("[MainMenu] Level is locked: " + levelIndex);
            RefreshLevelButtons();
            return;
        }

        string sceneName;
        string scenePath;
        if (LevelProgressSave.TryGetLevelScene(levelIndex, out sceneName, out scenePath))
        {
            CRTTransition.Ensure().TransitionToScene(sceneName, scenePath);
        }
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator FadeTransition(bool showButtons, bool showGrid)
    {
        if (showGrid) levelGrid.SetActive(true);
        if (showButtons) buttonContainer.SetActive(true);

        float t = 0f;
        float fromBtn = _btnCG.alpha;
        float toBtn = showButtons ? 1f : 0f;
        float fromGrid = _gridCG.alpha;
        float toGrid = showGrid ? 1f : 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float r = Mathf.Clamp01(t / fadeDuration);
            _btnCG.alpha = Mathf.Lerp(fromBtn, toBtn, r);
            _gridCG.alpha = Mathf.Lerp(fromGrid, toGrid, r);
            yield return null;
        }

        _btnCG.alpha = toBtn;
        _gridCG.alpha = toGrid;
        _btnCG.interactable = showButtons;
        _btnCG.blocksRaycasts = showButtons;
        _gridCG.interactable = showGrid;
        _gridCG.blocksRaycasts = showGrid;

        if (!showButtons) buttonContainer.SetActive(false);
        if (!showGrid) levelGrid.SetActive(false);
    }

    private static CanvasGroup GetOrAdd(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    private void RefreshLevelButtons()
    {
        if (levelGrid == null)
        {
            return;
        }

        HashSet<int> unlockedLevels = LevelProgressSave.LoadUnlockedLevels();
        for (int i = 0; i < levelGrid.transform.childCount; i++)
        {
            Transform child = levelGrid.transform.GetChild(i);
            bool isUnlocked = i < LevelProgressSave.LevelCount && unlockedLevels.Contains(i);

            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = isUnlocked;
            }

            child.gameObject.SetActive(isUnlocked);
        }
    }
}
