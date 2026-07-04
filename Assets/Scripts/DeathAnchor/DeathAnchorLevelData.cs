using System;
using UnityEngine;

[Serializable]
public sealed class DeathAnchorLevelData
{
    public string schema;
    public string title;
    public DeathAnchorRules rules;
    public DeathAnchorWorld world;
    public DeathAnchorPlayerSpec player;
    public DeathAnchorLevelObject spawn;
    public DeathAnchorLevelObject[] goals;
    public DeathAnchorLevelObject[] platforms;
    public DeathAnchorLevelObject[] movingPlatforms;
    public DeathAnchorLevelObject[] spikes;
    public DeathAnchorLevelObject[] keys;
    public DeathAnchorLevelObject[] doors;
    public DeathAnchorLevelObject[] buttons;
    public DeathAnchorLevelObject[] bridges;
    public DeathAnchorLevelObject[] anchorZones;
    public DeathAnchorLevelObject[] notes;
}

[Serializable]
public sealed class DeathAnchorRules
{
    public float recordWindowSec = 5f;
    public int maxGhosts = 1;
    public bool ghostSolid = true;
    public bool ghostCanStandOnPlayer = true;
    public bool ghostCanPressButtons = true;
    public bool ghostIgnoresHazards = true;
    public bool playerWallSlide = true;
    public float wallSlideMaxSpeed = 125f;
}

[Serializable]
public sealed class DeathAnchorWorld
{
    public float w = 2400f;
    public float h = 720f;
    public float grid = 20f;
}

[Serializable]
public sealed class DeathAnchorPlayerSpec
{
    public float w = 30f;
    public float h = 42f;
    public DeathAnchorPlayerAbilities abilities;
}

[Serializable]
public sealed class DeathAnchorPlayerAbilities
{
    public bool wallSlide = true;
    public float wallSlideMaxSpeed = 125f;
}

[Serializable]
public sealed class DeathAnchorLevelObject
{
    public string id;
    public string type;
    public string label;
    public float x;
    public float y;
    public float w;
    public float h;
    public float rotation;
    public string channel;
    public string[] links;
    public string[] tags;
    public string notes;
    public string requiredKey;
    public string requiredButton;
    public string mode;
    public string pressedBy;
    public string affects;
    public string defaultState;
    public string activeState;
    public float moveTargetX;
    public float moveTargetY;
    public float periodSec;
}

[Serializable]
public struct DeathAnchorReplayFrame
{
    public float time;
    public Vector2 footOffset;
    public int facing;

    public DeathAnchorReplayFrame(float time, Vector2 footOffset, int facing)
    {
        this.time = time;
        this.footOffset = footOffset;
        this.facing = facing;
    }
}
