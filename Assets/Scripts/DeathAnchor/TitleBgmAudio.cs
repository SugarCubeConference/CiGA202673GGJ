using UnityEngine;

/// <summary>
/// 在场景中播放标题 BGM（循环）。
/// 场景卸载时自动停止。挂载到任意带 AkGameObj 的对象即可。
/// </summary>
public sealed class TitleBgmAudio : MonoBehaviour
{
    [SerializeField] private string titleBgmEvent = DeathAnchorWwiseEvents.TitleBgm;

    private bool isPlaying;

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(titleBgmEvent))
            return;

        DeathAnchorWwiseAudio.StartLoop(gameObject, titleBgmEvent, ref isPlaying);
    }

    private void OnDisable()
    {
        StopBgm();
    }

    private void OnDestroy()
    {
        StopBgm();
    }

    private void StopBgm()
    {
        DeathAnchorWwiseAudio.StopLoop(gameObject, titleBgmEvent, ref isPlaying);
    }
}
