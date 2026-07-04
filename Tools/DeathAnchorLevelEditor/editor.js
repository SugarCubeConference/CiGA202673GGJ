const canvas = document.getElementById("mapCanvas");
const ctx = canvas.getContext("2d");

const ui = {
  status: document.getElementById("statusText"),
  toolGrid: document.getElementById("toolGrid"),
  title: document.getElementById("titleInput"),
  worldW: document.getElementById("worldWInput"),
  worldH: document.getElementById("worldHInput"),
  grid: document.getElementById("gridInput"),
  snap: document.getElementById("snapInput"),
  cameraX: document.getElementById("cameraXInput"),
  cameraY: document.getElementById("cameraYInput"),
  ruleNotes: document.getElementById("ruleNotesInput"),
  experience: document.getElementById("experienceInput"),
  inspector: document.getElementById("inspector"),
  emptyInspector: document.getElementById("emptyInspector"),
  validation: document.getElementById("validation"),
  jsonBox: document.getElementById("jsonBox"),
  playBtn: document.getElementById("playBtn"),
  stopPlayBtn: document.getElementById("stopPlayBtn"),
  downloadLevelBtn: document.getElementById("downloadLevelBtn"),
  saveBtn: document.getElementById("saveBtn"),
  copyBriefBtn: document.getElementById("copyBriefBtn"),
  resetBtn: document.getElementById("resetBtn"),
  blankBtn: document.getElementById("blankBtn"),
  duplicateBtn: document.getElementById("duplicateBtn"),
  rotateBtn: document.getElementById("rotateBtn"),
  deleteBtn: document.getElementById("deleteBtn"),
  fitBtn: document.getElementById("fitBtn"),
  exportBtn: document.getElementById("exportBtn"),
  exportLevelBtn: document.getElementById("exportLevelBtn"),
  copyJsonBtn: document.getElementById("copyJsonBtn"),
  importBtn: document.getElementById("importBtn"),
  importFileBtn: document.getElementById("importFileBtn"),
  fileInput: document.getElementById("fileInput"),
  loadBtn: document.getElementById("loadBtn")
};

const VIEW_W = canvas.width;
const VIEW_H = canvas.height;
let PLAYER_W = 32;
let PLAYER_H = 32;
const DEFAULT_WORLD_W = 1024;
const DEFAULT_WORLD_H = 576;
const DEFAULT_GRID = 32;
const STORAGE_KEY = "deathAnchorMapEditor.v1";
const PLAYER_PHYSICS_URLS = [
  "../../Assets/StreamingAssets/DeathAnchor/player-physics.json",
  "./player-physics.json"
];
let PLAY_GRAVITY = 2050;
let PLAY_FALL_GRAVITY_MULT = 1.22;
let PLAY_MOVE_SPEED = 285;
let PLAY_GROUND_ACCEL = 3000;
let PLAY_AIR_ACCEL = 1850;
let PLAY_GROUND_FRICTION = 3800;
let PLAY_JUMP_SPEED = 670;
let PLAY_MAX_FALL_SPEED = 920;
let PLAY_COYOTE_MS = 95;
let PLAY_JUMP_BUFFER_MS = 130;
let PLAY_JUMP_CUT_MULT = 0.52;
let PLAY_INSTANT_HORIZONTAL = false;
const PLAY_SAMPLE_MS = 1000 / 60;
const OLD_PLAY_WALL_SLIDE_SPEED = 185;
let PLAY_WALL_SLIDE_SPEED = 125;

const objectTypes = {
  platform: { label: "地形", color: "#667383", w: 32, h: 32 },
  movingPlatform: { label: "移动平台", color: "#8bd17c", w: 96, h: 32 },
  spike: { label: "尖刺", color: "#ff636d", w: 32, h: 32 },
  laser: { label: "激光发射器", color: "#ff4f9a", w: 32, h: 32 },
  key: { label: "钥匙", color: "#f4c95d", w: 32, h: 32 },
  door: { label: "门", color: "#ff8d66", w: 32, h: 64 },
  button: { label: "按钮", color: "#f4c95d", w: 32, h: 32 },
  bridge: { label: "实虚桥", color: "#71d7e8", w: 32, h: 32 },
  spawn: { label: "出生点", color: "#65d18a", w: PLAYER_W, h: PLAYER_H },
  goal: { label: "出口", color: "#f4c95d", w: 64, h: 64 },
  anchorZone: { label: "锚点区", color: "#b49cff", w: 64, h: 64 },
  note: { label: "备注", color: "#f6eddd", w: 190, h: 62 }
};

const tools = [
  { id: "select", label: "选择", icon: "↖", color: "#f6eddd" },
  { id: "platform", label: "地形", icon: "▰", color: objectTypes.platform.color, type: "platform" },
  { id: "movingPlatform", label: "按钮平台", icon: "⇄", color: objectTypes.movingPlatform.color, type: "movingPlatform", defaultMotionMode: "button" },
  { id: "autoMovingPlatform", label: "自动平台", icon: "↔", color: objectTypes.movingPlatform.color, type: "movingPlatform", defaultMotionMode: "auto" },
  { id: "spike", label: "尖刺", icon: "▲", color: objectTypes.spike.color, type: "spike" },
  { id: "laser", label: "激光", icon: "━", color: objectTypes.laser.color, type: "laser" },
  { id: "key", label: "钥匙", icon: "●", color: objectTypes.key.color, type: "key" },
  { id: "door", label: "门", icon: "▥", color: objectTypes.door.color, type: "door" },
  { id: "button", label: "按钮", icon: "▔", color: objectTypes.button.color, type: "button" },
  { id: "bridgeSolid", label: "实桥", icon: "═", color: objectTypes.bridge.color, type: "bridge", defaultState: "solid", activeState: "solid" },
  { id: "bridgePhantom", label: "按钮桥", icon: "⋯", color: objectTypes.bridge.color, type: "bridge", defaultState: "phantom", activeState: "solid" },
  { id: "spawn", label: "出生", icon: "◆", color: objectTypes.spawn.color, type: "spawn" },
  { id: "goal", label: "出口", icon: "◇", color: objectTypes.goal.color, type: "goal" },
  { id: "anchorZone", label: "锚点区", icon: "⌾", color: objectTypes.anchorZone.color, type: "anchorZone" },
  { id: "note", label: "备注", icon: "T", color: objectTypes.note.color, type: "note" }
];

const sampleMap = {
  schema: "death-anchor-map-v1",
  title: "1024x576 单屏机关草图",
  ruleNotes: "玩家开启时空锚点后需要在倒计时内死亡；死亡路线会固定成一个有实体的循环分身。玩家贴墙时会慢速下滑。分身可被玩家踩，也可以踩玩家头；分身能压按钮；分身免疫尖刺、激光和陷阱。最多一个分身。钥匙开门；按钮按下时让桥变实，也让移动平台向按钮方向平移；松开时桥变虚，移动平台回原位。激光会被地形、实体桥、移动平台阻挡。",
  experience: "先用死亡制造台阶，再让玩家把同一条死亡路线和钥匙门、按钮桥组合起来：玩家可能要让分身越过陷阱去压按钮，或自己站成分身的垫脚石。",
  rules: {
    recordWindowSec: 5,
    maxGhosts: 1,
    ghostSolid: true,
    ghostCanStandOnPlayer: true,
    ghostCanPressButtons: true,
    ghostIgnoresHazards: true,
    playerWallSlide: true,
    wallSlideMaxSpeed: PLAY_WALL_SLIDE_SPEED
  },
  world: { w: DEFAULT_WORLD_W, h: DEFAULT_WORLD_H, grid: DEFAULT_GRID },
  objects: [
    { id: "spawn", type: "spawn", label: "出生点", x: 64, y: 480, w: PLAYER_W, h: PLAYER_H, channel: "", links: [], tags: ["start"], notes: "" },
    { id: "floor", type: "platform", label: "地面", x: 0, y: 512, w: 1024, h: 64, channel: "", links: [], tags: ["ground"], notes: "按 32x32 地形块拼接。" },
    { id: "left-wall", type: "platform", label: "左墙", x: 0, y: 384, w: 32, h: 128, channel: "", links: [], tags: ["wall"], notes: "地形块侧边也算墙，可触发贴墙缓降。" },
    { id: "high-step", type: "platform", label: "分身高台", x: 320, y: 352, w: 160, h: 32, channel: "", links: [], tags: ["platform"], notes: "地面跳不到，需要借分身站位。" },
    { id: "death-spikes", type: "spike", label: "录制死亡刺", x: 224, y: 480, w: 96, h: 32, channel: "", affects: "player", links: [], tags: ["death", "record"], notes: "只杀玩家；分身经过不会消失。玩家需要在锚点倒计时内死在这里。" },
    { id: "anchor-window", type: "anchorZone", label: "建议开锚区", x: 64, y: 416, w: 64, h: 64, channel: "", links: ["death-spikes"], tags: ["anchor"], notes: "不是实体，只是标注玩家大概在哪里开时空锚点。" },
    { id: "key-a", type: "key", label: "钥匙 A", x: 384, y: 320, w: 32, h: 32, channel: "A", links: ["door-a"], tags: ["key"], notes: "" },
    { id: "door-a", type: "door", label: "门 A", x: 640, y: 448, w: 32, h: 64, channel: "A", requiredKey: "key-a", links: [], tags: ["door"], notes: "需要 key-a。" },
    { id: "button-b", type: "button", label: "按钮 B", x: 544, y: 480, w: 32, h: 32, channel: "B", mode: "hold", pressedBy: "both", links: ["bridge-b"], tags: ["button"], notes: "玩家或分身都能压；按住时让桥变实，松开桥变虚。" },
    { id: "bridge-b", type: "bridge", label: "按钮桥 B", x: 704, y: 352, w: 96, h: 32, channel: "B", requiredButton: "button-b", defaultState: "phantom", activeState: "solid", links: [], tags: ["bridge"], notes: "按钮被压住时是实桥，按钮松开时是虚桥。" },
    { id: "goal", type: "goal", label: "出口", x: 928, y: 448, w: 64, h: 64, channel: "", links: [], tags: ["goal"], notes: "" }
  ]
};

const blankMap = {
  schema: "death-anchor-map-v1",
  title: "新死亡锚点地图",
  ruleNotes: "死亡锚点：倒计时内死亡会生成一个固定的实体循环分身。玩家贴墙时会慢速下滑。机关：钥匙开门；按钮按下时让桥变实，也让移动平台向按钮方向平移；松开时桥变虚，移动平台回原位。激光会被地形、实体桥、移动平台阻挡。",
  experience: "",
  rules: {
    recordWindowSec: 5,
    maxGhosts: 1,
    ghostSolid: true,
    ghostCanStandOnPlayer: true,
    ghostCanPressButtons: true,
    ghostIgnoresHazards: true,
    playerWallSlide: true,
    wallSlideMaxSpeed: PLAY_WALL_SLIDE_SPEED
  },
  world: { w: DEFAULT_WORLD_W, h: DEFAULT_WORLD_H, grid: DEFAULT_GRID },
  objects: [
    { id: "spawn", type: "spawn", label: "出生点", x: 64, y: 480, w: PLAYER_W, h: PLAYER_H, channel: "", links: [], tags: ["start"], notes: "" },
    { id: "floor", type: "platform", label: "地面", x: 0, y: 512, w: 1024, h: 64, channel: "", links: [], tags: ["ground"], notes: "按 32x32 地形块拼接。" },
    { id: "goal", type: "goal", label: "出口", x: 928, y: 448, w: 64, h: 64, channel: "", links: [], tags: ["goal"], notes: "" }
  ]
};

let map = null;
let selectedId = null;
let tool = "select";
let camera = { x: 0, y: 0 };
let drag = null;
let snapEnabled = true;
let jsonMode = "editor";
let playtest = null;
let playFrame = 0;
let playLastTime = 0;
const playKeys = new Set();

function clone(value) {
  return JSON.parse(JSON.stringify(value));
}

function number(value, fallback = 0) {
  const n = Number(value);
  return Number.isFinite(n) ? n : fallback;
}

function normalizeRotation(value) {
  return ((Math.round(number(value, 0) / 90) * 90) % 360 + 360) % 360;
}

function parseList(value) {
  return String(value || "")
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);
}

function isHazardType(type) {
  return type === "spike" || type === "laser";
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;");
}

function setStatus(text) {
  ui.status.textContent = text;
}

async function loadPlayerPhysicsConfig() {
  for (const url of PLAYER_PHYSICS_URLS) {
    try {
      const response = await fetch(url, { cache: "no-store" });
      if (!response.ok) continue;
      applyPlayerPhysicsConfig(await response.json());
      return true;
    } catch {
      // Try the next location. Direct file opens usually cannot fetch JSON.
    }
  }
  return false;
}

function applyPlayerPhysicsConfig(config) {
  const editor = config?.editor || {};
  PLAYER_W = Math.max(1, number(editor.playerWidthPx, PLAYER_W));
  PLAYER_H = Math.max(1, number(editor.playerHeightPx, PLAYER_H));
  PLAY_GRAVITY = Math.max(1, number(editor.gravityPxPerSec2, PLAY_GRAVITY));
  PLAY_FALL_GRAVITY_MULT = Math.max(1, number(editor.fallGravityMultiplier, PLAY_FALL_GRAVITY_MULT));
  PLAY_MOVE_SPEED = Math.max(1, number(editor.moveSpeedPxPerSec, PLAY_MOVE_SPEED));
  PLAY_GROUND_ACCEL = Math.max(1, number(editor.groundAccelerationPxPerSec2, PLAY_GROUND_ACCEL));
  PLAY_AIR_ACCEL = Math.max(1, number(editor.airAccelerationPxPerSec2, PLAY_AIR_ACCEL));
  PLAY_GROUND_FRICTION = Math.max(1, number(editor.groundFrictionPxPerSec2, PLAY_GROUND_FRICTION));
  PLAY_JUMP_SPEED = Math.max(1, number(editor.jumpSpeedPxPerSec, PLAY_JUMP_SPEED));
  PLAY_MAX_FALL_SPEED = Math.max(1, number(editor.maxFallSpeedPxPerSec, PLAY_MAX_FALL_SPEED));
  PLAY_COYOTE_MS = Math.max(0, number(editor.coyoteTimeMs, PLAY_COYOTE_MS));
  PLAY_JUMP_BUFFER_MS = Math.max(0, number(editor.jumpBufferMs, PLAY_JUMP_BUFFER_MS));
  PLAY_JUMP_CUT_MULT = clampValue(number(editor.jumpCutMultiplier, PLAY_JUMP_CUT_MULT), 0.05, 1);
  PLAY_INSTANT_HORIZONTAL = editor.instantHorizontalMovement === true;
  PLAY_WALL_SLIDE_SPEED = Math.max(1, number(editor.wallSlideMaxSpeedPxPerSec, PLAY_WALL_SLIDE_SPEED));
  objectTypes.spawn.w = PLAYER_W;
  objectTypes.spawn.h = PLAYER_H;
  syncDefaultMapPhysics(sampleMap);
  syncDefaultMapPhysics(blankMap);
}

function syncDefaultMapPhysics(defaultMap) {
  defaultMap.rules.wallSlideMaxSpeed = PLAY_WALL_SLIDE_SPEED;
  for (const object of defaultMap.objects) {
    if (object.type !== "spawn") continue;
    object.w = PLAYER_W;
    object.h = PLAYER_H;
  }
}

function playerPhysicsExport() {
  return {
    editorUnits: "px",
    playerWidth: PLAYER_W,
    playerHeight: PLAYER_H,
    gravity: PLAY_GRAVITY,
    fallGravityMultiplier: PLAY_FALL_GRAVITY_MULT,
    moveSpeed: PLAY_MOVE_SPEED,
    groundAcceleration: PLAY_GROUND_ACCEL,
    airAcceleration: PLAY_AIR_ACCEL,
    groundFriction: PLAY_GROUND_FRICTION,
    jumpSpeed: PLAY_JUMP_SPEED,
    maxFallSpeed: PLAY_MAX_FALL_SPEED,
    coyoteTimeMs: PLAY_COYOTE_MS,
    jumpBufferMs: PLAY_JUMP_BUFFER_MS,
    jumpCutMultiplier: PLAY_JUMP_CUT_MULT,
    instantHorizontalMovement: PLAY_INSTANT_HORIZONTAL,
    wallSlideMaxSpeed: PLAY_WALL_SLIDE_SPEED
  };
}

function normalizeMap(source) {
  const data = clone(source || {});
  data.schema = "death-anchor-map-v1";
  data.title = String(data.title || "Untitled Map");
  data.ruleNotes = String(data.ruleNotes || "");
  data.experience = String(data.experience || "");
  const rawWallSlideMaxSpeed = number(data.rules?.wallSlideMaxSpeed, PLAY_WALL_SLIDE_SPEED);
  const wallSlideMaxSpeed = rawWallSlideMaxSpeed === OLD_PLAY_WALL_SLIDE_SPEED ? PLAY_WALL_SLIDE_SPEED : rawWallSlideMaxSpeed;
  data.rules = {
    recordWindowSec: Math.max(1, number(data.rules?.recordWindowSec, 5)),
    maxGhosts: Math.max(1, number(data.rules?.maxGhosts, 1)),
    ghostSolid: data.rules?.ghostSolid !== false,
    ghostCanStandOnPlayer: data.rules?.ghostCanStandOnPlayer !== false,
    ghostCanPressButtons: data.rules?.ghostCanPressButtons !== false,
    ghostIgnoresHazards: data.rules?.ghostIgnoresHazards !== false,
    playerWallSlide: data.rules?.playerWallSlide !== false,
    wallSlideMaxSpeed: Math.max(40, wallSlideMaxSpeed)
  };
  data.world = {
    w: Math.max(DEFAULT_WORLD_W, number(data.world?.w, DEFAULT_WORLD_W)),
    h: Math.max(DEFAULT_WORLD_H, number(data.world?.h, DEFAULT_WORLD_H)),
    grid: Math.max(16, number(data.world?.grid, DEFAULT_GRID))
  };
  data.objects = Array.isArray(data.objects) ? data.objects.map(normalizeObject) : [];
  hydrateRelations(data.objects);
  return data;
}

function normalizeObject(item, index = 0) {
  const type = objectTypes[item?.type] ? item.type : "platform";
  const info = objectTypes[type];
  const object = {
    id: String(item?.id || `${type}-${index + 1}`),
    type,
    label: String(item?.label || info.label),
    x: number(item?.x, 80),
    y: number(item?.y, 80),
    w: Math.max(1, number(item?.w, info.w)),
    h: Math.max(1, number(item?.h, info.h)),
    rotation: normalizeRotation(item?.rotation),
    channel: String(item?.channel || ""),
    links: Array.isArray(item?.links) ? item.links.map(String) : parseList(item?.links),
    tags: Array.isArray(item?.tags) ? item.tags.map(String) : parseList(item?.tags),
    notes: String(item?.notes || "")
  };
  if (type === "door") object.requiredKey = String(item?.requiredKey || "");
  if (type === "button") object.mode = "hold";
  if (type === "button") object.pressedBy = ["player", "ghost", "both"].includes(item?.pressedBy) ? item.pressedBy : "both";
  if (isHazardType(type)) object.affects = ["player", "ghost", "both"].includes(item?.affects) ? item.affects : "player";
  if (type === "laser") object.attachedTo = String(item?.attachedTo || "");
  if (type === "movingPlatform") {
    object.requiredButton = String(item?.requiredButton || "");
    object.motionMode = item?.motionMode === "auto" ? "auto" : "button";
    object.moveTargetX = number(item?.moveTargetX, object.x + DEFAULT_GRID * 4);
    object.moveTargetY = number(item?.moveTargetY, object.y);
    object.periodSec = Math.max(0.5, number(item?.periodSec, 3));
  }
  if (type === "bridge") {
    object.requiredButton = String(item?.requiredButton || "");
    object.defaultState = item?.defaultState === "phantom" ? "phantom" : "solid";
    object.activeState = item?.activeState === "phantom" ? "phantom" : "solid";
  }
  return object;
}

function hydrateRelations(objects) {
  const byId = new Map(objects.map((object) => [object.id, object]));
  for (const object of objects) {
    if (object.type !== "button") continue;
    object.links = object.links.filter((id) => {
      const linked = byId.get(id);
      return !(linked?.type === "movingPlatform" && linked.motionMode === "auto");
    });
  }
  for (const object of objects) {
    if (object.type !== "key") continue;
    for (const link of object.links) {
      const door = byId.get(link);
      if (door?.type !== "door") continue;
      if (!door.requiredKey) door.requiredKey = object.id;
      if (!door.channel && object.channel) door.channel = object.channel;
      if (!object.channel && door.channel) object.channel = door.channel;
    }
  }
  for (const object of objects) {
    if (object.type !== "button") continue;
    for (const link of object.links) {
      const bridge = byId.get(link);
      if (bridge?.type === "bridge") {
        if (!bridge.requiredButton) bridge.requiredButton = object.id;
        if (!bridge.channel && object.channel) bridge.channel = object.channel;
        if (!object.channel && bridge.channel) object.channel = bridge.channel;
      }
      const platform = byId.get(link);
      if (platform?.type === "movingPlatform" && platform.motionMode !== "auto") {
        if (!platform.requiredButton) platform.requiredButton = object.id;
        if (!platform.channel && object.channel) platform.channel = object.channel;
        if (!object.channel && platform.channel) object.channel = platform.channel;
      }
    }
  }
  for (const controlled of objects) {
    if (!["bridge", "movingPlatform"].includes(controlled.type) || !controlled.requiredButton) continue;
    if (controlled.type === "movingPlatform" && controlled.motionMode === "auto") {
      controlled.requiredButton = "";
      continue;
    }
    const button = byId.get(controlled.requiredButton);
    if (button?.type !== "button") continue;
    addLink(button, controlled.id);
    if (!controlled.channel && button.channel) controlled.channel = button.channel;
    if (!button.channel && controlled.channel) button.channel = controlled.channel;
  }
}

function initToolGrid() {
  ui.toolGrid.innerHTML = "";
  for (const item of tools) {
    const button = document.createElement("button");
    button.type = "button";
    button.dataset.tool = item.id;
    button.style.color = item.color;
    button.innerHTML = `<span class="tool-icon">${escapeHtml(item.icon)}</span><span>${escapeHtml(item.label)}</span>`;
    ui.toolGrid.appendChild(button);
  }
}

function selectedObject() {
  return map.objects.find((object) => object.id === selectedId) || null;
}

function setTool(nextTool) {
  tool = nextTool;
  for (const button of ui.toolGrid.querySelectorAll("button")) {
    button.classList.toggle("active", button.dataset.tool === tool);
  }
  canvas.style.cursor = tool === "select" ? "default" : "crosshair";
  const info = tools.find((item) => item.id === tool);
  setStatus(tool === "select" ? "选择对象后可拖拽、缩放、改属性。" : `点击画布放置：${info?.label || tool}`);
}

function selectObject(id) {
  selectedId = id;
  updateInspector();
  updateValidation();
  draw();
}

function uniqueId(type) {
  const used = new Set(map.objects.map((object) => object.id));
  let i = 1;
  while (used.has(`${type}-${i}`)) i += 1;
  return `${type}-${i}`;
}

function screenToWorld(x, y) {
  return { x: x + camera.x, y: y + camera.y };
}

function worldToScreen(x, y) {
  return { x: x - camera.x, y: y - camera.y };
}

function snap(value) {
  return snapEnabled ? Math.round(value / map.world.grid) * map.world.grid : Math.round(value);
}

function pointer(event) {
  const rect = canvas.getBoundingClientRect();
  const x = (event.clientX - rect.left) * (canvas.width / rect.width);
  const y = (event.clientY - rect.top) * (canvas.height / rect.height);
  return screenToWorld(x, y);
}

function addObject(toolId, x, y) {
  const toolInfo = tools.find((item) => item.id === toolId);
  if (!toolInfo?.type) return;
  const type = toolInfo.type;
  const info = objectTypes[type];
  const object = {
    id: uniqueId(type),
    type,
    label: defaultLabel(type, toolInfo),
    x: snap(x - info.w / 2),
    y: snap(y - info.h / 2),
    w: info.w,
    h: info.h,
    rotation: 0,
    channel: nextChannel(type),
    links: [],
    tags: [type],
    notes: ""
  };
  if (type === "bridge") {
    object.defaultState = toolInfo.defaultState || "solid";
    object.activeState = toolInfo.activeState || "solid";
    object.label = object.defaultState === "solid" ? "实桥" : "按钮桥";
  }
  if (type === "button") object.mode = "hold";
  if (type === "button") object.pressedBy = "both";
  if (isHazardType(type)) object.affects = "player";
  if (type === "laser") object.attachedTo = "";
  if (type === "movingPlatform") {
    object.requiredButton = "";
    object.motionMode = toolInfo.defaultMotionMode || "button";
    object.moveTargetX = snap(object.x + map.world.grid * 4);
    object.moveTargetY = object.y;
    object.periodSec = 3;
    if (object.motionMode === "auto") {
      object.label = "自动平台";
      object.tags = ["movingPlatform", "auto"];
    } else {
      object.label = "按钮平台";
    }
  }
  if (type === "door") object.requiredKey = "";
  if (type === "note") {
    object.label = "设计备注";
    object.notes = "写这里为什么要放这个结构。";
  }
  map.objects.push(object);
  selectObject(object.id);
  writeJson();
}

function defaultLabel(type, toolInfo) {
  if (type === "bridge") return toolInfo.defaultState === "phantom" ? "按钮桥" : "实桥";
  return objectTypes[type].label;
}

function nextChannel(type) {
  if (!["key", "door", "button", "bridge"].includes(type)) return "";
  const used = new Set(map.objects.map((object) => object.channel).filter(Boolean));
  const letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
  for (const letter of letters) {
    if (!used.has(letter)) return letter;
  }
  return String(used.size + 1);
}

function objectRect(object) {
  return { x: object.x, y: object.y, w: object.w, h: object.h };
}

function movingPlatformTargetRect(object) {
  const target = movingPlatformTarget(object);
  return { x: target.x, y: target.y, w: object.w, h: object.h };
}

function movingPlatformTargetHandleRect(object) {
  const target = movingPlatformTargetRect(object);
  const size = Math.max(20, Math.min(28, map.world.grid));
  return {
    x: target.x + target.w / 2 - size / 2,
    y: target.y + target.h / 2 - size / 2,
    w: size,
    h: size
  };
}

function centerOf(object) {
  const rect = objectRect(object);
  return { x: rect.x + rect.w / 2, y: rect.y + rect.h / 2 };
}

function pointInRect(x, y, rect) {
  return x >= rect.x && x <= rect.x + rect.w && y >= rect.y && y <= rect.y + rect.h;
}

function hitTest(x, y) {
  for (let i = map.objects.length - 1; i >= 0; i -= 1) {
    const object = map.objects[i];
    if (pointInRect(x, y, objectRect(object))) return object;
  }
  return null;
}

function hitMovingPlatformTarget(x, y) {
  const selected = selectedObject();
  const targetPadding = Math.max(8, map.world.grid / 4);
  if (selected?.type === "movingPlatform") {
    const target = movingPlatformTargetRect(selected);
    if (pointInRect(x, y, expandedRect(target, targetPadding))) return selected;
  }
  const platforms = [...map.objects].reverse().filter((object) => object?.type === "movingPlatform");
  for (const object of platforms) {
    if (!object || object.type !== "movingPlatform") continue;
    if (pointInRect(x, y, movingPlatformTargetHandleRect(object))) return object;
  }
  for (const object of platforms) {
    const target = movingPlatformTargetRect(object);
    if (pointInRect(x, y, expandedRect(target, targetPadding))) return object;
  }
  return null;
}

function pointerDown(event) {
  if (playtest) return;
  const pos = pointer(event);
  if (tool !== "select") {
    addObject(tool, pos.x, pos.y);
    setTool("select");
    return;
  }
  const targetHit = hitMovingPlatformTarget(pos.x, pos.y);
  if (targetHit) {
    selectObject(targetHit.id);
    setStatus(`拖动 ${targetHit.id} 的半透明目标框来设置目标位置。`);
    drag = {
      id: targetHit.id,
      mode: "moveTarget",
      startX: pos.x,
      startY: pos.y,
      base: clone(targetHit)
    };
    return;
  }
  const hit = hitTest(pos.x, pos.y);
  selectObject(hit?.id || null);
  if (!hit) return;
  const rect = objectRect(hit);
  const resize = Math.abs(pos.x - (rect.x + rect.w)) < 16 && Math.abs(pos.y - (rect.y + rect.h)) < 16;
  drag = {
    id: hit.id,
    mode: resize ? "resize" : "move",
    startX: pos.x,
    startY: pos.y,
    base: clone(hit),
    attachedLasers: hit.type === "movingPlatform" ? clone(map.objects.filter((object) => object.type === "laser" && object.attachedTo === hit.id)) : []
  };
}

function pointerMove(event) {
  if (playtest) return;
  if (!drag) return;
  const object = map.objects.find((item) => item.id === drag.id);
  if (!object) return;
  const pos = pointer(event);
  const dx = snap(pos.x - drag.startX);
  const dy = snap(pos.y - drag.startY);
  if (drag.mode === "moveTarget") {
    object.moveTargetX = snap(number(drag.base.moveTargetX, drag.base.x + DEFAULT_GRID * 4) + dx);
    object.moveTargetY = snap(number(drag.base.moveTargetY, drag.base.y) + dy);
  } else if (drag.mode === "resize") {
    object.w = Math.max(map.world.grid, snap(drag.base.w + dx));
    object.h = Math.max(map.world.grid, snap(drag.base.h + dy));
  } else {
    object.x = snap(drag.base.x + dx);
    object.y = snap(drag.base.y + dy);
    if (object.type === "movingPlatform") moveAttachedLasersFromDrag(object, drag, dx, dy);
  }
  updateInspector(false);
  draw();
}

function pointerUp() {
  if (playtest) return;
  if (!drag) return;
  drag = null;
  updateInspector();
  updateValidation();
  writeJson();
}

function wheel(event) {
  if (playtest) return;
  event.preventDefault();
  if (event.shiftKey) camera.y += event.deltaY;
  else camera.x += event.deltaX + event.deltaY;
  clampCamera();
  draw();
}

function clampCamera() {
  camera.x = Math.max(0, Math.min(Math.max(0, map.world.w - VIEW_W), camera.x));
  camera.y = Math.max(0, Math.min(Math.max(0, map.world.h - VIEW_H), camera.y));
  ui.cameraX.max = String(Math.max(0, map.world.w - VIEW_W));
  ui.cameraY.max = String(Math.max(0, map.world.h - VIEW_H));
  ui.cameraX.value = String(camera.x);
  ui.cameraY.value = String(camera.y);
}

function updateInspector(rebuild = true) {
  const object = selectedObject();
  ui.emptyInspector.style.display = object ? "none" : "block";
  if (!object) {
    ui.inspector.innerHTML = "";
    return;
  }
  if (!rebuild && document.activeElement?.dataset?.field) return;
  ui.inspector.innerHTML = "";
  const fields = [
    ["id", "ID", "text"],
    ["type", "类型", "select-type"],
    ["label", "名称", "text"],
    ["x", "X", "number"],
    ["y", "Y", "number"],
    ["w", "宽", "number"],
    ["h", "高", "number"],
    ["rotation", "旋转", "select-rotation"],
    ["channel", "频道", "text"]
  ];
  if (isHazardType(object.type)) fields.push(["affects", "伤害对象", "select-actor"]);
  if (object.type === "laser") fields.push(["attachedTo", "吸附平台ID", "text"]);
  if (object.type === "movingPlatform") {
    fields.push(["motionMode", "移动模式", "select-motion-mode"]);
    if (object.motionMode !== "auto") fields.push(["requiredButton", "控制按钮ID", "text"]);
    fields.push(["moveTargetX", "目标X", "number"]);
    fields.push(["moveTargetY", "目标Y", "number"]);
    fields.push(["periodSec", "移动秒", "number"]);
  }
  if (object.type === "door") fields.push(["requiredKey", "需要钥匙", "text"]);
  if (object.type === "button") fields.push(["pressedBy", "触发者", "select-actor"]);
  if (object.type === "bridge") fields.push(["requiredButton", "控制按钮ID", "text"]);
  fields.push(["links", linkFieldLabel(object), "text"]);
  fields.push(["tags", "标签", "text"]);
  fields.push(["notes", "备注", "textarea"]);

  for (const [key, label, kind] of fields) {
    ui.inspector.appendChild(makeField(object, key, label, kind));
  }
  makeInspectorActions(object);
}

function makeField(object, key, labelText, kind) {
  const label = document.createElement("label");
  label.className = kind === "textarea" ? "field wide" : "field";
  const span = document.createElement("span");
  span.textContent = labelText;
  label.appendChild(span);
  let input;
  if (kind === "select-type") {
    input = document.createElement("select");
    for (const [type, info] of Object.entries(objectTypes)) addOption(input, type, info.label);
  } else if (kind === "select-mode") {
    input = document.createElement("select");
    addOption(input, "hold", "按住");
    addOption(input, "toggle", "切换");
    addOption(input, "press", "踩一下");
  } else if (kind === "select-actor") {
    input = document.createElement("select");
    addOption(input, "player", "玩家");
    addOption(input, "ghost", "分身");
    addOption(input, "both", "玩家或分身");
  } else if (kind === "select-motion-mode") {
    input = document.createElement("select");
    addOption(input, "button", "按钮控制");
    addOption(input, "auto", "自动往返");
  } else if (kind === "select-state") {
    input = document.createElement("select");
    addOption(input, "solid", "实");
    addOption(input, "phantom", "虚");
  } else if (kind === "select-rotation") {
    input = document.createElement("select");
    addOption(input, "0", "0°");
    addOption(input, "90", "90°");
    addOption(input, "180", "180°");
    addOption(input, "270", "270°");
  } else if (kind === "textarea") {
    input = document.createElement("textarea");
    input.rows = 4;
  } else {
    input = document.createElement("input");
    input.type = kind;
    if (kind === "number") input.step = "1";
  }
  input.dataset.field = key;
  if (key === "tags" || key === "links") input.value = object[key].join(", ");
  else input.value = object[key] ?? "";
  input.addEventListener("input", () => {
    const field = input.dataset.field;
    const beforeX = object.x;
    const beforeY = object.y;
    if (field === "tags" || field === "links") object[field] = parseList(input.value);
    else if (["x", "y", "w", "h", "moveTargetX", "moveTargetY", "periodSec"].includes(field)) object[field] = number(input.value, object[field]);
    else if (field === "rotation") {
      if (object.type === "movingPlatform") rotateMovingPlatformTo(object, input.value);
      else object[field] = normalizeRotation(input.value);
    }
    else if (field === "type") changeObjectType(object, input.value);
    else object[field] = input.value;
    if (field === "requiredButton") syncBridgeRequiredButton(object);
    if (field === "requiredButton") syncMovingPlatformRequiredButton(object);
    if (object.type === "button" && field === "links") syncButtonBridgeLinks(object);
    if (object.type === "button" && field === "links") syncButtonMovingPlatformLinks(object);
    if (object.type === "button" && field === "links") removeAutoMovingPlatformLinks(object);
    if (object.type === "button" && ["x", "y", "w", "h", "links"].includes(field)) syncButtonMovingPlatformTargets(object);
    if (object.type === "movingPlatform" && field === "requiredButton") syncMovingPlatformTargetFromRequiredButton(object);
    if (object.type === "movingPlatform" && field === "motionMode" && object.motionMode === "auto") detachAutoMovingPlatformFromButtons(object);
    if (object.type === "laser" && field === "attachedTo") syncLaserAttachmentLink(object);
    if (object.type === "movingPlatform" && (field === "x" || field === "y")) moveAttachedLasersBy(object.id, object.x - beforeX, object.y - beforeY);
    if (field === "motionMode") updateInspector();
    updateValidation();
    draw();
    writeJson();
  });
  label.appendChild(input);
  return label;
}

function linkFieldLabel(object) {
  if (object.type === "key") return "绑定门ID";
  if (object.type === "button") return "控制机关ID";
  return "链接ID";
}

function addOption(select, value, text) {
  const option = document.createElement("option");
  option.value = value;
  option.textContent = text;
  select.appendChild(option);
}

function changeObjectType(object, nextType) {
  if (!objectTypes[nextType]) return;
  object.type = nextType;
  if (!object.tags.includes(nextType)) object.tags.push(nextType);
  if (nextType === "door" && object.requiredKey === undefined) object.requiredKey = "";
  if (nextType === "button") object.mode = "hold";
  if (nextType === "button" && !object.pressedBy) object.pressedBy = "both";
  if (isHazardType(nextType) && !object.affects) object.affects = "player";
  if (nextType === "movingPlatform") {
    object.requiredButton = object.requiredButton || "";
    object.motionMode = object.motionMode === "auto" ? "auto" : "button";
    object.moveTargetX = number(object.moveTargetX, object.x + DEFAULT_GRID * 4);
    object.moveTargetY = number(object.moveTargetY, object.y);
    object.periodSec = Math.max(0.5, number(object.periodSec, 3));
  }
  if (nextType === "laser") object.attachedTo = object.attachedTo || "";
  if (nextType === "bridge") {
    object.requiredButton = object.requiredButton || "";
    object.defaultState = object.defaultState || "phantom";
    object.activeState = "solid";
  }
  updateInspector();
}

function makeInspectorActions(object) {
  const wrap = document.createElement("div");
  wrap.className = "inspector-actions";
  const pairButton = document.createElement("button");
  pairButton.type = "button";
  pairButton.textContent = autoPairLabel(object);
  pairButton.disabled = !pairButton.textContent;
  pairButton.addEventListener("click", () => autoPair(object));
  wrap.appendChild(pairButton);

  const frontButton = document.createElement("button");
  frontButton.type = "button";
  frontButton.textContent = "置顶显示";
  frontButton.addEventListener("click", () => bringToFront(object.id));
  wrap.appendChild(frontButton);
  ui.inspector.appendChild(wrap);
}

function autoPairLabel(object) {
  if (object.type === "key") return "配最近的门";
  if (object.type === "door") return "配最近钥匙";
  if (object.type === "button") return "配最近机关";
  if (object.type === "bridge") return "配最近按钮";
  if (object.type === "movingPlatform" && object.motionMode !== "auto") return "配最近按钮";
  if (object.type === "laser") return "吸附最近移动平台";
  return "";
}

function autoPair(object) {
  const relation = autoPairTargetTypes(object);
  if (!relation.length) return;
  const target = nearestObjectOfTypes(object, relation);
  if (!target) {
    setStatus(`没有可配对的对象。`);
    return;
  }
  if (object.type === "key") {
    addLink(object, target.id);
    target.requiredKey = object.id;
    syncChannel(object, target);
  } else if (object.type === "door") {
    addLink(target, object.id);
    object.requiredKey = target.id;
    syncChannel(target, object);
  } else if (object.type === "button") {
    if (target.type === "bridge") bindButtonBridge(object, target);
    if (target.type === "movingPlatform") bindButtonMovingPlatform(object, target, true);
  } else if (object.type === "bridge") {
    bindButtonBridge(target, object);
  } else if (object.type === "movingPlatform") {
    bindButtonMovingPlatform(target, object, true);
  } else if (object.type === "laser") {
    bindLaserToPlatform(object, target);
  }
  updateInspector();
  updateValidation();
  draw();
  writeJson();
  setStatus(`已配对：${object.id} ↔ ${target.id}`);
}

function autoPairTargetTypes(object) {
  return {
    key: ["door"],
    door: ["key"],
    button: ["bridge", "movingPlatform"],
    bridge: ["button"],
    movingPlatform: ["button"],
    laser: ["movingPlatform"]
  }[object.type] || [];
}

function addLink(source, id) {
  if (!source.links.includes(id)) source.links.push(id);
}

function bindButtonBridge(button, bridge) {
  if (!button || !bridge || button.type !== "button" || bridge.type !== "bridge") return;
  addLink(button, bridge.id);
  bridge.requiredButton = button.id;
  syncChannel(button, bridge);
}

function bindButtonMovingPlatform(button, platform, updateTarget = false) {
  if (!button || !platform || button.type !== "button" || platform.type !== "movingPlatform") return;
  if (platform.motionMode === "auto") return;
  addLink(button, platform.id);
  platform.requiredButton = button.id;
  syncChannel(button, platform);
  if (updateTarget) setMovingPlatformTargetTowardButton(platform, button);
}

function bindLaserToPlatform(laser, platform) {
  if (!laser || laser.type !== "laser" || !platform || platform.type !== "movingPlatform") return;
  laser.attachedTo = platform.id;
  addLink(laser, platform.id);
  if (!laser.notes) laser.notes = `吸附在 ${platform.id} 上，试玩时会跟随平台移动。`;
}

function syncBridgeRequiredButton(bridge) {
  if (!bridge || bridge.type !== "bridge" || !bridge.requiredButton) return;
  const button = map.objects.find((object) => object.id === bridge.requiredButton && object.type === "button");
  if (button) bindButtonBridge(button, bridge);
}

function syncMovingPlatformRequiredButton(platform) {
  if (!platform || platform.type !== "movingPlatform" || !platform.requiredButton) return;
  if (platform.motionMode === "auto") {
    platform.requiredButton = "";
    return;
  }
  const button = map.objects.find((object) => object.id === platform.requiredButton && object.type === "button");
  if (button) bindButtonMovingPlatform(button, platform, true);
}

function syncLaserAttachmentLink(laser) {
  if (!laser || laser.type !== "laser") return;
  laser.links = laser.links.filter((id) => {
    const linked = map.objects.find((object) => object.id === id);
    return linked?.type !== "movingPlatform";
  });
  if (laser.attachedTo) addLink(laser, laser.attachedTo);
}

function moveAttachedLasersFromDrag(platform, dragState, dx, dy) {
  for (const baseLaser of dragState.attachedLasers || []) {
    const laser = map.objects.find((object) => object.id === baseLaser.id && object.type === "laser" && object.attachedTo === platform.id);
    if (!laser) continue;
    laser.x = snap(baseLaser.x + dx);
    laser.y = snap(baseLaser.y + dy);
  }
}

function moveAttachedLasersBy(platformId, dx, dy) {
  if (!dx && !dy) return;
  for (const laser of map.objects) {
    if (laser.type !== "laser" || laser.attachedTo !== platformId) continue;
    laser.x = snap(laser.x + dx);
    laser.y = snap(laser.y + dy);
  }
}

function syncButtonBridgeLinks(button) {
  if (!button || button.type !== "button") return;
  for (const id of button.links) {
    const bridge = map.objects.find((object) => object.id === id && object.type === "bridge");
    if (!bridge) continue;
    if (!bridge.requiredButton) bridge.requiredButton = button.id;
    syncChannel(button, bridge);
  }
}

function syncButtonMovingPlatformLinks(button) {
  if (!button || button.type !== "button") return;
  for (const id of button.links) {
    const platform = map.objects.find((object) => object.id === id && object.type === "movingPlatform");
    if (!platform) continue;
    if (platform.motionMode === "auto") continue;
    bindButtonMovingPlatform(button, platform, true);
  }
}

function removeAutoMovingPlatformLinks(button) {
  if (!button || button.type !== "button") return;
  button.links = button.links.filter((id) => {
    const linked = map.objects.find((object) => object.id === id);
    return !(linked?.type === "movingPlatform" && linked.motionMode === "auto");
  });
}

function detachAutoMovingPlatformFromButtons(platform) {
  if (!platform || platform.type !== "movingPlatform" || platform.motionMode !== "auto") return;
  platform.requiredButton = "";
  for (const object of map.objects) {
    if (object.type !== "button") continue;
    object.links = object.links.filter((id) => id !== platform.id);
  }
}

function syncButtonMovingPlatformTargets(button) {
  if (!button || button.type !== "button") return;
  const targets = map.objects.filter((object) => object.type === "movingPlatform" && object.motionMode !== "auto" && (object.requiredButton === button.id || button.links.includes(object.id)));
  for (const platform of targets) setMovingPlatformTargetTowardButton(platform, button);
}

function syncMovingPlatformTargetFromRequiredButton(platform) {
  if (!platform || platform.type !== "movingPlatform" || !platform.requiredButton) return;
  const button = map.objects.find((object) => object.id === platform.requiredButton && object.type === "button");
  if (button) setMovingPlatformTargetTowardButton(platform, button);
}

function syncChannel(a, b) {
  const channel = a.channel || b.channel || nextChannel(a.type);
  a.channel = channel;
  b.channel = channel;
}

function nearestObject(source, type) {
  return nearestObjectOfTypes(source, [type]);
}

function nearestObjectOfTypes(source, types) {
  const s = centerOf(source);
  const allowed = new Set(types);
  let best = null;
  let bestDist = Infinity;
  for (const object of map.objects) {
    if (object.id === source.id || !allowed.has(object.type)) continue;
    if (!canPairObjects(source, object)) continue;
    const c = centerOf(object);
    const dist = (c.x - s.x) ** 2 + (c.y - s.y) ** 2;
    if (dist < bestDist) {
      best = object;
      bestDist = dist;
    }
  }
  return best;
}

function canPairObjects(source, target) {
  if (source.type === "button" && target.type === "movingPlatform" && target.motionMode === "auto") return false;
  if (source.type === "movingPlatform" && source.motionMode === "auto" && target.type === "button") return false;
  return true;
}

function setMovingPlatformTargetTowardButton(platform, button) {
  const pc = centerOf(platform);
  const bc = centerOf(button);
  const dx = bc.x - pc.x;
  const dy = bc.y - pc.y;
  const len = Math.hypot(dx, dy) || 1;
  const currentTarget = movingPlatformTarget(platform);
  const currentDistance = Math.hypot(currentTarget.x - platform.x, currentTarget.y - platform.y);
  const distance = currentDistance > 8 ? currentDistance : Math.max(map.world.grid * 6, 160);
  platform.moveTargetX = snap(platform.x + (dx / len) * distance);
  platform.moveTargetY = snap(platform.y + (dy / len) * distance);
}

function bringToFront(id) {
  const index = map.objects.findIndex((object) => object.id === id);
  if (index < 0) return;
  const [object] = map.objects.splice(index, 1);
  map.objects.push(object);
  draw();
  writeJson();
}

function syncMetaFromUi() {
  map.title = ui.title.value;
  map.ruleNotes = ui.ruleNotes.value;
  map.experience = ui.experience.value;
  map.world.w = Math.max(DEFAULT_WORLD_W, number(ui.worldW.value, map.world.w));
  map.world.h = Math.max(DEFAULT_WORLD_H, number(ui.worldH.value, map.world.h));
  map.world.grid = Math.max(16, number(ui.grid.value, map.world.grid));
  snapEnabled = ui.snap.checked;
  clampCamera();
}

function syncUiFromMap() {
  ui.title.value = map.title;
  ui.ruleNotes.value = map.ruleNotes;
  ui.experience.value = map.experience;
  ui.worldW.value = String(map.world.w);
  ui.worldH.value = String(map.world.h);
  ui.grid.value = String(map.world.grid);
  ui.snap.checked = snapEnabled;
  clampCamera();
  updateInspector();
  updateValidation();
  writeJson();
  draw();
}

function updateValidation() {
  const warnings = [];
  const ids = map.objects.map((object) => object.id);
  const dupes = ids.filter((id, index) => ids.indexOf(id) !== index);
  if (dupes.length) warnings.push(`重复 ID：${[...new Set(dupes)].join(", ")}`);
  const idSet = new Set(ids);
  const byId = new Map(map.objects.map((object) => [object.id, object]));
  if (map.world.w !== DEFAULT_WORLD_W || map.world.h !== DEFAULT_WORLD_H) warnings.push(`当前项目规定单屏场景为 ${DEFAULT_WORLD_W}x${DEFAULT_WORLD_H}`);
  if (map.world.grid !== DEFAULT_GRID) warnings.push(`当前项目规定基础网格为 ${DEFAULT_GRID}px`);
  for (const object of map.objects) {
    for (const link of object.links) {
      if (!idSet.has(link)) warnings.push(`${object.id} 链接了不存在的对象：${link}`);
    }
    const rect = objectRect(object);
    if (rect.x + rect.w < 0 || rect.y + rect.h < 0 || rect.x > map.world.w || rect.y > map.world.h) {
      warnings.push(`${object.id} 在世界边界外`);
    }
    if (object.type === "movingPlatform") {
      const target = movingPlatformTarget(object);
      if (target.x + rect.w < 0 || target.y + rect.h < 0 || target.x > map.world.w || target.y > map.world.h) warnings.push(`${object.id} 的目标点在世界边界外`);
    }
    if (object.type === "key" && !object.links.some((id) => byId.get(id)?.type === "door")) warnings.push(`${object.id} 还没有链接门`);
    if (object.type === "door" && object.requiredKey && byId.get(object.requiredKey)?.type !== "key") warnings.push(`${object.id} 的钥匙 ID 不存在`);
    if (object.type === "door" && !object.requiredKey && !hasIncoming("key", object.id)) warnings.push(`${object.id} 还没有绑定钥匙`);
    if (object.type === "button" && !buttonControlsMechanism(object, byId)) warnings.push(`${object.id} 还没有绑定桥或移动平台`);
    if (object.type === "bridge" && object.requiredButton && byId.get(object.requiredButton)?.type !== "button") warnings.push(`${object.id} 的控制按钮 ID 不存在`);
    if (object.type === "bridge" && object.defaultState === "phantom" && !bridgeHasButtonControl(object, byId)) warnings.push(`${object.id} 还没有按钮控制`);
    if (object.type === "movingPlatform" && object.motionMode !== "auto" && object.requiredButton && byId.get(object.requiredButton)?.type !== "button") warnings.push(`${object.id} 的控制按钮 ID 不存在`);
    if (object.type === "movingPlatform" && object.motionMode !== "auto" && !movingPlatformHasButtonControl(object, byId)) warnings.push(`${object.id} 还没有按钮控制`);
    if (isHazardType(object.type) && map.rules.ghostIgnoresHazards && object.affects !== "player") warnings.push(`${object.id} 与规则冲突：当前设定分身免疫陷阱`);
    if (object.type === "laser" && object.attachedTo && byId.get(object.attachedTo)?.type !== "movingPlatform") warnings.push(`${object.id} 吸附的平台 ID 不存在或不是移动平台`);
  }
  const spawnCount = map.objects.filter((object) => object.type === "spawn").length;
  if (spawnCount !== 1) warnings.push(`出生点数量应为 1，现在是 ${spawnCount}`);
  if (!map.objects.some((object) => object.type === "goal")) warnings.push("没有出口");
  ui.validation.innerHTML = warnings.length
    ? warnings.map((warning) => `<div class="warn">${escapeHtml(warning)}</div>`).join("")
    : `<div class="ok">地图结构可同步。</div>`;
}

function hasIncoming(type, targetId) {
  return map.objects.some((object) => object.type === type && object.links.includes(targetId));
}

function buttonControlsMechanism(button, byId) {
  if (button.links.some((id) => byId.get(id)?.type === "bridge")) return true;
  if (button.links.some((id) => byId.get(id)?.type === "movingPlatform" && byId.get(id)?.motionMode !== "auto")) return true;
  return [...byId.values()].some((object) => (object.type === "bridge" || (object.type === "movingPlatform" && object.motionMode !== "auto")) && object.requiredButton === button.id);
}

function bridgeHasButtonControl(bridge, byId) {
  if (bridge.requiredButton && byId.get(bridge.requiredButton)?.type === "button") return true;
  return [...byId.values()].some((object) => object.type === "button" && object.links.includes(bridge.id));
}

function movingPlatformHasButtonControl(platform, byId) {
  if (platform.requiredButton && byId.get(platform.requiredButton)?.type === "button") return true;
  return [...byId.values()].some((object) => object.type === "button" && object.links.includes(platform.id));
}

function exportMap() {
  syncMetaFromUi();
  return normalizeMap(map);
}

function makeLevelJson() {
  const data = exportMap();
  const objects = data.objects;
  const byId = new Map(objects.map((object) => [object.id, object]));
  const spawn = objects.find((object) => object.type === "spawn") || { x: 80, y: 430, w: PLAYER_W, h: PLAYER_H };
  const goals = objects.filter((object) => object.type === "goal").map(runtimeObject);
  return {
    schema: "death-anchor-level-v1",
    title: data.title,
    ruleNotes: data.ruleNotes,
    experience: data.experience,
    rules: data.rules,
    world: data.world,
    player: {
      w: PLAYER_W,
      h: PLAYER_H,
      physics: playerPhysicsExport(),
      abilities: {
        wallSlide: data.rules.playerWallSlide,
        wallSlideMaxSpeed: data.rules.wallSlideMaxSpeed
      }
    },
    spawn: runtimeObject(spawn),
    goals,
    platforms: objects.filter((object) => object.type === "platform").map(runtimeObject),
    movingPlatforms: objects.filter((object) => object.type === "movingPlatform").map(runtimeObject),
    spikes: objects.filter((object) => object.type === "spike").map(runtimeObject),
    lasers: objects.filter((object) => object.type === "laser").map(runtimeObject),
    keys: objects.filter((object) => object.type === "key").map(runtimeObject),
    doors: objects.filter((object) => object.type === "door").map((object) => ({
      ...runtimeObject(object),
      requiredKey: object.requiredKey || incomingId(byId, "key", object.id)
    })),
    buttons: objects.filter((object) => object.type === "button").map(runtimeObject),
    bridges: objects.filter((object) => object.type === "bridge").map(runtimeObject),
    anchorZones: objects.filter((object) => object.type === "anchorZone").map(runtimeObject),
    notes: objects.filter((object) => object.type === "note").map(runtimeObject)
  };
}

function runtimeObject(object) {
  const result = {
    id: object.id,
    type: object.type,
    label: object.label,
    x: object.x,
    y: object.y,
    w: object.w,
    h: object.h
  };
  for (const key of ["rotation", "channel", "links", "tags", "notes", "requiredKey", "requiredButton", "mode", "pressedBy", "affects", "defaultState", "activeState", "motionMode", "moveTargetX", "moveTargetY", "periodSec", "attachedTo"]) {
    if (object[key] === undefined) continue;
    if (Array.isArray(object[key])) result[key] = [...object[key]];
    else result[key] = object[key];
  }
  return result;
}

function incomingId(byId, type, targetId) {
  for (const object of byId.values()) {
    if (object.type === type && object.links.includes(targetId)) return object.id;
  }
  return "";
}

function writeJson() {
  const data = jsonMode === "level" ? makeLevelJson() : exportMap();
  ui.jsonBox.value = JSON.stringify(data, null, 2);
}

function showEditorJson() {
  jsonMode = "editor";
  writeJson();
  setStatus("已生成编辑器 JSON，可用于继续编辑。");
}

function showLevelJson() {
  jsonMode = "level";
  writeJson();
  setStatus("已生成关卡 JSON，可下载或复制给运行时使用。");
}

function downloadLevelJson() {
  const levelJson = makeLevelJson();
  const text = JSON.stringify(levelJson, null, 2);
  const name = `${safeFileName(levelJson.title || "death-anchor-level")}.level.json`;
  downloadTextFile(name, text);
  ui.jsonBox.value = text;
  jsonMode = "level";
  setStatus(`已下载关卡 JSON：${name}`);
}

function safeFileName(value) {
  return String(value || "level")
    .trim()
    .replace(/[\\/:*?"<>|]+/g, "-")
    .replace(/\s+/g, "-")
    .slice(0, 60) || "level";
}

function downloadTextFile(fileName, text) {
  const blob = new Blob([text], { type: "application/json;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

function saveMap() {
  const data = exportMap();
  localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
  writeJson();
  setStatus("已保存到浏览器本地。");
}

function loadMap() {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) {
    setStatus("还没有保存过地图。");
    return;
  }
  try {
    map = normalizeMap(JSON.parse(raw));
    selectedId = null;
    syncUiFromMap();
    setStatus("已读取保存地图。");
  } catch {
    setStatus("读取失败：JSON 不合法。");
  }
}

function mapFromAnyJson(data) {
  if (data?.schema === "death-anchor-level-v1" || data?.platforms || data?.spawn) return mapFromLevelJson(data);
  return normalizeMap(data);
}

function mapFromLevelJson(level) {
  const objects = [];
  addRuntimeObject(objects, level.spawn, "spawn", "spawn", "出生点");
  addRuntimeList(objects, level.platforms, "platform");
  addRuntimeList(objects, level.movingPlatforms, "movingPlatform");
  addRuntimeList(objects, level.spikes, "spike");
  addRuntimeList(objects, level.lasers, "laser");
  addRuntimeList(objects, level.keys, "key");
  addRuntimeList(objects, level.doors, "door");
  addRuntimeList(objects, level.buttons, "button");
  addRuntimeList(objects, level.bridges, "bridge");
  addRuntimeList(objects, level.anchorZones, "anchorZone");
  addRuntimeList(objects, level.goals, "goal");
  addRuntimeList(objects, level.notes, "note");
  return normalizeMap({
    schema: "death-anchor-map-v1",
    title: level.title || "导入关卡",
    ruleNotes: level.ruleNotes || "",
    experience: level.experience || "",
    rules: level.rules || {},
    world: level.world || { w: DEFAULT_WORLD_W, h: DEFAULT_WORLD_H, grid: DEFAULT_GRID },
    objects
  });
}

function addRuntimeList(objects, list, type) {
  if (!Array.isArray(list)) return;
  for (const item of list) addRuntimeObject(objects, item, type);
}

function addRuntimeObject(objects, item, type, fallbackId, fallbackLabel) {
  if (!item) return;
  objects.push({
    id: String(item.id || fallbackId || uniqueImportId(objects, type)),
    type,
    label: String(item.label || fallbackLabel || objectTypes[type]?.label || type),
    x: number(item.x, 80),
    y: number(item.y, 80),
    w: Math.max(1, number(item.w, objectTypes[type]?.w || 40)),
    h: Math.max(1, number(item.h, objectTypes[type]?.h || 40)),
    rotation: normalizeRotation(item.rotation),
    channel: String(item.channel || ""),
    links: Array.isArray(item.links) ? item.links.map(String) : parseList(item.links),
    tags: Array.isArray(item.tags) ? item.tags.map(String) : parseList(item.tags),
    notes: String(item.notes || ""),
    requiredKey: item.requiredKey,
    requiredButton: item.requiredButton,
    mode: item.mode,
    pressedBy: item.pressedBy,
    affects: item.affects,
    defaultState: item.defaultState,
    activeState: item.activeState,
    motionMode: item.motionMode,
    moveTargetX: item.moveTargetX,
    moveTargetY: item.moveTargetY,
    periodSec: item.periodSec,
    attachedTo: item.attachedTo
  });
}

function uniqueImportId(objects, type) {
  const used = new Set(objects.map((object) => object.id));
  let i = 1;
  while (used.has(`${type}-${i}`)) i += 1;
  return `${type}-${i}`;
}

function applyImportedJson(data, sourceName = "JSON") {
  if (playtest) stopPlaytest();
  map = mapFromAnyJson(data);
  selectedId = null;
  camera = { x: 0, y: 0 };
  jsonMode = data?.schema === "death-anchor-level-v1" ? "level" : "editor";
  syncUiFromMap();
  setStatus(`已导入 ${sourceName}。`);
}

function importMap() {
  try {
    applyImportedJson(JSON.parse(ui.jsonBox.value), "文本 JSON");
  } catch {
    setStatus("导入失败：JSON 不合法。");
  }
}

function chooseJsonFile() {
  ui.fileInput.value = "";
  ui.fileInput.click();
}

async function importJsonFile() {
  const file = ui.fileInput.files?.[0];
  if (!file) return;
  try {
    const text = await file.text();
    const data = JSON.parse(text);
    ui.jsonBox.value = text;
    applyImportedJson(data, file.name);
  } catch {
    setStatus("文件导入失败：请选择合法 JSON 文件。");
  }
}

function resetMap() {
  map = normalizeMap(sampleMap);
  selectedId = null;
  camera = { x: 0, y: 0 };
  syncUiFromMap();
  setStatus("已恢复样例地图。");
}

function newBlankMap() {
  map = normalizeMap(blankMap);
  selectedId = null;
  camera = { x: 0, y: 0 };
  syncUiFromMap();
  setStatus("已新建空白地图。");
}

function duplicateSelected() {
  const object = selectedObject();
  if (!object) return;
  const copy = clone(object);
  copy.id = uniqueId(object.type);
  copy.x += map.world.grid * 2;
  copy.y += map.world.grid * 2;
  if (copy.type === "movingPlatform") {
    copy.moveTargetX = number(copy.moveTargetX, object.x + DEFAULT_GRID * 4) + map.world.grid * 2;
    copy.moveTargetY = number(copy.moveTargetY, object.y) + map.world.grid * 2;
  }
  map.objects.push(copy);
  selectObject(copy.id);
  writeJson();
}

function rotateMovingPlatformTo(object, nextRotation) {
  const current = normalizeRotation(object.rotation);
  const next = normalizeRotation(nextRotation);
  const delta = ((next - current) % 360 + 360) % 360;
  if (delta !== 0) {
    const center = centerOf(object);
    const targetCenter = centerOf(movingPlatformTargetRect(object));
    const dx = targetCenter.x - center.x;
    const dy = targetCenter.y - center.y;
    const rotatedOffset = rotateOffset(dx, dy, delta);
    const oldW = object.w;
    if ((delta / 90) % 2 === 1) {
      object.w = object.h;
      object.h = oldW;
    }
    object.x = snap(center.x - object.w / 2);
    object.y = snap(center.y - object.h / 2);
    object.moveTargetX = snap(center.x + rotatedOffset.x - object.w / 2);
    object.moveTargetY = snap(center.y + rotatedOffset.y - object.h / 2);
  }
  object.rotation = next;
}

function rotateOffset(dx, dy, degrees) {
  const rotation = normalizeRotation(degrees);
  if (rotation === 90) return { x: -dy, y: dx };
  if (rotation === 180) return { x: -dx, y: -dy };
  if (rotation === 270) return { x: dy, y: -dx };
  return { x: dx, y: dy };
}

function rotateSelected(delta = 90) {
  const object = selectedObject();
  if (!object) {
    setStatus("先选中一个物件再旋转。");
    return;
  }
  if (object.type === "movingPlatform") {
    rotateMovingPlatformTo(object, normalizeRotation((object.rotation || 0) + delta));
    updateInspector();
    updateValidation();
    draw();
    writeJson();
    setStatus(`${object.id} 已旋转到 ${object.rotation}°。`);
    return;
  }
  const center = centerOf(object);
  const oldW = object.w;
  object.w = object.h;
  object.h = oldW;
  object.x = snap(center.x - object.w / 2);
  object.y = snap(center.y - object.h / 2);
  object.rotation = normalizeRotation((object.rotation || 0) + delta);
  updateInspector();
  updateValidation();
  draw();
  writeJson();
  setStatus(`${object.id} 已旋转到 ${object.rotation}°。`);
}

function deleteSelected() {
  if (!selectedId) return;
  map.objects = map.objects.filter((object) => object.id !== selectedId);
  for (const object of map.objects) {
    object.links = object.links.filter((id) => id !== selectedId);
    if (object.requiredKey === selectedId) object.requiredKey = "";
    if (object.requiredButton === selectedId) object.requiredButton = "";
    if (object.attachedTo === selectedId) object.attachedTo = "";
  }
  selectedId = null;
  updateInspector();
  updateValidation();
  writeJson();
  draw();
}

function makeBrief() {
  const data = exportMap();
  const byId = new Map(data.objects.map((object) => [object.id, object]));
  const lines = [];
  lines.push(`# 地图同步：${data.title}`);
  lines.push("");
  lines.push(`世界：${data.world.w} x ${data.world.h}，网格 ${data.world.grid}`);
  lines.push(`规则：死亡锚点 ${data.rules.recordWindowSec}s 录制窗口，最多 ${data.rules.maxGhosts} 个实体分身。`);
  lines.push(`玩家能力：${data.rules.playerWallSlide ? `贴墙慢速下滑，最大下落速度 ${data.rules.wallSlideMaxSpeed}` : "无贴墙下滑"}。`);
  lines.push(`分身规则：${data.rules.ghostSolid ? "有实体" : "无实体"}；${data.rules.ghostCanStandOnPlayer ? "可踩玩家头" : "不可踩玩家"}；${data.rules.ghostCanPressButtons ? "可压按钮" : "不可压按钮"}；${data.rules.ghostIgnoresHazards ? "免疫陷阱" : "会被陷阱影响"}。`);
  lines.push("");
  lines.push("## 当前规则假设");
  lines.push(data.ruleNotes || "未填写");
  lines.push("");
  lines.push("## 玩家体验目标");
  lines.push(data.experience || "未填写");
  lines.push("");
  lines.push("## 机关关系");
  for (const object of data.objects) {
    if (object.type === "key") {
      const doors = object.links.filter((id) => byId.get(id)?.type === "door");
      lines.push(`- 钥匙 ${object.id} -> 门 ${doors.join(", ") || "未绑定"}`);
    }
    if (object.type === "button") {
      const bridges = controlledBridgeIds(object, byId);
      const platforms = controlledMovingPlatformIds(object, byId);
      lines.push(`- 按钮 ${object.id} (${actorLabel(object.pressedBy)}可触发，按住生效) -> 桥 ${bridges.join(", ") || "无"} / 移动平台 ${platforms.join(", ") || "无"}`);
    }
    if (object.type === "bridge") {
      lines.push(`- 按钮桥 ${object.id}: 控制按钮 ${controllingButtonId(object, byId) || "未绑定"}；被按钮压住时为实，按钮松开时为虚`);
    }
    if (object.type === "movingPlatform") {
      const target = movingPlatformTarget(object);
      if (object.motionMode === "auto") {
        lines.push(`- 自动移动平台 ${object.id}: 在 (${object.x}, ${object.y}) 和 (${target.x}, ${target.y}) 之间自动往返，单程 ${Math.max(0.5, number(object.periodSec, 3))}s`);
      } else {
        lines.push(`- 按钮移动平台 ${object.id}: 控制按钮 ${controllingButtonId(object, byId) || "未绑定"}；按住向 (${target.x}, ${target.y}) 平移，松开回 (${object.x}, ${object.y})，移动时间 ${Math.max(0.5, number(object.periodSec, 3))}s`);
      }
    }
    if (object.type === "spike") {
      lines.push(`- 尖刺 ${object.id}: 影响 ${actorLabel(object.affects)}，分身通常应免疫`);
    }
    if (object.type === "laser") {
      const beam = laserBeamRect(object, editorLaserBlockers(object));
      const attached = object.attachedTo ? `；吸附在 ${object.attachedTo}` : "";
      lines.push(`- 激光 ${object.id}: 影响 ${actorLabel(object.affects)}；方向 ${normalizeRotation(object.rotation)}°，射程 ${object.w}x${object.h}，当前被阻挡后光束 ${Math.round(beam.w)}x${Math.round(beam.h)}${attached}`);
    }
  }
  lines.push("");
  lines.push("## 对象列表");
  for (const object of data.objects) {
    const links = object.links.length ? ` links=${object.links.join("|")}` : "";
    const channel = object.channel ? ` channel=${object.channel}` : "";
    lines.push(`- ${object.id} [${objectTypes[object.type].label}] ${object.label} x=${object.x} y=${object.y} w=${object.w} h=${object.h}${channel}${links}`);
    if (object.notes) lines.push(`  备注：${object.notes}`);
  }
  lines.push("");
  lines.push("## JSON");
  lines.push("```json");
  lines.push(JSON.stringify(data, null, 2));
  lines.push("```");
  return lines.join("\n");
}

function controlledBridgeIds(button, byId) {
  const ids = new Set(button.links.filter((id) => byId.get(id)?.type === "bridge"));
  for (const object of byId.values()) {
    if (object.type === "bridge" && object.requiredButton === button.id) ids.add(object.id);
  }
  return [...ids];
}

function controlledMovingPlatformIds(button, byId) {
  const ids = new Set(button.links.filter((id) => byId.get(id)?.type === "movingPlatform" && byId.get(id)?.motionMode !== "auto"));
  for (const object of byId.values()) {
    if (object.type === "movingPlatform" && object.motionMode !== "auto" && object.requiredButton === button.id) ids.add(object.id);
  }
  return [...ids];
}

function controllingButtonId(bridge, byId) {
  if (bridge.requiredButton && byId.get(bridge.requiredButton)?.type === "button") return bridge.requiredButton;
  return incomingId(byId, "button", bridge.id);
}

function modeLabel(mode) {
  return { hold: "按住", toggle: "切换", press: "踩一下" }[mode] || mode || "按住";
}

function stateLabel(state) {
  return state === "phantom" ? "虚" : "实";
}

function actorLabel(actor) {
  return { player: "玩家", ghost: "分身", both: "玩家或分身" }[actor] || actor || "玩家或分身";
}

async function copyBrief() {
  const brief = makeBrief();
  ui.jsonBox.value = brief;
  ui.jsonBox.focus();
  ui.jsonBox.select();
  try {
    await navigator.clipboard.writeText(brief);
    setStatus("同步稿已复制，可以直接粘给 Codex。");
  } catch {
    setStatus("同步稿已生成在右下文本框，浏览器不允许自动复制。");
  }
}

async function copyJson() {
  writeJson();
  ui.jsonBox.focus();
  ui.jsonBox.select();
  try {
    await navigator.clipboard.writeText(ui.jsonBox.value);
    setStatus("JSON 已复制。");
  } catch {
    setStatus("JSON 已选中，浏览器不允许自动复制。");
  }
}

function draw() {
  ctx.clearRect(0, 0, VIEW_W, VIEW_H);
  drawBackground();
  drawGrid();
  drawWorldBounds();
  drawLinks();
  for (const object of map.objects) drawObject(object);
  drawSelection();
  drawHud();
}

function drawBackground() {
  const gradient = ctx.createLinearGradient(0, 0, 0, VIEW_H);
  gradient.addColorStop(0, "#111925");
  gradient.addColorStop(1, "#0a0d12");
  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, VIEW_W, VIEW_H);
}

function drawGrid() {
  const grid = map.world.grid;
  ctx.save();
  ctx.strokeStyle = "rgba(255,255,255,0.055)";
  ctx.lineWidth = 1;
  const startX = Math.floor(camera.x / grid) * grid;
  const startY = Math.floor(camera.y / grid) * grid;
  for (let x = startX; x <= camera.x + VIEW_W; x += grid) {
    const p = worldToScreen(x, 0);
    ctx.beginPath();
    ctx.moveTo(Math.round(p.x), 0);
    ctx.lineTo(Math.round(p.x), VIEW_H);
    ctx.stroke();
  }
  for (let y = startY; y <= camera.y + VIEW_H; y += grid) {
    const p = worldToScreen(0, y);
    ctx.beginPath();
    ctx.moveTo(0, Math.round(p.y));
    ctx.lineTo(VIEW_W, Math.round(p.y));
    ctx.stroke();
  }
  ctx.restore();
}

function drawWorldBounds() {
  const p = worldToScreen(0, 0);
  ctx.strokeStyle = "#80a9ff";
  ctx.lineWidth = 2;
  ctx.strokeRect(p.x, p.y, map.world.w, map.world.h);
}

function drawLinks() {
  const byId = new Map(map.objects.map((object) => [object.id, object]));
  ctx.save();
  ctx.strokeStyle = "rgba(244,201,93,0.5)";
  ctx.fillStyle = "rgba(244,201,93,0.85)";
  ctx.lineWidth = 2;
  ctx.setLineDash([8, 8]);
  for (const object of map.objects) {
    const from = centerOf(object);
    const fromScreen = worldToScreen(from.x, from.y);
    for (const link of objectVisualLinks(object, byId)) {
      const target = byId.get(link);
      if (!target) continue;
      const to = centerOf(target);
      const toScreen = worldToScreen(to.x, to.y);
      ctx.beginPath();
      ctx.moveTo(fromScreen.x, fromScreen.y);
      ctx.lineTo(toScreen.x, toScreen.y);
      ctx.stroke();
      ctx.beginPath();
      ctx.arc(toScreen.x, toScreen.y, 4, 0, Math.PI * 2);
      ctx.fill();
    }
  }
  ctx.restore();
}

function objectVisualLinks(object, byId) {
  const links = new Set(object.links);
  if (object.type === "key") {
    for (const target of byId.values()) {
      if (target.type === "door" && target.requiredKey === object.id) links.add(target.id);
    }
  }
  if (object.type === "button") {
    for (const target of byId.values()) {
      if (target.type === "bridge" && target.requiredButton === object.id) links.add(target.id);
      if (target.type === "movingPlatform" && target.requiredButton === object.id) links.add(target.id);
    }
  }
  return [...links];
}

function drawObject(object) {
  const info = objectTypes[object.type];
  const rect = objectRect(object);
  const p = worldToScreen(rect.x, rect.y);
  ctx.save();
  if (object.type === "platform") drawPlatform(p, rect, info);
  else if (object.type === "movingPlatform") drawMovingPlatform(p, rect, info, object);
  else if (object.type === "spike") drawSpike(p, rect, info, object);
  else if (object.type === "laser") drawLaser(p, rect, info, object);
  else if (object.type === "key") drawKey(p, rect, info);
  else if (object.type === "door") drawDoor(p, rect, info, object);
  else if (object.type === "button") drawButton(p, rect, info, object);
  else if (object.type === "bridge") drawBridge(p, rect, info, object);
  else if (object.type === "spawn") drawSpawn(p, rect, info);
  else if (object.type === "goal") drawGoal(p, rect, info);
  else if (object.type === "anchorZone") drawAnchorZone(p, rect, info);
  else if (object.type === "note") drawNote(p, rect, info, object);
  if (object.type !== "note") drawLabel(`${object.id}  ${object.label}`, p.x, p.y - 8, info.color);
  ctx.restore();
}

function drawPlatform(p, rect, info) {
  ctx.fillStyle = `${info.color}77`;
  ctx.fillRect(p.x, p.y, rect.w, rect.h);
  ctx.fillStyle = "rgba(255,255,255,0.14)";
  ctx.fillRect(p.x, p.y, rect.w, 5);
  ctx.strokeStyle = info.color;
  ctx.lineWidth = 2;
  ctx.strokeRect(p.x, p.y, rect.w, rect.h);
}

function drawMovingPlatform(p, rect, info, object) {
  const target = movingPlatformTarget(object);
  const startCenter = centerOf(object);
  const targetCenter = { x: target.x + rect.w / 2, y: target.y + rect.h / 2 };
  const targetScreen = worldToScreen(target.x, target.y);
  const targetCenterScreen = worldToScreen(targetCenter.x, targetCenter.y);
  const startCenterScreen = worldToScreen(startCenter.x, startCenter.y);

  ctx.save();
  ctx.strokeStyle = "rgba(139,209,124,0.65)";
  ctx.setLineDash([10, 7]);
  ctx.beginPath();
  ctx.moveTo(startCenterScreen.x, startCenterScreen.y);
  ctx.lineTo(targetCenterScreen.x, targetCenterScreen.y);
  ctx.stroke();
  ctx.fillStyle = "rgba(139,209,124,0.12)";
  ctx.fillRect(targetScreen.x, targetScreen.y, rect.w, rect.h);
  ctx.strokeRect(targetScreen.x, targetScreen.y, rect.w, rect.h);
  ctx.fillStyle = "rgba(139,209,124,0.82)";
  ctx.fillRect(targetCenterScreen.x - 5, targetCenterScreen.y - 5, 10, 10);
  ctx.setLineDash([]);
  ctx.restore();

  ctx.fillStyle = "rgba(139,209,124,0.58)";
  ctx.fillRect(p.x, p.y, rect.w, rect.h);
  ctx.fillStyle = "rgba(255,255,255,0.18)";
  ctx.fillRect(p.x, p.y, rect.w, 5);
  ctx.strokeStyle = info.color;
  ctx.lineWidth = 2;
  ctx.strokeRect(p.x, p.y, rect.w, rect.h);
  const moveMode = object.motionMode === "auto" ? "自动" : "按住";
  drawMiniText(`${moveMode} ${Math.max(0.5, number(object.periodSec, 3))}s`, p.x + 6, p.y + rect.h - 7, info.color);
  if (object.motionMode !== "auto" && object.requiredButton) drawMiniText(object.requiredButton, p.x + 6, p.y - 3, info.color);
}

function movingPlatformTarget(object) {
  return {
    x: number(object.moveTargetX, object.x + DEFAULT_GRID * 4),
    y: number(object.moveTargetY, object.y)
  };
}

function laserBeamRect(laser, blockers = []) {
  const rotation = normalizeRotation(laser.rotation);
  const x = laser.x;
  const y = laser.y;
  const w = Math.max(1, laser.w);
  const h = Math.max(1, laser.h);
  const length = rotation === 90 || rotation === 270 ? h : w;
  let distance = length;
  for (const blocker of blockers) {
    const hit = laserBlockDistance({ x, y, w, h, rotation }, blocker);
    if (hit !== null) distance = Math.min(distance, hit);
  }
  distance = clampValue(distance, 0, length);
  if (rotation === 90) return { x, y, w, h: distance, rotation };
  if (rotation === 180) return { x: x + w - distance, y, w: distance, h, rotation };
  if (rotation === 270) return { x, y: y + h - distance, w, h: distance, rotation };
  return { x, y, w: distance, h, rotation };
}

function laserBlockDistance(laser, blocker) {
  if (!blocker || blocker.w <= 0 || blocker.h <= 0) return null;
  const bx1 = blocker.x;
  const bx2 = blocker.x + blocker.w;
  const by1 = blocker.y;
  const by2 = blocker.y + blocker.h;
  const lx1 = laser.x;
  const lx2 = laser.x + laser.w;
  const ly1 = laser.y;
  const ly2 = laser.y + laser.h;
  if (laser.rotation === 90) {
    if (bx2 <= lx1 || bx1 >= lx2 || by2 <= ly1 || by1 >= ly2) return null;
    return Math.max(0, by1 - ly1);
  }
  if (laser.rotation === 180) {
    if (by2 <= ly1 || by1 >= ly2 || bx2 <= lx1 || bx1 >= lx2) return null;
    return Math.max(0, lx2 - bx2);
  }
  if (laser.rotation === 270) {
    if (bx2 <= lx1 || bx1 >= lx2 || by2 <= ly1 || by1 >= ly2) return null;
    return Math.max(0, ly2 - by2);
  }
  if (by2 <= ly1 || by1 >= ly2 || bx2 <= lx1 || bx1 >= lx2) return null;
  return Math.max(0, bx1 - lx1);
}

function laserEmitterRect(laser) {
  const rotation = normalizeRotation(laser.rotation);
  const size = 32;
  if (rotation === 90) return { x: laser.x, y: laser.y, w: laser.w, h: Math.min(size, laser.h) };
  if (rotation === 180) return { x: laser.x + Math.max(0, laser.w - size), y: laser.y, w: Math.min(size, laser.w), h: laser.h };
  if (rotation === 270) return { x: laser.x, y: laser.y + Math.max(0, laser.h - size), w: laser.w, h: Math.min(size, laser.h) };
  return { x: laser.x, y: laser.y, w: Math.min(size, laser.w), h: laser.h };
}

function editorLaserBlockers(laser) {
  return map.objects
    .filter((object) => object.id !== laser.id && object.id !== laser.attachedTo && (object.type === "platform" || object.type === "movingPlatform" || (object.type === "bridge" && object.defaultState !== "phantom")))
    .map(objectRect);
}

function drawSpike(p, rect, info, object) {
  ctx.fillStyle = "rgba(255,99,109,0.16)";
  ctx.fillRect(p.x, p.y, rect.w, rect.h);
  drawSpikeTriangles(p.x, p.y, rect.w, rect.h, info.color, object.rotation);
  drawMiniText("伤" + actorLabel(object.affects), p.x + 4, p.y + rect.h - 7, info.color);
}

function drawLaser(p, rect, info, object) {
  const beam = laserBeamRect(object, editorLaserBlockers(object));
  const beamP = worldToScreen(beam.x, beam.y);
  const emitter = laserEmitterRect(object);
  const emitterP = worldToScreen(emitter.x, emitter.y);
  ctx.save();
  ctx.fillStyle = "rgba(255,79,154,0.08)";
  ctx.fillRect(p.x, p.y, rect.w, rect.h);
  ctx.fillStyle = "rgba(255,79,154,0.30)";
  ctx.fillRect(beamP.x, beamP.y, beam.w, beam.h);
  ctx.fillStyle = info.color;
  ctx.fillRect(emitterP.x, emitterP.y, emitter.w, emitter.h);
  ctx.strokeStyle = info.color;
  ctx.lineWidth = 2;
  ctx.strokeRect(p.x, p.y, rect.w, rect.h);
  ctx.restore();
  const attachText = object.attachedTo ? ` 附${object.attachedTo}` : "";
  drawMiniText("伤" + actorLabel(object.affects) + attachText, p.x + 4, p.y - 3, info.color);
}

function drawSpikeTriangles(x, y, w, h, color, rotation = 0) {
  const angle = normalizeRotation(rotation);
  ctx.fillStyle = color;
  if (angle === 90 || angle === 270) {
    const count = Math.max(1, Math.floor(h / 18));
    const step = h / count;
    for (let i = 0; i < count; i += 1) {
      const top = y + i * step;
      ctx.beginPath();
      if (angle === 90) {
        ctx.moveTo(x, top);
        ctx.lineTo(x + w, top + step / 2);
        ctx.lineTo(x, top + step);
      } else {
        ctx.moveTo(x + w, top);
        ctx.lineTo(x, top + step / 2);
        ctx.lineTo(x + w, top + step);
      }
      ctx.closePath();
      ctx.fill();
    }
    return;
  }
  const count = Math.max(1, Math.floor(w / 18));
  const step = w / count;
  for (let i = 0; i < count; i += 1) {
    const left = x + i * step;
    ctx.beginPath();
    if (angle === 180) {
      ctx.moveTo(left, y);
      ctx.lineTo(left + step / 2, y + h);
      ctx.lineTo(left + step, y);
    } else {
      ctx.moveTo(left, y + h);
      ctx.lineTo(left + step / 2, y);
      ctx.lineTo(left + step, y + h);
    }
    ctx.closePath();
    ctx.fill();
  }
}

function drawKey(p, rect, info) {
  ctx.strokeStyle = info.color;
  ctx.fillStyle = `${info.color}44`;
  ctx.lineWidth = 4;
  const cy = p.y + rect.h / 2;
  ctx.beginPath();
  ctx.arc(p.x + rect.w * 0.35, cy, Math.min(rect.w, rect.h) * 0.22, 0, Math.PI * 2);
  ctx.stroke();
  ctx.beginPath();
  ctx.moveTo(p.x + rect.w * 0.5, cy);
  ctx.lineTo(p.x + rect.w * 0.88, cy);
  ctx.moveTo(p.x + rect.w * 0.72, cy);
  ctx.lineTo(p.x + rect.w * 0.72, cy + rect.h * 0.18);
  ctx.moveTo(p.x + rect.w * 0.84, cy);
  ctx.lineTo(p.x + rect.w * 0.84, cy + rect.h * 0.14);
  ctx.stroke();
}

function drawDoor(p, rect, info, object) {
  ctx.fillStyle = "rgba(255,141,102,0.24)";
  ctx.fillRect(p.x, p.y, rect.w, rect.h);
  ctx.strokeStyle = info.color;
  ctx.lineWidth = 3;
  ctx.strokeRect(p.x + 3, p.y + 3, rect.w - 6, rect.h - 6);
  ctx.fillStyle = info.color;
  ctx.fillRect(p.x + rect.w * 0.56, p.y + rect.h * 0.5, 6, 6);
  if (object.requiredKey) drawMiniText(object.requiredKey, p.x + 5, p.y + rect.h - 8, info.color);
}

function drawButton(p, rect, info, object) {
  ctx.fillStyle = "rgba(244,201,93,0.18)";
  ctx.fillRect(p.x, p.y, rect.w, rect.h);
  ctx.fillStyle = info.color;
  ctx.fillRect(p.x + 7, p.y + Math.max(2, rect.h - 8), rect.w - 14, 6);
  ctx.strokeStyle = info.color;
  ctx.lineWidth = 2;
  ctx.strokeRect(p.x, p.y, rect.w, rect.h);
  drawMiniText(`按住 ${actorLabel(object.pressedBy)}`, p.x + 4, p.y - 3, info.color);
}

function drawBridge(p, rect, info, object) {
  const isSolid = object.defaultState !== "phantom";
  ctx.save();
  if (!isSolid) ctx.setLineDash([10, 8]);
  ctx.fillStyle = isSolid ? "rgba(113,215,232,0.38)" : "rgba(113,215,232,0.10)";
  ctx.strokeStyle = info.color;
  ctx.lineWidth = isSolid ? 3 : 2;
  ctx.fillRect(p.x, p.y, rect.w, rect.h);
  ctx.strokeRect(p.x, p.y, rect.w, rect.h);
  ctx.restore();
  drawMiniText(isSolid ? "静态实桥" : "按下实/松开虚", p.x + 6, p.y + rect.h - 7, info.color);
  if (object.requiredButton) drawMiniText(object.requiredButton, p.x + 6, p.y - 3, info.color);
}

function drawSpawn(p, rect, info) {
  ctx.fillStyle = info.color;
  roundRect(p.x, p.y, rect.w, rect.h, 8, true);
  ctx.fillStyle = "#102019";
  ctx.fillRect(p.x + rect.w * 0.63, p.y + 13, 4, 5);
}

function drawGoal(p, rect, info) {
  ctx.fillStyle = "rgba(244,201,93,0.24)";
  ctx.fillRect(p.x, p.y, rect.w, rect.h);
  ctx.strokeStyle = info.color;
  ctx.lineWidth = 3;
  ctx.strokeRect(p.x + 3, p.y + 3, rect.w - 6, rect.h - 6);
  ctx.fillStyle = info.color;
  ctx.beginPath();
  ctx.moveTo(p.x + rect.w / 2, p.y + 10);
  ctx.lineTo(p.x + rect.w - 10, p.y + rect.h / 2);
  ctx.lineTo(p.x + rect.w / 2, p.y + rect.h - 10);
  ctx.lineTo(p.x + 10, p.y + rect.h / 2);
  ctx.closePath();
  ctx.fill();
}

function drawAnchorZone(p, rect, info) {
  ctx.save();
  ctx.fillStyle = "rgba(180,156,255,0.08)";
  ctx.strokeStyle = info.color;
  ctx.setLineDash([10, 8]);
  ctx.fillRect(p.x, p.y, rect.w, rect.h);
  ctx.strokeRect(p.x, p.y, rect.w, rect.h);
  ctx.setLineDash([]);
  ctx.beginPath();
  ctx.arc(p.x + rect.w / 2, p.y + rect.h / 2, Math.min(rect.w, rect.h) * 0.28, 0, Math.PI * 2);
  ctx.stroke();
  ctx.restore();
}

function drawNote(p, rect, info, object) {
  ctx.fillStyle = "rgba(10,12,16,0.78)";
  roundRect(p.x, p.y, rect.w, rect.h, 6, true);
  ctx.strokeStyle = "rgba(246,237,221,0.28)";
  roundRect(p.x, p.y, rect.w, rect.h, 6, false);
  ctx.fillStyle = info.color;
  wrapText(object.notes || object.label, p.x + 8, p.y + 18, rect.w - 16, 17);
}

function drawSelection() {
  const object = selectedObject();
  if (!object) return;
  const rect = objectRect(object);
  const p = worldToScreen(rect.x, rect.y);
  ctx.save();
  ctx.strokeStyle = "#f4c95d";
  ctx.lineWidth = 2;
  ctx.setLineDash([8, 6]);
  ctx.strokeRect(p.x - 4, p.y - 4, rect.w + 8, rect.h + 8);
  ctx.setLineDash([]);
  ctx.fillStyle = "#f4c95d";
  ctx.fillRect(p.x + rect.w - 5, p.y + rect.h - 5, 10, 10);
  if (object.type === "movingPlatform") {
    const target = movingPlatformTargetRect(object);
    const handle = movingPlatformTargetHandleRect(object);
    const tp = worldToScreen(target.x, target.y);
    const hp = worldToScreen(handle.x, handle.y);
    ctx.fillStyle = "rgba(139,209,124,0.10)";
    ctx.fillRect(tp.x - 4, tp.y - 4, target.w + 8, target.h + 8);
    ctx.strokeStyle = "#8bd17c";
    ctx.setLineDash([5, 5]);
    ctx.strokeRect(tp.x - 4, tp.y - 4, target.w + 8, target.h + 8);
    ctx.setLineDash([]);
    ctx.fillStyle = "#8bd17c";
    ctx.fillRect(hp.x, hp.y, handle.w, handle.h);
  }
  ctx.restore();
}

function drawHud() {
  ctx.fillStyle = "rgba(7,8,11,0.78)";
  ctx.fillRect(14, VIEW_H - 46, 540, 32);
  ctx.fillStyle = "#aeb6bd";
  ctx.font = "13px Microsoft YaHei, Segoe UI, sans-serif";
  ctx.fillText(`镜头 x=${Math.round(camera.x)} y=${Math.round(camera.y)}  网格=${map.world.grid}  对象=${map.objects.length}`, 28, VIEW_H - 25);
}

function drawLabel(text, x, y, color) {
  ctx.font = "12px Microsoft YaHei, Segoe UI, sans-serif";
  const w = Math.max(46, ctx.measureText(text).width + 8);
  ctx.fillStyle = "rgba(0,0,0,0.68)";
  ctx.fillRect(x - 4, y - 13, w, 18);
  ctx.fillStyle = color;
  ctx.fillText(text, x, y);
}

function drawMiniText(text, x, y, color) {
  ctx.font = "11px Microsoft YaHei, Segoe UI, sans-serif";
  ctx.fillStyle = "rgba(0,0,0,0.62)";
  ctx.fillRect(x - 3, y - 12, ctx.measureText(text).width + 6, 16);
  ctx.fillStyle = color;
  ctx.fillText(text, x, y);
}

function wrapText(text, x, y, maxWidth, lineHeight) {
  ctx.font = "13px Microsoft YaHei, Segoe UI, sans-serif";
  const chars = String(text).split("");
  let line = "";
  for (const ch of chars) {
    const test = line + ch;
    if (ctx.measureText(test).width > maxWidth && line) {
      ctx.fillText(line, x, y);
      line = ch;
      y += lineHeight;
    } else {
      line = test;
    }
  }
  if (line) ctx.fillText(line, x, y);
}

function roundRect(x, y, w, h, r, fill) {
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.lineTo(x + w - r, y);
  ctx.quadraticCurveTo(x + w, y, x + w, y + r);
  ctx.lineTo(x + w, y + h - r);
  ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
  ctx.lineTo(x + r, y + h);
  ctx.quadraticCurveTo(x, y + h, x, y + h - r);
  ctx.lineTo(x, y + r);
  ctx.quadraticCurveTo(x, y, x + r, y);
  if (fill) ctx.fill();
  else ctx.stroke();
}

function startPlaytest() {
  const source = exportMap();
  playtest = makePlaytest(source);
  playKeys.clear();
  playLastTime = performance.now();
  if (ui.playBtn) ui.playBtn.hidden = true;
  if (ui.stopPlayBtn) ui.stopPlayBtn.hidden = false;
  document.body.classList.add("play-mode");
  canvas.focus();
  setStatus("试玩中：A/D 移动，Space 跳，E 开锚点，R 重开，Esc 退出试玩。");
  cancelAnimationFrame(playFrame);
  playFrame = requestAnimationFrame(playLoop);
}

function stopPlaytest() {
  cancelAnimationFrame(playFrame);
  playFrame = 0;
  playtest = null;
  playKeys.clear();
  if (ui.playBtn) ui.playBtn.hidden = false;
  if (ui.stopPlayBtn) ui.stopPlayBtn.hidden = true;
  document.body.classList.remove("play-mode");
  setStatus("已退出试玩。");
  draw();
}

function restartPlaytest() {
  if (!playtest) return;
  const source = playtest.source;
  playtest = makePlaytest(source);
  playLastTime = performance.now();
  setStatus("试玩已重开。");
}

function makePlaytest(source) {
  const data = normalizeMap(source);
  const runtime = {
    source: data,
    world: data.world,
    rules: data.rules,
    platforms: data.objects.filter((object) => object.type === "platform").map(runtimeObject),
    movingPlatforms: data.objects.filter((object) => object.type === "movingPlatform").map((object) => ({
      ...runtimeObject(object),
      homeX: object.x,
      homeY: object.y,
      dx: 0,
      dy: 0,
      progress: 0
    })),
    spikes: data.objects.filter((object) => object.type === "spike").map(runtimeObject),
    lasers: data.objects.filter((object) => object.type === "laser").map((object) => ({
      ...runtimeObject(object),
      homeX: object.x,
      homeY: object.y,
      attachOffsetX: 0,
      attachOffsetY: 0,
      attachedPlatform: null
    })),
    keys: data.objects.filter((object) => object.type === "key").map((object) => ({
      ...runtimeObject(object),
      homeX: object.x,
      homeY: object.y,
      carried: false,
      used: false
    })),
    doors: data.objects.filter((object) => object.type === "door").map((object) => ({ ...runtimeObject(object), open: false })),
    buttons: data.objects.filter((object) => object.type === "button").map((object) => ({
      ...runtimeObject(object),
      active: false,
      latched: false,
      pressedLast: false,
      timerUntil: 0
    })),
    bridges: data.objects.filter((object) => object.type === "bridge").map(runtimeObject),
    goals: data.objects.filter((object) => object.type === "goal").map(runtimeObject),
    anchorZones: data.objects.filter((object) => object.type === "anchorZone").map(runtimeObject),
    camera: { x: 0, y: 0 },
    startTime: performance.now(),
    anchor: null,
    respawnAnchor: null,
    recording: null,
    ghostRecord: null,
    ghost: null,
    message: "E 开启时空锚点，倒计时内死在刺上会生成实体分身。",
    messageUntil: performance.now() + 3600,
    completed: false,
    carriedKeyId: null,
    jumpPressedAt: -99999
  };
  const spawn = data.objects.find((object) => object.type === "spawn") || { x: 80, y: 430 };
  runtime.player = makePlayActor(spawn.x, spawn.y);
  hydrateRuntimeRelations(runtime);
  updatePlayCamera(runtime);
  return runtime;
}

function hydrateRuntimeRelations(runtime) {
  const keys = new Map(runtime.keys.map((object) => [object.id, object]));
  const buttons = new Map(runtime.buttons.map((object) => [object.id, object]));
  for (const door of runtime.doors) {
    if (!door.requiredKey) door.requiredKey = runtime.keys.find((key) => key.links.includes(door.id))?.id || "";
    door.keyRef = keys.get(door.requiredKey) || null;
  }
  for (const bridge of runtime.bridges) {
    bridge.controllers = runtime.buttons.filter((button) => button.links.includes(bridge.id) || bridge.requiredButton === button.id);
    if (!bridge.controllers.length && bridge.channel) {
      bridge.controllers = runtime.buttons.filter((button) => button.channel && button.channel === bridge.channel);
    }
  }
  for (const button of runtime.buttons) {
    button.bridges = runtime.bridges.filter((bridge) => button.links.includes(bridge.id) || bridge.requiredButton === button.id);
    if (!button.bridges.length && button.channel) {
      button.bridges = runtime.bridges.filter((bridge) => bridge.channel === button.channel);
    }
    button.movingPlatforms = runtime.movingPlatforms.filter((platform) => platform.motionMode !== "auto" && (button.links.includes(platform.id) || platform.requiredButton === button.id));
    if (!button.movingPlatforms.length && button.channel) {
      button.movingPlatforms = runtime.movingPlatforms.filter((platform) => platform.motionMode !== "auto" && platform.channel === button.channel);
    }
    button.controllerRef = buttons.get(button.id);
  }
  for (const platform of runtime.movingPlatforms) {
    platform.controllers = platform.motionMode === "auto"
      ? []
      : runtime.buttons.filter((button) => button.links.includes(platform.id) || platform.requiredButton === button.id);
    if (platform.motionMode !== "auto" && !platform.controllers.length && platform.channel) {
      platform.controllers = runtime.buttons.filter((button) => button.channel && button.channel === platform.channel);
    }
    platform.direction = 1;
  }
  const movingPlatforms = new Map(runtime.movingPlatforms.map((platform) => [platform.id, platform]));
  for (const laser of runtime.lasers) {
    if (!laser.attachedTo) continue;
    const platform = movingPlatforms.get(laser.attachedTo);
    if (!platform) continue;
    laser.attachedPlatform = platform;
    laser.attachOffsetX = laser.x - platform.x;
    laser.attachOffsetY = laser.y - platform.y;
  }
}

function makePlayActor(x, y) {
  return {
    x,
    y,
    vx: 0,
    vy: 0,
    face: 1,
    grounded: false,
    onGhost: false,
    onMovingPlatformId: null,
    touchingWall: 0,
    wallSliding: false,
    lastGroundedAt: -99999
  };
}

function playLoop(now) {
  if (!playtest) return;
  try {
    const dt = Math.min(0.033, (now - playLastTime) / 1000);
    playLastTime = now;
    updatePlaytest(dt, now);
    drawPlaytest(now);
    playFrame = requestAnimationFrame(playLoop);
  } catch (error) {
    const text = error?.message || String(error);
    stopPlaytest();
    setStatus(`试玩出错：${text}`);
    console.error(error);
  }
}

function updatePlaytest(dt, now) {
  updatePlayGhost(now);
  updatePlayButtons(now);
  updatePlayMovingPlatforms(dt);
  updatePlayAttachedLasers();
  if (!playtest.completed) updatePlayPlayer(dt, now);
  if (playtest.recording && now >= playtest.recording.expiresAt) expirePlayRecording();
  if (playtest.recording) samplePlayRecording(now);
  updatePlayButtons(now);
  updatePlayCamera(playtest);
}

function updatePlayPlayer(dt, now) {
  carryPlayerOnPlayGhost();
  carryPlayerOnMovingPlatform();
  const player = playtest.player;
  const left = playKeys.has("a") || playKeys.has("arrowleft");
  const right = playKeys.has("d") || playKeys.has("arrowright");
  player.touchingWall = 0;
  player.wallSliding = false;
  const targetVx = (right ? PLAY_MOVE_SPEED : 0) - (left ? PLAY_MOVE_SPEED : 0);
  if (PLAY_INSTANT_HORIZONTAL) {
    player.vx = targetVx;
  } else if (targetVx === 0) {
    player.vx = approachValue(player.vx, 0, PLAY_GROUND_FRICTION * dt);
  } else {
    player.vx = approachValue(player.vx, targetVx, (player.grounded ? PLAY_GROUND_ACCEL : PLAY_AIR_ACCEL) * dt);
  }
  if (player.vx !== 0) player.face = Math.sign(player.vx);
  tryPlayBufferedJump(now);

  const gravityScale = player.vy > 0 ? PLAY_FALL_GRAVITY_MULT : 1;
  player.vy = Math.min(PLAY_MAX_FALL_SPEED, player.vy + PLAY_GRAVITY * gravityScale * dt);
  player.onGhost = false;
  player.onMovingPlatformId = null;
  playMoveAndCollide(player, player.vx * dt, 0);
  player.touchingWall = playWallContactDirection(player);
  applyPlayWallSlide(player, left, right);
  playMoveAndCollide(player, 0, player.vy * dt);
  if (player.grounded) player.lastGroundedAt = now;
  tryPlayBufferedJump(now);

  collectPlayKeys();
  updateCarriedKeyPosition();
  unlockTouchedDoors();
  if (player.y > playtest.world.h + 220) {
    playDie();
    return;
  }
  for (const spike of playtest.spikes) {
    if ((spike.affects === "player" || spike.affects === "both") && rectsOverlap(playActorRect(player), spike)) {
      playDie();
      return;
    }
  }
  for (const laser of playtest.lasers) {
    if ((laser.affects === "player" || laser.affects === "both") && rectsOverlap(playActorRect(player), playLaserBeamRect(laser))) {
      playDie();
      return;
    }
  }
  for (const goal of playtest.goals) {
    if (rectsOverlap(playActorRect(player), goal)) {
      playtest.completed = true;
      playSay("通关。这个地图至少能被当前试玩规则走到出口。", 8000);
      return;
    }
  }
}

function updatePlayMovingPlatforms(dt) {
  for (const platform of playtest.movingPlatforms) {
    const target = movingPlatformTarget(platform);
    const duration = Math.max(0.5, number(platform.periodSec, 3));
    const step = dt / duration;
    if (platform.motionMode === "auto") {
      const direction = platform.direction || 1;
      platform.progress = clampValue((platform.progress || 0) + step * direction, 0, 1);
      if (platform.progress >= 1) platform.direction = -1;
      if (platform.progress <= 0) platform.direction = 1;
    } else {
      const active = movingPlatformActive(platform);
      platform.progress = clampValue((platform.progress || 0) + (active ? step : -step), 0, 1);
    }
    const nextX = lerpValue(platform.homeX, target.x, platform.progress);
    const nextY = lerpValue(platform.homeY, target.y, platform.progress);
    platform.dx = nextX - platform.x;
    platform.dy = nextY - platform.y;
    platform.x = nextX;
    platform.y = nextY;
  }
}

function movingPlatformActive(platform) {
  if (platform.motionMode === "auto") return true;
  const controllers = platform.controllers || [];
  return controllers.length ? controllers.some((button) => button.active) : false;
}

function updatePlayAttachedLasers() {
  for (const laser of playtest.lasers) {
    const platform = laser.attachedPlatform;
    if (!platform) continue;
    laser.x = platform.x + laser.attachOffsetX;
    laser.y = platform.y + laser.attachOffsetY;
  }
}

function applyPlayWallSlide(player, left, right) {
  if (!playtest.rules.playerWallSlide) return;
  if (player.grounded || player.vy <= 0 || player.touchingWall === 0) return;
  const pushingIntoWall = (player.touchingWall < 0 && left) || (player.touchingWall > 0 && right);
  if (!pushingIntoWall) return;
  player.vy = Math.min(player.vy, playtest.rules.wallSlideMaxSpeed || PLAY_WALL_SLIDE_SPEED);
  player.wallSliding = true;
}

function playWallContactDirection(actor) {
  const inset = 4;
  const probeW = 3;
  const leftProbe = { x: actor.x - probeW, y: actor.y + inset, w: probeW, h: PLAYER_H - inset * 2 };
  const rightProbe = { x: actor.x + PLAYER_W, y: actor.y + inset, w: probeW, h: PLAYER_H - inset * 2 };
  const walls = playWallRects();
  if (walls.some((solid) => rectsOverlap(leftProbe, solid))) return -1;
  if (walls.some((solid) => rectsOverlap(rightProbe, solid))) return 1;
  return 0;
}

function playJump() {
  if (!playtest || playtest.completed) return;
  playtest.jumpPressedAt = performance.now();
  tryPlayBufferedJump(playtest.jumpPressedAt);
}

function tryPlayBufferedJump(now) {
  const player = playtest.player;
  if (now - playtest.jumpPressedAt > PLAY_JUMP_BUFFER_MS) return;
  if (!player.grounded && now - player.lastGroundedAt > PLAY_COYOTE_MS) return;
  player.vy = -PLAY_JUMP_SPEED;
  player.grounded = false;
  player.onGhost = false;
  playtest.jumpPressedAt = -99999;
}

function cutPlayJump() {
  if (!playtest) return;
  if (playtest.player.vy < 0) playtest.player.vy *= PLAY_JUMP_CUT_MULT;
}

function placePlayAnchor() {
  if (!playtest || playtest.completed) return;
  const player = playtest.player;
  const now = performance.now();
  playtest.anchor = { x: player.x + PLAYER_W / 2, y: player.y + PLAYER_H };
  playtest.recording = {
    start: now,
    expiresAt: now + playtest.rules.recordWindowSec * 1000,
    lastSample: 0,
    frames: []
  };
  playtest.jumpPressedAt = -99999;
  samplePlayRecording(now);
  playSay("锚点已开启：倒计时内死亡才会录成新分身。", 2600);
}

function samplePlayRecording(now) {
  const recording = playtest.recording;
  const anchor = playtest.anchor;
  if (!recording || !anchor) return;
  if (now - recording.lastSample < PLAY_SAMPLE_MS) return;
  recording.lastSample = now;
  const player = playtest.player;
  const footX = player.x + PLAYER_W / 2;
  const footY = player.y + PLAYER_H;
  recording.frames.push({
    t: now - recording.start,
    dx: footX - anchor.x,
    dy: footY - anchor.y,
    face: player.face
  });
}

function expirePlayRecording() {
  playtest.anchor = null;
  playtest.recording = null;
  playSay("锚点超时消失。按 E 可以重新开启。", 2200);
}

function playDie() {
  const now = performance.now();
  resetCarriedKeyToHome();
  const canRecord = playtest.recording && playtest.anchor && now <= playtest.recording.expiresAt && playtest.recording.frames.length >= 2;
  if (canRecord) {
    samplePlayRecording(now);
    const anchor = { ...playtest.anchor };
    const frames = playtest.recording.frames.map((frame) => ({ ...frame }));
    const duration = Math.max(500, frames[frames.length - 1].t);
    playtest.ghostRecord = { duration, frames };
    playtest.ghost = {
      start: now,
      anchor,
      x: anchor.x - PLAYER_W / 2,
      y: anchor.y - PLAYER_H,
      face: 1,
      localT: 0,
      dx: 0,
      dy: 0,
      looped: false
    };
    playtest.respawnAnchor = anchor;
    playtest.anchor = null;
    playtest.recording = null;
    playtest.player = makePlayActor(anchor.x - PLAYER_W / 2, anchor.y - PLAYER_H);
    playSay("分身已固定。它有实体、可压按钮、不会被刺杀死。", 3200);
    return;
  }

  if (playtest.recording && now > playtest.recording.expiresAt) expirePlayRecording();
  const target = playtest.respawnAnchor || playSpawnFoot();
  playtest.player = makePlayActor(target.x - PLAYER_W / 2, target.y - PLAYER_H);
  playSay(playtest.respawnAnchor ? "死亡回到锚点，已录好的分身保持不变。" : "没有有效锚点，回到出生点。", 2200);
}

function playSpawnFoot() {
  const spawn = playtest.source.objects.find((object) => object.type === "spawn") || { x: 80, y: 430 };
  return { x: spawn.x + PLAYER_W / 2, y: spawn.y + PLAYER_H };
}

function updatePlayGhost(now) {
  const ghost = playtest.ghost;
  const record = playtest.ghostRecord;
  if (!ghost || !record) return;
  const t = (now - ghost.start) % record.duration;
  const frame = samplePlayGhostRecord(t);
  const nextX = ghost.anchor.x + frame.dx - PLAYER_W / 2;
  const nextY = ghost.anchor.y + frame.dy - PLAYER_H;
  ghost.looped = t < ghost.localT;
  ghost.dx = ghost.looped ? 0 : nextX - ghost.x;
  ghost.dy = ghost.looped ? 0 : nextY - ghost.y;
  ghost.localT = t;
  ghost.x = nextX;
  ghost.y = nextY;
  ghost.face = frame.face || 1;
  if (playtest.rules.ghostCanStandOnPlayer) placeGhostOnPlayerIfNeeded();
}

function samplePlayGhostRecord(t) {
  const frames = playtest.ghostRecord.frames;
  if (t <= frames[0].t) return frames[0];
  for (let i = 1; i < frames.length; i += 1) {
    const a = frames[i - 1];
    const b = frames[i];
    if (t <= b.t) {
      const k = (t - a.t) / Math.max(1, b.t - a.t);
      return {
        dx: lerpValue(a.dx, b.dx, k),
        dy: lerpValue(a.dy, b.dy, k),
        face: k < 0.5 ? a.face : b.face
      };
    }
  }
  return frames[frames.length - 1];
}

function placeGhostOnPlayerIfNeeded() {
  const ghost = playtest.ghost;
  const player = playtest.player;
  if (!ghost || !player) return;
  const gr = playGhostRect();
  const pr = playActorRect(player);
  if (!rectsOverlap(gr, pr)) return;
  if (ghost.dy >= 0 && gr.y + gr.h <= pr.y + 18) {
    ghost.y = pr.y - PLAYER_H;
    ghost.dy = 0;
  }
}

function updatePlayButtons(now) {
  for (const button of playtest.buttons) {
    const pressed = playButtonPressed(button);
    button.active = pressed;
    button.pressedLast = pressed;
  }
}

function playButtonPressed(button) {
  const actorRects = [];
  if (button.pressedBy === "player" || button.pressedBy === "both") actorRects.push(playActorRect(playtest.player));
  if ((button.pressedBy === "ghost" || button.pressedBy === "both") && playtest.rules.ghostCanPressButtons && playtest.ghost) {
    actorRects.push(playGhostRect());
  }
  return actorRects.some((rect) => rectsOverlap(rect, button));
}

function collectPlayKeys() {
  const pr = playActorRect(playtest.player);
  if (playtest.carriedKeyId) return;
  for (const key of playtest.keys) {
    if (!key.used && !key.carried && rectsOverlap(pr, key)) {
      key.carried = true;
      playtest.carriedKeyId = key.id;
      updateCarriedKeyPosition();
      playSay(`带上 ${key.label || key.id}，去碰对应的门。`, 1800);
      return;
    }
  }
}

function updateCarriedKeyPosition() {
  const key = carriedPlayKey();
  if (!key) return;
  const player = playtest.player;
  key.x = player.x + PLAYER_W / 2 - key.w / 2;
  key.y = player.y - key.h - 8;
}

function carriedPlayKey() {
  if (!playtest.carriedKeyId) return null;
  return playtest.keys.find((key) => key.id === playtest.carriedKeyId && key.carried && !key.used) || null;
}

function unlockTouchedDoors() {
  const key = carriedPlayKey();
  if (!key) return;
  const touchRect = expandedRect(playActorRect(playtest.player), 4);
  for (const door of playtest.doors) {
    if (door.open || !keyUnlocksDoor(key, door)) continue;
    if (!rectsOverlap(touchRect, door)) continue;
    door.open = true;
    key.used = true;
    key.carried = false;
    playtest.carriedKeyId = null;
    playSay(`${door.label || door.id} 已开锁，门消失了。`, 2200);
    return;
  }
}

function keyUnlocksDoor(key, door) {
  if (door.requiredKey) return door.requiredKey === key.id;
  if (key.links.includes(door.id)) return true;
  return key.channel && door.channel && key.channel === door.channel;
}

function resetCarriedKeyToHome() {
  const key = carriedPlayKey();
  if (!key) return;
  key.carried = false;
  key.x = key.homeX;
  key.y = key.homeY;
  playtest.carriedKeyId = null;
}

function playMoveAndCollide(actor, dx, dy) {
  if (dx === 0 && dy === 0) return;
  actor.x += dx;
  actor.y += dy;
  if (dy !== 0) actor.grounded = false;

  const ar = playActorRect(actor);
  for (const solid of playSolidRects()) {
    if (!rectsOverlap(ar, solid)) continue;
    if (dx > 0) {
      actor.x = solid.x - PLAYER_W;
      actor.vx = 0;
      actor.touchingWall = 1;
    }
    if (dx < 0) {
      actor.x = solid.x + solid.w;
      actor.vx = 0;
      actor.touchingWall = -1;
    }
    if (dy > 0) {
      actor.y = solid.y - PLAYER_H;
      actor.vy = 0;
      actor.grounded = true;
      actor.onGhost = !!solid.isGhost;
      actor.onMovingPlatformId = solid.isMovingPlatform ? solid.id : null;
    }
    if (dy < 0) {
      actor.y = solid.y + solid.h;
      actor.vy = 0;
    }
    ar.x = actor.x;
    ar.y = actor.y;
  }
  actor.x = clampValue(actor.x, 0, Math.max(0, playtest.world.w - PLAYER_W));
}

function playSolidRects() {
  const solids = [
    ...playtest.platforms,
    ...playtest.movingPlatforms.map((platform) => ({ ...platform, isMovingPlatform: true }))
  ];
  for (const bridge of playtest.bridges) {
    if (playBridgeSolid(bridge)) solids.push(bridge);
  }
  for (const door of playtest.doors) {
    door.open = playDoorOpen(door);
    if (!door.open) solids.push(door);
  }
  if (playtest.rules.ghostSolid && playtest.ghost && playtest.ghost.localT >= 220) {
    solids.push({ ...playGhostRect(), isGhost: true });
  }
  return solids;
}

function playWallRects() {
  const walls = [...playtest.platforms, ...playtest.movingPlatforms];
  for (const bridge of playtest.bridges) {
    if (playBridgeSolid(bridge)) walls.push(bridge);
  }
  for (const door of playtest.doors) {
    if (!door.open) walls.push(door);
  }
  return walls;
}

function playDoorOpen(door) {
  return !!door.open;
}

function playBridgeSolid(bridge) {
  const hasController = (bridge.controllers?.length || 0) > 0;
  if (hasController) return bridge.controllers.some((button) => button.active);
  const active = false;
  const state = active ? bridge.activeState : bridge.defaultState;
  return state !== "phantom";
}

function playLaserBlockers(laser) {
  const blockers = [
    ...playtest.platforms,
    ...playtest.movingPlatforms.filter((platform) => platform.id !== laser?.attachedTo)
  ];
  for (const bridge of playtest.bridges) {
    if (playBridgeSolid(bridge)) blockers.push(bridge);
  }
  return blockers;
}

function playLaserBeamRect(laser) {
  return laserBeamRect(laser, playLaserBlockers(laser));
}

function carryPlayerOnPlayGhost() {
  const player = playtest.player;
  const ghost = playtest.ghost;
  if (!ghost || !player.onGhost || ghost.looped) return;
  player.x = clampValue(player.x + ghost.dx, 0, Math.max(0, playtest.world.w - PLAYER_W));
  player.y += ghost.dy;
}

function carryPlayerOnMovingPlatform() {
  const player = playtest.player;
  if (!player.onMovingPlatformId) return;
  const platform = playtest.movingPlatforms.find((item) => item.id === player.onMovingPlatformId);
  if (!platform) return;
  player.x = clampValue(player.x + platform.dx, 0, Math.max(0, playtest.world.w - PLAYER_W));
  player.y += platform.dy;
}

function playActorRect(actor) {
  return { x: actor.x, y: actor.y, w: PLAYER_W, h: PLAYER_H };
}

function playGhostRect() {
  return { x: playtest.ghost.x, y: playtest.ghost.y, w: PLAYER_W, h: PLAYER_H };
}

function rectsOverlap(a, b) {
  return a.x < b.x + b.w && a.x + a.w > b.x && a.y < b.y + b.h && a.y + a.h > b.y;
}

function expandedRect(rect, amount) {
  return {
    x: rect.x - amount,
    y: rect.y - amount,
    w: rect.w + amount * 2,
    h: rect.h + amount * 2
  };
}

function updatePlayCamera(runtime) {
  const player = runtime.player;
  runtime.camera.x = clampValue(player.x + PLAYER_W / 2 - VIEW_W * 0.42, 0, Math.max(0, runtime.world.w - VIEW_W));
  runtime.camera.y = clampValue(player.y + PLAYER_H / 2 - VIEW_H * 0.55, 0, Math.max(0, runtime.world.h - VIEW_H));
}

function playWorldToScreen(x, y) {
  return { x: x - playtest.camera.x, y: y - playtest.camera.y };
}

function drawPlaytest(now) {
  ctx.clearRect(0, 0, VIEW_W, VIEW_H);
  drawPlayBackground();
  drawPlayWorldBounds();
  for (const zone of playtest.anchorZones) drawPlayAnchorZone(zone);
  for (const platform of playtest.platforms) drawPlayPlatform(platform);
  for (const platform of playtest.movingPlatforms) drawPlayMovingPlatform(platform);
  for (const bridge of playtest.bridges) drawPlayBridge(bridge);
  for (const door of playtest.doors) drawPlayDoor(door);
  for (const spike of playtest.spikes) drawPlaySpike(spike);
  for (const laser of playtest.lasers) drawPlayLaser(laser);
  for (const button of playtest.buttons) drawPlayButton(button);
  for (const key of playtest.keys) if (!key.used) drawPlayKey(key);
  for (const goal of playtest.goals) drawPlayGoal(goal);
  drawPlayAnchor(now);
  drawPlayGhost(now);
  drawPlayPlayer();
  drawPlayHud(now);
  if (playtest.completed) drawPlayComplete();
}

function drawPlayBackground() {
  const gradient = ctx.createLinearGradient(0, 0, 0, VIEW_H);
  gradient.addColorStop(0, "#101926");
  gradient.addColorStop(1, "#080b10");
  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, VIEW_W, VIEW_H);
  ctx.strokeStyle = "rgba(255,255,255,0.045)";
  const grid = playtest.world.grid || 20;
  const startX = Math.floor(playtest.camera.x / grid) * grid;
  const startY = Math.floor(playtest.camera.y / grid) * grid;
  let verticalLines = 0;
  for (let x = startX; x <= playtest.camera.x + VIEW_W && verticalLines < 360; x += grid, verticalLines += 1) {
    const p = playWorldToScreen(x, 0);
    ctx.beginPath();
    ctx.moveTo(Math.round(p.x), 0);
    ctx.lineTo(Math.round(p.x), VIEW_H);
    ctx.stroke();
  }
  let horizontalLines = 0;
  for (let y = startY; y <= playtest.camera.y + VIEW_H && horizontalLines < 240; y += grid, horizontalLines += 1) {
    const p = playWorldToScreen(0, y);
    ctx.beginPath();
    ctx.moveTo(0, Math.round(p.y));
    ctx.lineTo(VIEW_W, Math.round(p.y));
    ctx.stroke();
  }
}

function drawPlayWorldBounds() {
  const p = playWorldToScreen(0, 0);
  ctx.strokeStyle = "rgba(128,169,255,0.8)";
  ctx.strokeRect(p.x, p.y, playtest.world.w, playtest.world.h);
}

function drawPlayPlatform(rect) {
  const p = playWorldToScreen(rect.x, rect.y);
  ctx.fillStyle = "rgba(102,115,131,0.72)";
  ctx.fillRect(p.x, p.y, rect.w, rect.h);
  ctx.fillStyle = "rgba(255,255,255,0.14)";
  ctx.fillRect(p.x, p.y, rect.w, 5);
}

function drawPlayMovingPlatform(platform) {
  const home = { x: platform.homeX, y: platform.homeY };
  const target = movingPlatformTarget(platform);
  const homeP = playWorldToScreen(home.x, home.y);
  const targetP = playWorldToScreen(target.x, target.y);
  const p = playWorldToScreen(platform.x, platform.y);
  ctx.save();
  ctx.strokeStyle = "rgba(139,209,124,0.38)";
  ctx.setLineDash([9, 7]);
  ctx.strokeRect(homeP.x, homeP.y, platform.w, platform.h);
  ctx.strokeRect(targetP.x, targetP.y, platform.w, platform.h);
  ctx.beginPath();
  ctx.moveTo(homeP.x + platform.w / 2, homeP.y + platform.h / 2);
  ctx.lineTo(targetP.x + platform.w / 2, targetP.y + platform.h / 2);
  ctx.stroke();
  ctx.setLineDash([]);
  ctx.fillStyle = "rgba(139,209,124,0.68)";
  ctx.fillRect(p.x, p.y, platform.w, platform.h);
  ctx.fillStyle = "rgba(255,255,255,0.18)";
  ctx.fillRect(p.x, p.y, platform.w, 5);
  ctx.strokeStyle = "#8bd17c";
  ctx.lineWidth = 2;
  ctx.strokeRect(p.x, p.y, platform.w, platform.h);
  ctx.restore();
}

function drawPlaySpike(rect) {
  const p = playWorldToScreen(rect.x, rect.y);
  ctx.fillStyle = "rgba(255,99,109,0.16)";
  ctx.fillRect(p.x, p.y, rect.w, rect.h);
  drawSpikeTriangles(p.x, p.y, rect.w, rect.h, "#ff636d", rect.rotation);
}

function drawPlayLaser(laser) {
  const beam = playLaserBeamRect(laser);
  const emitter = laserEmitterRect(laser);
  const fullP = playWorldToScreen(laser.x, laser.y);
  const beamP = playWorldToScreen(beam.x, beam.y);
  const emitterP = playWorldToScreen(emitter.x, emitter.y);
  ctx.save();
  ctx.fillStyle = "rgba(255,79,154,0.05)";
  ctx.fillRect(fullP.x, fullP.y, laser.w, laser.h);
  ctx.fillStyle = "rgba(255,79,154,0.48)";
  ctx.fillRect(beamP.x, beamP.y, beam.w, beam.h);
  ctx.fillStyle = "#ff4f9a";
  ctx.fillRect(emitterP.x, emitterP.y, emitter.w, emitter.h);
  ctx.strokeStyle = "rgba(255,255,255,0.30)";
  ctx.lineWidth = 1;
  ctx.strokeRect(emitterP.x, emitterP.y, emitter.w, emitter.h);
  ctx.restore();
}

function drawPlayButton(button) {
  const p = playWorldToScreen(button.x, button.y);
  ctx.fillStyle = button.active ? "#f4c95d" : "rgba(244,201,93,0.22)";
  ctx.fillRect(p.x, p.y, button.w, button.h);
  ctx.strokeStyle = "#f4c95d";
  ctx.strokeRect(p.x, p.y, button.w, button.h);
}

function drawPlayBridge(bridge) {
  const p = playWorldToScreen(bridge.x, bridge.y);
  const solid = playBridgeSolid(bridge);
  ctx.save();
  if (!solid) ctx.setLineDash([10, 8]);
  ctx.fillStyle = solid ? "rgba(113,215,232,0.42)" : "rgba(113,215,232,0.09)";
  ctx.strokeStyle = "#71d7e8";
  ctx.lineWidth = solid ? 3 : 2;
  ctx.fillRect(p.x, p.y, bridge.w, bridge.h);
  ctx.strokeRect(p.x, p.y, bridge.w, bridge.h);
  ctx.restore();
}

function drawPlayDoor(door) {
  if (playDoorOpen(door)) return;
  const p = playWorldToScreen(door.x, door.y);
  ctx.fillStyle = "rgba(255,141,102,0.32)";
  ctx.fillRect(p.x, p.y, door.w, door.h);
  ctx.strokeStyle = "#ff8d66";
  ctx.lineWidth = 3;
  ctx.strokeRect(p.x + 3, p.y + 3, door.w - 6, door.h - 6);
}

function drawPlayKey(key) {
  const p = playWorldToScreen(key.x, key.y);
  ctx.strokeStyle = "#f4c95d";
  ctx.lineWidth = 4;
  ctx.beginPath();
  ctx.arc(p.x + key.w * 0.35, p.y + key.h / 2, Math.min(key.w, key.h) * 0.22, 0, Math.PI * 2);
  ctx.stroke();
  ctx.beginPath();
  ctx.moveTo(p.x + key.w * 0.5, p.y + key.h / 2);
  ctx.lineTo(p.x + key.w * 0.9, p.y + key.h / 2);
  ctx.stroke();
}

function drawPlayGoal(goal) {
  const p = playWorldToScreen(goal.x, goal.y);
  ctx.fillStyle = "rgba(244,201,93,0.24)";
  ctx.fillRect(p.x, p.y, goal.w, goal.h);
  ctx.strokeStyle = "#f4c95d";
  ctx.lineWidth = 3;
  ctx.strokeRect(p.x + 3, p.y + 3, goal.w - 6, goal.h - 6);
}

function drawPlayAnchorZone(zone) {
  const p = playWorldToScreen(zone.x, zone.y);
  ctx.save();
  ctx.strokeStyle = "rgba(180,156,255,0.6)";
  ctx.setLineDash([10, 8]);
  ctx.strokeRect(p.x, p.y, zone.w, zone.h);
  ctx.restore();
}

function drawPlayAnchor(now) {
  if (playtest.respawnAnchor) drawPlayAnchorMarker(playtest.respawnAnchor, "#71d7e8", now, false);
  if (playtest.anchor) drawPlayAnchorMarker(playtest.anchor, "#f4c95d", now, true);
}

function drawPlayAnchorMarker(point, color, now, active) {
  const p = playWorldToScreen(point.x, point.y - 22);
  ctx.save();
  ctx.fillStyle = active ? "rgba(244,201,93,0.16)" : "rgba(113,215,232,0.12)";
  ctx.beginPath();
  ctx.arc(p.x, p.y, (active ? 36 : 26) + Math.sin(now * 0.006) * 2, 0, Math.PI * 2);
  ctx.fill();
  ctx.strokeStyle = color;
  ctx.lineWidth = active ? 4 : 2;
  ctx.beginPath();
  ctx.arc(p.x, p.y, active ? 26 : 19, 0, Math.PI * 2);
  ctx.stroke();
  ctx.restore();
}

function drawPlayGhost(now) {
  const ghost = playtest.ghost;
  if (!ghost) return;
  const p = playWorldToScreen(ghost.x, ghost.y);
  ctx.save();
  ctx.globalAlpha = 0.58;
  ctx.fillStyle = "#71d7e8";
  drawPlayBody(p.x, p.y, ghost.face, true);
  ctx.globalAlpha = 0.14 + Math.sin(now * 0.012) * 0.04;
  ctx.fillRect(p.x - 16, p.y + 4, PLAYER_W + 32, PLAYER_H - 8);
  ctx.restore();
}

function drawPlayPlayer() {
  const p = playWorldToScreen(playtest.player.x, playtest.player.y);
  ctx.fillStyle = "#f4c95d";
  drawPlayBody(p.x, p.y, playtest.player.face, false);
}

function drawPlayBody(x, y, face, ghostBody) {
  roundRect(x, y, PLAYER_W, PLAYER_H, 8, true);
  ctx.fillStyle = ghostBody ? "#0d2830" : "#281f09";
  const eyeX = face >= 0 ? x + 20 : x + 8;
  ctx.fillRect(eyeX, y + 13, 4, 5);
  ctx.fillRect(x + 7, y + PLAYER_H - 6, 7, 6);
  ctx.fillRect(x + PLAYER_W - 14, y + PLAYER_H - 6, 7, 6);
}

function drawPlayHud(now) {
  ctx.save();
  ctx.fillStyle = "rgba(7,8,11,0.82)";
  roundRect(18, 18, 610, 104, 8, true);
  ctx.strokeStyle = "rgba(244,237,221,0.14)";
  roundRect(18, 18, 610, 104, 8, false);
  ctx.fillStyle = "#f6eddd";
  ctx.font = "18px Microsoft YaHei, Segoe UI, sans-serif";
  ctx.fillText("试玩模式", 36, 49);
  ctx.font = "14px Microsoft YaHei, Segoe UI, sans-serif";
  ctx.fillStyle = "#aeb6bd";
  ctx.fillText("A/D 移动    Space 跳    E 开锚点    R 重开    Esc 退出", 36, 76);
  const carriedKey = carriedPlayKey();
  const state = carriedKey
    ? `携带 ${carriedKey.label || carriedKey.id}：碰到匹配的门会开锁`
    : playtest.player.wallSliding
    ? "贴墙下滑中：下落速度降低"
    : playtest.recording
      ? "录制中：倒计时内死亡会固定新分身"
      : playtest.ghost
        ? "分身已固定：可踩、可压按钮、免疫陷阱"
        : "未录制分身";
  ctx.fillText(state, 36, 100);
  drawPlayCountdown(now);
  if (now < playtest.messageUntil && playtest.message) {
    const boxW = Math.min(780, Math.max(360, ctx.measureText(playtest.message).width + 42));
    ctx.fillStyle = "rgba(8,10,13,0.84)";
    roundRect((VIEW_W - boxW) / 2, VIEW_H - 62, boxW, 40, 8, true);
    ctx.strokeStyle = "rgba(244,201,93,0.24)";
    roundRect((VIEW_W - boxW) / 2, VIEW_H - 62, boxW, 40, 8, false);
    ctx.fillStyle = "#f6eddd";
    ctx.textAlign = "center";
    ctx.fillText(playtest.message, VIEW_W / 2, VIEW_H - 37);
    ctx.textAlign = "left";
  }
  ctx.restore();
}

function drawPlayCountdown(now) {
  if (!playtest.recording) return;
  const total = playtest.rules.recordWindowSec * 1000;
  const remaining = clampValue(playtest.recording.expiresAt - now, 0, total);
  const blocks = Math.max(1, Math.round(playtest.rules.recordWindowSec));
  const filled = Math.ceil((remaining / total) * blocks);
  const size = 20;
  const gap = 6;
  const totalW = blocks * size + (blocks - 1) * gap;
  const x = (VIEW_W - totalW) / 2;
  const y = 26;
  for (let i = 0; i < blocks; i += 1) {
    const bx = x + i * (size + gap);
    ctx.strokeStyle = "rgba(244,201,93,0.7)";
    ctx.strokeRect(bx, y, size, size);
    if (i < filled) {
      ctx.fillStyle = "#f4c95d";
      ctx.fillRect(bx + 3, y + 3, size - 6, size - 6);
    }
  }
}

function drawPlayComplete() {
  ctx.save();
  ctx.fillStyle = "rgba(5,6,8,0.62)";
  ctx.fillRect(0, 0, VIEW_W, VIEW_H);
  ctx.fillStyle = "#f4c95d";
  ctx.font = "34px Microsoft YaHei, Segoe UI, sans-serif";
  ctx.textAlign = "center";
  ctx.fillText("试玩通过", VIEW_W / 2, VIEW_H / 2 - 12);
  ctx.fillStyle = "#f6eddd";
  ctx.font = "16px Microsoft YaHei, Segoe UI, sans-serif";
  ctx.fillText("按 R 重新试玩，按 Esc 回到编辑器。", VIEW_W / 2, VIEW_H / 2 + 28);
  ctx.textAlign = "left";
  ctx.restore();
}

function playSay(text, duration = 2200) {
  playtest.message = text;
  playtest.messageUntil = performance.now() + duration;
}

function handlePlayKeyDown(event) {
  const key = event.key.toLowerCase();
  if (["a", "d", "w", " ", "space", "spacebar", "arrowleft", "arrowright", "arrowup", "e", "r", "escape"].includes(key)) {
    event.preventDefault();
  }
  playKeys.add(key);
  if (key === " " || key === "space" || key === "spacebar" || key === "w" || key === "arrowup") playJump();
  if (key === "e") placePlayAnchor();
  if (key === "r") restartPlaytest();
  if (key === "escape") stopPlaytest();
}

function handlePlayKeyUp(event) {
  const key = event.key.toLowerCase();
  playKeys.delete(key);
  if (key === " " || key === "space" || key === "spacebar" || key === "w" || key === "arrowup") cutPlayJump();
}

function clampValue(value, min, max) {
  return Math.max(min, Math.min(max, value));
}

function lerpValue(a, b, k) {
  return a + (b - a) * k;
}

function approachValue(value, target, step) {
  if (value < target) return Math.min(target, value + step);
  if (value > target) return Math.max(target, value - step);
  return target;
}

ui.toolGrid.addEventListener("click", (event) => {
  const button = event.target.closest("button[data-tool]");
  if (button) setTool(button.dataset.tool);
});

for (const input of [ui.title, ui.ruleNotes, ui.experience, ui.worldW, ui.worldH, ui.grid, ui.snap]) {
  input.addEventListener("input", () => {
    syncMetaFromUi();
    updateValidation();
    draw();
    writeJson();
  });
}

ui.cameraX.addEventListener("input", () => {
  camera.x = number(ui.cameraX.value, 0);
  clampCamera();
  draw();
});

ui.cameraY.addEventListener("input", () => {
  camera.y = number(ui.cameraY.value, 0);
  clampCamera();
  draw();
});

canvas.addEventListener("pointerdown", pointerDown);
canvas.addEventListener("pointermove", pointerMove);
canvas.addEventListener("pointerup", pointerUp);
canvas.addEventListener("pointerleave", pointerUp);
canvas.addEventListener("wheel", wheel, { passive: false });

ui.playBtn.addEventListener("click", startPlaytest);
ui.stopPlayBtn.addEventListener("click", stopPlaytest);
ui.downloadLevelBtn.addEventListener("click", downloadLevelJson);
ui.saveBtn.addEventListener("click", saveMap);
ui.copyBriefBtn.addEventListener("click", copyBrief);
ui.resetBtn.addEventListener("click", resetMap);
ui.blankBtn.addEventListener("click", newBlankMap);
ui.duplicateBtn.addEventListener("click", duplicateSelected);
ui.rotateBtn.addEventListener("click", () => rotateSelected(90));
ui.deleteBtn.addEventListener("click", deleteSelected);
ui.fitBtn.addEventListener("click", () => {
  camera = { x: 0, y: 0 };
  clampCamera();
  draw();
});
ui.exportBtn.addEventListener("click", showEditorJson);
ui.exportLevelBtn.addEventListener("click", showLevelJson);
ui.copyJsonBtn.addEventListener("click", copyJson);
ui.importBtn.addEventListener("click", importMap);
ui.importFileBtn.addEventListener("click", chooseJsonFile);
ui.fileInput.addEventListener("change", importJsonFile);
ui.loadBtn.addEventListener("click", loadMap);

window.addEventListener("keydown", (event) => {
  if (playtest) {
    handlePlayKeyDown(event);
    return;
  }
  if (document.activeElement?.tagName === "INPUT" || document.activeElement?.tagName === "TEXTAREA" || document.activeElement?.tagName === "SELECT") return;
  if (event.key === "Delete" || event.key === "Backspace") {
    event.preventDefault();
    deleteSelected();
    return;
  }
  if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "d") {
    event.preventDefault();
    duplicateSelected();
    return;
  }
  if (event.key.toLowerCase() === "r") {
    event.preventDefault();
    rotateSelected(event.shiftKey ? -90 : 90);
    return;
  }
  const object = selectedObject();
  if (!object) return;
  const amount = event.shiftKey ? map.world.grid : 1;
  const beforeX = object.x;
  const beforeY = object.y;
  if (event.key === "ArrowLeft") object.x -= amount;
  else if (event.key === "ArrowRight") object.x += amount;
  else if (event.key === "ArrowUp") object.y -= amount;
  else if (event.key === "ArrowDown") object.y += amount;
  else return;
  event.preventDefault();
  if (object.type === "movingPlatform") moveAttachedLasersBy(object.id, object.x - beforeX, object.y - beforeY);
  updateInspector();
  updateValidation();
  draw();
  writeJson();
}, true);

window.addEventListener("keyup", (event) => {
  if (playtest) handlePlayKeyUp(event);
}, true);

async function bootEditor() {
  const loadedPhysics = await loadPlayerPhysicsConfig();
  map = normalizeMap(sampleMap);
  const saved = localStorage.getItem(STORAGE_KEY);
  if (saved) {
    try {
      map = normalizeMap(JSON.parse(saved));
      setStatus(loadedPhysics ? "已载入上次保存的地图，并同步角色物理参数。" : "已载入上次保存的地图。");
    } catch {
      map = normalizeMap(sampleMap);
    }
  } else if (loadedPhysics) {
    setStatus("已同步角色物理参数。");
  }
  initToolGrid();
  setTool("select");
  syncUiFromMap();
}

bootEditor();
