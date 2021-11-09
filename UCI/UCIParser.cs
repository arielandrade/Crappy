using System;
using System.Linq;
using Crappy.Pieces;

namespace Crappy.UCI
{
    /// <summary>
    /// Parser methods specific to UCI formats
    /// </summary>
    public static class UCIParser
    {
        public static Move ParseMove(Position position, string move)
        {
            try
            {
                if (!new[] { 4, 5 }.Contains(move.Length))
                    throw new ArgumentException($"Invalid move length: {move.Length}");

                BoardCoordinates firstSource = BoardCoordinates.Parse(move.Substring(0, 2));
                BoardCoordinates firstTarget = BoardCoordinates.Parse(move.Substring(2, 2));
                Piece promotion = move.Length == 5 ? Piece.Parse(move[4], position.SideToMove) : null;

                return
                    position.
                    GetAllMoves(position.SideToMove).
                    Where(
                        x => 
                        x.Sources.First().Coordinates == firstSource && 
                        x.Targets.First().Coordinates == firstTarget &&
                        (promotion is null || x.Targets.First().Piece == promotion)).
                    Single();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing move {move} from {position}", ex);
            }
        }

        public static string MoveToString(Move move)
        {
            return 
                move is null ? 
                "0000" :
                $"{move.Sources.First().Coordinates}" +
                $"{move.Targets.First().Coordinates}" +
                $"{(move.IsPromotion ? move.Targets.First().Piece.ToString().ToLower() : string.Empty)}";
        }
    }
}