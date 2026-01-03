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
            votes: mapVotes,
            totalClients: getClientCount(),
            doorCooldown: Math.max(0, doorCooldownEnds - Date.now())
        }
    });
});

// API endpoint to start a repair task (testing without Unity)
app.post('/api/start-repair', (req, res) => {
    const { sectionId, text } = req.body || {};
    const assigned = startRepairTask(sectionId, text);
    res.json({ ok: true, ...assigned });
});

// API endpoint to mark repair completed (clears message and lights)
app.post('/api/repair-completed', (req, res) => {
    const { sectionId, text } = req.body || {};
    // Set completion message for users
    const msg = text && typeof text === 'string' ? text : 'REPAIR COMPLETED';
    vrMessage = msg;
    broadcast({ type: "notification", message: msg });

    // Turn off specific section if provided; otherwise clear all
    if (sectionId && sections[sectionId]) {
        sections[sectionId].lightsOn = false;
    } else {
        Object.values(sections).forEach(s => { s.lightsOn = false; });
    }

    broadcastMapState();
    broadcastMessageState();

    // If all pipes repaired → trigger purge + red alert
    try {
        const lower = (msg || '').toLowerCase();
        if (lower.includes('all pipes repaired')) {
            broadcast({ type: 'notification', message: 'PURGE THE PLAYER' });
            // Start breakdown effect on web (uses existing code_red overlay)
            broadcast({ type: 'code_red', duration: 30 });
        }
    } catch {}

    res.json({ ok: true, sectionId: sectionId || null });
});

// Catch-all for Angular routing
app.get('*', (req, res) => {
    res.sendFile(path.join(__dirname, 'client/dist/client/browser/index.html'));
});

const server = http.createServer(app);
server.listen(PORT, () => {
    console.log(`HTTP server running on http://localhost:${PORT}`);
});

const wss = new WebSocket.Server({ server });

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

// --- MAP SYSTEM STATE ---
const SECTIONS_COUNT = 21;
const DOORS_COUNT = 17;
const MAX_LIGHTS_ON = 3;
const MAX_DOORS_CLOSED = 2;
const DOOR_COOLDOWN_MS = 6000;

let sections = {}; // { "section_0": { id: "section_0", lightsOn: false }, ... }
let doors = {};    // { "door_0": { id: "door_0", isClosed: false, lastClosedTime: 0 }, ... }
let doorCooldownEnds = 0;

// Initialize State
for (let i = 0; i < SECTIONS_COUNT; i++) {
    const id = `section_${i}`;
    sections[id] = { id: id, lightsOn: false };
}
for (let i = 0; i < DOORS_COUNT; i++) {
    const id = `door_${i}`;
    doors[id] = { id: id, isClosed: false, isLocked: false, lastClosedTime: 0 };
}

// Voting Buckets
let mapVotes = {};
let clientMapVotes = new Map(); // ClientID -> Set<EntityID>

function broadcastMapState() {
    // Prepare lists for Unity/Client
    const sectionsList = Object.values(sections);
    const doorsList = Object.values(doors);

    broadcast({
        type: "map_update",
        sections: sectionsList,
        doors: doorsList,
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

    // Turn off all lights, then highlight target section
    Object.values(sections).forEach(s => { s.lightsOn = false; });
    sections[targetSection].lightsOn = true;

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
                    broadcast({ type: "code_red", duration: 120 });
                    broadcast({ type: "notification", message: "⚠️ CODE RED INITIATED ⚠️" });
                } else if (data.event_id === 'CodeRedEscape') {
                    broadcast({ type: "code_red_result", result: "escaped" });
                    broadcast({ type: "notification", message: "⚠️ SUBJECT ESCAPED ⚠️" });
                } else if (data.event_id === 'CodeRedFail') {
                    broadcast({ type: "code_red_result", result: "failed" });
                    broadcast({ type: "notification", message: "⚠️ SUBJECT TERMINATED ⚠️" });
                } else if (data.event_id === 'PlayerSpawned') {
                    // Unity can include optional sectionId/text
                    startRepairTask(data.sectionId, data.text);
                    broadcast({ type: "notification", message: "PLAYER SPAWNED → Repair task assigned." });
                } else if (data.event_id === 'RepairCompleted') {
                    // Clear the repair task
                    vrMessage = "";
                    Object.values(sections).forEach(s => { s.lightsOn = false; });
                    broadcast({ type: "notification", message: "REPAIR COMPLETED" });
                    broadcastMapState();
                    broadcastMessageState();
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