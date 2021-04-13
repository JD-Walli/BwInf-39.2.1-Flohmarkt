using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BwInf_39._2._1_Flohmarkt {
    class Program {
        static void Main(string[] args) {
            int dataSetNumber = 1; int duration = 10; int starttime = 8; int streetLength = 1000; List<Registration> registrations = readData(dataSetNumber);

            (List<Registration> newRegistrations, bool valid) validated = validateData(registrations, streetLength, starttime, duration);
            if (validated.valid) { registrations = validated.newRegistrations; }
            else { return; }

            simulatedAnnealing simAnn = new simulatedAnnealing(dataSetNumber, registrations, streetLength, starttime, duration, 25, 70, 0.99995);
            simAnn.borderPos = new List<int>() { 440, 402 };
            simAnn.energyType = (simAnn.energy, "energy");
            simAnn.moveType = (simAnn.move4, "move4");
            //simAnn.setRandomPositions2(0);
            simAnn.setPositions5((true, true, true));
            //simAnn.simulate();
            simAnn.printSaveResult();
            simAnn.analyseResults();
            //simAnn.findFreePositionsInRange(19, 16, 2, 3, 10, 15);

            Console.ReadLine();
        }

        private static List<Registration> readData(int number) {
            string[] lines = System.IO.File.ReadAllLines(System.IO.Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + "/flohmarkt " + number + ".txt");
            List<Registration> registrations = new List<Registration>();
            for (int i = 1; i < lines.Length; i++) {
                string[] line = lines[i].Split(' ');
                registrations.Add(new Registration(i - 1, int.Parse(line[0].Trim()), int.Parse(line[1].Trim()), int.Parse(line[2].Trim()), 0));
            }
            Console.WriteLine(registrations.Count + "  " + lines.Length);
            return registrations;
        }

        private static (List<Registration> newRegistrations, bool valid) validateData(List<Registration> registrations, int streetLength, int starttime, int duration) {
            List<int> invalidIDs = new List<int>();
            foreach (Registration reg in registrations) {
                if (reg.rentStart < starttime) {
                    Console.WriteLine("invalid data (rent starts to early) at line {0}", reg.id + 1);
                    Console.WriteLine("  rentStart={0}, earliestStart={1}", reg.rentStart, starttime);
                    invalidIDs.Add(reg.id);
                }
                if (reg.rentEnd > starttime + duration) {
                    Console.WriteLine("invalid data (rent ends to late) at line {0}", reg.id + 1);
                    Console.WriteLine("  rentEnd={0}, latestEnd={1}", reg.rentEnd, (starttime + duration));
                    invalidIDs.Add(reg.id);
                }
                if (reg.rentLength > streetLength) {
                    Console.WriteLine("invalid data (length to big) at line {0}", reg.id + 1);
                    Console.WriteLine("  länge={0}, streetLength={1}", reg.rentLength, streetLength);
                    invalidIDs.Add(reg.id);
                }
                if (reg.rentStart >= reg.rentEnd) {
                    Console.WriteLine("invalid data (rent start must be smaller than rent end) at line {0}", reg.id + 1);
                    Console.WriteLine("  rentStart={0}, rentEnd={1}", reg.rentStart, reg.rentEnd);
                    invalidIDs.Add(reg.id);
                }
            }
            if (invalidIDs.Count != 0) {
                Console.WriteLine("remove invalid data (r) or exit program (e)? ");
                if (Console.ReadKey().Key.ToString() == "R") {
                    registrations.RemoveAll(reg => invalidIDs.Contains(reg.id));
                    Console.WriteLine("\n\n");
                    return (registrations, true);
                }
                else {
                    Console.WriteLine("\n\n");
                    return (registrations, false);
                }
            }
            Console.WriteLine("data is valid!");
            return (registrations, true);
        }

        
    }
}
