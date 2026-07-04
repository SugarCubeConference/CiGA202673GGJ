using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Bridges DeathAnchorPlayerController movement state to Wwise events.
/// Attach to the same GameObject as DeathAnchorPlayerController.
/// </summary>
[RequireComponent(typeof(DeathAnchorPlayerController))]
[RequireComponent(typeof(AkGameObj))]
public sealed class PlayerWwiseAudio : MonoBehaviour
{
    [Header("Wwise Event Names")]
    [FormerlySerializedAs("footstepEventName")]
    [SerializeField] private string moveLoopEventName = DeathAnchorWwiseEvents.PlayerMove;
    [FormerlySerializedAs("jumpEventName")]
    [SerializeField] private string jumpEventName = DeathAnchorWwiseEvents.PlayerJump;
    [SerializeField] private string landEventName = DeathAnchorWwiseEvents.PlayerLand;
    [SerializeField] private string wallSlideLoopEventName = DeathAnchorWwiseEvents.WallSlide;

    [Header("Movement Audio")]
    [Tooltip("Minimum absolute input before footstep events are emitted.")]
    [SerializeField] private float walkInputThreshold = 0.1f;

    private DeathAnchorPlayerController controller;
    private bool wasGrounded;
    private bool moveLoopPlaying;
    private bool wallSlideLoopPlaying;

    private void Awake()
    {
        controller = GetComponent<DeathAnchorPlayerController>();
        NormalizeEventNames();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        NormalizeEventNames();
    }
#endif

    private void Start()
    {
        wasGrounded = controller.Grounded;
    }

    private void Update()
    {
        float horizontalInput = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontalInput -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontalInput += 1f;

        bool isMovingOnGround = controller.Grounded && Mathf.Abs(horizontalInput) > walkInputThreshold;
        SetLoop(moveLoopEventName, isMovingOnGround, ref moveLoopPlaying);
        SetLoop(wallSlideLoopEventName, controller.WallSliding, ref wallSlideLoopPlaying);

        if (wasGrounded && !controller.Grounded && !string.IsNullOrWhiteSpace(jumpEventName))
        {
            DeathAnchorWwiseAudio.Post(gameObject, jumpEventName);
        }
        else if (!wasGrounded && controller.Grounded && !string.IsNullOrWhiteSpace(landEventName))
        {
            DeathAnchorWwiseAudio.Post(gameObject, landEventName);
        }

        wasGrounded = controller.Grounded;
    }

    private void OnDisable()
    {
        StopAllLoops();
    }

    private void OnDestroy()
    {
        StopAllLoops();
    }

    private void SetLoop(string eventName, bool shouldPlay, ref bool isPlaying)
    {
        if (shouldPlay)
        {
            DeathAnchorWwiseAudio.StartLoop(gameObject, eventName, ref isPlaying);
        }
        else
        {
            DeathAnchorWwiseAudio.StopLoop(gameObject, eventName, ref isPlaying);
        }
    }

    private void StopAllLoops()
    {
        DeathAnchorWwiseAudio.StopLoop(gameObject, moveLoopEventName, ref moveLoopPlaying);
        DeathAnchorWwiseAudio.StopLoop(gameObject, wallSlideLoopEventName, ref wallSlideLoopPlaying);
    }

    private void NormalizeEventNames()
    {
        if (string.IsNullOrWhiteSpace(moveLoopEventName) || moveLoopEventName == "footstep")
        {
            moveLoopEventName = DeathAnchorWwiseEvents.PlayerMove;
        }

        if (string.IsNullOrWhiteSpace(jumpEventName))
        {
            jumpEventName = DeathAnchorWwiseEvents.PlayerJump;
        }

        if (string.IsNullOrWhiteSpace(landEventName))
        {
            landEventName = DeathAnchorWwiseEvents.PlayerLand;
        }

        if (string.IsNullOrWhiteSpace(wallSlideLoopEventName))
        {
            wallSlideLoopEventName = DeathAnchorWwiseEvents.WallSlide;
        }
    }
}
