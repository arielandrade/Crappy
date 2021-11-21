namespace Crappy.Pieces
{
    internal static class PieceColorHelper
    {
        public static PieceColor Toggle(this PieceColor color) =>
            color == PieceColor.White ? 
            PieceColor.Black : 
            PieceColor.White;

        /// <summary>
        /// Used to multiply values so that white is positive and black is negative.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static int Sign(this PieceColor color) => color == PieceColor.White ? 1 : -1;
    }
}