using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BwInf_39._2._1_Flohmarkt {
    class Program {
        static void Main(string[] args) {
            //Parameter aus Konsole abfragen
            (int dataSetNumber, int duration, int starttime, int streetLength, List<int> borders,
            int positioning, bool optimalPos, int positioningArgs,
            bool simulate, int runs, int startTemperature, double tempDecreaseRate, int move, int energy) = getUserInput();
      
            //lese und validiere Daten
            List<Registration> registrations = readData(dataSetNumber, Environment.CurrentDirectory);//System.IO.Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName
            (List<Registration> newRegistrations, bool valid) validated = validateData(registrations, streetLength, starttime, duration);
            if (validated.valid) { registrations = validated.newRegistrations; }
            else { return; }

            //run program
            solver solverObj = new solver(dataSetNumber, registrations, streetLength, starttime, duration, startTemperature, runs, tempDecreaseRate);
            //solverObj.fileSavePath = Environment.CurrentDirectory + "/data/"; //only when program runs "standalone" as exe
            solverObj.borderPos = borders; //Grenzpositionen festlegen
            //set energy
            if(energy == 0) {   solverObj.energyType = (solverObj.energy, "energy"); }
            else {              solverObj.energyType =(solverObj.energy2, "energy2"); }
            //set move
            if (move == 0) {    solverObj.moveType = (solverObj.move, "move"); }
            else {              solverObj.moveType = (solverObj.move2, "move2"); }
            //set Positions
            if (positioning == 0) { solverObj.setRandomPositions(positioningArgs); }
            else {                  solverObj.setPositions(positioningArgs, optimalPos); }

            if (simulate) {      solverObj.simulate(); }

            solverObj.printSaveResult();
            solverObj.analyseResults();

            solverObj.findFreePositionsInRange(12, 16, 2, 3, 10, 15);

            Console.ReadLine();
        }


        /// <summary> fragt in der Konsole Paramter ab </summary>
        private static (int dataSetNumber, int duration, int starttime, int streetLength , List<int> borders, 
            int positioning, bool optimalPos,int positioningArgs, 
            bool simulate, int runs, int startTemperature, double tempDecreaseRate, int move, int energy) getUserInput() {

            Console.WriteLine();

            Console.Write("dataSetNumber (int): ");
            int dataSetNumber = int.Parse(Console.ReadLine());

            Console.Write("Flohmarktdauer [10]: ");
            string input = Console.ReadLine();
            int duration;
            if (input == "") { duration = 10; }
            else { duration = int.Parse(input); }

            Console.Write("Startzeit [8]: ");
            input = Console.ReadLine();
            int starttime;
            if (input == "") { starttime = 8; }
            else { starttime = int.Parse(input); }

            Console.Write("Flohmarktlänge [1000]: ");
            input = Console.ReadLine();
            int streetLength;
            if (input == "") { streetLength = 1000; }
            else { streetLength = int.Parse(input); }
            
            Console.Write("simulated Annealing? [y/n]: ");
            input = Console.ReadLine();
            bool simulate = false;
            double tempDecreaseRate=0;
            int runs=0;
            int startTemperature=0;
            int move = 0;
            int energy = 0;

            if (input == "" || input == "y") {
                simulate = true;

                Console.Write("simulated Annealing durchläufe [70000]: ");
                input = Console.ReadLine();
                if (input == "") { runs = 70000; }
                else { runs = int.Parse(input); }

                Console.Write("simulated Annealing start Temperatur [25]: ");
                input = Console.ReadLine();
                if (input == "") { startTemperature = 25; }
                else { startTemperature = int.Parse(input); }

                Console.Write("simulated Annealing tempDecreaseRate [0.99995]: ");
                input = Console.ReadLine();
                if (input == "") { tempDecreaseRate = 0.99995; }
                else { tempDecreaseRate = double.Parse(input); }

                Console.Write("Veränderung bei simAnn (move (a) / move2 (b) ) [a]: ");
                input = Console.ReadLine();
                if (input == "" || input == "a") { move = 0; }
                else { move = 1; }

                Console.Write("Energieberechnung bei simAnn (energy (a) / energy2 (b) ) [a]: ");
                input = Console.ReadLine();
                if (input == "" || input == "a") { move = 0; }
                else { energy = 1; }
            }

            Console.Write("borders []: ");
            input = Console.ReadLine();
            List<int> borders=new List<int>();
            if (input == "") { }
            else {
                List<string> bordersString =input.Split(' ').ToList();
                foreach (string b in bordersString) {
                    borders.Add(int.Parse(b));
                }
            }

            Console.Write("setRandomPositions (a) oder setPositions (b) [a]: ");
            input = Console.ReadLine();
            int positioning;
            int positioningArgs = 0;
            bool optimalPos = false;
            if (input == "b") {
                positioning = 1;
                Console.Write("optimal positions? (y/n): ");
                input = Console.ReadLine();
                if (input == "y") {
                    optimalPos = true;
                    Console.Write("nicht sortiert (0); sorted nach Miete (1); sortiert nach Dauer (2), sortiert nach Länge (3) [0]: ");
                    input = Console.ReadLine();
                    if (input == "") { positioningArgs = 0; }
                    else { positioningArgs = int.Parse(input); }
                }
                else { }
            }
            else {
                positioning = 0;
                Console.Write("percentage of rejected registrations [20]: ");
                input = Console.ReadLine();
                if (input == "") { positioningArgs = 20; }
                else { tempDecreaseRate = double.Parse(input); }
            }

            Console.WriteLine();

            return (dataSetNumber, duration, starttime, streetLength, borders, positioning, optimalPos, positioningArgs, simulate, runs, startTemperature, tempDecreaseRate, move, energy);
        }

        /// <summary>
        /// reads data. Flohmarkt files should be named e.g. "flohmarkt 3.txt"
        /// </summary>
        /// <param name="dataSetNumber">number of flohmarkt file</param>
        /// <param name="readFilePath">path to flohmarkt files</param>
        private static List<Registration> readData(int dataSetNumber, string readFilePath) {
            string[] lines = System.IO.File.ReadAllLines(readFilePath + "/flohmarkt " + dataSetNumber + ".txt");
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
