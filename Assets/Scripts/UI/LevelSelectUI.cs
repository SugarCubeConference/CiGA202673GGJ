using UnityEngine;

/// <summary>
/// 选关界面控制器——管理关卡按钮网格，点击后通过CRT转场加载对应关卡场景。
/// 按钮布局由 C# 在 InitializeGrid 中动态生成，与图片设计一致：
///   第 0 行: [0]
///   第 1 行: [1] [2] [3] [4] [5]
///   第 2 行: [6] [7] [8] [9] [10]
/// </summary>
public class LevelSelectUI : MonoBehaviour
{
    [Header("关卡场景名（按序号 0-10）")]
    [SerializeField] private string[] levelSceneNames = new string[]
    {
        "第零关（0705重置）",
        "第一关（0705重置）",
        "第二关（0705重置）",
        "第三关（0705重置）",
        "第四关（0705重置）",
        "第五关（0705重置）",
        "第六关",
        "第七关",
        "第八关",
        "第九关（0705重置）",
        "第十关（0705）"
    };

    /// <summary>CRT转场加载指定序号的关卡</summary>
    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelSceneNames.Length)
        {
            Debug.LogWarning("[LevelSelectUI] Invalid level index: " + levelIndex);
            return;
        }

        string sceneName = levelSceneNames[levelIndex];
        string fullPath = "Assets/Scenes/DeathAnchor/" + sceneName + ".unity";
        CRTTransition.Ensure().TransitionToScene(sceneName, fullPath);
    }
}
