using System;
using System.Collections.Generic;
using System.Linq;
using GamePlay.Cards;
using Random = System.Random;

namespace GamePlay.Bot
{
    public static class GameSimulator
    {
        public static Random Random = new();
        
        public static SimulatedGameState DeterminizeGame(GameState gameState)
        {
            GameLogger.ShowLog("🧠 Starting determinization...");

            // Step 1: Full deck (24 cards)
            var allCards = GenerateFullDeck();
            var knownCards = new HashSet<CardData>();

            // Step 2: Track known cards
            foreach (var card in gameState.Hand)
                knownCards.Add(card);

            foreach (var card in gameState.PlayedCards)
                knownCards.Add(card);

            foreach (var card in gameState.CurrentTrickCards.Values)
                knownCards.Add(card);
            
            foreach (var card in gameState.Kitty)
                knownCards.Add(card);

            // Step 3: Remaining unknown cards to be assigned randomly
            var unknownCards = allCards
                .Where(card => !knownCards.Contains(card))
                .OrderBy(_ => Random.Next())
                .ToList();

            Dictionary<int, List<CardData>> hands = new();

            foreach (var player in gameState.AllPlayers)
            {
                var playerIndex = player.PlayerIndex;

                // Start with empty simulated hand
                hands[playerIndex] = new List<CardData>();

                if (playerIndex == gameState.BotPlayerIndex)
                {
                    hands[playerIndex].AddRange(gameState.Hand);
                    GameLogger.ShowLog($"🃏 Player {playerIndex} has {hands[playerIndex].Count} cards : [{string.Join(", ", hands[playerIndex].Select(c => c.ToString()))}]");
                    continue;
                }

                // Count how many cards already known for this player
                int alreadyPlayed = player.CardsPlayed.Count;
                // if (gameState.CurrentTrickCards.ContainsKey(playerIndex))
                //     alreadyPlayed++;

                int cardsNeeded = 5 - alreadyPlayed;

                // Safety check
                if (unknownCards.Count < cardsNeeded)
                {
                    GameLogger.ShowLog(
                        $"⚠️ Not enough unknown cards left to assign to player {playerIndex}. Assigning {unknownCards.Count} instead of {cardsNeeded}",
                        GameLogger.LogType.Warning
                    );
                    cardsNeeded = unknownCards.Count;
                }

                var assigned = unknownCards.Take(cardsNeeded).ToList();
                unknownCards.RemoveRange(0, cardsNeeded);

                hands[playerIndex].AddRange(assigned);

                GameLogger.ShowLog($"🃏 Player {playerIndex} has {hands[playerIndex].Count} cards : [{string.Join(", ", hands[playerIndex].Select(c => c.ToString()))}]");
            }
            
            // Final sanity check
            int totalAssigned = hands.Values.Sum(h => h.Count) +       //3 + 4 + 4 + 4 = 15
                                gameState.PlayedCards.Count +                       //5
                                gameState.Kitty.Count;                              //4

            if (totalAssigned != 24)
            {
                GameLogger.ShowLog(
                    $"🚨 Invalid total cards in simulation! Expected 24, got {totalAssigned}.",
                    GameLogger.LogType.Error
                );
            }
            else
            {
                GameLogger.ShowLog("✅ All cards accounted for in simulation (24/24)");
            }

            return new SimulatedGameState
            {
                Hands = hands,
                Trick = new Dictionary<int, CardData>(gameState.CurrentTrickCards),
                CurrentPlayer = gameState.BotPlayerIndex,
                LeadSuit = gameState.TrickSuit
            };
        }

        private static List<CardData> GenerateFullDeck()
        {
            var deck = new List<CardData>();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                if (suit == Suit.None) continue;
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    deck.Add(new CardData(suit, rank));
                }
            }

            GameLogger.ShowLog($"Generated full Euchre deck with {deck.Count} cards");
            return deck;
        }

        public static void ApplyMove(SimulatedGameState state, CardData move)
        {
            if (!state.Hands[state.CurrentPlayer].Contains(move))
            {
                GameLogger.ShowLog($"Error: Player {state.CurrentPlayer} attempted to play a card not in hand: {move}", GameLogger.LogType.Error);
                return;
            }

            if (!state.Trick.TryAdd(state.CurrentPlayer, move))
            {
                GameLogger.ShowLog($"Player {state.CurrentPlayer} has already played the card. SO can not play move : {move}", GameLogger.LogType.Error);
                return;
            }
            
            state.Hands[state.CurrentPlayer].Remove(move);
            if (state.LeadSuit is Suit.None)
                state.LeadSuit = move.suit;

            GameLogger.ShowLog($"Player {state.CurrentPlayer} played {move}"); 
            state.CurrentPlayer = (state.CurrentPlayer + 1) % 4;
        }

        public static float SimulatePlayout(SimulatedGameState simState, Suit trumpSuit, int botTeam)
        {
            GameLogger.ShowLog($"Starting simulated playout for team {botTeam} with trump suit {trumpSuit}");

            int[] teamScores = new int[2];

            while (PlayersHaveCards(simState))
            {
                var trickCards = new Dictionary<int, CardData>(simState.Trick);
                int startPlayer = simState.CurrentPlayer;

                // If trick is already partially played, make sure we only let remaining players play
                for (int i = 0; i < 4; i++)
                {
                    int player = (startPlayer + i) % 4;
                    
                    if (trickCards.ContainsKey(player))
                        continue;
                    
                    var hand = simState.Hands[player];
                    if (hand.Count == 0)
                    {
                        GameLogger.ShowLog($"Warning: Player {player} has no cards left during playout", GameLogger.LogType.Warning);
                        continue;
                    }
                    
                    var playable = GetValidMoves(hand, simState.LeadSuit, trumpSuit);

                    var card = playable.OrderByDescending(c => c.GetCardPower(trumpSuit, simState.LeadSuit)).First();
                    
                    if (simState.LeadSuit == Suit.None)
                        simState.LeadSuit = card.suit; 

                    if (trickCards.TryAdd(player, card))
                    {
                        hand.Remove(card);
                        GameLogger.ShowLog($"Playout: Player {player} plays {card}");
                    }
                    else
                        GameLogger.ShowLog($"Error: Player {player} has already played the card, Can not Add {card}");
                }

                int winningPlayer = GetTrickWinner(trickCards, simState.LeadSuit, trumpSuit);
                int winningTeam = winningPlayer % 2;
                teamScores[winningTeam]++;
                GameLogger.ShowLog($"Trick won by Player {winningPlayer} (Team {winningTeam})");

                // Clear the trick for the next round
                simState.Trick.Clear();
                simState.LeadSuit = Suit.None;
                simState.CurrentPlayer = winningPlayer;
            }

            GameLogger.ShowLog($"Playout complete. Bot's team (Team {botTeam}) scored {teamScores[botTeam]}");
            return teamScores[botTeam];
        }

        public static List<CardData> GetValidMoves(List<CardData> hand, Suit trickSuit, Suit trumpSuit)
        {
            if (trickSuit == Suit.None)
                return hand;

            var matchingSuit = hand.Where(c => GetEffectiveSuit(c) == trickSuit).ToList();

            return matchingSuit.Count > 0 ? matchingSuit : hand;

            bool IsSameColor(Suit s1, Suit s2)
            {
                return s1 is Suit.Clubs or Suit.Spades &&
                       s2 is Suit.Clubs or Suit.Spades
                       ||
                       s1 is Suit.Hearts or Suit.Diamonds &&
                       s2 is Suit.Hearts or Suit.Diamonds;
            }

            Suit GetEffectiveSuit(CardData card)
            {
                if (card.rank == Rank.Jack && card.suit == trumpSuit || card.rank == Rank.Jack && IsSameColor(card.suit, trumpSuit))
                    return trumpSuit;

                return card.suit;
            }
        }

        private static bool PlayersHaveCards(SimulatedGameState state)
        {
            return state.Hands.Values.Any(h => h.Count > 0);
        }

        private static int GetTrickWinner(Dictionary<int, CardData> playedCards, Suit leadSuit, Suit trumpSuit)
        {
            CardData winningCard = null;
            int winner = -1;

            var debugString = string.Empty;
            foreach (var (playerIndex, cardData) in playedCards)
            {
                if (cardData is null) continue;
                debugString += $"Card : {cardData.rank} of {cardData.suit} Power : {cardData.GetCardPower(trumpSuit, leadSuit)}\n";
                if (winningCard != null && cardData.GetCardPower(trumpSuit, leadSuit) <= winningCard.GetCardPower(trumpSuit, leadSuit)) 
                    continue;
                winner = playerIndex;
                winningCard = cardData;
            }
            GameLogger.ShowLog(debugString);

            return winner;
        }
        
        public static void Backpropagate(EuchreBotDecisionEngine.TreeNode node, float result)
        {
            GameLogger.ShowLog($"Backpropagating with result = {result}");

            while (node != null)
            {
                node.Visits++;
                node.Wins += result;
                GameLogger.ShowLog($"Updated node: Move={node.Move}, Visits={node.Visits}, Wins={node.Wins}");
                node = node.Parent;
            }
        }
    }
}
