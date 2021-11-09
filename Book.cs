using System;
using System.Collections.Generic;
using System.IO;
using Crappy.UCI;
using Newtonsoft.Json;

namespace Crappy
{
    public class Book
    {
        private readonly Dictionary<string, Opening> Openings = new Dictionary<string, Opening>();

        public Book(string fileName)
        {
            Load(fileName);
        }

        private void Load(string fileName)
        {
            if (File.Exists(fileName))
            {
                string fileContent = File.ReadAllText(fileName);
                
                try
                {               
                    List<Opening> openings = JsonConvert.DeserializeObject<List<Opening>>(fileContent);

                    foreach(Opening opening in openings)
                    {
                        Openings.Add(opening.FEN, opening);
                    }
                }
                catch(Exception ex)
                {
                    throw new FileLoadException($"Error loading book file {fileName}", ex);
                }
            }
        }

        public Move GetMove(Position position)
        {
            Openings.TryGetValue(position.RemoveCounters().ToString(), out Opening opening);

            if (opening != null && opening.Moves.Length > 0)
            {
                Console.WriteLine($"info string Opening found: {opening.Name}");

                return UCIParser.ParseMove(position, opening.Moves[new Random().Next(opening.Moves.Length)]);
            }

            Console.WriteLine($"info string No opening found for FEN: '{position.RemoveCounters()}'");

            return null;
        }
    }
}