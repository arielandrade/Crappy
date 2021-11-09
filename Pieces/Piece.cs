using System.Collections.Generic;
using System.Linq;

namespace Crappy.Pieces
{
    /// <summary>
    /// Provides a base class to implement all pieces and exposes a method to query all available moves giving a specific set of 
    /// board coordinates.
    /// </summary>
    public abstract class Piece
    {
        public PieceColor Color { get; set; }
        public override string ToString() => PieceParser.ToString(this);
        public static Piece Parse(char value, PieceColor color) => PieceParser.Parse(value, color);
        public static Piece Parse(char value) => PieceParser.Parse(value);
        public Piece Clone() => PieceParser.Parse(ToString());
        public override bool Equals(object o) => o is Piece p && p.ToString() == ToString();
        public override int GetHashCode() => ToString().GetHashCode();
        public static bool operator ==(Piece p1, Piece p2) => p1?.Equals(p2) ?? p2 is null;
        public static bool operator !=(Piece p1, Piece p2) => !(p1==p2);

        /// <summary>
        /// ALL moves for the piece excluding ONLY the capture of own pieces and the trespassing of opposite ones.
        /// Kings can be captured, own king can be left in check and castling moves can pass through checked squares.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="sourceCoordinates"></param>
        /// <returns></returns>
        public abstract IEnumerable<Move> GetAllMoves(Position position, BoardCoordinates sourceCoordinates);

        /// <summary>
        /// Sweep in all directions specified while incrementing a distance until reaching the length specified, or finding a 
        /// capture, a blockage by an owned piece or the end of the board.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="sourceCoordinates"></param>
        /// <returns></returns>
        protected IEnumerable<Move> GetSweepMoves(
            Position position, 
            BoardCoordinates sourceCoordinates, 
            IEnumerable<(int x, int y)> directions, 
            int length)
        {
            foreach ((int x, int y) in directions)
            {
                foreach (int distance in Enumerable.Range(1, length))
                {
                    var targetCoordinates = new BoardCoordinates
                    {
                        ColumnIndex = sourceCoordinates.ColumnIndex + x * distance,
                        RankIndex = sourceCoordinates.RankIndex + y * distance
                    };

                    if (targetCoordinates.IsValid())
                    {
                        Piece piece = position.GetPieceAt(targetCoordinates);

                        if (piece is null || piece.Color != Color)
                        {
                            yield return new Move
                            {
                                Sources = new[] { (sourceCoordinates, this as Piece) },
                                Targets = new[] { (targetCoordinates, this as Piece) }
                            };
                        }

                        if (piece != null)
                            break;
                    }
                    else
                        break;
                }
            }
        }
    }
}