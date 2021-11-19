using System;
using System.Collections.Generic;
using System.Linq;
using Crappy.Pieces;

namespace Crappy
{
    /// <summary>
    /// Represent a position of a chess game and provide methods to query its legal moves.
    /// </summary>
    public class Position
    {
        #region Fields
        /// <summary>
        /// Square matrix indexed by [rank][column]: a1=[0][0]..h8=[7][7]
        /// </summary>
        public Piece[][] Board { get; set; }
        public PieceColor SideToMove { get; set; }
        public IEnumerable<Piece> CastlingFlags { get; set; }
        public BoardCoordinates EnPassantTarget { get; set; }
        public int HalfmoveClock { get; set; }
        public int FullmoveClock { get; set; }
        private IEnumerable<(Piece Piece, BoardCoordinates Coordinates)> allPieces;

        #endregion Fields

        #region Mutable
        public void SetSquare(BoardCoordinates coordinates, Piece piece)
        {
            Board[coordinates.RankIndex][coordinates.ColumnIndex] = piece;
        }

        private void ClearSquare(BoardCoordinates coordinates) => SetSquare(coordinates, null);

        private void ApplyMove(Move move)
        {
            try
            {
                if (move is null)
                    throw new NullReferenceException("Null move.");

                if (move.Color != SideToMove)
                    throw new ArgumentException($"Cannot apply a {move.Color} move in {SideToMove} side to move.");

                bool isCapture = move.IsCapture(this);

                //Clear sources
                foreach (BoardCoordinates coordinates in move.Sources.Select(x => x.Coordinates))
                {
                    ClearSquare(coordinates);
                }

                //Set targets
                foreach ((BoardCoordinates coordinates, Piece piece) in move.Targets)
                {
                    SetSquare(coordinates, piece);
                }

                SideToMove = SideToMove.Toggle();

                IEnumerable<Piece> newCastlingFlags = CastlingFlags.Except(move.GetCastlingFlagsRemoved());
                bool castlingFlagsChanged = !newCastlingFlags.SequenceEqual(CastlingFlags);
                CastlingFlags = newCastlingFlags;

                EnPassantTarget = move.GetEnPassantTarget();

                //Half move clock is reset on captures, pawn moves, and moves that remove a castling right. Incremented otherwise.
                HalfmoveClock =                   
                    isCapture || move.Sources.Any(x => x.Piece is Pawn) || castlingFlagsChanged ? 0 : HalfmoveClock + 1;

                if (SideToMove == PieceColor.White)
                    FullmoveClock++;
            }
            catch (Exception ex)
            {
                string moveCoordinates = move is null ? null : move.ToString();

                throw new Exception($"Error moving {moveCoordinates} in {this}", ex);
            }
        }
        #endregion Mutable
        
        #region Immutable
        
        public override string ToString() => FEN.ToString(this);
        public Position Clone() => FEN.Parse(ToString());
        public Piece GetPieceAt(BoardCoordinates coordinates) => Board[coordinates.RankIndex][coordinates.ColumnIndex];
       
        public Position PlayMove(Move move)
        {
            Position result = Clone();
            result.ApplyMove(move);
            return result;
        }

        #endregion Immutable

        public IEnumerable<(Piece Piece, BoardCoordinates Coordinates)> GetAllPieces() =>
            allPieces = allPieces ??
                Board.
                SelectMany(
                    (rank, rankIndex) =>
                    rank.
                    Select(
                        (piece, columnIndex) =>
                        (Piece: piece,
                        Coordinates: new BoardCoordinates
                        {
                            RankIndex = rankIndex,
                            ColumnIndex = columnIndex
                        })).
                    Where(x => x.Piece != null)).
                    ToList();

        private IEnumerable<BoardCoordinates> GetAllCoordinatesForPiece(Piece source) =>
            GetAllPieces().
            Where(x => x.Piece == source).
            Select(x => x.Coordinates);

        public IEnumerable<Move> GetAllMoves(PieceColor color) =>
            GetAllPieces().
            Where(x => x.Piece.Color == color).
            SelectMany(x => x.Piece.GetAllMoves(this, x.Coordinates));

        /// Returns all moves regardless of the side to move
        public IEnumerable<Move> GetAllMoves() =>
            GetAllPieces().
            SelectMany(x => x.Piece.GetAllMoves(this, x.Coordinates));

        public IEnumerable<Move> GetLegalMoves() => GetLegalMoves(SideToMove);

        private IEnumerable<Move> GetLegalMoves(PieceColor color) =>
            GetAllPieces().
            Where(x => x.Piece.Color == color).
            SelectMany(x => GetLegalMovesForPiece(x.Piece, x.Coordinates));

        private IEnumerable<Move> GetLegalMovesForPiece(Piece piece, BoardCoordinates coordinates) =>
            piece.
            GetAllMoves(this, coordinates).
            Where(x => PlayMove(x).IsLegal());

        /// <summary>
        /// Determines whether a square is attacked by any piece except a king. 
        /// This method is invoked to determine which castle moves are available, thus it must not evaluate moves of a king to 
        /// avoid a circular reference.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="attackingColor"></param>
        /// <returns></returns>
        public bool IsCastlePreventedAtCoordinates(BoardCoordinates coordinates, PieceColor attackingColor) =>
            GetAllPieces().
            Where(x => x.Piece.Color == attackingColor && !(x.Piece is King)).
            Any(
                square => 
                square.Piece.GetAllMoves(this, square.Coordinates).
                Any(move => move.Targets.Any(target => target.Coordinates == coordinates)));

        /// <summary>
        /// Square attacked by any piece
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="attackingColor"></param>
        /// <returns></returns>
        private bool IsSquareAttacked(BoardCoordinates coordinates, PieceColor attackingColor) =>
            GetAllMoves(attackingColor).
            Any(move => move.Targets.Any(target => target.Coordinates == coordinates));

        public bool IsKingInCheck(PieceColor color) => IsSquareAttacked(GetAllCoordinatesForPiece(Piece.Get<King>(color)).Single(), color.Toggle());

        /// <summary>
        /// ONLY checks that:
        ///     1: There is one king of each color.
        ///     2: There are not pawns at the first or eight rank.
        ///     3: The side to move is not already checking the opposite king.
        /// </summary>
        /// <returns></returns>
        private bool IsLegal()
        {
            try
            {
                #region One king of each color

                void SingleKing(PieceColor color)
                {
                    if (GetAllCoordinatesForPiece(Piece.Get<King>(color)).SingleOrDefault() is null)
                        throw new Exception($"No single {color} king found in {this}");
                }
                SingleKing(PieceColor.White);
                SingleKing(PieceColor.Black);

                #endregion

                #region No pawns at first or eight rank

                if (GetAllPieces().Any(x => x.Piece is Pawn && new[] { 0, 7 }.Contains(x.Coordinates.RankIndex)))
                    throw new Exception($"Invalid pawn rank in {this}");

                #endregion

                #region No checks in opposite king         

                if (IsKingInCheck(SideToMove.Toggle()))
                {
                    throw new Exception($"Opposite king already in check in {this}");
                }

                #endregion

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool IsCheckMate() => IsKingInCheck(SideToMove) && !GetLegalMoves().Any();

        public bool IsDraw() => 
            HalfmoveClock == 100 || 
            (!GetLegalMoves().Any() && !IsKingInCheck(SideToMove)); //Threefold evaluation is inside Engine.Evaluate

        public string RemoveCounters() => string.Join(" ", ToString().Split(' ').Take(4));
    }
}