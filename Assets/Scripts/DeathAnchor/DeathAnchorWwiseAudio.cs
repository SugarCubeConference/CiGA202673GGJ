using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class DeathAnchorWwiseEvents
{
    public const string PlayerMove = "Play_sfx_player_move";
    public const string PlayerJump = "Play_sfx_player_jump";
    public const string PlayerLand = "Play_sfx_player_land";
    public const string WallSlide = "Play_sfx_wall_slide";
    public const string PlayerDeath = "Play_sfx_player_death";
    public const string AnchorStart = "Play_sfx_anchor_start";
    public const string AnchorTick = "Play_sfx_anchor_tick";
    public const string GhostLoop = "Play_sfx_ghost_loop";
    public const string KeyPickup = "Play_sfx_key_pickup";
    public const string DoorUnlock = "Play_sfx_door_unlock";
    public const string Goal = "Play_sfx_goal";
    public const string UiSelect = "Play_sfx_ui_select";
}

public static class DeathAnchorWwiseAudio
{
    private const string MainBankName = "main";
    private const string InitBankName = "Init.bnk";
    private const string SoundBankRelativePath = "GeneratedSoundBanks/Windows";

    private static readonly HashSet<string> warnedPostFailures = new HashSet<string>();
    private static bool banksLoaded;
    private static bool warnedBankLoadFailure;

    public static void Post(GameObject emitter, string eventName)
    {
        if (!EnsureReady(emitter) || string.IsNullOrWhiteSpace(eventName))
        {
            return;
        }

        uint playingId = AkUnitySoundEngine.PostEvent(eventName, emitter);
        WarnIfPostFailed(eventName, playingId, emitter);
    }

    public static bool StartLoop(GameObject emitter, string eventName, ref bool isPlaying)
    {
        if (isPlaying)
        {
            return true;
        }

        if (!EnsureReady(emitter) || string.IsNullOrWhiteSpace(eventName))
        {
            return false;
        }

        uint playingId = AkUnitySoundEngine.PostEvent(eventName, emitter);
        bool started = playingId != AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
        WarnIfPostFailed(eventName, playingId, emitter);
        isPlaying = started;
        return started;
    }

    public static void StopLoop(GameObject emitter, string eventName, ref bool isPlaying, int fadeMs = 80)
    {
        if (!isPlaying)
        {
            return;
        }

        isPlaying = false;
        if (!EnsureReady(emitter) || string.IsNullOrWhiteSpace(eventName))
        {
            return;
        }

        AkUnitySoundEngine.ExecuteActionOnEvent(
            eventName,
            AkActionOnEventType.AkActionOnEventType_Stop,
            emitter,
            Mathf.Max(0, fadeMs));
    }

    private static bool EnsureReady(GameObject emitter)
    {
        if (emitter == null || !AkUnitySoundEngine.IsInitialized())
        {
            return false;
        }

        if (emitter.GetComponent<AkGameObj>() == null)
        {
            emitter.AddComponent<AkGameObj>();
        }

        if (!banksLoaded)
        {
            banksLoaded = TryLoadBanks(emitter);
        }

        return banksLoaded;
    }

    private static bool TryLoadBanks(GameObject emitter)
    {
        string bankPath = Path.Combine(Application.streamingAssetsPath, SoundBankRelativePath);
        AkUnitySoundEngine.SetBasePath(bankPath);

        uint initBankId;
        AKRESULT initResult = AkUnitySoundEngine.LoadBank(InitBankName, out initBankId);
        if (initResult != AKRESULT.AK_Success && initResult != AKRESULT.AK_BankAlreadyLoaded)
        {
            WarnBankLoadFailure($"Init bank load failed from '{bankPath}' with result {initResult}.", emitter);
            return false;
        }

        uint mainBankId;
        AKRESULT mainResult = AkUnitySoundEngine.LoadBank(MainBankName, out mainBankId);
        if (mainResult != AKRESULT.AK_Success && mainResult != AKRESULT.AK_BankAlreadyLoaded)
        {
            WarnBankLoadFailure($"Main bank load failed from '{bankPath}' with result {mainResult}.", emitter);
            return false;
        }

        return true;
    }

    private static void WarnBankLoadFailure(string message, GameObject emitter)
    {
        if (warnedBankLoadFailure)
        {
            return;
        }

        warnedBankLoadFailure = true;
        Debug.LogWarning(message, emitter);
    }

    private static void WarnIfPostFailed(string eventName, uint playingId, GameObject emitter)
    {
        if (playingId != AkUnitySoundEngine.AK_INVALID_PLAYING_ID || warnedPostFailures.Contains(eventName))
        {
            return;
        }

        warnedPostFailures.Add(eventName);
        Debug.LogWarning($"Wwise event '{eventName}' failed to post. Check that the event exists and the main SoundBank is loaded.", emitter);
    }
}
