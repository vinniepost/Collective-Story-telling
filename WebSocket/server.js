const express = require('express');
const http = require('http');
const WebSocket = require('ws');
const { v4: uuidv4 } = require('uuid');
const path = require('path');
const app = express();
const PORT = process.env.PORT || 3000;

app.use(express.static(path.join(__dirname, 'client/dist/client/browser')));

// Catch-all for Angular routing
app.get('*', (req, res) => {
  res.sendFile(path.join(__dirname, 'client/dist/client/browser/index.html'));
});

const server = http.createServer(app);
server.listen(PORT, () => {
    console.log(`HTTP server running on http://localhost:${PORT}`);
});

const wss = new WebSocket.Server({ server });

const availableActions = ["close_door", "light", "sound_1", "sound_2"];
let uniqueActionVotes = {};
let clientVotes = new Map();
let playerInSecretArea = false;

// Operator Chat System
let nextOperatorId = 1;
let chatHistory = []; // Store last 50 messages

// Message System State
let vrMessage = "";
let messageOptions = []; // ["Option A", "Option B", "Option C"]
let messageVotes = {}; // { "Option A": 5 }
let clientMessageVotes = new Map(); // ClientID -> Set<Option>

availableActions.forEach(action => uniqueActionVotes[action] = 0);
console.log('Available Actions Initialized.');

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

function resetVotingCycle() {
    clientVotes.clear();
    availableActions.forEach(action => uniqueActionVotes[action] = 0);

    broadcastStateUpdate();
    console.log("Voting cycle reset by server.");
}

function resetMessageVoting() {
    messageVotes = {};
    messageOptions.forEach(opt => messageVotes[opt] = 0);
    clientMessageVotes.clear();
    broadcastMessageState();
}

function broadcastStateUpdate() {
    broadcast({
        type: "update",
        votes: uniqueActionVotes,
        totalClients: getClientCount(),
        playerInArea: playerInSecretArea
    });
}

function broadcastMessageState() {
    broadcast({
        type: "message_state",
        vrMessage: vrMessage,
        options: messageOptions,
        votes: messageVotes
    });
}

function checkThreshold(action, currentVotes, totalClients) {
    // "half of the connected clients - 1(the vr player doesn't need to be counted)"
    const audienceCount = Math.max(0, totalClients - 1);
    const requiredVotes = audienceCount === 0 ? 1 : Math.ceil(audienceCount / 2);
    return currentVotes >= requiredVotes && totalClients > 0;
}

function checkMessageThreshold(currentVotes, totalClients) {
    // "a third of the votes to be sent"
    const audienceCount = Math.max(0, totalClients - 1);
    const requiredVotes = audienceCount === 0 ? 1 : Math.ceil(audienceCount / 3);
    return currentVotes >= requiredVotes && totalClients > 0;
}

function handleVote(ws, action) {
    const clientId = ws.id;
    const totalClients = getClientCount();

    if (clientVotes.has(clientId)) {
        console.log(`Client ${clientId} already voted.`);
        ws.send(JSON.stringify({ type: "feedback", message: "You have already cast your vote.", votedAction: clientVotes.get(clientId) }));
        return;
    }

    clientVotes.set(clientId, action);
    uniqueActionVotes[action]++;
    const newCount = uniqueActionVotes[action];

    broadcastStateUpdate();

    if (checkThreshold(action, newCount, totalClients)) {
        console.log(`THRESHOLD REACHED for ${action}! Total Clients: ${totalClients}, Votes: ${newCount}`);

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
        // Toggle vote off? Or just ignore? Let's assume ignore for now, or toggle.
        // "people can vote for multiple messages"
        // Let's implement toggle for better UX
        userVotes.delete(option);
        messageVotes[option]--;
    } else {
        userVotes.add(option);
        messageVotes[option]++;
    }

    const newCount = messageVotes[option];
    broadcastMessageState();

    if (checkMessageThreshold(newCount, totalClients)) {
        console.log(`MESSAGE THRESHOLD REACHED for "${option}"!`);
        
        // Send to VR Player (and everyone else as notification)
        broadcast({ type: "vr_message_sent", message: option });
        
        // Reset options (hide them)
        messageOptions = [];
        resetMessageVoting();
    }
}

wss.on('connection', ws => {
    ws.id = uuidv4();
    
    // Assign Operator ID
    const operatorId = String(nextOperatorId).padStart(3, '0');
    ws.username = `Operator-${operatorId}`;
    nextOperatorId++;

    console.log(`Client connected (${ws.id}) as ${ws.username}. Total clients: ${getClientCount()}`);

    // Send assigned username to client
    ws.send(JSON.stringify({
        type: "assign_username",
        username: ws.username
    }));

    // Send chat history
    ws.send(JSON.stringify({
        type: "chat_history",
        messages: chatHistory
    }));

    ws.send(JSON.stringify({
        type: "update",
        votes: uniqueActionVotes,
        totalClients: getClientCount(),
        playerInArea: playerInSecretArea
    }));
    
    // Send initial message state
    ws.send(JSON.stringify({
        type: "message_state",
        vrMessage: vrMessage,
        options: messageOptions,
        votes: messageVotes
    }));

    broadcast({ type: "client_count", count: getClientCount() });

    ws.on('message', message => {
        try {
            const data = JSON.parse(message);

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
                if (chatHistory.length > 50) chatHistory.shift(); // Keep last 50

                broadcast({
                    type: "chat_message",
                    message: chatMsg
                });
            }

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
                }
                return;
            }
            
            // Handle VR Player sending a message to the audience
            if (data.type === 'vr_message') {
                vrMessage = data.message;
                broadcastMessageState();
                broadcast({ type: "notification", message: `INCOMING MESSAGE: ${data.message}` });
            }

            // Handle VR Player starting a vote
            if (data.type === 'start_message_vote') {
                messageOptions = data.options; // Expecting array of strings
                resetMessageVoting();
                broadcast({ type: "notification", message: "NEW RESPONSE OPTIONS AVAILABLE" });
            }

            if (data.type === 'request_state') {
                broadcastStateUpdate();
                ws.send(JSON.stringify({
                    type: "message_state",
                    vrMessage: vrMessage,
                    options: messageOptions,
                    votes: messageVotes
                }));
            }

            // Allow Unity/Admin to send direct logs to terminals
            if (data.type === 'broadcast_log') {
                broadcast({ type: "notification", message: data.message });
            }

        } catch (e) {
            console.error('Error parsing message:', e);
        }
    });

    ws.on('close', () => {
        const votedAction = clientVotes.get(ws.id);
        if (votedAction) {
            uniqueActionVotes[votedAction]--;
            clientVotes.delete(ws.id);
        }
        
        // Clean up message votes
        if (clientMessageVotes.has(ws.id)) {
            const userVotes = clientMessageVotes.get(ws.id);
            userVotes.forEach(opt => {
                if (messageVotes[opt] > 0) messageVotes[opt]--;
            });
            clientMessageVotes.delete(ws.id);
            broadcastMessageState();
        }

        console.log(`Client disconnected (${ws.id}). Total clients: ${getClientCount()}`);
        broadcast({ type: "client_count", count: getClientCount() });
        broadcastStateUpdate(); // Ensure map votes are updated
    });
});