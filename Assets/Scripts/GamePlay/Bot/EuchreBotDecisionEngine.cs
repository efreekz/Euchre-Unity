using System.Collections.Generic;
using System.Linq;
using GamePlay.Cards;
using UnityEngine;

namespace GamePlay.Bot
{
    public class EuchreBotDecisionEngine
    {
        private readonly int _simulationCount;
        private static float _uctC;

        public EuchreBotDecisionEngine(int simulationCount, float uctC = 1.41f)
        {
            _simulationCount = simulationCount;
            _uctC = uctC;
        }
        public CardData SelectCardToPlay(GameState gameState)
        {
            GameLogger.ShowLog("Starting decision using flat simulations...");

            var validMoves = GameSimulator.GetValidMoves(gameState.Hand, gameState.TrickSuit, gameState.TrumpSuit);

            if (validMoves.Count == 1)
            {
                GameLogger.ShowLog($"Only one valid move: {validMoves[0]}");
                return validMoves[0];
            }

            var moveResults = new Dictionary<CardData, List<float>>();

            foreach (var move in validMoves)
            {
                moveResults[move] = new List<float>();

                for (int i = 0; i < _simulationCount; i++)
                {
                    GameLogger.ShowLog($"--- Simulation {i + 1}/{_simulationCount} for move {move} ---");

                    // Clone & determinize the game
                    var simulatedGameState = GameSimulator.DeterminizeGame(gameState);

                    // Apply bot move
                    GameSimulator.ApplyMove(simulatedGameState, move);

                    // Simulate rest of the game
                    var result = GameSimulator.SimulatePlayout(simulatedGameState, gameState.TrumpSuit, gameState.TeamNumber);

                    GameLogger.ShowLog($"Simulation {i + 1} for move {move}: score = {result}");
                    moveResults[move].Add(result);
                }
            }

            // Choose move with best average score
            var bestMove = moveResults.OrderByDescending(kvp => kvp.Value.Average()).First().Key;

            foreach (var kvp in moveResults)
            {
                GameLogger.ShowLog($"Move {kvp.Key} -> Average Score = {kvp.Value.Average()} from {kvp.Value.Count} simulations");
            }

            GameLogger.ShowLog($"Best move selected: {bestMove} with expected value = {moveResults[bestMove].Average()}");

            return bestMove;
        }

        // public CardData SelectCardToPlay(GameState gameState)
        // {
        //     GameLogger.ShowLog("Starting MCTS decision for bot...");
        //
        //     var validMoves = GameSimulator.GetValidMoves(gameState.Hand, gameState.TrickSuit);
        //
        //     if (validMoves.Count == 1)
        //     {
        //         GameLogger.ShowLog($"Only one valid move: {validMoves[0]}");
        //         return validMoves[0];
        //     }
        //
        //     var root = new TreeNode(null, null);
        //
        //     for (int i = 0; i < _simulationCount; i++)
        //     {
        //         GameLogger.ShowLog($"--- Simulation {i + 1}/{_simulationCount} ---");
        //
        //         var simulatedGameState = GameSimulator.DeterminizeGame(gameState);
        //         var node = root;
        //
        //         // 1. Selection
        //         while (node.IsFullyExpanded(validMoves))
        //         {
        //             node = node.BestUct();
        //             GameLogger.ShowLog($"--- UCT {node.Uct(node)}");
        //             GameSimulator.ApplyMove(simulatedGameState, node.Move);
        //             GameLogger.ShowLog($"Selection: Applied move {node.Move}");
        //         }
        //
        //         // 2. Expansion
        //         var triedMoves = node.Children.Select(c => c.Move).ToHashSet();
        //         var untriedMoves = validMoves.Where(m => !triedMoves.Contains(m)).ToList();
        //
        //         if (untriedMoves.Count > 0)
        //         {
        //             GameLogger.ShowLog($"--- Expansion: Untried Moves : {untriedMoves.Count}");
        //             var move = untriedMoves[Random.Range(0, untriedMoves.Count)];
        //             GameSimulator.ApplyMove(simulatedGameState, move);
        //
        //             var child = new TreeNode(move, node);
        //             node.Children.Add(child);
        //             node = child;
        //
        //             GameLogger.ShowLog($"Added child node with move {move}");
        //         }
        //
        //         // 3. Simulation
        //         float result = GameSimulator.SimulatePlayout(simulatedGameState, gameState.TrumpSuit, gameState.TeamNumber);
        //         GameLogger.ShowLog($"Simulation result: Bot team score = {result}");
        //
        //         // 4. Backpropagation
        //         GameSimulator.Backpropagate(node, result);
        //         GameLogger.ShowLog($"Backpropagation complete for simulation {i + 1}");
        //     }
        //
        //     var best = root.BestChild();
        //     GameLogger.ShowLog($"Best move selected: {best.Move} with {best.Visits} visits and win score {best.Wins}");
        //     return best.Move;
        // }

        public Suit ChooseTrump(GameState gameState)
        {
            GameLogger.ShowLog("ChooseTrump called (not implemented yet)", GameLogger.LogType.Warning);
            return Suit.None;
        }

        public bool ShouldAcceptTrump(GameState gameState)
        {
            GameLogger.ShowLog("ShouldAcceptTrump called (not implemented yet)", GameLogger.LogType.Warning);
            return false;
        }

        // public (Card cardToDiscard, Card cardToPickup) ExchangeTrumpCard(GameState gameState, Card topKittyCard)
        // {
        //     GameLogger.ShowLog("ExchangeTrumpCard called (not implemented yet)", GameLogger.LogType.Warning);
        //     return (null, null);
        // }

        public class TreeNode
        {
            public CardData Move;
            public TreeNode Parent;
            public List<TreeNode> Children = new();
            public int Visits = 0;
            public float Wins = 0;

            public TreeNode(CardData move, TreeNode parent)
            {
                Move = move;
                Parent = parent;
            }

            public bool IsFullyExpanded(List<CardData> validMoves) =>
                Children.Count == validMoves.Count;

            public TreeNode BestChild() =>
                Children.OrderByDescending(c => c.Visits).First();

            public TreeNode BestUct()
            {
                return Children
                    .OrderByDescending(Uct)
                    .First();
            }

            public float Uct(TreeNode node)
            {
                if (node.Visits == 0)
                    return float.MaxValue;

                return (node.Wins / node.Visits) + _uctC * Mathf.Sqrt(Mathf.Log(this.Visits) / node.Visits);
            }
        }
    }
}
