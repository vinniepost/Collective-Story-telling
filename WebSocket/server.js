// Simple WebSocket server using ws
const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 8080 });

let votes = { sunny: 0, rain: 0 };

wss.on('connection', ws => {
    console.log("Client connected.");

    // Send initial vote state
    ws.send(JSON.stringify({
        type: "update",
        votes
    }));

    ws.on('message', msg => {
        let data;

        try {
            data = JSON.parse(msg);
        } catch (e) {
            console.error("Invalid JSON received:", msg);
            return;
        }

        // -----------------------
        // Handle Votes
        // -----------------------
        if (data.type === "vote") {
            if (votes[data.option] !== undefined) {
                votes[data.option]++;

                console.log(`Vote received: ${data.option}`);
                console.log("New totals:", votes);

                broadcast({
                    type: "update",
                    votes
                });
            } else {
                console.log("Invalid vote option:", data.option);
            }
        }

        // -----------------------
        // Handle Player Position
        // -----------------------
        if (data.type === "player_position") {
            // Log position for debugging
            console.log(`Player position: x=${data.x}, y=${data.y}, z=${data.z}`);

            // Send to all OTHER clients (not back to Unity)
            broadcastToOthers(ws, {
                type: "player_position",
                x: data.x,
                y: data.y,
                z: data.z
            });
        }
    });

    ws.on('close', () => console.log("Client disconnected."));
});

// ----------------------------------
// Broadcast to ALL clients
// ----------------------------------
function broadcast(obj) {
    const msg = JSON.stringify(obj);

    wss.clients.forEach(client => {
        if (client.readyState === WebSocket.OPEN) {
            client.send(msg);
        }
    });
}

// ----------------------------------
// Broadcast to all EXCEPT the sender
// ----------------------------------
function broadcastToOthers(sender, obj) {
    const msg = JSON.stringify(obj);

    wss.clients.forEach(client => {
        if (client !== sender && client.readyState === WebSocket.OPEN) {
            client.send(msg);
        }
    });
}

console.log("WebSocket server running on ws://localhost:8080");
