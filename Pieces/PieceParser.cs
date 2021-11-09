using System;
using System.Collections.Generic;
using System.Linq;

namespace Crappy.Pieces
{
    /// <summary>
    /// Parsing methods used both in UCI and FEN.
    /// </summary>
    internal static class PieceParser
    {
        /// <summary>
        /// The way I see it, it's either this or Ninject.
        /// </summary>
        private static readonly Dictionary<string, Type> _pieceTypes = new Dictionary<string, Type>
        {
            { "p", typeof(Pawn) },
            { "n", typeof(Knight) },
            { "b", typeof(Bishop) },
            { "r", typeof(Rook) },
            { "q", typeof(Queen) },
            { "k", typeof(King) },
        };

        public static Piece Parse(string piece)
        {
            return
                string.IsNullOrEmpty(piece) ?
                throw new NullReferenceException("Error parsing piece: Empty string") :
                Parse(piece.First());
        }

        public static Piece Parse(char piece) => 
            Parse(piece, char.IsUpper(piece) ? PieceColor.White : PieceColor.Black);

        public static Piece Parse(char piece, PieceColor color)
        {
            if (_pieceTypes.TryGetValue($"{piece}".ToLower(), out Type type))
            {
                Piece result = Activator.CreateInstance(type) as Piece;
                result.Color = color;
                return result;
            }
            else
            {
                throw new ArgumentException($"Invalid piece: {piece}");
            }
        }

        public static string ToString(this Piece piece)
        {
            try
            {
                string result = _pieceTypes.
                    Single(x => x.Value == piece.GetType()).
                    Key;

                return piece.Color == PieceColor.White ? result.ToUpper() : result.ToLower();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error serializing piece", ex);
            }
        }
    }
}
