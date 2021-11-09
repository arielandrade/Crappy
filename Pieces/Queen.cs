using System.Collections.Generic;
using System.Linq;

namespace Crappy.Pieces
{
    public class Queen : Piece
    {
        public static IEnumerable<(int x, int y)> Directions => 
            Bishop.Directions.Concat(Rook.Directions);
            
        public override IEnumerable<Move> GetAllMoves(Position position, BoardCoordinates sourceCoordinates) => 
            GetSweepMoves(position, sourceCoordinates, Directions, length: 7);
    }
}
