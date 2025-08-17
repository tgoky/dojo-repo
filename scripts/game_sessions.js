#!/usr/bin/env node

/**
 * Game Session Time Converter
 * 
 * This script fetches game sessions from the GraphQL endpoint and converts timestamp values
 * to human-readable dates. It doesn't require any external dependencies.
 */

// Function to convert hex string timestamp to human readable date
function hexToDate(hexTimestamp) {
    if (!hexTimestamp) return { error: 'Missing timestamp' };
    
    // Remove '0x' prefix if present and convert to decimal
    const decimal = parseInt(hexTimestamp.replace(/^0x/, ''), 16);
    
    // Create a date object from the Unix timestamp (seconds)
    const date = new Date(decimal * 1000);
    
    // Format the date in a readable way
    return {
        unixTimestamp: decimal,
        iso: date.toISOString(),
        localTime: date.toLocaleString(),
        relative: getRelativeTimeString(date)
    };
}

// Helper to show a relative time string like "2 minutes ago"
function getRelativeTimeString(date) {
    const now = new Date();
    const diffSeconds = Math.floor((now - date) / 1000);
    
    if (diffSeconds < 0) return 'in the future';
    if (diffSeconds < 60) return `${diffSeconds} seconds ago`;
    if (diffSeconds < 3600) return `${Math.floor(diffSeconds/60)} minutes ago`;
    if (diffSeconds < 86400) return `${Math.floor(diffSeconds/3600)} hours ago`;
    return `${Math.floor(diffSeconds/86400)} days ago`;
}

// Format session duration in a human-friendly way
function formatDuration(seconds) {
    if (seconds < 60) return `${seconds} seconds`;
    if (seconds < 3600) return `${Math.floor(seconds/60)} minutes ${seconds%60} seconds`;
    const hours = Math.floor(seconds/3600);
    const minutes = Math.floor((seconds%3600)/60);
    return `${hours} hours ${minutes} minutes`;
}

// Function to fetch game sessions from GraphQL endpoint
async function fetchGameSessions() {
    const graphqlEndpoint = 'http://0.0.0.0:8080/graphql';
    const query = `
    query GetGameSessions {
      entities(keys: ["*"], first: 10) {
        edges {
          node {
            keys
            models {
              ... on dojo_starter_GameSession {
                player
                start_time
                remaining_time
                active
                score
                last_update
              }
            }
          }
        }
      }
    }
    `;

    try {
        const response = await fetch(graphqlEndpoint, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify({ query })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }

        const data = await response.json();
        return data;
    } catch (error) {
        console.error('Failed to fetch data:', error);
        return null;
    }
}

// Process and display game sessions
async function processGameSessions() {
    console.log('Fetching game sessions from GraphQL endpoint...');
    const data = await fetchGameSessions();
    
    if (!data || !data.data) {
        console.log('No data received or server is not reachable. Using sample data instead...');
        // Use sample data as fallback
        processSampleData();
        return;
    }
    
    const edges = data.data.entities.edges;
    console.log(`\nFound ${edges.length} game sessions:\n`);
    
    edges.forEach((edge, index) => {
        // The game session appears to be in the second item of the models array (index 1)
        // and doesn't have a __typename property
        const session = edge.node.models[1];
        if (!session || !session.player) return;
        
        console.log(`\n==== Game Session ${index + 1} ====`);
        console.log(`Player: ${session.player}`);
        console.log('Score:', session.score);
        console.log('Active:', session.active);
        
        // Process time data
        const startTime = hexToDate(session.start_time);
        const lastUpdate = hexToDate(session.last_update);
        
        console.log('\nStart time:', startTime.localTime, `(${startTime.relative})`);
        console.log('Last update:', lastUpdate.localTime, `(${lastUpdate.relative})`);
        
        // Calculate session duration
        if (session.start_time && session.last_update) {
            const startTimestamp = parseInt(session.start_time.replace(/^0x/, ''), 16);
            const lastUpdateTimestamp = parseInt(session.last_update.replace(/^0x/, ''), 16);
            const duration = lastUpdateTimestamp - startTimestamp;
            console.log('Session duration:', formatDuration(duration));
        }
        
        console.log('====================');
    });
}

// Process sample data if GraphQL endpoint is not available
function processSampleData() {
    const gameSession = {
        "player": "0x127fd5f1fe78a71f8bcd1fec63e3fe2f0486b6ecd5c86a0466c3a21fa5cfcec",
        "start_time": "0x68516b39",
        "remaining_time": 0,
        "active": true,
        "score": 9,
        "last_update": "0x68516b56"
    };
    
    console.log('\n==== Sample Game Session ====');
    console.log(`Player: ${gameSession.player}`);
    console.log('Score:', gameSession.score);
    console.log('Active:', gameSession.active);
    
    const startTime = hexToDate(gameSession.start_time);
    const lastUpdate = hexToDate(gameSession.last_update);
    
    console.log('\nStart time:', startTime.localTime, `(${startTime.relative})`);
    console.log('Last update:', lastUpdate.localTime, `(${lastUpdate.relative})`);
    
    const startTimestamp = parseInt(gameSession.start_time.replace(/^0x/, ''), 16);
    const lastUpdateTimestamp = parseInt(gameSession.last_update.replace(/^0x/, ''), 16);
    const duration = lastUpdateTimestamp - startTimestamp;
    console.log('Session duration:', formatDuration(duration));
    console.log('====================');
}

// Run the script
processGameSessions();