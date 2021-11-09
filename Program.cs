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

            var configuration = new Configuration("Settings.json");
            var book = new Book("Book.json");
            var engine = new Engine
            {
                Configuration = configuration,
                Book = book
            };

            new UCIHandler(engine).Loop();
        }
    }
}