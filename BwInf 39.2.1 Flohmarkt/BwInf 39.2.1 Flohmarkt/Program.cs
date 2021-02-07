﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BwInf_39._2._1_Flohmarkt {
    class Program {
        static void Main(string[] args) {
            simulatedAnnealing simAnn = new simulatedAnnealing(readData(1), 1000, 10, 74);
            simAnn.setRandomPos();
            simAnn.simulate1();
            Console.ReadKey();
        }

        static List<Anfrage> readData(int number) {
            string[] lines = System.IO.File.ReadAllLines(System.IO.Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + "/flohmarkt " + number + ".txt");
            List<Anfrage> anfragen = new List<Anfrage>();
            for (int i = 1; i < lines.Length; i++) {
                string[] line = lines[i].Split(' ');
                anfragen.Add(new Anfrage(i - 1, int.Parse(line[0].Trim()), int.Parse(line[1].Trim()), int.Parse(line[2].Trim()),0));
            }
            Console.WriteLine(anfragen.Count + "  " + lines.Length);
            return anfragen;
        }
    }
}
