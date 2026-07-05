using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器工具：快速切换选中对象下所有子 SpriteRenderer 的透明度。
/// 右键 Hierarchy 或通过菜单操作，直接修改 Alpha 值。
/// </summary>
public static class ToggleSpriteAlpha
{
    private const string MenuPath = "Tools/Sprite Alpha/";

    [MenuItem(MenuPath + "设为透明 %&T", false, 20)]
    private static void SetTransparent() => SetAlpha(0f);

    [MenuItem(MenuPath + "设为不透明 %&Y", false, 21)]
    private static void SetOpaque() => SetAlpha(1f);

    [MenuItem("GameObject/Sprite透明度/透明", false, 30)]
    private static void ContextTransparent() => SetAlpha(0f);

    [MenuItem("GameObject/Sprite透明度/不透明", false, 31)]
    private static void ContextOpaque() => SetAlpha(1f);

    private static void SetAlpha(float alpha)
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("请先选择目标对象。");
            return;
        }

        var renderers = Selection.activeGameObject.GetComponentsInChildren<SpriteRenderer>(true);
        Undo.RecordObjects(renderers, "Toggle Sprite Alpha");

        int count = 0;
        foreach (var sr in renderers)
        {
            var c = sr.color;
            c.a = alpha;
            sr.color = c;
            EditorUtility.SetDirty(sr.gameObject);
            count++;
        }

        Debug.Log($"已将 {Selection.activeGameObject.name} 下 {count} 个 SpriteRenderer 设为 alpha={alpha}");
    }

    [MenuItem(MenuPath + "设为透明 %&T", true)]
    [MenuItem(MenuPath + "设为不透明 %&Y", true)]
    [MenuItem("GameObject/Sprite透明度/透明", true)]
    [MenuItem("GameObject/Sprite透明度/不透明", true)]
    private static bool ValidateSelected() => Selection.activeGameObject != null;
}