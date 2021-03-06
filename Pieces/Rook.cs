using System.Collections.Generic;

namespace Crappy.Pieces
{
    public class Rook : Piece
    {
        internal Rook(){}
        public static IEnumerable<(int x, int y)> Directions = new[]
        {
            (0, 1), //Up
            (0, -1), //Down
            (-1, 0), //Left
            (1, 0), //Right
        };

        public override IEnumerable<Move> GetAllMoves(Position position, Coordinates sourceCoordinates) => 
            GetSweepMoves(position, sourceCoordinates, Directions, length: 7);
    }
}