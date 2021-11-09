using System;
using System.Collections.Generic;
using System.Linq;
using Crappy.Pieces;

namespace Crappy
{
    /// <summary>
    /// Provides the conversion methods between FEN string and Position object
    /// </summary>
    public static class FEN
    {
        public const string START = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"; //a8..h1

        public static Position Parse(string fen)
        {
            #region Field getters
            PieceColor GetSideToMove(string fenSideToMove)
            {
                switch (fenSideToMove)
                {
                    case "w":
                        return PieceColor.White;
                    case "b":
                        return PieceColor.Black;
                    default:
                        throw new ArgumentException($"Invalid side to move: {fenSideToMove}");
                }
            }

            //[rank][column] a8..h1
            Piece[][] GetSquares(string fenRanks)
            {
                try
                {
                    var result = Enumerable.
                        Range(0, 8).
                        Select(x => new Piece[8]).
                        ToArray();

                    string[] ranks = fenRanks.Split('/');

                    if (ranks.Length != 8)
                    {
                        throw new ArgumentException($"{ranks.Length} ranks found in {fenRanks}");
                    }

                    foreach (int rankIndex in Enumerable.Range(0, ranks.Length))
                    {
                        string rank = ranks[rankIndex];
                        int column = 0;

                        foreach (char c in rank)
                        {
                            if (int.TryParse($"{c}", out int spaces))
                            {
                                column += spaces;
                            }
                            else
                            {
                                result[7 - rankIndex][column] = Piece.Parse(c);
                                column++;
                            }

                            if (column > 8)
                            {
                                throw new ArgumentException($"{column + 1} columns in rank: {rank}");
                            }
                        }

                        if (column < 8)
                        {
                            throw new ArgumentException($"{column + 1} columns in rank: {rank}");
                        }
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Cannot parse ranks from {fenRanks}", ex);
                }
            }

            Piece[] GetCastlingFlags(string fenCastlingFlags)
            {
                try
                {
                    return fenCastlingFlags.
                        Select(x => Piece.Parse(x)).
                        ToArray();
                }
                catch
                {
                    return new Piece[] { };
                }
            }
            #endregion

            try
            {
                string[] fields = fen.Split(' ');

                int.TryParse(fields.ElementAtOrDefault(4), out int halfmoveClock);
                int.TryParse(fields.ElementAtOrDefault(5), out int fullmoveClock);
                 
                return new Position
                {
                    Board = GetSquares(fields[0]),
                    SideToMove = GetSideToMove(fields[1]),
                    CastlingFlags = GetCastlingFlags(fields[2]),
                    EnPassantTarget = BoardCoordinates.TryParse(fields[3]),
                    HalfmoveClock = halfmoveClock,
                    FullmoveClock = fullmoveClock,
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid FEN string: {fen}", ex);
            }
        }

        public static string ToString(Position position)
        {
            #region Field getters

            string GetSquares(Piece[][] squares)
            {
                string GetRank(Piece[] rank)
                {
                    string result = string.Empty;
                    int spaces = 0;

                    foreach (Piece piece in rank)
                    {
                        if (piece is null)
                        {
                            spaces++;
                        }
                        else
                        {
                            result = $"{result}{(spaces == 0 ? string.Empty : spaces.ToString())}{piece}";
                            spaces = 0;
                        }
                    }

                    return $"{result}{(spaces == 0 ? string.Empty : spaces.ToString())}";
                }

                return string.Join("/", squares.Reverse().Select(x => GetRank(x)));
            }

            string GetSideToMove(PieceColor sideToMove) => sideToMove == PieceColor.White ? "w" : "b";

            string GetCastlingFlags(IEnumerable<Piece> flags) => flags.Any() ? string.Join(string.Empty, flags) : "-";

            #endregion Field getters

            return
                $"{GetSquares(position.Board)} " +
                $"{GetSideToMove(position.SideToMove)} " +
                $"{GetCastlingFlags(position.CastlingFlags)} " +
                (position.EnPassantTarget is null ? "- " : $"{position.EnPassantTarget} ") +
                $"{position.HalfmoveClock} " +
                $"{position.FullmoveClock}";
        }
    }
}