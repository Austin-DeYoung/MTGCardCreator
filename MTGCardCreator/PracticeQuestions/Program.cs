using Lucene.Net.Support;
using System;
using System.Collections;

namespace PracticeQuestions
{
    class Program
    {
        static void Main(string[] args)
        {
            DoesContain();
        }


        static bool DoesContain()
        {
            Hashtable table = new Hashtable();

            table.Add("A", "B");
            table.Add("B", "A, D");
            table.Add("C", "D");

            Console.WriteLine(table["B"]);
            return true;
        }


        


    }
}
