namespace Blockchain
{
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            Level1.Program.Run();
            Console.WriteLine("-- Level 1 / done --");

            Level2.Program.Run();
            Console.WriteLine("-- Level 2 / done --");

            Level3.Program.Run();
            Console.WriteLine("-- Level 3 / done --");

            Level4.Program.Run();
            Console.WriteLine("-- Level 4 / done --");

            Level5.Program.Run();
            Console.WriteLine("-- Level 5 / done --");
        }
    }
}
