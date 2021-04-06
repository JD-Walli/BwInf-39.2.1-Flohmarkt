using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BwInf_39._2._1_Flohmarkt {
    class Program {
        static void Main(string[] args) {
            int number = 1; int duration = 10; int starttime = 8; int streetLength = 1000; List<Anfrage> anfragen = readData(number);

            (List<Anfrage> newAnfragen, bool valid) validated = validateData(anfragen, streetLength, starttime, duration);
            if (validated.valid) {
                anfragen = validated.newAnfragen;
            }
            else { return; }

            simulatedAnnealing simAnn = new simulatedAnnealing(number, anfragen, streetLength, starttime, duration, 25, 70, 0.99995);
            simAnn.energyType = (simAnn.energy, "energy");
            simAnn.moveType = (simAnn.move4, "move4");
            //simAnn.setRandomPositions2(0);
            simAnn.setPositions5((true, true, true));
            //simAnn.simulate();
            simAnn.finish();
            //simAnn.findFreePositionsInRange(19, 16, 2, 3, 10, 15);

            Console.ReadLine();
        }

        private static List<Anfrage> readData(int number) {
            string[] lines = System.IO.File.ReadAllLines(System.IO.Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + "/flohmarkt " + number + ".txt");
            List<Anfrage> anfragen = new List<Anfrage>();
            for (int i = 1; i < lines.Length; i++) {
                string[] line = lines[i].Split(' ');
                anfragen.Add(new Anfrage(i - 1, int.Parse(line[0].Trim()), int.Parse(line[1].Trim()), int.Parse(line[2].Trim()), 0));
            }
            Console.WriteLine(anfragen.Count + "  " + lines.Length);
            return anfragen;
        }

        private static (List<Anfrage> newAnfragen, bool valid) validateData(List<Anfrage> anfragen, int streetLength, int starttime, int duration) {
            List<int> invalidIDs = new List<int>();
            foreach (Anfrage afr in anfragen) {
                if (afr.mietbeginn < starttime) {
                    Console.WriteLine("invalid data (rent starts to early) at line {0}", afr.id + 1);
                    Console.WriteLine("  rentStart={0}, earliestStart={1}", afr.mietbeginn, starttime);
                    invalidIDs.Add(afr.id);
                }
                if (afr.mietende > starttime + duration) {
                    Console.WriteLine("invalid data (rent ends to late) at line {0}", afr.id + 1);
                    Console.WriteLine("  rentEnd={0}, latestEnd={1}", afr.mietende, (starttime + duration));
                    invalidIDs.Add(afr.id);
                }
                if (afr.länge > streetLength) {
                    Console.WriteLine("invalid data (length to big) at line {0}", afr.id + 1);
                    Console.WriteLine("  länge={0}, streetLength={1}", afr.länge, streetLength);
                    invalidIDs.Add(afr.id);
                }
                if (afr.mietbeginn >= afr.mietende) {
                    Console.WriteLine("invalid data (rent start must be smaller than rent end) at line {0}", afr.id + 1);
                    Console.WriteLine("  rentStart={0}, rentEnd={1}", afr.mietbeginn, afr.mietende);
                    invalidIDs.Add(afr.id);
                }
            }
            if (invalidIDs.Count != 0) {
                Console.WriteLine("remove invalid data (r) or exit program (e)? ");
                if (Console.ReadKey().Key.ToString() == "R") {
                    anfragen.RemoveAll(afr => invalidIDs.Contains(afr.id));
                    Console.WriteLine("\n\n");
                    return (anfragen, true);
                }
                else {
                    Console.WriteLine("\n\n");
                    return (anfragen, false);
                }
            }
            Console.WriteLine("data is vaid!");
            return (anfragen, true);
        }

        
    }
}
