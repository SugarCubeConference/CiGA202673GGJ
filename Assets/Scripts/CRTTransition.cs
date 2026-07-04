using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// CRT像素化转场：旧场景像素化拉大 → 切场景 → 新场景像素化拉小复原。
/// DontDestroyOnLoad单例，通过 Ensure() 自动创建。
/// </summary>
public class CRTTransition : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float pixelateDuration = 0.35f;

    [Header("Effect")]
    [SerializeField] private float peakRgbPixelSize = 30f;

    private static CRTTransition _instance;
    private CRTDisplayRendererFeature.CRTDisplaySettings _crtSettings;
    private float _originalRgbPixelSize;
    private bool _hasOriginal;
    private bool _transitioning;

    public static CRTTransition Instance => _instance;
    public bool IsTransitioning => _transitioning;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSettings();
    }

    // --- Public API ---

    public void TransitionToScene(int buildIndex)
    {
        if (!_transitioning) StartCoroutine(RunTransition(buildIndex));
    }

    public void TransitionToScene(string sceneName)
    {
        if (!_transitioning) StartCoroutine(RunTransition(sceneName));
    }

    public void TransitionToScene(string sceneName, string fallbackPath)
    {
        if (!_transitioning) StartCoroutine(RunTransition(sceneName, fallbackPath));
    }

    public void RestartScene()
    {
        if (!_transitioning)
            StartCoroutine(RunTransition(SceneManager.GetActiveScene().buildIndex));
    }

    public static CRTTransition Ensure()
    {
        if (_instance != null) return _instance;
        var go = new GameObject("[CRTTransition]");
        return go.AddComponent<CRTTransition>();
    }

    // --- Coroutines ---

    private IEnumerator RunTransition(int buildIndex)
    {
        if (!BeginTransition()) yield break;
        yield return PlayTransition(SceneManager.LoadSceneAsync(buildIndex));
    }

    private IEnumerator RunTransition(string sceneName)
    {
        if (!BeginTransition()) yield break;
        yield return PlayTransition(SceneManager.LoadSceneAsync(sceneName));
    }

    private IEnumerator RunTransition(string sceneName, string fallbackPath)
    {
        if (!BeginTransition()) yield break;

        AsyncOperation op = null;
        if (Application.CanStreamedLevelBeLoaded(sceneName))
            op = SceneManager.LoadSceneAsync(sceneName);
        else if (!string.IsNullOrEmpty(fallbackPath))
            op = SceneManager.LoadSceneAsync(fallbackPath);

        if (op == null)
        {
            Debug.LogError("[CRTTransition] Cannot load scene: " + sceneName);
            RestoreImmediate();
            _transitioning = false;
            yield break;
        }
        yield return PlayTransition(op);
    }

    // --- Shared ---

    private bool BeginTransition()
    {
        _transitioning = true;
        EnsureSettings();
        if (_crtSettings == null) { _transitioning = false; return false; }

        // Capture original value ONCE, before any modification.
        if (!_hasOriginal)
        {
            _originalRgbPixelSize = _crtSettings.rgbPixelSize;
            _hasOriginal = true;
        }

        return true;
    }

    private IEnumerator PlayTransition(AsyncOperation op)
    {
        if (op == null)
        {
            RestoreImmediate();
            _transitioning = false;
            yield break;
        }

        op.allowSceneActivation = false;

        yield return LerpPixelSize(_crtSettings.rgbPixelSize, peakRgbPixelSize, pixelateDuration);

        while (op.progress < 0.9f) yield return null;

        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        yield return LerpPixelSize(_crtSettings.rgbPixelSize, _originalRgbPixelSize, pixelateDuration);
        _transitioning = false;
    }

    /// <summary>Restore the original rgbPixelSize. Only uses the cached value.</summary>
    private void RestoreImmediate()
    {
        if (_crtSettings != null && _hasOriginal)
            _crtSettings.rgbPixelSize = _originalRgbPixelSize;
    }

    private IEnumerator LerpPixelSize(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (_crtSettings == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            _crtSettings.rgbPixelSize = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        _crtSettings.rgbPixelSize = to;
    }

    /// <summary>Find the CRT settings reference. Does NOT touch _originalRgbPixelSize.</summary>
    private void EnsureSettings()
    {
        if (_crtSettings != null) return;
        var features = Resources.FindObjectsOfTypeAll<CRTDisplayRendererFeature>();
        if (features.Length > 0)
            _crtSettings = features[0].settings;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            RestoreImmediate();
            _instance = null;
        }
    }
}
