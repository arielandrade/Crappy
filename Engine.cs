using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Crappy.Pieces;

namespace Crappy
{
    /// <summary>
    /// Provides the engine state and evaluation methods.
    /// </summary>
    public class Engine    
    {
        #region Fields

        public const string NAME = "Crappy";
        public const string AUTHOR = "Ariel Andrade";
        private int EvaluationCount;
        private int FlatEvaluationCount;
        
        private Book _book;
        public Book Book
        {
            get => _book ?? throw new NullReferenceException("No book set");
            set { _book = value; }
        }

        /// More than 3 is reaaaly slow. This ain't in the config.json file cause is not set by the setoption command
        public int Depth = 3; 

        public Action<string> OnInfo { get; set; }

        #endregion

        #region UCI commands

        public void ClearCache()
        {
            //Si tuviera cache lo borraría
            GC.Collect();
        }

        public (Move bestMove, decimal score) Search(
            Position position,
            IEnumerable<Move> moves,            
            IList<string> positionHistory,
            bool infinite,
            int? movetime,
            int? whiteTime,
            int? blackTime,
            int? whiteIncrement,
            int? blackIncrement,
            int? depth) //Remember not to use a depth > 3 or you'll wait a year for it to move
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            EvaluationCount = 0;
            FlatEvaluationCount = 0;

            try
            {                               
                Move move = Book.GetMove(position);

                if (move != null)                
                {
                    WriteInfo($"Book move {move}");

                    return (bestMove: move, score: 0);
                }

                return InternalSearch(position, moves, positionHistory, depth ?? Depth);               
            }
            finally 
            {
                stopwatch.Stop();
                WriteInfo($"Elapsed: {stopwatch.Elapsed.TotalSeconds.ToString("N2")}");
                WriteInfo($"{EvaluationCount} positions evaluated");
                WriteInfo($"{FlatEvaluationCount} flat positions evaluated");
                WriteInfo($"{(FlatEvaluationCount / stopwatch.Elapsed.TotalSeconds).ToString("0")} flat evaluations per second");
            }
        }
        #endregion UCI commands

        private void WriteInfo(string info) => OnInfo?.Invoke(info);

        /// <summary>
        /// Evaluates the moves specified and calculates the score resulting for each of them given a specified depth.
        /// Then returns one of the best moves according to the randomness setting.
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        private (Move move, decimal score) InternalSearch(
            Position position, 
            IEnumerable<Move> moves, 
            IEnumerable<string> positionHistory, 
            int depth)
        {
            decimal alpha = -MAX_SCORE;
            decimal beta = MAX_SCORE;
            decimal randomness = Configuration.Get().GetValue<int>(SettingType.Randomness) / 100M;

            IEnumerable<(Move move, decimal score)> rankedMoves = moves.
                AsParallel().
                Select(
                    x => 
                    {
                        Position newPosition = position.PlayMove(x);
                        decimal evaluation = -Evaluate(newPosition, depth, positionHistory, -beta, -alpha);

                        return (move: x, score: evaluation);
                    }).
                ToList().
                OrderByDescending(x => x.score);
           
            (Move move, decimal score) bestMove = rankedMoves.First();

            List<(Move move, decimal score)> candidateMoves = rankedMoves.
                TakeWhile(x => Math.Abs(bestMove.score - x.score) <= randomness).
                ToList();

            WriteInfo($"Best ranked move: {bestMove}");
            WriteInfo("Ranked moves:");

            foreach((Move move, decimal score) item in rankedMoves)
            {
                WriteInfo(item.ToString());
            }

            WriteInfo("Deltas:");

            foreach((Move move, decimal score) item in rankedMoves)
            {
                decimal delta = Math.Abs(bestMove.score - item.score);
                
                if (delta > randomness)
                {
                    break;
                }

                WriteInfo($"{item.move}: {delta}");
            }

            WriteInfo("Candidate moves:");

            foreach((Move move, decimal score) item in candidateMoves)
            {
                WriteInfo(item.ToString());
            }
            
            return candidateMoves.ElementAt(new Random().Next(candidateMoves.Count()));
        }

        /// <summary>
        /// Rough attempt to value each piece.
        /// </summary>
        public static readonly Dictionary<Type, decimal> PieceValues = new Dictionary<Type, decimal>
        {
            { typeof(Pawn), 1M },
            { typeof(Knight), 3M },
            { typeof(Bishop), 3.1M },
            { typeof(Rook), 5M },
            { typeof(Queen), 9M },
            { typeof(King), 0M }, //I'm evaluating checkmate in a position as 1000 so I won't value the king for now
        };
       
        /// Base evaluation function
        public decimal Evaluate(
            Position position, 
            int depth, 
            IEnumerable<string> positionHistory, 
            decimal alpha, 
            decimal beta)
        {
            Interlocked.Increment(ref EvaluationCount);

            #region Threefold

            string barePosition = position.RemoveCounters();

            positionHistory = positionHistory.Append(barePosition);
           
            if (positionHistory.Count(x => x == barePosition) == 3)
            {
                WriteInfo($"THREEFOLD FOUND IN {barePosition}");
                return 0;
            }

            #endregion
            
            #region Negamax

            if (--depth <= 0)
                return FlatScore(position);
            
            IEnumerable<Move> moves = position.GetLegalMoves();

            if (moves.Any())
            {
                decimal result = -MAX_SCORE;

                foreach(Move move in moves)
                {
                    Position newPosition = position.PlayMove(move);

                    decimal evaluation = -Evaluate(newPosition, depth, positionHistory, -beta, -alpha);

                    result = Math.Max(result, evaluation);
                    alpha = Math.Max(result, alpha);

                    if (alpha >= beta)
                        break;
                }

                return result;
            }

            return FlatScore(position);

            #endregion Negamax
        }

        private const int MAX_SCORE = 1000; //Cause int.MaxValue always overflows somewhere

        /// <summary>
        /// Calculates a score in the board as the sum of material plus some centipawns for each available move.
        /// The value returned is the difference between the scores of white and black.
        /// This method is intended to be called only by the last level of the calculation tree.
        /// </summary>
        /// <param name="position"></param>
        /// <returns>Score in position.SideToMove's point of view</returns>
        public decimal FlatScore(Position position)
        {
            Interlocked.Increment(ref FlatEvaluationCount);

            //Check for draws.
            //Threefold repetition is checked in the main evaluation, using the position history
            if (position.IsDraw())
            {
                WriteInfo($"DRAW FOUND IN {position}");
                return 0;
            }

            //Checkmate
            if (position.IsCheckMate())
            {
                WriteInfo($"MATE FOUND IN {position}");
                return -MAX_SCORE;
            }

            /* Core position evaulation should go here, but I'll just count material and mobility for now.
             * Future implementations can include piece-square tables or other fancy stuff. */
            decimal material = position.
                GetAllPieces().
                Sum(x => PieceValues[x.Piece.GetType()] * x.Piece.Color.Sign());

            int mobility = position.
                GetAllMoves().
                Where(x => !(x.Sources.First().Piece is King)).
                Sum(x => x.Sources.First().Piece.Color.Sign());

            return (material * 100 + mobility) / 100 * position.SideToMove.Sign();
        }
    }
}