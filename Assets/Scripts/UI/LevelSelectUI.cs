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
    /// <summary>CRT转场加载指定序号的关卡</summary>
    public void LoadLevel(int levelIndex)
    {
        if (!LevelProgressSave.IsLevelUnlocked(levelIndex))
        {
            Debug.LogWarning("[LevelSelectUI] Level is locked: " + levelIndex);
            return;
        }

        string sceneName;
        string scenePath;
        if (!LevelProgressSave.TryGetLevelScene(levelIndex, out sceneName, out scenePath))
        {
            Debug.LogWarning("[LevelSelectUI] Invalid level index: " + levelIndex);
            return;
        }

        CRTTransition.Ensure().TransitionToScene(sceneName, scenePath);
    }
}
