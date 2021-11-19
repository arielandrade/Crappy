using System.Collections.Generic;

namespace Crappy.Pieces
{
    public class Knight : Piece
    {
        internal Knight(){}
        public static IEnumerable<(int x, int y)> Directions = new[]
        {
            (-2, 1), //Left top
            (-2, -1), //Left bottom
            (-1, 2), //Top left
            (-1, -2), //Bottom left
            (1, 2), //Top right
            (1, -2), //Bottom right
            (2, 1), //Right top
            (2, -1), //Right bottom
        };

        public override IEnumerable<Move> GetAllMoves(Position position, BoardCoordinates sourceCoordinates) => 
            GetSweepMoves(position, sourceCoordinates, Directions, length: 1);
    }
}