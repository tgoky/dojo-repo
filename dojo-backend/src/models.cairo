use starknet::{ContractAddress};

// === Whack-a-Mole specific models ===

#[derive(Copy, Drop, Serde, Debug)]
#[dojo::model]
pub struct PlayerScore {
    #[key]
    pub player: ContractAddress,
    pub score: u32,
    pub high_score: u32,       // Player's highest score
}

#[derive(Copy, Drop, Serde, Debug)]
#[dojo::model]
pub struct GameSession {
    #[key]
    pub player: ContractAddress,
    pub start_time: u64,       // Block timestamp when game started
    pub remaining_time: u32,   // Remaining time in seconds (scaled by 1000)
    pub active: bool,          // Whether game is still active
    pub score: u32,            // Current score in this session
    pub last_update: u64,      // Last time the game state was updated
}
