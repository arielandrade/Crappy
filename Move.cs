using System;
using System.Collections.Generic;
using System.Linq;
using Crappy.Pieces;
using Crappy.UCI;

namespace Crappy
{
    public class Move
    {
        /// <summary>
        /// Coordinates to clear and source pieces to determine promotion in case the fist source is a different 
        /// than the first target.
        /// </summary>
        public (Coordinates Coordinates, Piece Piece)[] Sources { get; set; }

        /// <summary>
        /// Pieces to set.
        /// </summary>
        public (Coordinates Coordinates, Piece Piece)[] Targets { get; set; }

        public bool IsPromotion => Sources.First().Piece != Targets.First().Piece;
        public PieceColor Color => Sources.First().Piece.Color;

        public Coordinates GetEnPassantTarget()
        {
            (Coordinates sourceCoordinates, Piece sourcePiece) = Sources.First();

            return
                sourcePiece is Pawn && Math.Abs(sourceCoordinates.RankIndex - Targets.First().Coordinates.RankIndex) == 2 ?
                Coordinates.Get(sourceCoordinates.RankIndex + sourcePiece.Color.Sign(), sourceCoordinates.ColumnIndex) :
                null;
        }

        private static readonly IEnumerable<(Coordinates coordinates, Piece flag)> _castlingFlags = new List<(Coordinates coordinates, Piece flag)>
        {
            (Coordinates.Parse("a1"), Piece.Get<Queen>(PieceColor.White)),
            (Coordinates.Parse("a8"), Piece.Get<Queen>(PieceColor.Black)),
            (Coordinates.Parse("h1"), Piece.Get<King>(PieceColor.White)),
            (Coordinates.Parse("h8"), Piece.Get<King>(PieceColor.Black))
        };

        /// <summary>
        /// Returns the castling flags removed in case that this is the movement of a rook, its capture, or the movement of a 
        /// king.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Piece> GetCastlingFlagsRemoved()
        {
            return
                _castlingFlags.
                Where(x =>
                    Sources.
                    Select(source => source.Coordinates).
                    Concat(Targets.Select(target => target.Coordinates)).
                    Contains(x.coordinates)).
                Select(x => x.flag).
                Concat(
                    Sources.
                    Select(x => x.Piece).
                    Concat(Targets.Select(x => x.Piece)).
                    SelectMany(x => 
                        x is King ?
                        new Piece[]
                        {
                            Piece.Get<King>(x.Color),
                            Piece.Get<Queen>(x.Color)
                        } :
                        new Piece[] {} ));
        }

        public bool IsCapture(Position position) => 
            Sources.First().Piece is Pawn && Targets.Count() == 2 ||
            position.GetPieceAt(Targets.First().Coordinates) != null; 

        public override string ToString() => UCIParser.MoveToString(this);
    }
}