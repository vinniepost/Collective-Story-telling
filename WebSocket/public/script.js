let socket;
const ReconnectDelay = 3000;
let hasVoted = false;

function connectWebSocket() {
    if (socket && (socket.readyState === WebSocket.OPEN || socket.readyState === WebSocket.CONNECTING)) {
        return;
    }

    const url = 'wss://' + window.location.host;
    socket = new WebSocket(url);

    const optionsContainer = document.getElementById('options-container');
    const resultsContainer = document.getElementById('results-container');
    const networkStatusElement = document.getElementById('network-status');
    const viewerCountElement = document.getElementById('viewer-count');
    const thresholdTextElement = document.getElementById('threshold-text');
    const specificAreaButtons = document.querySelectorAll('.specific-area-button');

    function setVotingState(isDisabled) {
        hasVoted = isDisabled;
        const buttons = optionsContainer.querySelectorAll('.action-button');
        buttons.forEach(btn => {
            btn.disabled = isDisabled;
            btn.style.opacity = isDisabled ? '0.5' : '1.0';
        });
    }

    function toggleSpecificButtons(isVisible) {
        const displayStyle = isVisible ? 'inline-block' : 'none';
        specificAreaButtons.forEach(btn => {
            btn.style.display = displayStyle;
        });
    }

    function initializeButtons() {
        const buttons = optionsContainer.querySelectorAll('.action-button');
        buttons.forEach(btn => {
            btn.onclick = null;

            btn.onclick = () => {
                if (hasVoted) return;

                const action = btn.getAttribute('data-action');
                if (socket.readyState === WebSocket.OPEN) {
                    socket.send(JSON.stringify({ type: 'vote', option: action }));
                    setVotingState(true);
                }
            };
        });
    }

    function displayResults(votes, totalClients) {
        const requiredVotes = Math.ceil(totalClients * 0.50);

        resultsContainer.innerHTML = '<h4>LIVE TALLIES:</h4>';

        if (thresholdTextElement) {
            thresholdTextElement.textContent = `(50% Majority: ${requiredVotes} votes required / ${totalClients} clients)`;
        }

        for (const option in votes) {
            const count = votes[option];
            const p = document.createElement('p');

            let color = '#00ff00';
            if (count >= requiredVotes && totalClients > 0) {
                color = '#ff0000';
            } else if (count > 0 && count >= requiredVotes * 0.75) {
                color = '#ffaa00';
            } else {
                color = '#00ff00';
            }

            p.style.fontWeight = 'bold';
            p.style.color = color;
            p.textContent = `${option.toUpperCase().replace('_', ' ')}: ${count} VOTES`;
            resultsContainer.appendChild(p);
        }
    }

    socket.onopen = () => {
        networkStatusElement.innerHTML = 'Network: <span style="color: #00ff00;">STABLE</span>';
        socket.send(JSON.stringify({ type: "request_state" }));
        initializeButtons();
        toggleSpecificButtons(false);
    };

    socket.onclose = () => {
        networkStatusElement.innerHTML = 'Network: <span style="color: #ff0000;">RECONNECTING...</span>';
        setTimeout(connectWebSocket, ReconnectDelay);
    };

    socket.onmessage = (event) => {
        const data = JSON.parse(event.data);

        if (data.type === 'update') {
            displayResults(data.votes, data.totalClients);
            if (data.playerInArea !== undefined) {
                toggleSpecificButtons(data.playerInArea);
            }

            const totalVotes = Object.values(data.votes).reduce((a, b) => a + b, 0);
            if (totalVotes === 0 && hasVoted) {
                setVotingState(false);
            }

        } else if (data.type === 'client_count') {
            if (viewerCountElement) {
                viewerCountElement.textContent = data.count;
            }
        } else if (data.type === 'action') {
            console.warn(`ACTION TRIGGERED BY SERVER: ${data.command}`);
        } else if (data.type === 'feedback') {
            console.warn(`VOTE REJECTED: ${data.message}`);
            setVotingState(true);
        }
    };
}

window.onload = connectWebSocket;