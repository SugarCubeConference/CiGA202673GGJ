using UnityEngine;

/// <summary>
/// 运行时自动将所有子对象的 SpriteRenderer 颜色设为透明。
/// 编辑器中保留原色用于辅助设计。
/// </summary>
public class HideAllSpriteRenderersOnRun : MonoBehaviour
{
    private void Awake()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Color c = renderers[i].color;
            c.a = 0f;
            renderers[i].color = c;
        }
    }
}