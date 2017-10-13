using System;
using System.Collections;
using Visitor = FileSystemVisitor.FileSystemVisitor;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            int counter = 0;

            var visitor = new Visitor(@"D:\test");

            visitor.Start += () => Console.WriteLine("Started");

            visitor.Finish += () => Console.WriteLine("Finished");

            visitor.FileFound += (s, e) => Console.WriteLine("File Finded");

            visitor.DirectoryFound += (s, e) => Console.WriteLine("Dir Finded");

            visitor.FilteredDirectoryFound += (s, e) =>
            {
                Console.WriteLine("Filtered dir finded");

                e.RemoveItem = true;
            };
            visitor.FilteredFileFound += (s, e) => Console.WriteLine("Filtered file finded");

            visitor.FilteredFileFound += (s, e) =>
            {
                counter++;

                if (counter > 4)
                {
                    e.RemoveItem = true;
                }

                if (counter > 8)
                {
                    e.StopSearch = true;
                }
            };
            try
            {
                foreach (var item in visitor)
                {
                    Console.WriteLine(item);
                    Console.WriteLine("-------------------------");
                }
            }

            catch (UnauthorizedAccessException)
            {
                
            }

            Console.Read();
        }
    }
}
