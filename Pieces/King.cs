using System.Collections.Generic;
using System.Linq;

namespace Crappy.Pieces
{
    public class King : Piece
    {
        public static IEnumerable<(int x, int y)> Directions => Bishop.Directions.Concat(Rook.Directions);

        private Move GetCastlingMoveBySide(Position position, BoardCoordinates kingSourceCoordinates, Piece side)
        {
            PieceColor oppositeColor = Color.Toggle();

            if (position.CastlingFlags.Contains(side) && 
                !position.IsCastlePreventedAtCoordinates(kingSourceCoordinates, oppositeColor))
            {
                var kingTargetCoordinates = new BoardCoordinates
                {
                    ColumnIndex = side is King ? 6 : 2,
                    RankIndex = kingSourceCoordinates.RankIndex
                };

                var rookSourceCoordinates = new BoardCoordinates
                {
                    ColumnIndex = side is King ? 7 : 0,
                    RankIndex = kingSourceCoordinates.RankIndex
                };

                var rookTargetCoordinates = new BoardCoordinates
                {
                    ColumnIndex = side is King ? 5 : 3,
                    RankIndex = kingSourceCoordinates.RankIndex
                };

                var queenKnightCoordinates = new BoardCoordinates
                {
                    ColumnIndex = 1,
                    RankIndex = kingSourceCoordinates.RankIndex
                };

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
                            (rookSourceCoordinates, new Rook { Color = Color }),
                        },
                        Targets = new[]
                        {
                            (kingTargetCoordinates, this as Piece),
                            (rookTargetCoordinates, new Rook { Color = Color })
                        }
                    };
                }
            }

            return null;
        }

        private IEnumerable<Move> GetCastlingMoves(Position position, BoardCoordinates sourceCoordinates)
        {
            foreach (var side in new Piece[] { this, new Queen { Color = Color } })
            {
                Move castling = GetCastlingMoveBySide(position, sourceCoordinates, side);

                if (castling != null)
                    yield return castling;
            }
        }

        public override IEnumerable<Move> GetAllMoves(Position position, BoardCoordinates sourceCoordinates) =>
            GetSweepMoves(position, sourceCoordinates, Directions, length: 1).
            Concat(GetCastlingMoves(position, sourceCoordinates));
    }
}