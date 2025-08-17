use dojo_starter::models::*;

// define the interface
#[starknet::interface]
pub trait IActions<T> {
    // Game session management
    fn start_game(ref self: T);
    fn hit_mole(ref self: T, points: u32);
    fn miss_mole(ref self: T, is_mole: u8);
    fn update_frame(ref self: T, remaining_time: u32);
    fn game_over(ref self: T, score: u32, reason: u8);
}

// dojo decorator
#[dojo::contract]
pub mod actions {
    use super::IActions;
    use starknet::{ContractAddress, get_caller_address, get_block_timestamp};
    use dojo_starter::models::{PlayerScore, GameSession};

    use dojo::model::{ModelStorage};
    use dojo::event::{EventStorage};

    #[derive(Copy, Drop, Serde)]
    #[dojo::event]
    pub struct GameStarted {
        #[key]
        pub player: ContractAddress,
        pub timestamp: u64,
    }
    
    #[derive(Copy, Drop, Serde)]
    #[dojo::event]
    pub struct MoleHit {
        #[key]
        pub player: ContractAddress,
        pub points: u32,
        pub score: u32,
    }
    
    #[derive(Copy, Drop, Serde)]
    #[dojo::event]
    pub struct MoleMissed {
        #[key]
        pub player: ContractAddress,
        pub is_mole: bool,
    }
    
    #[derive(Copy, Drop, Serde)]
    #[dojo::event]
    pub struct GameTimeUpdated {
        #[key]
        pub player: ContractAddress,
        pub remaining: u32,
    }
    
    #[derive(Copy, Drop, Serde)]
    #[dojo::event]
    pub struct GameEnded {
        #[key]
        pub player: ContractAddress,
        pub score: u32,
        pub reason: u8, // 0: time expired, 1: bomb hit
    }

    #[abi(embed_v0)]
    impl ActionsImpl of IActions<ContractState> {
        /// Start a new game session
        fn start_game(ref self: ContractState) {
            let mut world = self.world_default();
            let player = get_caller_address();
            let now = get_block_timestamp();
            
            // Try to read existing score to preserve high score
            let existing_score: PlayerScore = world.read_model(player);
            let high_score = if existing_score.player == player {
                existing_score.high_score
            } else {
                0_u32
            };
            
            // Create or reset game session
            let session = GameSession {
                player,
                start_time: now,
                remaining_time: 30000, // 30 seconds, scaled by 1000
                active: true,
                score: 0,
                last_update: now,
            };
            world.write_model(@session);
            
            // Reset player score with preserved high score
            let player_score = PlayerScore {
                player, 
                score: 0,
                high_score,
            };
            
            world.write_model(@player_score);
            
            // Emit event
            world.emit_event(@GameStarted { player, timestamp: now });
        }
        
        /// Handle successful mole hit (per Unity GameManager.AddScore)
        fn hit_mole(
            ref self: ContractState,
            points: u32
        ) {
            let mut world = self.world_default();
            let player = get_caller_address();
            let now = get_block_timestamp();
            
            // Get current session
            let mut session: GameSession = world.read_model(player);
            
            // Only process if session is active
            if !session.active {
                return;
            }
            
            // Update score in session
            session.score += points;
            session.remaining_time += 1000; // Add 1 second (scaled by 1000)
            session.last_update = now;
            world.write_model(@session);
            
            // Update player high score if needed
            let mut player_score: PlayerScore = world.read_model(player);
            player_score.score = session.score; // Keep in sync
            if player_score.score > player_score.high_score {
                player_score.high_score = player_score.score;
            }
            world.write_model(@player_score);
            
            // Emit event
            world.emit_event(@MoleHit { player, points, score: session.score });
        }
        
        /// Handle mole miss (per Unity GameManager.Missed)
        fn miss_mole(
            ref self: ContractState,
            is_mole: u8
        ) {
            let mut world = self.world_default();
            let player = get_caller_address();
            let now = get_block_timestamp();
            
            // Get current session
            let mut session: GameSession = world.read_model(player);
            
            // Only process if session is active
            if !session.active {
                return;
            }
            
            // Penalize time if it was actually a mole (is_mole = 1)
            if is_mole == 1 {
                // Subtract 2 seconds (scaled by 1000)
                if session.remaining_time > 2000 {
                    session.remaining_time -= 2000;
                } else {
                    session.remaining_time = 0;
                    // Could trigger game over here, but let the regular update handle it
                }
            }
            
            session.last_update = now;
            world.write_model(@session);
            
            // Emit event
            let is_mole_bool = is_mole == 1;
            world.emit_event(@MoleMissed { player, is_mole: is_mole_bool });
        }
        
        /// Update frame time (per Unity GameManager.Update)
        fn update_frame(
            ref self: ContractState,
            remaining_time: u32 // Scaled by 1000 to handle fractions
        ) {
            let mut world = self.world_default();
            let player = get_caller_address();
            let now = get_block_timestamp();
            
            // Get current session
            let mut session: GameSession = world.read_model(player);
            
            // Only update if session is active
            if !session.active {
                return;
            }
            
            // Update remaining time
            session.remaining_time = remaining_time;
            session.last_update = now;
            world.write_model(@session);
            
            // Auto game over if time ran out
            if remaining_time == 0 {
                self.game_over(session.score, 0);
                return;
            }
            
            // Emit event
            world.emit_event(@GameTimeUpdated { player, remaining: remaining_time });
        }
        
        /// End a game session
        fn game_over(
            ref self: ContractState,
            score: u32,
            reason: u8 // 0: time expired, 1: bomb hit
        ) {
            let mut world = self.world_default();
            let player = get_caller_address();
            let now = get_block_timestamp();
            
            // Get current session
            let mut session: GameSession = world.read_model(player);
            
            // Only process if session is active
            if !session.active {
                return;
            }
            
            // Mark session as ended
            session.active = false;
            session.last_update = now;
            session.score = score; // Use final score from client
            world.write_model(@session);
            
            // Update player high score if needed
            let mut player_score: PlayerScore = world.read_model(player);
            player_score.score = score;
            if score > player_score.high_score {
                player_score.high_score = score;
                world.write_model(@player_score);
            }
            
            // Emit event
            world.emit_event(@GameEnded { player, score, reason });
        }
    }

    #[generate_trait]
    impl InternalImpl of InternalTrait {
        /// Use the default namespace "dojo_starter". This function is handy since the ByteArray
        /// can't be const.
        fn world_default(self: @ContractState) -> dojo::world::WorldStorage {
            self.world(@"dojo_starter")
        }
    }
}
