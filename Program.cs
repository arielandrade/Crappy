using System;
using Crappy.UCI;

namespace Crappy
{
    class Program
    {
        [STAThread]
        static void Main() => new Program().Run();
       
        private void Run()
        {
            Console.WriteLine("Crappy started.");

            var configuration = Configuration.Get();
            configuration.FileName = "Settings.json";

            var engine = new Engine { Book = new Book("Book.json") };

            var uci = new UCIHandler { Engine = engine };

            uci.Loop();
        }
    }
}