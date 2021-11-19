using System.Collections.Generic;

namespace Crappy.Pieces
{
    public class Bishop : Piece
    {
        internal Bishop(){}
        
        public static IEnumerable<(int x, int y)> Directions = new[]
        {
            (-1, 1), //Up left
            (1, 1), //Up right
            (-1, -1), //Bottom left
            (1, -1), //Bottom right
        };

        public override IEnumerable<Move> GetAllMoves(Position position, BoardCoordinates sourceCoordinates) => 
            GetSweepMoves(position, sourceCoordinates, Directions, length: 7);
    }
}