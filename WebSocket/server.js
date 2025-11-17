const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 8080 });

const rounds = [
    { name: "weather", options: ["sunny", "rain"], duration: 40 }, // seconds
    { name: "lights", options: ["light1", "light2"], duration: 40 } // seconds
];

let currentRound = 0;
let votes = {};
let roundTimer = null;

function startRound(roundIndex) {
    if (roundIndex >= rounds.length) {
        console.log("All rounds finished!");
        broadcast({ type: "voting_end" });
        return;
    }

    currentRound = roundIndex;
    votes = {};
    rounds[currentRound].options.forEach(opt => votes[opt] = 0);

    console.log(`Starting round ${currentRound}: ${rounds[currentRound].name}`);
    broadcast({ type: "round_start", round: rounds[currentRound] });

    // Start automatic timer for this round
    if (roundTimer) clearTimeout(roundTimer);
    roundTimer = setTimeout(() => {
        console.log(`Round ${rounds[currentRound].name} ended automatically.`);
        startRound(currentRound + 1);
    }, rounds[currentRound].duration * 1000);
}

function broadcast(obj) {
    const msg = JSON.stringify(obj);
    wss.clients.forEach(client => {
        if (client.readyState === WebSocket.OPEN) {
            client.send(msg);
        }
    });
}

// Start first round
startRound(0);

wss.on('connection', ws => {
    console.log('Client connected');

    // Send current round info and votes
    ws.send(JSON.stringify({ type: "round_start", round: rounds[currentRound] }));
    ws.send(JSON.stringify({ type: "update", votes, round: rounds[currentRound] }));

    ws.on('message', message => {
        try {
            const data = JSON.parse(message);

            if (data.type === 'vote') {
                if (votes[data.option] !== undefined) {
                    votes[data.option]++;
                    broadcast({ type: "update", votes, round: rounds[currentRound] });
                }
            }

            if (data.type === "player_position") {
                broadcast({
                    type: "player_position",
                    x: data.x,
                    y: data.y,
                    z: data.z
                });
            }

        } catch (e) {
            console.error('Error parsing message:', e);
        }
    });

    ws.on('close', () => console.log('Client disconnected'));
});

console.log('WebSocket server running on ws://localhost:8080');
