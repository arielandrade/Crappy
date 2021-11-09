using System;
using System.Collections.Generic;
using System.Linq;

namespace Crappy
{
    /// <summary>
    /// Represents a position in the board.
    /// Contains parsing methods cause are used both in UCI and FEN.
    /// </summary>
    public class BoardCoordinates
    {
        private static readonly int ColumnOffset = Convert.ToInt32('a');
        public int RankIndex { get; set; }
        public int ColumnIndex { get; set; }

        public bool IsValid()
        {
            IEnumerable<int> allowedRange = Enumerable.Range(0, 8);

            return allowedRange.Contains(RankIndex) && allowedRange.Contains(ColumnIndex);
        }

        public static BoardCoordinates TryParse(string coordinates)
        {
            try
            {
                return Parse(coordinates);
            }
            catch
            {
                return null;
            }
        }

        public static BoardCoordinates Parse(string coordinates)
        {
            try
            {
                return new BoardCoordinates
                {
                    ColumnIndex = Convert.ToInt32(coordinates[0]) - ColumnOffset,
                    RankIndex = int.Parse($"{coordinates[1]}") - 1
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid board coordinate: {coordinates}", ex);
            }
        }

        public override string ToString()
        {
            return (char)(ColumnIndex + ColumnOffset) + (RankIndex + 1).ToString();
        }

        public override bool Equals(object obj) => (obj is BoardCoordinates target) && target.ToString() == ToString();
        public override int GetHashCode() => ToString().GetHashCode();
        public static bool operator ==(BoardCoordinates c1, BoardCoordinates c2) => c1?.Equals(c2) ?? c2 is null;
        public static bool operator !=(BoardCoordinates c1, BoardCoordinates c2) => !(c1 == c2);
    }
}