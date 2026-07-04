using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主菜单控制器——处理「开始游戏」按钮等 UI 交互。
/// 场景切换使用异步加载，防止卡顿，并包含加载失败回退。
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("按钮")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    [Header("场景")]
    [SerializeField] private string firstLevelSceneName = "教学关";

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitGame);
    }

    /// <summary>按下「开始游戏」→ 异步加载第一个关卡场景</summary>
    public void OnStartGame()
    {
        StartCoroutine(LoadGameSceneAsync());
    }

    /// <summary>异步加载场景——先按 Build Settings 名称查找，再按完整路径回退</summary>
    private IEnumerator LoadGameSceneAsync()
    {
        // 先尝试按名称加载（需要场景已在 Build Settings 中）
        if (Application.CanStreamedLevelBeLoaded(firstLevelSceneName))
        {
            SceneManager.LoadScene(firstLevelSceneName);
            yield break;
        }

        // 回退：按完整 Assets 路径加载
        string fullPath = "Assets/Scenes/DeathAnchor/" + firstLevelSceneName + ".unity";
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(fullPath);
        if (asyncOp == null)
        {
            Debug.LogError($"[MainMenu] 无法加载场景 '{firstLevelSceneName}'，请检查 Build Settings 或文件路径。");
            yield break;
        }

        asyncOp.allowSceneActivation = true;
        while (!asyncOp.isDone)
        {
            // 可在此处显示加载进度条
            yield return null;
        }
    }

    /// <summary>退出游戏（编辑器中为停止运行）</summary>
    public void OnQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}