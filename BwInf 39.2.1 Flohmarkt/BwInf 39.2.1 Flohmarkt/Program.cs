﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BwInf_39._2._1_Flohmarkt {
    class Program {
        static void Main(string[] args) {
            simulatedAnnealing simAnn = new simulatedAnnealing(readData(2), 1000, 10, 24,70000,0.99994);
            simAnn.setRandomPos3();
            Console.ReadLine();

            simAnn.simulate1();
            Console.WriteLine("bestenergy: " + simAnn.bestEnergy());
            Console.WriteLine(simAnn.anfragen.verwendet.Count);
            simAnn.printFinish(simAnn.anfragen.verwendet);
            Console.ReadLine();
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
