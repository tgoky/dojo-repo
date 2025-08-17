#!/usr/bin/env node

/**
 * Player Scores Viewer
 * 
 * This script fetches player scores from the GraphQL endpoint and displays them
 * in a formatted, human-readable way. It doesn't require any external dependencies.
 */

// Function to fetch player scores from GraphQL endpoint
async function fetchPlayerScores() {
    const graphqlEndpoint = 'http://0.0.0.0:8080/graphql';
    const query = `
    query GetPlayerScores {
      entities(keys: ["*"], first: 10) {
        edges {
          node {
            keys
            models {
              ... on dojo_starter_PlayerScore {
                player
                score
                high_score
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

// Process and display player scores
async function processPlayerScores() {
    console.log('Fetching player scores from GraphQL endpoint...');
    const data = await fetchPlayerScores();
    
    if (!data || !data.data) {
        console.log('No data received or server is not reachable. Using sample data instead...');
        // Use sample data as fallback
        processSampleData();
        return;
    }
    
    const edges = data.data.entities.edges;
    const scores = edges
        .map(edge => {
            // Find the PlayerScore model in the models array
            for (const model of edge.node.models) {
                if (model && model.player && 'high_score' in model) {
                    return model;
                }
            }
            return null;
        })
        .filter(score => score !== null);
    
    if (scores.length === 0) {
        console.log('\nNo player scores found in the database.');
        processSampleData();
        return;
    }
    
    console.log(`\nFound ${scores.length} player scores:\n`);
    
    // Sort scores by high_score (descending)
    const sortedScores = [...scores].sort((a, b) => b.high_score - a.high_score);
    
    // Display as leaderboard
    console.log('======== LEADERBOARD ========');
    console.log('Rank | Player Address                                            | Score | High Score');
    console.log('---------------------------------------------------------------------------------');
    
    sortedScores.forEach((score, index) => {
        // Format player address to fit nicely in the table
        const shortenedAddress = shortenAddress(score.player);
        
        console.log(`${(index + 1).toString().padStart(4)} | ${shortenedAddress} | ${score.score.toString().padStart(5)} | ${score.high_score.toString().padStart(9)}`);
    });
    
    console.log('======== END OF LEADERBOARD ========\n');
    
    // Display detailed view
    console.log('======== DETAILED PLAYER SCORES ========');
    sortedScores.forEach((score, index) => {
        console.log(`\n==== Player ${index + 1} ====`);
        console.log(`Address: ${score.player}`);
        console.log(`Current Score: ${score.score}`);
        console.log(`High Score: ${score.high_score}`);
        console.log('====================');
    });
}

// Helper to shorten address for display purposes
function shortenAddress(address) {
    if (!address) return 'Unknown Address';
    
    if (address.length > 20) {
        return `${address.substring(0, 10)}...${address.substring(address.length - 8)}`;
    }
    
    return address;
}

// Process sample data if GraphQL endpoint is not available
function processSampleData() {
    const sampleScores = [
        {
            "player": "0x127fd5f1fe78a71f8bcd1fec63e3fe2f0486b6ecd5c86a0466c3a21fa5cfcec",
            "score": 35,
            "high_score": 120
        },
        {
            "player": "0x72fc9ebfa41b327fc50a66073428b9153ab39252b312f7d62919c2b83d16664",
            "score": 93,
            "high_score": 93
        },
        {
            "player": "0x1fa6f3c05a1653c38d1a81d181e8669f49dd5f7f3c6fab877837401bfc9d1fa2",
            "score": 0,
            "high_score": 45
        }
    ];
    
    console.log('\n======== SAMPLE LEADERBOARD ========');
    console.log('Rank | Player Address                                            | Score | High Score');
    console.log('---------------------------------------------------------------------------------');
    
    sampleScores
        .sort((a, b) => b.high_score - a.high_score)
        .forEach((score, index) => {
            const shortenedAddress = shortenAddress(score.player);
            console.log(`${(index + 1).toString().padStart(4)} | ${shortenedAddress} | ${score.score.toString().padStart(5)} | ${score.high_score.toString().padStart(9)}`);
        });
    
    console.log('======== END OF SAMPLE LEADERBOARD ========');
    
    console.log('\n======== DETAILED SAMPLE PLAYER SCORES ========');
    sampleScores.forEach((score, index) => {
        console.log(`\n==== Player ${index + 1} ====`);
        console.log(`Address: ${score.player}`);
        console.log(`Current Score: ${score.score}`);
        console.log(`High Score: ${score.high_score}`);
        console.log('====================');
    });
}

// Run the script
processPlayerScores();
