using System.Collections.Generic;
using System.Linq;

namespace Crappy.Pieces
{
    public class Pawn : Piece
    {
        internal Pawn(){}

        /// <summary>
        /// Returns the promotion pieces or a pawn depending on the target's rank
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="targetCoordinates"></param>
        /// <returns></returns>
        private IEnumerable<Piece> GetTargetPiecesByRank(int targetRankIndex)
        {
            return
                targetRankIndex == (Color == PieceColor.White ? 7 : 0) ?
                new Piece[]
                {
                    Get<Knight>(Color),
                    Get<Bishop>(Color),
                    Get<Rook>(Color),
                    Get<Queen>(Color),
                } :
                new Piece[] { this };
        }

        private IEnumerable<Move> GetPushMoves(Position position, Coordinates sourceCoordinates)
        {
            var sources = new[] { (sourceCoordinates, this as Piece) };

            var singleAdvanceTarget = Coordinates.Get(
                rank: sourceCoordinates.RankIndex + (Color == PieceColor.White ? 1 : -1), 
                column: sourceCoordinates.ColumnIndex);

            if (position.GetPieceAt(singleAdvanceTarget) is null)
            {
                #region Single advance
                foreach (Piece piece in GetTargetPiecesByRank(singleAdvanceTarget.RankIndex))
                {
                    yield return new Move
                    {
                        Sources = sources,
                        Targets = new[] { (singleAdvanceTarget, piece) },
                    };
                }
                #endregion

                #region Double advance
                var doubleAdvanceTarget = Coordinates.Get(
                    rank: sourceCoordinates.RankIndex + (Color == PieceColor.White ? 2 : -2), 
                    column: sourceCoordinates.ColumnIndex);

                if (sourceCoordinates.RankIndex == (Color == PieceColor.White ? 1 : 6) && 
                    position.GetPieceAt(doubleAdvanceTarget) is null)
                {
                    yield return new Move
                    {
                        Sources = sources,
                        Targets = new[] { (doubleAdvanceTarget, this as Piece) },
                    };
                }
                #endregion Double advance
            }
        }

        private IEnumerable<Move> GetCaptureMoves(Position position, Coordinates sourceCoordinates)
        {
            var sources = new[] { (sourceCoordinates, this as Piece) };

            foreach (int columnOffset in new[] { -1, 1 })
            {
                var targetCoordinates = Coordinates.Get(
                    rank: sourceCoordinates.RankIndex + (Color == PieceColor.White ? 1 : -1), 
                    column: sourceCoordinates.ColumnIndex + columnOffset);

                if (targetCoordinates != null)
                {
                    #region En passant capture
                    if (targetCoordinates == position.EnPassantTarget && 
                        ((targetCoordinates.RankIndex == 5 && Color == PieceColor.White) ||
                         (targetCoordinates.RankIndex == 2 && Color == PieceColor.Black)))
                    {
                        var capturedPawnCoordinates = Coordinates.Get(sourceCoordinates.RankIndex, targetCoordinates.ColumnIndex);

                        yield return new Move
                        {
                            Sources = sources,
                            Targets = new[]
                            {
                                (targetCoordinates, this as Piece), //New pawn position
                                (capturedPawnCoordinates, null) //Captured pawn removal
                            }
                        };
                    }
                    #endregion

                    #region Regular capture
                    Piece targetPiece = position.GetPieceAt(targetCoordinates);

                    if (targetPiece != null && targetPiece.Color != Color)
                    {
                        foreach (Piece piece in GetTargetPiecesByRank(targetCoordinates.RankIndex))
                        {
                            yield return new Move
                            {
                                Sources = sources,
                                Targets = new[] { (targetCoordinates, piece) }
                            };
                        }
                    }
                    #endregion
                }
            }
        }

        public override IEnumerable<Move> GetAllMoves(Position position, Coordinates sourceCoordinates) =>
            GetPushMoves(position, sourceCoordinates).
            Concat(GetCaptureMoves(position, sourceCoordinates));
    }
}