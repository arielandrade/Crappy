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
        public (BoardCoordinates Coordinates, Piece Piece)[] Sources { get; set; }

        /// <summary>
        /// Pieces to set.
        /// </summary>
        public (BoardCoordinates Coordinates, Piece Piece)[] Targets { get; set; }

        public bool IsPromotion => Sources.First().Piece != Targets.First().Piece;
        public PieceColor Color => Sources.First().Piece.Color;

        public BoardCoordinates GetEnPassantTarget()
        {
            (BoardCoordinates sourceCoordinates, Piece sourcePiece) = Sources.First();

            return
                sourcePiece is Pawn && Math.Abs(sourceCoordinates.RankIndex - Targets.First().Coordinates.RankIndex) == 2 ?
                new BoardCoordinates
                {
                    ColumnIndex = sourceCoordinates.ColumnIndex,
                    RankIndex = sourceCoordinates.RankIndex + sourcePiece.Color.Sign()
                } :
                null;
        }

        /// <summary>
        /// Returns the castling flags removed in case that this is the movement of a rook, its capture, or the movement of a 
        /// king.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Piece> GetCastlingFlagsRemoved()
        {
            var flags = new (BoardCoordinates coordinates, Piece flag)[]
            {
                (BoardCoordinates.Parse("a1"), new Queen { Color = PieceColor.White } ),
                (BoardCoordinates.Parse("a8"), new Queen { Color = PieceColor.Black } ),
                (BoardCoordinates.Parse("h1"), new King { Color = PieceColor.White } ),
                (BoardCoordinates.Parse("h8"), new King { Color = PieceColor.Black } )
            };

            return
                flags.
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
                            new King { Color = x.Color },
                            new Queen { Color = x.Color }
                        } :
                        new Piece[] {} ));
        }

        public bool IsCapture(Position position) => 
            Sources.First().Piece is Pawn && Targets.Count() == 2 ||
            position.GetPieceAt(Targets.First().Coordinates) != null; 

        public override string ToString() => UCIParser.MoveToString(this);
    }
}