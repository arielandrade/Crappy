using System.Collections.Generic;
using System.Linq;

namespace Crappy.Pieces
{
    public class King : Piece
    {
        internal King(){}
        public static IEnumerable<(int x, int y)> Directions => Bishop.Directions.Concat(Rook.Directions);

        private Move GetCastlingMoveBySide(Position position, Coordinates kingSourceCoordinates, Piece side)
        {
            PieceColor oppositeColor = Color.Toggle();

            if (position.CastlingFlags.Contains(side) && 
                !position.IsCastlePreventedAtCoordinates(kingSourceCoordinates, oppositeColor))
            {
                var kingTargetCoordinates = Coordinates.Get(rank: kingSourceCoordinates.RankIndex, column: side is King ? 6 : 2);
                var rookSourceCoordinates = Coordinates.Get(rank: kingSourceCoordinates.RankIndex, column: side is King ? 7 : 0);
                var rookTargetCoordinates = Coordinates.Get(rank: kingSourceCoordinates.RankIndex, column: side is King ? 5 : 3);
                var queenKnightCoordinates = Coordinates.Get(rank: kingSourceCoordinates.RankIndex, column: 1);

                if (position.GetPieceAt(rookTargetCoordinates) is null &&
                    position.GetPieceAt(kingTargetCoordinates) is null &&
                    (side is King || position.GetPieceAt(queenKnightCoordinates) is null) &&
                    !position.IsCastlePreventedAtCoordinates(rookTargetCoordinates, oppositeColor))
                {
                    return new Move
                    {
                        Sources = new[]
                        {
                            (kingSourceCoordinates, this as Piece),
                            (rookSourceCoordinates, Get<Rook>(Color)),
                        },
                        Targets = new[]
                        {
                            (kingTargetCoordinates, this as Piece),
                            (rookTargetCoordinates, Get<Rook>(Color))
                        }
                    };
                }
            }

            return null;
        }

        private IEnumerable<Move> GetCastlingMoves(Position position, Coordinates sourceCoordinates)
        {
            foreach (var side in new Piece[] { this, Get<Queen>(Color) })
            {
                Move castling = GetCastlingMoveBySide(position, sourceCoordinates, side);

                if (castling != null)
                    yield return castling;
            }
        }

        public override IEnumerable<Move> GetAllMoves(Position position, Coordinates sourceCoordinates) =>
            GetSweepMoves(position, sourceCoordinates, Directions, length: 1).
            Concat(GetCastlingMoves(position, sourceCoordinates));
    }
}