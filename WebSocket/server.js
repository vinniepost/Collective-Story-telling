const express = require('express');
const http = require('http');
const WebSocket = require('ws');
const { v4: uuidv4 } = require('uuid');
const path = require('path');
const app = express();
const PORT = process.env.PORT || 3000;

app.use(express.static(path.join(__dirname, 'client/dist/client/browser')));
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// Simple debug endpoint to view current server state without the Angular client
app.get('/api/state', (req, res) => {
    res.json({
        message_state: {
            vrMessage,
            options: messageOptions,
            votes: messageVotes
        },
        map_update: {
            sections: Object.values(sections),
            doors: Object.values(doors),
            pipes: Object.values(pipes),
            votes: mapVotes,
            totalClients: getClientCount(),
            doorCooldown: Math.max(0, doorCooldownEnds - Date.now())
        },
        clues: {
            collected: cluesCollected,
            required: CLUES_REQUIRED
        },
        code_red: {
            active: codeRedActive,
            remainingSeconds: codeRedActive ? Math.max(0, Math.floor((codeRedEndTime - Date.now()) / 1000)) : 0,
            endsAt: codeRedEndTime
        }
    });
});

// API endpoint to start a repair task (testing without Unity)
app.post('/api/start-repair', (req, res) => {
    const { sectionId, text } = req.body || {};
    const assigned = startRepairTask(sectionId, text);
    res.json({ ok: true, ...assigned });
});

// API: Get/Set pipe repair threshold for starting purge (1 or 3)
app.get('/api/pipe-threshold', (req, res) => {
    res.json({ ok: true, threshold: PIPE_REPAIR_THRESHOLD });
});
app.post('/api/pipe-threshold', (req, res) => {
    const { threshold } = req.body || {};
    const n = Number(threshold);
    if (n === 1 || n === 3) {
        PIPE_REPAIR_THRESHOLD = n;
        broadcast({ type: 'notification', message: `Pipe repair threshold set to ${n}` });
        return res.json({ ok: true, threshold: PIPE_REPAIR_THRESHOLD });
    }
    res.status(400).json({ ok: false, message: 'Invalid threshold. Use 1 or 3.' });
});

// API endpoint to mark a single pipe repaired (incremental, no light changes)
app.post('/api/repair-completed', (req, res) => {
    const { sectionId, pipeId, text } = req.body || {};
    const msg = text && typeof text === 'string' ? text : 'REPAIR COMPLETED';
    vrMessage = msg;
    broadcast({ type: 'notification', message: msg });

    let targetPid = null;
    if (pipeId && isRepairPipeId(pipeId)) {
        targetPid = pipeId;
    } else if (sectionId && INITIAL_PIPE_SECTIONS.includes(sectionId)) {
        targetPid = pipesBySection[sectionId] || `pipe_${sectionId}`;
    } else {
        targetPid = getNextUnrepairedPipeId();
    }

    if (targetPid) {
        if (!pipes[targetPid]) {
            const sec = targetPid.replace('pipe_', '');
            pipes[targetPid] = { id: targetPid, sectionId: sec, repaired: true };
        } else {
            pipes[targetPid].repaired = true;
        }
    }

    // If threshold met, start VO → purge
    if (shouldStartPurgeByThreshold()) {
        startVoiceOverThenPurge(8);
    }

    broadcastMapState();
    broadcastMessageState();

    try {
        const lower = (msg || '').toLowerCase();
        if (lower.includes('all pipes repaired') || lower.includes('escape to the start')) {
            startVoiceOverThenPurge(8);
        }
    } catch { }

    res.json({ ok: true, sectionId: sectionId || null });
});

// (Removed pipe position API; pipes are positioned via CSS like doors.)

// API to collect a clue (increments up to CLUES_REQUIRED). Applies +10s if Code Red active
app.post('/api/clue-collect', (req, res) => {
    const { silent = false } = req.body || {};
    const before = cluesCollected;
    cluesCollected = Math.min(CLUES_REQUIRED, cluesCollected + 1);
    if (!silent) {
        broadcast({ type: 'notification', message: `Clue collected (${cluesCollected}/${CLUES_REQUIRED})` });
    }

    let bonusApplied = false; // never apply bonus while purge is active
    let bonusQueued = false;
    if (before < CLUES_REQUIRED && cluesCollected >= CLUES_REQUIRED) {
        // Completed full set of clues
        // Only apply the +10s at purge start; do not extend if already active
        if (!silent) {
            const note = codeRedActive
                ? 'All clues collected! Bonus will apply at next purge start.'
                : 'All clues collected! +10s will be applied when purge starts.';
            broadcast({ type: 'notification', message: note });
        }
        if (!codeRedActive) {
            bonusQueued = true;
        }
    }

    res.json({ ok: true, clues: cluesCollected, bonusApplied, bonusQueued, codeRedActive, remainingSeconds: codeRedActive ? Math.max(0, Math.floor((codeRedEndTime - Date.now()) / 1000)) : 0 });
});

// API to reset clue progress
app.post('/api/clue-reset', (req, res) => {
    cluesCollected = 0;
    broadcast({ type: 'notification', message: 'Clue progress reset' });
    res.json({ ok: true, clues: cluesCollected });
});

// Catch-all for Angular routing
app.get('*', (req, res) => {
    res.sendFile(path.join(__dirname, 'client/dist/client/browser/index.html'));
});

const server = http.createServer(app);
server.listen(PORT, () => {
    console.log(`HTTP server running on http://localhost:${PORT}`);
});

// Explicitly listen on the /ws path
const wss = new WebSocket.Server({ server, path: '/ws' });

// --- HELPER FUNCTIONS (Must be defined before use) ---
function getClientCount() {
    return wss.clients.size;
}

function broadcast(obj) {
    const msg = JSON.stringify(obj);
    wss.clients.forEach(client => {
        if (client.readyState === WebSocket.OPEN) {
            client.send(msg);
        }
    });
}

// --- CODE RED / CLUE SYSTEM STATE & HELPERS ---
const CLUES_REQUIRED = 4;
let PURGE_DEFAULT_SECONDS = 120; // Configurable via Unity 'CodeRedConfig' event
let cluesCollected = 0;
let codeRedActive = false;
let codeRedEndTime = 0; // epoch ms
let voiceOverActive = false;
let voiceOverTimeout = null;
let PIPE_REPAIR_THRESHOLD = 3; // Toggle: set to 1 to start purge after a single pipe

function startCodeRed(baseDurationSeconds, opts = {}) {
    const { silentBonusNotice = false } = opts;
    let durationSeconds = baseDurationSeconds;
    if (cluesCollected >= CLUES_REQUIRED) {
        durationSeconds += 10; // bonus time if all clues found before start
        if (!silentBonusNotice) {
            broadcast({ type: 'notification', message: '+10s bonus applied (all clues collected)' });
        }
    }
    codeRedActive = true;
    const proposedEnd = Date.now() + durationSeconds * 1000;
    // If already active, prefer the later end time
    codeRedEndTime = Math.max(codeRedEndTime, proposedEnd);
    const remaining = Math.max(0, Math.floor((codeRedEndTime - Date.now()) / 1000));
    broadcast({ type: 'code_red', duration: remaining });

    // Countdown handled client-side in Unity; no per-second vrMessage updates
}

function extendCodeRed(extraSeconds, opts = {}) {
    const { silent = false } = opts;
    if (!codeRedActive) return false;
    codeRedEndTime += extraSeconds * 1000;
    const remaining = Math.max(0, Math.floor((codeRedEndTime - Date.now()) / 1000));
    broadcast({ type: 'code_red_extend', extra: extraSeconds, remaining });
    if (!silent) {
        broadcast({ type: 'notification', message: `+${extraSeconds}s added to purge timer` });
    }
    // Also rebroadcast code_red with new remaining for clients that don't handle code_red_extend
    broadcast({ type: 'code_red', duration: remaining });
    return true;
}

// Removed server-side countdown spam; Unity shows countdown locally

function startVoiceOverThenPurge(voiceSeconds = 8, purgeSeconds = PURGE_DEFAULT_SECONDS) {
    // Prevent multiple triggers
    if (voiceOverActive || codeRedActive) return;
    voiceOverActive = true;
    // Use the same text channel as tasks
    vrMessage = 'ESCAPE TO THE START';
    broadcastMessageState();
    broadcast({ type: 'voice_over', text: vrMessage, duration: voiceSeconds });
    // After VO, start purge
    if (voiceOverTimeout) clearTimeout(voiceOverTimeout);
    voiceOverTimeout = setTimeout(() => {
        voiceOverActive = false;
        startCodeRed(purgeSeconds, { silentBonusNotice: true });
    }, Math.max(0, voiceSeconds) * 1000);
}

function getRepairedPipeCount() {
    return getRepairPipeIds().reduce((acc, id) => acc + ((pipes[id] && pipes[id].repaired) ? 1 : 0), 0);
}

function shouldStartPurgeByThreshold() {
    return getRepairedPipeCount() >= PIPE_REPAIR_THRESHOLD;
}

// --- MAP SYSTEM STATE ---
const SECTIONS_COUNT = 21;
const DOORS_COUNT = 17;
const MAX_LIGHTS_ON = 3;
const MAX_DOORS_CLOSED = 2;
const DOOR_COOLDOWN_MS = 6000;

let sections = {}; // { "section_0": { id: "section_0", lightsOn: false }, ... }
let doors = {};    // { "door_0": { id: "door_0", isClosed: false, isLocked: false, lastClosedTime: 0 }, ... }
let pipes = {};    // { "pipe_section_1": { id: "pipe_section_1", sectionId: "section_1", repaired: false }, ... }
let pipesBySection = {}; // sectionId -> pipeId
let doorCooldownEnds = 0;
let activeRepairSectionId = null; // Track currently assigned repair section

// Helper: list the three repair pipe IDs
function getRepairPipeIds() {
    return INITIAL_PIPE_SECTIONS.map(sec => `pipe_${sec}`);
}
function getNextUnrepairedPipeId() {
    const ids = getRepairPipeIds();
    for (const id of ids) {
        if (pipes[id] && !pipes[id].repaired) return id;
    }
    return null;
}

function isRepairPipeId(pid) {
    return getRepairPipeIds().includes(pid);
}

// Normalize section identifiers coming from Unity (number or string variants)
function normalizeSectionId(input) {
    if (input === undefined || input === null) return null;
    if (typeof input === 'number') return `section_${input}`;
    if (typeof input === 'string') {
        const lower = input.trim().toLowerCase();
        // Matches: 'section_1', 'section 1', 'section-1', '1'
        const m = lower.match(/^(?:section[ _-]?)(\d+)$/) || lower.match(/^(\d+)$/);
        if (m) return `section_${m[1]}`;
        // Already canonical?
        if (lower.startsWith('section_')) return lower;
    }
    return null;
}

// Resolve a target pipe ID from a Unity event payload
function resolvePipeIdFromEvent(data) {
    // Prefer explicit pipeId fields
    const rawPipeId = data.pipeId || data.pipe_id || data.pipe;
    if (typeof rawPipeId === 'string' && isRepairPipeId(rawPipeId)) {
        return rawPipeId;
    }

    // Try various section fields
    const rawSection = data.sectionId ?? data.section_id ?? data.section ?? data.sectionIndex ?? data.section_index;
    const normalizedSection = normalizeSectionId(rawSection);
    if (normalizedSection && INITIAL_PIPE_SECTIONS.includes(normalizedSection)) {
        return pipesBySection[normalizedSection] || `pipe_${normalizedSection}`;
    }

    // Fall back to assigned repair section if available
    if (activeRepairSectionId && INITIAL_PIPE_SECTIONS.includes(activeRepairSectionId)) {
        return pipesBySection[activeRepairSectionId] || `pipe_${activeRepairSectionId}`;
    }

    // Finally, the next unrepaired pipe
    return getNextUnrepairedPipeId();
}

// Initialize State
for (let i = 0; i < SECTIONS_COUNT; i++) {
    const id = `section_${i}`;
    sections[id] = { id: id, lightsOn: false };
}
for (let i = 0; i < DOORS_COUNT; i++) {
    const id = `door_${i}`;
    doors[id] = { id: id, isClosed: false, isLocked: false, lastClosedTime: 0 };
}

// Initialize Pipes only for three sections to show exactly three repair targets on the map
const INITIAL_PIPE_SECTIONS = ['section_1', 'section_2', 'section_3'];
INITIAL_PIPE_SECTIONS.forEach(sectionId => {
    const pipeId = `pipe_${sectionId}`;
    pipes[pipeId] = { id: pipeId, sectionId: sectionId, repaired: false };
    pipesBySection[sectionId] = pipeId;
});

// Voting Buckets
let mapVotes = {};
let clientMapVotes = new Map(); // ClientID -> Set<EntityID>

function broadcastMapState() {
    // Prepare lists for Unity/Client
    const sectionsList = Object.values(sections);
    const doorsList = Object.values(doors);
    const pipesList = Object.values(pipes);

    broadcast({
        type: "map_update",
        sections: sectionsList,
        doors: doorsList,
        pipes: pipesList,
        votes: mapVotes,
        totalClients: getClientCount(),
        doorCooldown: Math.max(0, doorCooldownEnds - Date.now())
    });
}

function resetMapVoting() {
    mapVotes = {};
    for (let i = 0; i < SECTIONS_COUNT; i++) mapVotes[`section_${i}`] = 0;
    for (let i = 0; i < DOORS_COUNT; i++) mapVotes[`door_${i}`] = 0;
    clientMapVotes.clear();
    broadcastMapState();
}

// Initialize empty votes (Safe to call now)
resetMapVoting();

// --- LEGACY VOTING STATE ---
const availableActions = ["close_door", "light", "sound_1", "sound_2"];
let uniqueActionVotes = {};
let clientVotes = new Map();
let playerInSecretArea = false;
let playerLocation = { x: 0, y: 0 };
availableActions.forEach(action => uniqueActionVotes[action] = 0);

function broadcastStateUpdate() {
    broadcast({
        type: "update",
        votes: uniqueActionVotes,
        totalClients: getClientCount(),
        playerInArea: playerInSecretArea
    });
}

function resetVotingCycle() {
    clientVotes.clear();
    availableActions.forEach(action => uniqueActionVotes[action] = 0);
    broadcastStateUpdate();
    console.log("Voting cycle reset by server.");
}

// --- MESSAGE SYSTEM STATE ---
let vrMessage = "";
let messageOptions = [];
let messageVotes = {};
let clientMessageVotes = new Map();

function broadcastMessageState() {
    broadcast({
        type: "message_state",
        vrMessage: vrMessage,
        options: messageOptions,
        votes: messageVotes
    });
}

function resetMessageVoting() {
    messageVotes = {};
    messageOptions.forEach(opt => messageVotes[opt] = 0);
    clientMessageVotes.clear();
    broadcastMessageState();
}

// --- REPAIR TASK SYSTEM ---
const REPAIR_TASKS = [
    "Repair the water pump",
    "Fix broken wiring",
    "Replace the air filter",
    "Seal the leaking pipe",
    "Calibrate the control panel",
    "Restart the generator",
    "Secure loose vent cover",
];

function pickRandomSectionId() {
    const idx = Math.floor(Math.random() * SECTIONS_COUNT);
    return `section_${idx}`;
}

function startRepairTask(sectionId, text) {
    const targetSection = (sectionId && sections[sectionId]) ? sectionId : pickRandomSectionId();
    const taskText = (text && typeof text === 'string') ? text : REPAIR_TASKS[Math.floor(Math.random() * REPAIR_TASKS.length)];

    // Do not toggle section lights for pipe repair flow

    // Track the active repair target
    activeRepairSectionId = targetSection;

    // Ensure pipe entries exist (they are initialized at startup)

    // Set the VR message for the web app's Communication panel (friendly Section N)
    let readableSection = targetSection;
    if (typeof targetSection === 'string' && targetSection.startsWith('section_')) {
        const num = targetSection.substring('section_'.length);
        readableSection = `Section ${num}`;
    }
    vrMessage = `Technician: ${taskText} in ${readableSection}`;

    // Broadcast to clients
    broadcast({ type: "notification", message: `REPAIR TASK ISSUED → ${taskText} (${targetSection})` });
    broadcast({ type: "vr_task", text: taskText, sectionId: targetSection }); // Unity can listen for this
    broadcastMapState();
    broadcastMessageState();
    return { sectionId: targetSection, text: taskText };
}

// --- THRESHOLD CHECKS ---
function checkMapThreshold(currentVotes, totalClients) {
    const audienceCount = Math.max(0, totalClients - 1);
    const requiredVotes = audienceCount === 0 ? 1 : Math.ceil(audienceCount / 2);
    return currentVotes >= requiredVotes && totalClients > 0;
}

function checkThreshold(action, currentVotes, totalClients) {
    const audienceCount = Math.max(0, totalClients - 1);
    const requiredVotes = audienceCount === 0 ? 1 : Math.ceil(audienceCount / 2);
    return currentVotes >= requiredVotes && totalClients > 0;
}

function checkMessageThreshold(currentVotes, totalClients) {
    const audienceCount = Math.max(0, totalClients - 1);
    const requiredVotes = audienceCount === 0 ? 1 : Math.ceil(audienceCount / 3);
    return currentVotes >= requiredVotes && totalClients > 0;
}

// --- HANDLERS ---
function handleMapVote(ws, entityId) {
    const clientId = ws.id;
    const totalClients = getClientCount();

    // Check if ID exists
    if (!sections[entityId] && !doors[entityId]) return;

    if (!clientMapVotes.has(clientId)) {
        clientMapVotes.set(clientId, new Set());
    }

    const userVotes = clientMapVotes.get(clientId);
    if (userVotes.has(entityId)) {
        userVotes.delete(entityId);
        if (mapVotes[entityId] > 0) mapVotes[entityId]--;
    } else {
        userVotes.add(entityId);
        mapVotes[entityId]++;
    }

    const newCount = mapVotes[entityId];
    broadcastMapState();

    if (checkMapThreshold(newCount, totalClients)) {
        executeMapAction(entityId);
        resetMapVoting();
    }
}

// --- SERVER-SIDE DOOR LOGIC ---
// Lock Duration: How long the door stays RED (Locked)
const DOOR_LOCK_DURATION_MS = 10000;

function executeMapAction(entityId) {
    // Is it a Section?
    if (sections[entityId]) {
        const section = sections[entityId];

        if (section.lightsOn) {
            section.lightsOn = false;
            broadcast({ type: "notification", message: `SECTION ${entityId} POWERED OFF` });
        } else {
            const currentOn = Object.values(sections).filter(s => s.lightsOn).length;
            if (currentOn >= MAX_LIGHTS_ON) {
                broadcast({ type: "feedback", message: `GRID OVERLOAD: Max ${MAX_LIGHTS_ON} sections allowed.` });
                return;
            }
            section.lightsOn = true;
            broadcast({ type: "notification", message: `SECTION ${entityId} POWERED ON` });
        }
    }
    // Is it a Door?
    else if (doors[entityId]) {
        const door = doors[entityId];

        // Global Cooldown (Operator spam prevention)
        if (Date.now() < doorCooldownEnds) {
            broadcast({ type: "feedback", message: "DOOR SYSTEM COOLING DOWN." });
            return;
        }

        // If door is already closed, we can't close it again (it needs to be opened by Player)
        if (door.isClosed) {
            broadcast({ type: "feedback", message: `DOOR ${entityId} IS ALREADY SEALED.` });
            return;
        }

        // Close the Door
        door.isClosed = true;
        door.isLocked = true; // RED LIGHT
        door.lastClosedTime = Date.now();
        door.unlockTime = Date.now() + DOOR_LOCK_DURATION_MS;

        broadcast({ type: "notification", message: `DOOR ${entityId} LOCKED (SEALED)` });
        broadcast({ type: "door_closed", doorId: entityId }); // Signal Unity to Close & Red Light

        // Set Global Cooldown
        doorCooldownEnds = Date.now() + DOOR_COOLDOWN_MS;

        // Start Timer to Unlock (GREEN LIGHT)
        setTimeout(() => {
            if (door.isClosed) { // Ensure it's still closed
                door.isLocked = false;
                broadcast({ type: "notification", message: `DOOR ${entityId} UNLOCKED (MANUAL OVERRIDE AVAILABLE)` });
                broadcast({ type: "door_unlockable", doorId: entityId }); // Signal Unity to Green Light
                broadcastMapState();
            }
        }, DOOR_LOCK_DURATION_MS);
    }

    broadcastMapState();
}

// Function to handle UNITY/LEVER actions
function handleDoorOpen(doorId) {
    if (doors[doorId]) {
        const door = doors[doorId];
        // Can only open if it was closed and is now unlockable (isLocked = false)
        // Actually, if Unity sends "open", it means the player successfully pulled the lever.
        // We trust Unity's validation (Lever only works when Green).

        door.isClosed = false;
        door.isLocked = false;
        broadcast({ type: "notification", message: `DOOR ${doorId} OPENED BY MANUAL OVERRIDE` });
        broadcast({ type: "door_opened", doorId: doorId }); // Signal Web Client

        // Cooldown before it can be closed again? The global cooldown handles operator clicks.
        // We might want a short specific cooldown for this door, but global is fine for now.

        broadcastMapState();
    }
}

function handleVote(ws, action) {
    const clientId = ws.id;
    const totalClients = getClientCount();

    if (clientVotes.has(clientId)) {
        ws.send(JSON.stringify({ type: "feedback", message: "You have already cast your vote.", votedAction: clientVotes.get(clientId) }));
        return;
    }

    clientVotes.set(clientId, action);
    uniqueActionVotes[action]++;
    const newCount = uniqueActionVotes[action];

    broadcastStateUpdate();

    if (checkThreshold(action, newCount, totalClients)) {
        console.log(`THRESHOLD REACHED for ${action}!`);
        broadcast({ type: "action", command: action });
        resetVotingCycle();
    }
}

function handleMessageVote(ws, option) {
    const clientId = ws.id;
    const totalClients = getClientCount();

    if (!messageOptions.includes(option)) return;

    if (!clientMessageVotes.has(clientId)) {
        clientMessageVotes.set(clientId, new Set());
    }

    const userVotes = clientMessageVotes.get(clientId);
    if (userVotes.has(option)) {
        userVotes.delete(option);
        messageVotes[option]--;
    } else {
        userVotes.add(option);
        messageVotes[option]++;
    }

    const newCount = messageVotes[option];
    broadcastMessageState();

    if (checkMessageThreshold(newCount, totalClients)) {
        broadcast({ type: "vr_message_sent", message: option });
        messageOptions = [];
        resetMessageVoting();
    }
}

// --- OPERATOR CHAT ---
let nextOperatorId = 1;
let chatHistory = [];

wss.on('connection', ws => {
    ws.id = uuidv4();

    // Assign Operator ID
    const operatorId = String(nextOperatorId).padStart(3, '0');
    ws.username = `Operator-${operatorId}`;
    nextOperatorId++;

    console.log(`Client connected (${ws.id}) as ${ws.username}. Total clients: ${getClientCount()}`);

    // Send assigned username
    ws.send(JSON.stringify({ type: "assign_username", username: ws.username }));

    // Send chat history
    ws.send(JSON.stringify({ type: "chat_history", messages: chatHistory }));

    // Send Map State
    ws.send(JSON.stringify({
        type: "map_update",
        sections: Object.values(sections),
        doors: Object.values(doors),
        pipes: Object.values(pipes),
        votes: mapVotes,
        totalClients: getClientCount(),
        doorCooldown: Math.max(0, doorCooldownEnds - Date.now())
    }));

    // Send Message State
    ws.send(JSON.stringify({
        type: "message_state",
        vrMessage: vrMessage,
        options: messageOptions,
        votes: messageVotes
    }));

    // Send current Code Red state if active
    if (codeRedActive) {
        const remaining = Math.max(0, Math.floor((codeRedEndTime - Date.now()) / 1000));
        ws.send(JSON.stringify({ type: 'code_red', duration: remaining }));
    }

    // Send Player Location
    ws.send(JSON.stringify({ type: "player_location", playerLocation: playerLocation }));

    // Update Client Count
    broadcast({ type: "client_count", count: getClientCount() });

    ws.on('message', message => {
        try {
            const data = JSON.parse(message);

            if (data.type === 'vote_map') {
                handleMapVote(ws, data.entityId);
            }

            // Unity Door Open Event
            if (data.type === 'door_opened') {
                handleDoorOpen(data.doorId);
            }

            if (data.type === 'vote') {
                handleVote(ws, data.option);
            }

            if (data.type === 'vote_message') {
                handleMessageVote(ws, data.option);
            }

            if (data.type === 'chat_message') {
                const chatMsg = {
                    username: ws.username,
                    text: data.text,
                    timestamp: new Date()
                };
                chatHistory.push(chatMsg);
                if (chatHistory.length > 50) chatHistory.shift();
                broadcast({ type: "chat_message", message: chatMsg });
            }

            // Game Events
            if (data.type === 'game_event') {
                console.log(`Unity Event Triggered: ${data.event_id}`);
                if (data.event_id === 'ZoneEnter_Secret') {
                    playerInSecretArea = true;
                    broadcastStateUpdate();
                } else if (data.event_id === 'ZoneExit_Secret') {
                    playerInSecretArea = false;
                    broadcastStateUpdate();
                } else if (data.event_id === 'VoteResetArea') {
                    broadcast({ type: "notification", message: "Checkpoint Reached! Voting Cycle Reset." });
                    resetVotingCycle();
                } else if (data.event_id === 'CodeRedStart') {
                    const dur = Number(data.duration);
                    const startSeconds = (Number.isFinite(dur) && dur > 0) ? dur : PURGE_DEFAULT_SECONDS;
                    startCodeRed(startSeconds);
                    broadcast({ type: "notification", message: `⚠️ CODE RED INITIATED (${startSeconds}s) ⚠️` });
                } else if (data.event_id === 'CodeRedConfig') {
                    const dur = Number(data.duration);
                    if (Number.isFinite(dur) && dur > 0) {
                        PURGE_DEFAULT_SECONDS = dur;
                        broadcast({ type: 'notification', message: `Purge duration set to ${dur}s` });
                    }
                } else if (data.event_id === 'CodeRedEscape') {
                    codeRedActive = false;
                    codeRedEndTime = 0;
                    vrMessage = '';
                    broadcast({ type: "code_red_result", result: "escaped" });
                    broadcast({ type: "notification", message: "⚠️ SUBJECT ESCAPED ⚠️" });
                } else if (data.event_id === 'CodeRedFail') {
                    codeRedActive = false;
                    codeRedEndTime = 0;
                    vrMessage = '';
                    broadcast({ type: "code_red_result", result: "failed" });
                    broadcast({ type: "notification", message: "⚠️ SUBJECT TERMINATED ⚠️" });
                } else if (data.event_id === 'PlayerSpawned') {
                    // Unity can include optional sectionId/text; normalize if provided
                    const norm = normalizeSectionId(data.sectionId ?? data.section);
                    startRepairTask(norm || data.sectionId, data.text);
                    broadcast({ type: "notification", message: "PLAYER SPAWNED → Repair task assigned." });
                } else if (data.event_id === 'RepairCompleted') {
                    // Clear the repair task and mark one pipe as repaired (incremental)
                    vrMessage = "";
                    const targetPid = resolvePipeIdFromEvent(data);
                    console.log('RepairCompleted payload:', {
                        sectionId: data.sectionId ?? data.section,
                        pipeId: data.pipeId ?? data.pipe,
                        resolved: targetPid,
                        activeRepairSectionId
                    });
                    if (targetPid) {
                        if (!pipes[targetPid]) {
                            const sec = targetPid.replace('pipe_', '');
                            pipes[targetPid] = { id: targetPid, sectionId: sec, repaired: true };
                        } else {
                            pipes[targetPid].repaired = true;
                        }
                    }

                    broadcast({ type: "notification", message: "REPAIR COMPLETED" });
                    broadcastMapState();
                    broadcastMessageState();
                    // If threshold met, start VO → purge
                    if (shouldStartPurgeByThreshold()) {
                        startVoiceOverThenPurge(8);
                    }
                }
                return;
            }

            if (data.type === 'vr_message') {
                vrMessage = data.message;
                broadcastMessageState();
                broadcast({ type: "notification", message: `INCOMING MESSAGE: ${data.message}` });
            }

            if (data.type === 'start_message_vote') {
                messageOptions = data.options;
                resetMessageVoting();
                broadcast({ type: "notification", message: "NEW RESPONSE OPTIONS AVAILABLE" });
            }

            if (data.type === 'request_state') {
                broadcastStateUpdate();
                // Resend other states if needed, but client usually requests specific updates
                // For now, request_state just broadcasting legacy update is fine or we can resend everything.
            }

            if (data.type === 'broadcast_log') {
                broadcast({ type: "notification", message: data.message });
            }

            if (data.type === 'player_location') {
                playerLocation = data.location;
                broadcast({ type: 'player_location', playerLocation: playerLocation });
            }

        } catch (e) {
            console.error('Error parsing message:', e);
        }
    });

    ws.on('close', () => {
        // Cleanup Legacy Votes
        const votedAction = clientVotes.get(ws.id);
        if (votedAction) {
            uniqueActionVotes[votedAction]--;
            clientVotes.delete(ws.id);
        }

        // Cleanup Message Votes
        if (clientMessageVotes.has(ws.id)) {
            const userVotes = clientMessageVotes.get(ws.id);
            userVotes.forEach(opt => {
                if (messageVotes[opt] > 0) messageVotes[opt]--;
            });
            clientMessageVotes.delete(ws.id);
            broadcastMessageState();
        }

        // Cleanup Map Votes
        if (clientMapVotes.has(ws.id)) {
            const userVotes = clientMapVotes.get(ws.id);
            userVotes.forEach(entityId => {
                if (mapVotes[entityId] > 0) mapVotes[entityId]--;
            });
            clientMapVotes.delete(ws.id);
            broadcastMapState();
        }

        console.log(`Client disconnected (${ws.id}). Total clients: ${getClientCount()}`);
        broadcast({ type: "client_count", count: getClientCount() });
        broadcastStateUpdate();
    });
});