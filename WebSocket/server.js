const express = require('express');
const http = require('http');
const WebSocket = require('ws');
const { v4: uuidv4 } = require('uuid');
const app = express();
const PORT = process.env.PORT || 3000;

app.use(express.static('public'));

const server = http.createServer(app);
server.listen(PORT, () => {
    console.log(`HTTP server running on http://localhost:${PORT}`);
});

const wss = new WebSocket.Server({ server });

const availableActions = ["close_door", "light", "sound_1", "sound_2"];
let uniqueActionVotes = {};
let clientVotes = new Map();
let playerInSecretArea = false;

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

function broadcastStateUpdate() {
    broadcast({
        type: "update",
        votes: uniqueActionVotes,
        totalClients: getClientCount(),
        playerInArea: playerInSecretArea
    });
}

function checkThreshold(action, currentVotes, totalClients) {
    const requiredVotes = Math.ceil(totalClients * 0.50);
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

wss.on('connection', ws => {
    ws.id = uuidv4();
    console.log(`Client connected (${ws.id}). Total clients: ${getClientCount()}`);

    ws.send(JSON.stringify({
        type: "update",
        votes: uniqueActionVotes,
        totalClients: getClientCount(),
        playerInArea: playerInSecretArea
    }));

    broadcast({ type: "client_count", count: getClientCount() });

    ws.on('message', message => {
        try {
            const data = JSON.parse(message);

            if (data.type === 'vote') {
                handleVote(ws, data.option);
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

            if (data.type === 'request_state') {
                broadcastStateUpdate();
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
            broadcastStateUpdate();
        }

        console.log(`Client disconnected (${ws.id}). Total clients: ${getClientCount()}`);
        broadcast({ type: "client_count", count: getClientCount() });
    });
});