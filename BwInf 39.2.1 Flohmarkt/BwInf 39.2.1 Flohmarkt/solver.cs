using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BwInf_39._2._1_Flohmarkt {
    class solver {
        public (List<Registration> accepted, List<Registration> rejected) registrations;

        readonly int streetLength;
        readonly int duration;
        readonly int starttime;
        public List<int> borderPos; //Stellen, die nicht von einer Anmeldung überschritten werden darf

        private bool[,] unoccupiedFields; //Array mit allen Tisch|Zeit Feldern: true->nicht belegt; false-> mit anmeldung belegt //nur bei Varianten ohne Überschneidungen gebraucht

        readonly int dataSetNumber;
        readonly string programStartTime;
        public List<string> metaToSave = new List<string>();
        public List<string> logToSave = new List<string>() {"" };
        public string fileSavePath = System.IO.Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + "/data/";

        readonly int runs;//anzahl der Durchläufe bei simulated Annealing
        readonly int startTemperature;
        readonly double tempDecreaseRate;
        public (EnDel del, string name) energyType;
        public (moveDel del, string name) moveType;

        public (int bestEnergy, List<int> energies, List<int> overlaps, (List<Registration> accepted, List<Registration> rejected) lastDistribution, (List<Registration> accepted, List<Registration> rejected) bestDistribution) output;

        Random rnd = new Random();

        #region constructors

        public solver(int number, List<Registration> registrations, int streetLength = 1000, int starttime = 8, int duration = 10, int startTemperature = 25, int durchläufe = 70000, double tempDecreaseRate = 0.99995) {
            this.registrations.accepted = registrations;
            this.registrations.rejected = new List<Registration>();
            this.dataSetNumber = number;
            this.streetLength = streetLength;
            this.duration = duration;
            this.starttime = starttime;
            this.startTemperature = startTemperature;
            this.runs = durchläufe;
            this.tempDecreaseRate = tempDecreaseRate;
            energyType = (energy, "energy");
            moveType = (move2, "move4");
            programStartTime = DateTime.Now.ToString("yyMMdd HHmmss") + "";
            unoccupiedFields = new bool[streetLength, duration];
            for (int i = 0; i < unoccupiedFields.GetLength(0); i++) { for (int j = 0; j < unoccupiedFields.GetLength(1); j++) { unoccupiedFields[i, j] = true; } }
            borderPos = new List<int>();
        }

        public solver() { }

        #endregion

        #region setPositions

        //O(registrations.Count+registrations.Count^2)
        /// <summary>
        /// setzt zufällige Positionen, auch auf rejected Liste
        /// </summary>
        /// <param name="percentageRejected">anteil in Prozent der auf die Warteliste gesetzten</param>
        public void setRandomPositions(int percentageRejected) {
            foreach (Registration a in registrations.accepted) {
                a.position = rnd.Next(streetLength - a.rentLength + 1);
            }
            for (int i = 0; i < (((float)percentageRejected / 100) * (registrations.accepted.Count + registrations.rejected.Count)); i++) {
                int rnd1 = rnd.Next(registrations.accepted.Count);
                registrations.rejected.Add(registrations.accepted[rnd1]);
                registrations.accepted.RemoveAt(rnd1);
            }
            metaToSave.Add("Positioning: setRandomPositions2(" + percentageRejected + ")");
        }

        //unsorted random: O(registrations.Count+ registrations.Count* (findFreePositions + reg.duration*reg.length)) = O(registrations.Count* streetLength^2 *reg.duration)
        //unsorted best: O(registrations.Count+ registrations.Count* (findFreePositions +findBestPos+ reg.duration*reg.length)) = O(registrations.Count* streetLength^2 *duration * )
        //sorted best: O(registrations.Count+ registrations.Count*log(registrations.Count)+ registrations.Count* (findFreePositions +findBestPos+ reg.duration*reg.length)) = O(registrations.Count*log(registrations.Count)+ registrations.Count*streetLength^2*duration * 3) 
        //O(cloneList + streetLength*gesamtdauer + registrations.Count*(findFreePositions5+dauer*länge) = 10^4 + registrations.Count*(10^3*reg.länge*reg.dauer))
        /// <summary>
        /// setzt Positionen, auch auf rejected Liste; keine Überschneidungen zugelassen; nach Wunsch: - fängt bei sperrigen registrations an (nach Wunsch gilt A oder Dauer als sperrig)  - immer an Position, die am wenigsten freie Positionen einschließt
        /// </summary>
        /// <param name="sorted">0: nicht sortiert; 1: sorted nach Miete; 2: sortiert nach Dauer, 3: sortiert nach Länge</param>
        /// <param name="optimalPos">true: optimale Position (findBestPosition); false: zufällige Position</param>
        public void setPositions(int sorted, bool optimalPos) {
            (List<Registration> accepted, List<Registration> rejected) registrationsLoc = cloneLists(registrations);
            registrations.accepted.Clear(); registrations.rejected.Clear();

            //sortiere registrations nach Sperrigkeit, wenn gewünscht
            if (sorted!=0) {
                if (sorted==1) { registrationsLoc.accepted.Sort(compareByRent); }
                else if(sorted==2){ registrationsLoc.accepted.Sort(compareByDuration); }
                else if(sorted == 3) { registrationsLoc.accepted.Sort(compareByLength); ; }
            }
            //setze Positionen der Anmeldungen
            foreach (Registration reg in registrationsLoc.accepted) {
                List<int> freePositions = findFreePositions(unoccupiedFields, reg); //alle Positionen auf denen keine Überschneidungen auftreten
                if (freePositions.Count > 0) {
                    //wenn gewünscht optimale Position, sonst zufällige
                    if (optimalPos) {
                        reg.position = findBestPosition(unoccupiedFields, reg, freePositions)[0];
                    }
                    else { reg.position = freePositions[rnd.Next(freePositions.Count)]; }
                    registrations.accepted.Add(reg);
                    unoccupiedFields = setRegUnoccupiedFields(unoccupiedFields, reg, false); //unoccupiedFields updaten
                }
                else {
                    reg.position = -1;
                    registrations.rejected.Add(reg);
                }
            }
            output = (energy3(registrations.accepted), new List<int>(), new List<int>(), registrations, registrations);
            metaToSave.Add("Positioning: setPositions (" + (sorted!=0 ? "sorted " + (sorted==1 ? "by compareByRent; " : (sorted == 2 ? "by compareByDuration; " : "by compareByLength; ")) : "not Sorted; ") + (optimalPos ? "optimalPosition)" : "randomPosition)"));
        }

        #endregion

        //simulated Annealing
        public void simulate() {
            double temp = startTemperature;
            int currentEnergy = energyType.del(registrations.accepted, startTemperature);//variabel
            List<int> energies = new List<int>();//Datenliste für log/meta Datei
            List<int> overlaps = new List<int>();//Datenliste für log/meta Datei
            (List<Registration> accepted, List<Registration> rejected) bestDistribution; bestDistribution.accepted = new List<Registration>(); bestDistribution.rejected = new List<Registration>();
            (List<Registration> accepted, List<Registration> rejected) currentRegistrations; currentRegistrations.accepted = new List<Registration>(); currentRegistrations.rejected = new List<Registration>();
            (List<Registration> accepted, List<Registration> rejected) changedRegistrations; changedRegistrations.accepted = new List<Registration>(); changedRegistrations.rejected = new List<Registration>();
            changedRegistrations = cloneLists(registrations); currentRegistrations = cloneLists(registrations);
            int bestEnergy = energy3(currentRegistrations.accepted);
            bestDistribution = currentRegistrations;

            for (int i = 0; i < runs; i++) {
                //Veränderung machen und Energie berechnen
                changedRegistrations = moveType.del(changedRegistrations, temp); //variabel
                int newEnergy = energyType.del(changedRegistrations.accepted, temp);//variabel
                logToSave[logToSave.Count - 1] += " " + newEnergy;
                if (i % 100 == 0) Console.WriteLine(i + "  " + currentEnergy);
                //überprüfen ob Änderung angenommen wird
                if (newEnergy <= currentEnergy || rnd.NextDouble() < Math.Exp(-(newEnergy - currentEnergy) / temp)) {
                    currentEnergy = newEnergy;
                    logToSave[logToSave.Count - 1] += " " + sumOverlap(changedRegistrations.accepted).number; logToSave.Add("");
                    currentRegistrations = cloneLists(changedRegistrations);
                    //bestDistribution neu festlegen
                    if (sumOverlap(currentRegistrations.accepted).number == 0 && energy3(currentRegistrations.accepted) < bestEnergy) {
                        bestEnergy = energy3(currentRegistrations.accepted);
                        bestDistribution = cloneLists(changedRegistrations);
                    }
                }
                else {
                    changedRegistrations = cloneLists(currentRegistrations);
                }
                energies.Add(energy3(currentRegistrations.accepted));
                overlaps.Add(sumOverlap(currentRegistrations.accepted).number);
                //temperature verkleinern
                temp *= tempDecreaseRate;
            }

            Console.WriteLine("done");

            output = (bestEnergy, energies, overlaps, currentRegistrations, bestDistribution);
            registrations = cloneLists(currentRegistrations);
        }

        #region moves
        
        //O(registrations.count)
        /// <summary>
        ///verschieben und swappen; überschneidungen zulassen
        /// </summary>
        public (List<Registration> accepted, List<Registration> rejected) move((List<Registration> accepted, List<Registration> rejected) registrations, double temp) {
            int rnd1 = rnd.Next(100);
            if (rnd1 < 50 && registrations.accepted.Count > 0) { //verschiebe
                int index = rnd.Next(registrations.accepted.Count());
                int x = rnd.Next(streetLength - registrations.accepted[index].rentLength + 1) - registrations.accepted[index].position;
                int move = (int)(x * (temp / startTemperature)); //kleinere Veränderungsschritte bei sinkender Temperatur
                move = (move == 0) ? ((x > 0) ? +1 : -1) : move;
                registrations.accepted[index].position += move;
                logToSave.Add("verschiebe");
            }
            else {
                int rnd2 = rnd.Next(100);
                if ((rnd2 < 50 || registrations.rejected.Count == 0) && registrations.accepted.Count > 0) {//accepted->rejected
                    int index = rnd.Next(registrations.accepted.Count);
                    registrations.rejected.Add(registrations.accepted[index]);
                    registrations.accepted.RemoveAt(index);
                    logToSave.Add("swap->rejected");
                }
                else {//rejected->accepted
                    int index = rnd.Next(registrations.rejected.Count);
                    registrations.rejected[index].position = rnd.Next(streetLength - registrations.rejected[index].rentLength + 1);
                    registrations.accepted.Add(registrations.rejected[index]);
                    registrations.rejected.RemoveAt(index);
                    logToSave.Add("swap->accepted");
                }
            }
            return (registrations.accepted, registrations.rejected);
        }

        //O(findFreePositions+reg.length*reg.duration+ log(freePositions.count)+registrations.Count)
        /// <summary>
        ///verschieben und swappen ohne Überschneidungen (nur an Position wo sicher keine Überschneidungen auftreten)
        /// </summary>
        public (List<Registration> accepted, List<Registration> rejected) move2((List<Registration> accepted, List<Registration> rejected) registrations, double temp) {
            int rnd1 = rnd.Next(100);
            if (rnd1 < 50 && registrations.accepted.Count > 0) { //verschiebe
                int index = rnd.Next(registrations.accepted.Count());
                int x = rnd.Next(streetLength - registrations.accepted[index].rentLength + 1) - registrations.accepted[index].position;
                int move = x;
                move = (int)(move * (temp / startTemperature));//kleinere Veränderungsschritte bei sinkender Temperatur
                move = (move == 0) ? ((x > 0) ? +1 : -1) : move;

                List<int> freePositions = findFreePositions(unoccupiedFields, registrations.accepted[index]);
                if (freePositions.Count > 0) {
                    unoccupiedFields = setRegUnoccupiedFields(unoccupiedFields, registrations.accepted[index], true);
                    //zufälliger Wert möglicherweise nicht in freePositions; daher wird nächster Wert von findclosestVal gesucht
                    registrations.accepted[index].position = freePositions[findClosestValue(registrations.accepted[index].position + move, freePositions)];
                    unoccupiedFields = setRegUnoccupiedFields(unoccupiedFields, registrations.accepted[index], false);
                }
                logToSave.Add("verschiebe");
            }
            else {// swappe
                int rnd2 = rnd.Next(100);
                if ((rnd2 < 50 || registrations.rejected.Count == 0) && registrations.accepted.Count > 0) {//accepted->rejected
                    int index = rnd.Next(registrations.accepted.Count);
                    registrations.rejected.Add(registrations.accepted[index]);
                    unoccupiedFields = setRegUnoccupiedFields(unoccupiedFields, registrations.accepted[index], true);
                    registrations.accepted.RemoveAt(index);
                    logToSave.Add("swap->rejected");
                }
                else {//rejected->accepted
                    int index = rnd.Next(registrations.rejected.Count);
                    List<int> freePositions = findFreePositions(unoccupiedFields, registrations.rejected[index]);
                    if (freePositions.Count > 0) {
                        registrations.rejected[index].position = freePositions[rnd.Next(freePositions.Count)];
                        registrations.accepted.Add(registrations.rejected[index]);
                        unoccupiedFields = setRegUnoccupiedFields(unoccupiedFields, registrations.rejected[index], false);
                        registrations.rejected.RemoveAt(index);
                        logToSave.Add("swap->accepted");
                    }
                }
            }
            return (registrations.accepted, registrations.rejected);
        }

        #endregion

        #region energy

        //O(registrations.Count)
        /// <summary>
        /// summiert die Miete, die der Veranstalter mit gegebener Verteilung einnimmt. Überschneidungen nicht berücksichtigt!
        /// </summary>
        /// <returns>-rent</returns>
        private int sumRent(List<Registration> registrationsLoc) {
            int energy = 0;
            for (int i = 0; i < registrationsLoc.Count; i++) {
                energy -= registrationsLoc[i].rentLength * registrationsLoc[i].rentDuration;
            }
            return energy;
        }

        //O(registrations.Count)
        /// <summary>
        /// summiert die Miete aller Anmeldungen, auch die der abgelehnten
        /// </summary>
        private int sumAllRent((List<Registration> accepted, List<Registration> rejected) registrationsLoc) {
            List<Registration> mergedRegistrations = new List<Registration>();
            foreach (Registration reg in registrationsLoc.accepted) { mergedRegistrations.Add(reg.clone()); }
            mergedRegistrations.AddRange(registrationsLoc.rejected);
            return sumRent(mergedRegistrations);
        }

        //O(registrations.Count^2)
        /// <summary>
        /// summiert die Überschneidung aller Anmeldungen und zählt die Anzahl der Überschneidungen
        /// </summary>
        /// <returns>Tuple (anzahl der Überschneidungen, summe der Überschneidungen)</returns>
        private (int number, int sum) sumOverlap(List<Registration> registrationsLoc) {
            int sum = 0; int number = 0;
            for (int i = 0; i < registrationsLoc.Count; i++) {
                for (int j = 0; j < i; j++) {
                    if (registrationsLoc[i].id != registrationsLoc[j].id) {
                        int overlap = registrationsLoc[i].overlap(registrationsLoc[j]);
                        sum += overlap;
                        number += (overlap > 0 ? 1 : 0);
                    }
                }
            }
            return (number, sum);
        }

        //O(registrations.Count + sumOverlap + registrations.Count*borders)=O(registrations.Count + registrations.count^2 + registrations.Count*borders)
        /// <summary>
        /// berechnet energy:
        /// -rent + n*overlap
        /// </summary>
        /// <param name="registrationsLoc">nur auf dem Feld plazierte Anmeldungen, keine auf Warteliste</param>
        /// <returns>-rent + n*overlap</returns>
        public int energy(List<Registration> registrationsLoc, double temperature) {
            int energy = sumRent(registrationsLoc);
            energy += sumOverlap(registrationsLoc).sum * (int)((1 - (temperature / startTemperature) * 20) + 1); //Überschneidung wird stärker bestraft, je größer die Temperatur wird
            foreach (Registration reg in registrationsLoc) { //border überschreitung prüfen
                foreach (int border in borderPos) {
                    if (reg.position < border && reg.position + reg.rentLength > border) {
                        energy += 2 * reg.rentLength * reg.rentDuration;
                    }
                }
            }
            return energy;
        }

        //O(registrations.Count*checkIfOverlap) = O(registrations.Count^2)
        /// <summary>
        ///  berechnet energy:
        ///  - (Miete aller Anmeldungen die sich nicht überschneiden)
        /// </summary>
        public int energy2(List<Registration> registrationsLoc, double temperature = 0) {
            int energy = 0;
            foreach (Registration reg in registrationsLoc) {
                if (checkIfOverlap(reg, registrationsLoc) == false) {
                    energy -= reg.rentLength * reg.rentDuration;
                }
                foreach (int border in borderPos) { //Borderüberschreitungen prüfen und bestrafen
                    if (reg.position < border && reg.position + reg.rentLength > border) {
                        energy += 2 * reg.rentLength * reg.rentDuration;
                    }
                }
            }
            return energy;
        }

        //O(registrations.Count+sumOverlap) = O(registrations.Count^2+registrations.Count)
        /// <summary>
        ///  berechnet energy:
        ///  -rent+overlap; berechnet Kosten, die für das Diagramm accepted werden
        /// </summary>
        /// <param name="registrationsLoc"></param>
        /// <returns></returns>
        private int energy3(List<Registration> registrationsLoc) {
            int energy = 0;
            for (int i = 0; i < registrationsLoc.Count; i++) {
                energy -= registrationsLoc[i].rentLength * registrationsLoc[i].rentDuration;
            }
            energy += sumOverlap(registrationsLoc).sum;
            return energy;
        }

        #endregion

        #region printSavePlot

        /// <summary>
        /// prints result und speicher-dialog in Konsole
        /// </summary>
        public void printSaveResult() {
            Console.WriteLine("\nLETZTE VERTEILUNG");
            printEnding(output.lastDistribution);
            Console.WriteLine("\nBESTE VERTEILUNG");
            printEnding(output.bestDistribution);
            Console.Write("\nplot energies (y/n): ");
            if (Console.ReadLine() == "y") {
                plotEnergy(output.energies);
            }
            try {
                Console.Write("save result, meta, log (y/n y/n y/n): ");
                string[] input = Console.ReadLine().Split(' ');
                if (input[0] == "y") {
                    saveResult(output.bestDistribution);
                    Console.WriteLine("saved result");
                }
                if (input[1] == "y") {
                    saveMeta(output.energies, output.overlaps, output.bestDistribution, output.bestEnergy);
                    Console.WriteLine("saved meta");
                }
                if (input[2] == "y") {
                    saveLog();
                    Console.WriteLine("saved log");
                }
            }
            catch (IndexOutOfRangeException) {

            }
        }

        /// <summary>
        /// prints infos (sumOverlap.Anzahl)
        /// </summary>
        public void printEnding((List<Registration> accepted, List<Registration> rejected) registrationsLoc) {
            Console.WriteLine(registrationsLoc.accepted.Count + " accepted;  " + registrationsLoc.rejected.Count + " rejected");
            Console.WriteLine(sumOverlap(registrationsLoc.accepted).number + " Überschneidungen");
            Console.WriteLine("Mietsumme aller Verwendeten: " + sumRent(registrationsLoc.accepted) + ";   Mietsumme - Überschneidungen: " + energy3(registrationsLoc.accepted)); ;
        }

        /// <summary>
        /// plottet gegebene Energien in Graph; x=zeitpunkt y=energie
        /// </summary>
        /// <param name="energies">liste mit Energien</param>
        private void plotEnergy(List<int> energies) {
            List<float> xVals = new List<float>();
            List<float> yVals = new List<float>();
            for (int i = 0; i < energies.Count; i++) {
                xVals.Add(i); yVals.Add(energies[i]);
            }
            Application.Run(new chart(xVals.ToArray(), yVals.ToArray()));
        }


        /// <summary>
        /// speichert ergebnis (Positionen der Anmeldungen) in data Ordner
        /// </summary>
        public void saveResult((List<Registration> accepted, List<Registration> rejected) registrations) {
            string filename = fileSavePath + dataSetNumber + " savedResult " + programStartTime + ".csv";
            StreamWriter txt = new StreamWriter(filename);
            txt.WriteLine("Mietbegin Mietende Länge ID position");
            foreach (var reg in registrations.accepted) {
                txt.WriteLine("{0} {1} {2} {3} {4}", reg.rentStart, reg.rentEnd, reg.rentLength, reg.id, reg.position);
            }
            foreach (var reg in registrations.rejected) {
                txt.WriteLine("{0} {1} {2} {3} -1", reg.rentStart, reg.rentEnd, reg.rentLength, reg.id);
            }
            txt.Close(); txt.Dispose();
        }

        /// <summary>
        /// speichert meta daten (durchläufe, starttemperatur, ... und die energien vom simAnn im Zeitverlauf)  in data Ordner
        /// </summary>
        public void saveMeta(List<int> energies, List<int> overlaps, (List<Registration> accepted, List<Registration> rejected) bestDistribution, int bestEnergy) {
            string filename = fileSavePath + dataSetNumber + " savedMeta " + programStartTime + ".csv";
            StreamWriter txt = new StreamWriter(filename);
            txt.WriteLine("SIMANN META");
            txt.WriteLine("Anzahl Durchläufe: " + runs);
            txt.WriteLine("Starttemperatur: " + startTemperature);
            txt.WriteLine("Verkleinerungsrate: " + tempDecreaseRate);
            txt.WriteLine();
            txt.WriteLine("BESTE VERTEILUNG");
            txt.WriteLine("Anzahl Anmeldungen: " + (bestDistribution.accepted.Count + bestDistribution.rejected.Count));
            txt.WriteLine("   davon abgelehnt: " + bestDistribution.rejected.Count);
            txt.WriteLine("Anzahl Überschneidungen: " + sumOverlap(bestDistribution.accepted).number);
            txt.WriteLine("Energie/-Mieteinnahmen: " + bestEnergy);
            txt.WriteLine();
            txt.WriteLine("Energieberechnung: " + energyType.name);
            txt.WriteLine("Moves: " + moveType.name);
            foreach (string s in metaToSave) {
                txt.WriteLine(s);
            }
            txt.WriteLine("\n Energie, Überschneidungen im Zeitverlauf");
            for (int i = 0; i < energies.Count; i++) {
                txt.WriteLine(energies[i] + "  " + overlaps[i]);
            }
            txt.Close(); txt.Dispose();
        }

        /// <summary>
		/// speichert log daten in data Ordner
		/// </summary>
		public void saveLog() {
            string filename = fileSavePath + dataSetNumber + " savedLog " + programStartTime + ".csv";
            StreamWriter txt = new StreamWriter(filename);
            foreach (string s in logToSave) {
                txt.WriteLine(s);
            }
            txt.Close(); txt.Dispose();
        }

        #endregion

        #region specialFunctions

        //O(registrations.Count)
        /// <summary>
        /// prüft, ob die gegebene Anmeldung eine andere in der Liste überschneidet
        /// </summary>
        /// <param name="reg">zu prüfende Anmeldung</param>
        /// <param name="registrationsLoc">Liste mit allen Anmeldungen, mit denen geprüft werden soll</param>
        /// <returns>true: Überschneidung; false: keine Überschneidung</returns>
        private bool checkIfOverlap(Registration reg, List<Registration> registrationsLoc) {
            for (int i = 0; i < registrationsLoc.Count; i++) {
                if (registrationsLoc[i].overlap(reg) > 0 && reg.id != registrationsLoc[i].id) {
                    return true;
                }
            }
            return false;
        }


        //O(streetLength*(borders.Count+reg.length*reg.duration))
        /// <summary>
        /// findet alle möglichen Positionen für eine gegebene Anmeldung, die keine Grenze überschreitet und an denen keine Überschneidung auftritt auf Basis von occupiedFields[]
        /// </summary>
        /// <param name="reg">Anmeldung</param>
        /// <param name="unoccupiedFieldsLoc">2D Array das die Ort-Zeit-tafel darstellt. true: frei; false: besetzt</param>
        /// <returns>Liste mit allen Positionen im Flohmarkt bei denen für die Anmeldung keine Überschneidung auftritt</returns>
        private List<int> findFreePositions(bool[,] unoccupiedFieldsLoc, Registration reg) {
            List<int> positions = new List<int>();
            for (int x = 0; x < streetLength - reg.rentLength; x++) {
                bool crossBorder = false;
                foreach (int border in borderPos) { //checkt ob aktuelle Position borders überschneiden würde (Erweiterung)
                    if (x < border && x + reg.rentLength > border) { crossBorder = true; break; }
                }
                if (crossBorder == false) {
                    //geht alle Tisch|Uhrzeit Felder durch, die an dieser Position besetzt sein würden; wenn eines dieser Felder schon besetzt wird, ist die Position nicht möglich ohne Überschneidungen
                    bool horizontalPosition = true;
                    for (int j = 0; j < reg.rentLength; j++) {
                        bool vertikalPosition = true;
                        for (int y = reg.rentStart - starttime; y < reg.rentEnd - starttime; y++) {
                            if (unoccupiedFieldsLoc[x + j, y] == false) { vertikalPosition = false; break; }
                        }
                        if (vertikalPosition == false) { horizontalPosition = false; x += j; break; }
                    }
                    if (horizontalPosition) {
                        positions.Add(x);
                    }
                }
            }
            return positions;
        }

        //O(log(list.count))
        /// <summary>
        /// findet nähesten Wert zu targetvalue in einer int liste
        /// </summary>
        /// <param name="target">Wert, dessen nähester aus Liste gefunden werden soll</param>
        /// <returns>integer with index of closest value in list</returns>
        private int findClosestValue(int target, List<int> list) {
            int index;
            if (target >= list[list.Count - 1]) { index = list.Count - 1; }
            else if (target <= list[0]) { index = 0; }
            else {
                index = list.BinarySearch(target);
                if (index <= 0) {
                    index = ~index;
                    index = (list[index] - target < target - list[index - 1]) ? index : index - 1;
                }
            }
            return index;
        }

        //O(freePositions.Count*getSpaceAround) = 10^7 = 10^3*(dauer*10^3+länge*10)
        /// <summary>
        /// findet beste Position für gegebene Anmeldung in einer Liste von freien Positionen
        /// überprüft für jede freie Position, wie nah ihr Abstand zu den nächsten Anmeldungen ist
        /// </summary>
        private List<int> findBestPosition(bool[,] unoccupiedFieldsLoc, Registration reg, List<int> freePositions) {
            (int smallestArea, List<int> positions) best = (int.MaxValue, new List<int>() { -2 });
            for (int i = 0; i < freePositions.Count; i++) {
                (int left, int right, int above, int below) = getSpaceAround(unoccupiedFieldsLoc, reg, freePositions[i]);
                int area = Math.Min(left, right) + Math.Min(above, below); //Summe von (kleinste Außenfläche rechts links) und (kleinste Außenfläche oben unten)
                if (area < best.smallestArea) {
                    best = (area, new List<int>() { freePositions[i] });
                }
                else if (area == best.smallestArea) {
                    best.positions.Add(freePositions[i]);
                }
            }
            return best.positions;
        }

        //O(reg.duration*streetLength + reg.length*duration); worst: dauer=10, länge=1 -> ca 10000 bzw 10^4 = dauer*10^3+länge*10
        /// <summary>
        /// berechnet an eine Anmeldung grenzende Fläche auf der Zeit-Ort-Tafel bis zur nächsten Anmeldung; 
        /// </summary>
        /// <param name="unoccupiedFieldsLoc">bool array mit unbelegten Feldern</param>
        /// <param name="position">position von reg</param>
        /// <returns></returns>
        private (int left, int right, int above, int below) getSpaceAround(bool[,] unoccupiedFieldsLoc, Registration reg, int position) {
            int left = 0;
            for (int i = reg.rentStart - starttime; i < reg.rentEnd - starttime; i++) {
                for (int j = position - 1; j >= 0; j--) {
                    if (unoccupiedFieldsLoc[j, i] == false) { break; }
                    left++;
                }
            }

            int right = 0;
            for (int i = reg.rentStart - starttime; i < reg.rentEnd - starttime; i++) {
                for (int j = position + reg.rentLength; j < streetLength; j++) {
                    if (unoccupiedFieldsLoc[j, i] == false) { break; }
                    right++;
                }
            }

            int above = 0;
            for (int i = position; i < position + reg.rentLength; i++) {
                for (int j = reg.rentStart - starttime - 1; j >= 0; j--) {
                    if (unoccupiedFieldsLoc[i, j] == false) { break; }
                    above++;
                }
            }

            int below = 0;
            for (int i = position; i < position + reg.rentLength; i++) {
                for (int j = reg.rentEnd - starttime; j < duration; j++) {
                    if (unoccupiedFieldsLoc[i, j] == false) { break; }
                    below++;
                }
            }

            return (left, right, above, below);
        }


        //O(registrations.Count)
        /// <summary>
        /// gibt ein geklontes (Wert-kopiertes) Anmeldungen-Tuple (accepted, rejected) zurück.
        /// </summary>
        /// <param name="registrationsLoc">zu klonendes Tuple</param>
        /// <returns></returns>
        public (List<Registration> accepted, List<Registration> rejected) cloneLists((List<Registration> accepted, List<Registration> rejected) registrationsLoc) {
            (List<Registration> accepted, List<Registration> rejected) returnRegistrations; returnRegistrations.accepted = new List<Registration>(); returnRegistrations.rejected = new List<Registration>();
            foreach (Registration reg in registrationsLoc.accepted) { returnRegistrations.accepted.Add(reg.clone()); }
            foreach (Registration reg in registrationsLoc.rejected) { returnRegistrations.rejected.Add(reg.clone()); }
            return returnRegistrations;
        }


        /// <summary>
        /// vergleicht zwei Anmeldungen nach Miete; x>y -> -1  x<y ->1
        /// </summary>
        private static int compareByRent(Registration x, Registration y) {
            int rentX = x.rentDuration * x.rentLength;
            int rentY = y.rentDuration * y.rentLength;
            if (rentX > rentY) { return -1; } else if (rentX < rentY) { return +1; }
            return 0;
        }

        /// <summary>
        /// vergleicht zwei Anmeldungen nach Dauer; x.duration>y.duration -> -1  x.duration<y.duration ->1
        /// </summary>
        private static int compareByDuration(Registration x, Registration y) {
            if (x.rentDuration > y.rentDuration) { return -1; } else if (x.rentDuration < y.rentDuration) { return +1; } else if (x.rentLength > y.rentLength) { return -1; } else if (x.rentLength < y.rentLength) { return +1; }
            return 0;
        }

        /// <summary>
        /// vergleicht zwei Anmeldungen nach Länge; x.length>y.length -> -1  x.length<y.length ->1
        /// </summary>
        private static int compareByLength(Registration x, Registration y) {
            if (x.rentLength > y.rentLength) { return -1; } else if (x.rentLength < y.rentLength) { return +1; } else if (x.rentDuration > y.rentDuration) { return -1; } else if (x.rentDuration < y.rentDuration) { return +1; }
            return 0;
        }

        /// <summary>
        /// gibt unoccupiedFieldsLoc zurück; setzt den Bereich der Anmeldung im unoccupiedFieldsLoc Array entweder true oder false 
        /// </summary>
        /// <param name="reg">Registration, deren Werte neu besetzt werden müssen</param>
        /// <param name="boolVal">neuer Wert im Array. true: nicht besetzt; false : besetzt</param>
        private bool[,] setRegUnoccupiedFields(bool[,] unoccupiedFieldsLoc, Registration reg, bool boolVal) {
            for (int i = reg.rentStart - starttime; i < reg.rentEnd - starttime; i++) {
                for (int j = reg.position; j < reg.position + reg.rentLength; j++) {
                    unoccupiedFieldsLoc[j, i] = boolVal;
                }
            }
            return unoccupiedFieldsLoc;
        }

        public delegate int EnDel(List<Registration> registrationsLoc, double temperature);
        public delegate (List<Registration> accepted, List<Registration> rejected) moveDel((List<Registration> accepted, List<Registration> rejected) registrations, double temp);

        #endregion

        #region Erweiterungen

        /// <summary>
        /// findet freie Positionen für alle Anmeldungsparameter im angegebenen Spektrum
        /// </summary>
        /// <param name="minStartTime"></param>
        /// <param name="maxEndTime"></param>
        /// <param name="minDuration"></param>
        /// <param name="maxDuration"></param>
        /// <param name="minLength"></param>
        /// <param name="maxLength"></param>
        /// <param name="maxRent"></param>
        public void findFreePositionsInRange(int minStartTime, int maxEndTime, int minDuration, int maxDuration, int minLength, int maxLength, int maxRent = int.MaxValue) {
            if (minStartTime >= maxEndTime) { Console.WriteLine("invalid input (minStartTime >= maxEndTime)"); return; }
            if (minDuration >= maxDuration) { Console.WriteLine("invalid input (minDauer >= maxDauer)"); return; }
            if (minLength >= maxLength) { Console.WriteLine("invalid input (minLänge >= maxLänge)"); return; }
            if (minStartTime + maxDuration > maxEndTime) { Console.WriteLine("invalid input (minStartTime+dauer > maxEndTime)"); return; }
            if (maxDuration * maxLength > maxRent) { Console.WriteLine("invalid input (länge+dauer > maxKosten)"); return; }
            if (maxLength > streetLength) { Console.WriteLine("invalid input (maxLänge > streetLength)"); return; }
            if (maxEndTime > starttime + duration) { Console.WriteLine("invalid input (maxEndTime> startzeit + duration)"); return; }

            //alle Parameterkombinationen in angegebenen Grenzen durchgehen und jeweils Positionen suchen
            List<(List<int> positions, Registration reg)> positions = new List<(List<int>, Registration)>();
            for (int länge = minLength; länge < maxLength; länge++) {
                for (int dauer = minDuration; dauer <= maxDuration; dauer++) {
                    for (int starttime = minStartTime; starttime <= maxEndTime - dauer; starttime++) {
                        if (länge * dauer <= maxRent) {
                            Registration thisreg = new Registration(-1, starttime, starttime + dauer, länge, 0);
                            List<int> thisPositions = findFreePositions(unoccupiedFields, thisreg);
                            if (thisPositions.Count != 0) {
                                positions.Add((thisPositions, thisreg));
                            }
                        }
                    }
                }
            }

            //Ausgabe
            foreach ((List<int> positions, Registration reg) pos in positions) {
                pos.reg.print();
                List<int> bestPositions = findBestPosition(unoccupiedFields, pos.reg, pos.positions);
                foreach (int pos2 in bestPositions) {
                    Console.Write("  " + pos2);
                }
                Console.WriteLine();
            }
            if (positions.Count() == 0) {
                Console.WriteLine("\nkeine freien Positionen für eingegebene Eckdaten gefunden");
            }
        }

        /// <summary>
        /// analysiert Ergebnis
        /// schätzt Werte für: Toilettenbesuchen pro Stunde, Parkplätze pro Stunde, neu ankommende Fahrzeuge pro Stunde
        /// berechnet Werte für: Anzahl an Anmeldungen pro Stunde, Einnachmen pro Stunde
        /// </summary>
        public void analyseResults() {
            Console.WriteLine("\nANALYSE RESULT:");
            double carThreshold = 5;//ab wievielen Metern Stand braucht man ein zusätzlichen Autostellplatz?
            int hoursBetweenToiletUse = 2;// alle wieviel Stunden geht man durchschnittlich auf Toilette? (abhängig von Wetter, Essensangebot, Geschlechterverteilung,...)
            int[] parkingSpots = new int[duration];
            int[] registrationsNum = new int[duration];
            int[] toiletUsesPerHour = new int[duration];
            int[] newCarsPerHour = new int[duration];

            for (int t = starttime; t < starttime + duration; t++) {
                foreach (Registration reg in registrations.accepted) {
                    if (reg.rentStart <= t && reg.rentEnd > t) {
                        parkingSpots[t - starttime] += (int)Math.Ceiling((double)reg.rentLength / (double)carThreshold);
                        registrationsNum[t - starttime]++;
                    }

                    if (t - reg.rentStart >= 0 && (t - reg.rentStart) % hoursBetweenToiletUse == 0) {
                        toiletUsesPerHour[t - starttime]++;
                    }

                    if (reg.rentStart == t) {
                        newCarsPerHour[t - starttime] += (int)Math.Ceiling((double)reg.rentLength / (double)carThreshold);
                    }
                }
            }
            printArray(registrationsNum, "registrations per hour");
            printArray(parkingSpots, "parking spots per hour");
            printArray(newCarsPerHour, "new Cars Per Hour");
            printArray(toiletUsesPerHour, "toilet Uses Per Hour");

            int[] earningsPerHour = new int[duration];
            for (int t = 0; t < unoccupiedFields.GetLength(1); t++) {
                for (int x = 0; x < unoccupiedFields.GetLength(0); x++) {
                    if (unoccupiedFields[x, t] == false) {
                        earningsPerHour[t]++;
                    }
                }
            }
            printArray(earningsPerHour, "earnings Per Hour");

        }

        /// <summary>
        /// prints Array in zwei Zeilen
        /// </summary>
        private void printArray(int[] array, string name) {
            Console.WriteLine("  " + name.ToUpper() + ":");
            string firstLine = "    ";
            string secondLine = "    ";
            for (int i = 0; i < array.Length; i++) {
                firstLine += "\t" + (i + starttime);
                secondLine += "\t" + array[i];
            }
            Console.WriteLine(firstLine);
            Console.WriteLine(secondLine);
        }

        #endregion
    }
}

