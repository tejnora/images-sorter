using System;

namespace ImagesSorter
{
    class Program
    {
        static void Main(string[] args)
        {
            var currentDirectory = System.IO.Directory.GetCurrentDirectory();
            var sorter = new ImageSorter(currentDirectory);
            if (args.Length != 1)
            {
                PrintHelp();
                return;
            }
            var splitByDateToDirectory = false;
            if (args[0] == "-intodir")
            {
                splitByDateToDirectory = true;
            }
            else if (args[0] != "-intofile")
            {
                PrintHelp();
                return;
            }
            try
            {
                sorter.PerformSorting(splitByDateToDirectory);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("-intodir  --sort into directories");
            Console.WriteLine("-intofile --only rename");
        }
    }
}
