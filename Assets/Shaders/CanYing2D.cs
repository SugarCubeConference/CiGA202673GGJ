using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2D精灵拖影生成器 - 修复父子层级问题
/// </summary>
public class CanYing2D : MonoBehaviour
{
    [Header("===== 拖影参数 =====")]
    [Tooltip("残影生成间隔（秒），越小越密集")]
    public float interval = 0.06f;

    [Tooltip("残影单个生命周期（秒），越长拖影越长")]
    public float lifeCycle = 0.6f;

    [Tooltip("残影颜色（推荐半透明）")]
    public Color canYingColor = new Color(0.3f, 0.6f, 1f, 0.5f);

    [Tooltip("残影是否逐渐缩小消失")]
    public bool scaleDown = true;

    [Tooltip("残影是否逐渐变暗")]
    public bool fadeToDark = true;

    [Header("===== 性能优化 =====")]
    [Tooltip("最大同时存在的残影数量")]
    public int maxCanYingCount = 25;

    // 私有变量
    private SpriteRenderer spriteRenderer;
    private Material canYingMaterial;
    private List<GameObject> canYingList = new List<GameObject>();
    private float timer = 0f;
    private Sprite currentSprite;
    private Transform rootParent; // 用于存放残影的父物体

    void Start()
    {
        // 获取精灵渲染器
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError($"CanYing2D: {gameObject.name} 没有 SpriteRenderer 组件！");
            enabled = false;
            return;
        }

        currentSprite = spriteRenderer.sprite;
        if (currentSprite == null)
        {
            Debug.LogError($"CanYing2D: {gameObject.name} 的 SpriteRenderer 没有设置精灵图片！");
            enabled = false;
            return;
        }

        // 创建残影材质
        Shader shader = Shader.Find("Custom/CanYingShader2D");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        canYingMaterial = new Material(shader);
        canYingMaterial.mainTexture = currentSprite.texture;
        canYingMaterial.color = canYingColor;

        // ★★★ 关键修复：创建一个独立的父物体来存放所有残影 ★★★
        CreateGhostContainer();

        Debug.Log($"✅ CanYing2D 启动成功！精灵: {currentSprite.name}");
    }

    /// <summary>
    /// 创建一个专门存放残影的容器，与角色同级
    /// </summary>
    void CreateGhostContainer()
    {
        // 查找是否已存在容器
        string containerName = $"{gameObject.name}_Ghosts";
        rootParent = GameObject.Find(containerName)?.transform;

        if (rootParent == null)
        {
            // 创建新容器，放在场景根目录下（与角色同级）
            GameObject container = new GameObject(containerName);
            rootParent = container.transform;
            // 可选：把容器放到一个固定的父物体下（比如Canvas或其他管理对象）
            // 如果不设置，默认就在场景根目录
            Debug.Log($"📁 创建残影容器: {containerName}");
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= interval)
        {
            timer = 0f;
            SpawnGhost();
        }

        UpdateGhosts();
    }

    void SpawnGhost()
    {
        // 数量限制
        if (canYingList.Count >= maxCanYingCount)
        {
            DestroyOldestGhost();
        }

        // ★★★ 关键修复：残影放到独立容器下，而不是角色下面 ★★★
        GameObject ghost = new GameObject($"Ghost2D_{canYingList.Count}");
        ghost.transform.SetParent(rootParent); // 挂在容器下，与角色同级
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.localScale;

        // 添加 SpriteRenderer
        SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();
        sr.sprite = currentSprite;
        sr.sortingLayerID = spriteRenderer.sortingLayerID;
        sr.sortingOrder = spriteRenderer.sortingOrder - 1;

        // 复制材质
        Material mat = new Material(canYingMaterial);
        mat.color = canYingColor;
        sr.sharedMaterial = mat;

        // 记录数据
        GhostData2D data = ghost.AddComponent<GhostData2D>();
        data.birthTime = Time.time;
        data.lifeCycle = lifeCycle;
        data.startScale = transform.localScale.x;

        canYingList.Add(ghost);
    }

    void UpdateGhosts()
    {
        float now = Time.time;

        for (int i = canYingList.Count - 1; i >= 0; i--)
        {
            GameObject g = canYingList[i];
            if (g == null)
            {
                canYingList.RemoveAt(i);
                continue;
            }

            GhostData2D data = g.GetComponent<GhostData2D>();
            if (data == null)
            {
                Destroy(g);
                canYingList.RemoveAt(i);
                continue;
            }

            float age = now - data.birthTime;
            float progress = Mathf.Clamp01(age / data.lifeCycle);

            if (progress >= 1f)
            {
                Destroy(g);
                canYingList.RemoveAt(i);
                continue;
            }

            SpriteRenderer sr = g.GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            // 透明度渐隐
            float fade = Mathf.Pow(progress, 1.5f);
            Color c = canYingColor;
            c.a *= (1 - progress);

            if (fadeToDark)
            {
                c.r *= (1 - progress * 0.4f);
                c.g *= (1 - progress * 0.4f);
                c.b *= (1 - progress * 0.4f);
            }

            sr.color = c;

            if (scaleDown)
            {
                float scale = Mathf.Lerp(data.startScale, data.startScale * 0.3f, progress);
                g.transform.localScale = new Vector3(scale, scale, 1);
            }
        }
    }

    void DestroyOldestGhost()
    {
        if (canYingList.Count == 0) return;

        GameObject oldest = canYingList[0];
        canYingList.RemoveAt(0);
        if (oldest != null) Destroy(oldest);
    }

    public void ClearAll()
    {
        foreach (GameObject g in canYingList)
        {
            if (g != null) Destroy(g);
        }
        canYingList.Clear();
    }

    public void SetLength(float length)
    {
        lifeCycle = Mathf.Max(0.1f, length);
    }

    public void SetDensity(float density)
    {
        interval = Mathf.Max(0.02f, 0.2f / Mathf.Max(1, density));
    }

    void OnDestroy()
    {
        ClearAll();
        if (canYingMaterial != null) Destroy(canYingMaterial);
    }
}

public class GhostData2D : MonoBehaviour
{
    public float birthTime;
    public float lifeCycle;
    public float startScale;
}