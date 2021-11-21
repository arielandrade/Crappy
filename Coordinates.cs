using System;
using System.Collections.Generic;
using System.Linq;

namespace Crappy
{
    /// <summary>
    /// Represents a position in the board.
    /// Contains parsing methods cause are used both in UCI and FEN.
    /// </summary>
    public class Coordinates
    {
        #region Static
        //Rank, Column
        private static readonly Coordinates[,] _collection = GetCollection();

        private static Coordinates[,] GetCollection()
        {
            var result = new Coordinates[8, 8];

            foreach(int rank in Enumerable.Range(0, 8))
            {
                foreach(int column in Enumerable.Range(0, 8))
                {
                    result[rank, column] = new Coordinates { RankIndex = rank, ColumnIndex = column };
                }
            }

            return result;
        }

        private static readonly IEnumerable<int> _validRange = Enumerable.Range(0, 8).ToList();

        public static Coordinates Get(int rank, int column) =>
            _validRange.Contains(rank) && _validRange.Contains(column) ? _collection[rank, column] : null;

        private static readonly int ColumnOffset = Convert.ToInt32('a');

        #endregion
        
        public int RankIndex { get; set; }
        public int ColumnIndex { get; set; }

        private Coordinates(){}

        public static Coordinates TryParse(string coordinates)
        {
            try
            {
                return Get(rank: int.Parse($"{coordinates[1]}") - 1, column: Convert.ToInt32(coordinates[0]) - ColumnOffset);               
            }
            catch
            {
                return null;
            }            
        }

        public static Coordinates Parse(string coordinates) =>
            TryParse(coordinates) ?? throw new ArgumentException($"Invalid board coordinates: {coordinates}");

        public override string ToString()
        {
            return (char)(ColumnIndex + ColumnOffset) + (RankIndex + 1).ToString();
        }

        public override bool Equals(object obj) => (obj is Coordinates target) && target.ToString() == ToString();
        public override int GetHashCode() => ToString().GetHashCode();
        public static bool operator ==(Coordinates c1, Coordinates c2) => c1?.Equals(c2) ?? c2 is null;
        public static bool operator !=(Coordinates c1, Coordinates c2) => !(c1 == c2);
    }
}