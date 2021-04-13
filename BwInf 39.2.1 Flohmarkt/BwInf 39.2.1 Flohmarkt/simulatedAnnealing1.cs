using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BwInf_39._2._1_Flohmarkt {
    class simulatedAnnealing {
        public (List<Registration> accepted, List<Registration> rejected) registrations;

        readonly int streetLength;
        readonly int duration;
        readonly int starttime;
        public List<int> borderPos;

        private bool[,] unoccupiedFields; //nur für Varianten ohne Überschneidungen erlaubt/brauchbar

        readonly int dataSetNumber;
        readonly string programStartTime;
        public List<string> metaToSave = new List<string>();
        public List<string> logToSave = new List<string>();

        readonly int runs;
        readonly int startTemperature;
        readonly double tempDecreaseRate;
        public (EnDel del, string name) energyType;
        public (moveDel del, string name) moveType;

        public (int bestEnergy, List<int> energies, List<int> overlaps, (List<Registration> accepted, List<Registration> rejected) lastDistribution, (List<Registration> accepted, List<Registration> rejected) bestDistribution) output;

        Random rnd = new Random();

        #region constructors

        public simulatedAnnealing(int number, List<Registration> registrations, int streetLength = 1000, int starttime = 8, int duration = 10, int startTemperature = 25, int durchläufe = 70000, double tempDecreaseRate = 0.99995) {
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
            moveType = (move4, "move4");
            programStartTime = DateTime.Now.ToString("yyMMdd HHmmss") + "";
            unoccupiedFields = new bool[streetLength, duration];
            for (int i = 0; i < unoccupiedFields.GetLength(0); i++) { for (int j = 0; j < unoccupiedFields.GetLength(1); j++) { unoccupiedFields[i, j] = true; } }
            borderPos = new List<int>();
        }

        public simulatedAnnealing() { }

        #endregion

        #region setPositions
        
        //O(registrations.Count+registrations.Count^2)
        /// <summary>
        /// setzt zufällige Positionen, auch auf rejected Liste
        /// </summary>
        /// <param name="percentageRejected">anteil in Prozent der auf die Warteliste gesetzten</param>
        public void setRandomPositions2(int percentageRejected) {
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

        //sorted&&optimalpos O(cloneList + streetLength*gesamtdauer + registrations.Count*log(registrations.Count) + registrations.Count*(findFreePositions5+findBestPosition5+dauer*länge) = 10^4 + registrations.Count*log(registrations.Count) + registrations.Count*(10^7)
        //sorted&&optimalpos reg O(cloneList + streetLength*gesamtdauer + registrations.Count*log(registrations.Count) + registrations.Count*(findFreePositions5+findBestPosition5+dauer*länge) = 10^4 + registrations.Count*log(registrations.Count) + registrations.Count*(reg.dauer*10^6+reg.länge*10^4)
        //sorted O(cloneList + streetLength*gesamtdauer + registrations.Count*log(registrations.Count) + registrations.Count*(findFreePositions5+dauer*länge) =  10^4 + registrations.Count*log(registrations.Count) + registrations.Count*(10^3*reg.länge*reg.dauer)
        //O(cloneList + streetLength*gesamtdauer + registrations.Count*(findFreePositions5+dauer*länge) = 10^4 + registrations.Count*(10^3*reg.länge*reg.dauer))
        /// <summary>
        /// setzt Positionen, auch auf rejected Liste; keine Überschneidungen zugelassen; nach Wunsch: - fängt bei sperrigen registrations an (nach Wunsch gilt A oder Dauer als sperrig)  - immer an Position, die am wenigsten freie Positionen einschließt
        /// </summary>
        /// <param name="state">Konfiguration; ´sorted: Liste wird vor positionierung sortiert; compare5: compareByRent5 wird als comparer accepted (standard ist compareByRent4); optimal: Anmeldung wird an optimaler Position positioniert, ansonsten an zufälliger Position</param>
        public void setPositions5((bool sorted, bool comparer5, bool optimalPos) state) {
            (List<Registration> accepted, List<Registration> rejected) registrationsLoc = cloneLists(registrations);
            registrations.accepted.Clear(); registrations.rejected.Clear();

            if (state.sorted) {
                if (state.comparer5) { registrationsLoc.accepted.Sort(compareByLength5); }
                else { registrationsLoc.accepted.Sort(compareByRent4); }
            }
            foreach (Registration reg in registrationsLoc.accepted) {
                List<int> freePositions = findFreePositions5(unoccupiedFields, reg);
                if (freePositions.Count > 0) {
                    if (state.optimalPos) {
                        reg.position = findBestPosition5(unoccupiedFields, reg, freePositions)[0];
                    }
                    else { reg.position = freePositions[rnd.Next(freePositions.Count)]; }
                    registrations.accepted.Add(reg);
                    unoccupiedFields = setRegUnoccupiedFields(unoccupiedFields, reg, false);
                }
                else {
                    reg.position = -1;
                    registrations.rejected.Add(reg);
                }
            }
            output = (energyChart(registrations.accepted), new List<int>(), new List<int>(), registrations, registrations);
            metaToSave.Add("Positioning: setPositions5 (" + (state.sorted ? "sorted " + (state.comparer5 ? " by compareByRent5; " : "by compareByRent4; ") : "not Sorted; ") + (state.optimalPos ? "optimalPosition)" : "randomPosition)"));
        }

        #endregion

        // move4 O(cloneList+energy+durchläufe*(move+energy+cloneList+energyChart) = (reg:) registrations.Count^2+durchläufe*(10^3*reg.länge+registrations.Count^2) = (nonreg:) registrations.Count^2+durchläufe*(10^6+registrations.Count^2))
        // move2 O(registrations.Count^2+durchläufe*(registrations.Count+2*registrations.Count^2) = durchläufe*(registrations.Count^2))
        public void simulate() {
            double temp = startTemperature;
            int currentEnergy = energyType.del(registrations.accepted, startTemperature);//variabel
            List<int> energies = new List<int>();
            List<int> overlaps = new List<int>();
            (List<Registration> accepted, List<Registration> rejected) bestDistribution; bestDistribution.accepted = new List<Registration>(); bestDistribution.rejected = new List<Registration>();
            (List<Registration> accepted, List<Registration> rejected) currentRegistrations; currentRegistrations.accepted = new List<Registration>(); currentRegistrations.rejected = new List<Registration>();
            (List<Registration> accepted, List<Registration> rejected) newRegistrations; newRegistrations.accepted = new List<Registration>(); newRegistrations.rejected = new List<Registration>();
            newRegistrations = cloneLists(registrations); currentRegistrations = cloneLists(registrations);
            int bestEnergy = energyChart(currentRegistrations.accepted);
            bestDistribution = currentRegistrations;

            for (int i = 0; i < runs; i++) {
                newRegistrations = moveType.del(newRegistrations, temp); //variabel
                int newEnergy = energyType.del(newRegistrations.accepted, temp);//variabel
                logToSave[logToSave.Count - 1] += " " + newEnergy;
                if (i % 100 == 0) Console.WriteLine(i + "  " + currentEnergy);
                if (newEnergy <= currentEnergy || rnd.NextDouble() < Math.Exp(-(newEnergy - currentEnergy) / temp)) {//|| rnd.NextDouble() < Math.Exp(-(newEnergy - currentEnergy) / temp)
                    currentEnergy = newEnergy;
                    logToSave[logToSave.Count - 1] += " " + sumOverlap(newRegistrations.accepted).anzahl; logToSave.Add("");
                    //Console.WriteLine(i + " : " + currentEnergy + " \t" + newRegistrations.accepted.Count + " " + newRegistrations.rejected.Count + "  " + sumOverlap(newRegistrations.accepted).anzahl + " Üs");
                    currentRegistrations = cloneLists(newRegistrations);
                    if (sumOverlap(currentRegistrations.accepted).anzahl == 0 && energyChart(currentRegistrations.accepted) < bestEnergy) {
                        bestEnergy = energyChart(currentRegistrations.accepted);
                        bestDistribution = cloneLists(newRegistrations);
                    }
                }
                else {
                    newRegistrations = cloneLists(currentRegistrations);
                }
                energies.Add(energyChart(currentRegistrations.accepted));
                overlaps.Add(sumOverlap(currentRegistrations.accepted).anzahl);
                temp *= tempDecreaseRate;//200000, 130, 0.999972 //70000,74,0.99997 //70000,23,0.99994
            }

            Console.WriteLine("done");

            output = (bestEnergy, energies, overlaps, currentRegistrations, bestDistribution);
            registrations = cloneLists(currentRegistrations);
        }

        #region moves
        /// <summary>
        /// verschieben: weiter weg wird unwahrscheinlicher je kleiner temp wird (reg.pos wird als koordinatenursprung angenommen; move=rnd*(temp/startTemp))
        /// </summary>
        public (List<Registration> accepted, List<Registration> rejected) move1((List<Registration> accepted, List<Registration> rejected) registrations, double temp) {
            int index = rnd.Next(registrations.accepted.Count());
            int x = rnd.Next(streetLength - registrations.accepted[index].rentLength + 1) - registrations.accepted[index].position;
            int move = (int)(x * (temp / startTemperature));
            move = (move == 0) ? ((x > 0) ? +1 : -1) : move;
            registrations.accepted[index].position += move;
            return (registrations.accepted, registrations.rejected);
        }

        //O(registrations.count)
        /// <summary>
        ///verschieben (wie move1) und swappen
        /// </summary>
        public (List<Registration> accepted, List<Registration> rejected) move2((List<Registration> accepted, List<Registration> rejected) registrations, double temp) {
            int rnd1 = rnd.Next(100);
            if (rnd1 < 50 && registrations.accepted.Count > 0) { //verschiebe
                registrations = move1(registrations, temp);
                logToSave.Add("verschiebe");
            }
            else {// swappe
                int rnd2 = rnd.Next(100);
                if ((rnd2 < 50 || registrations.rejected.Count == 0) && registrations.accepted.Count > 0) {//reinswap
                    int index = rnd.Next(registrations.accepted.Count);
                    registrations.rejected.Add(registrations.accepted[index]);
                    registrations.accepted.RemoveAt(index);
                    logToSave.Add("swap->rejected");
                }
                else {//rausswap
                    int index = rnd.Next(registrations.rejected.Count);
                    registrations.rejected[index].position = rnd.Next(streetLength - registrations.rejected[index].rentLength + 1);
                    registrations.accepted.Add(registrations.rejected[index]);
                    registrations.rejected.RemoveAt(index);
                    logToSave.Add("swap->accepted");
                }
            }
            return (registrations.accepted, registrations.rejected);
        }

        //O(findFreePositions4+registrations.count= 10^6+10^3*registrations.Count) = O(registrations.count*reg.länge+10^3*reg.länge)
        /// <summary>
        ///verschieben und swappen ohne Überschneidungen (nur an Position wo sicher keine Überschneidungen auftreten)
        /// </summary>
        public (List<Registration> accepted, List<Registration> rejected) move4((List<Registration> accepted, List<Registration> rejected) registrations, double temp) {
            int rnd1 = rnd.Next(100);
            if (rnd1 < 50 && registrations.accepted.Count > 0) { //verschiebe //variabel
                int index = rnd.Next(registrations.accepted.Count());
                int x = rnd.Next(streetLength - registrations.accepted[index].rentLength + 1) - registrations.accepted[index].position;
                int move = x;
                move = (int)(move * (temp / startTemperature)); 
                move = (move == 0) ? ((x > 0) ? +1 : -1) : move;
                List<int> freePositions = findFreePositions5(unoccupiedFields, registrations.accepted[index]);
                if (freePositions.Count > 0) {
                    unoccupiedFields = setRegUnoccupiedFields(unoccupiedFields, registrations.accepted[index], true);
                    registrations.accepted[index].position = freePositions[findClosestValue4(registrations.accepted[index].position + move, freePositions)]; //kleinere Sprünge bei niedrigerer temperature
                    unoccupiedFields = setRegUnoccupiedFields(unoccupiedFields, registrations.accepted[index], false);
                }
                logToSave.Add("verschiebe");
            }
            else {// swappe
                int rnd2 = rnd.Next(100);
                if ((rnd2 < 50 || registrations.rejected.Count == 0) && registrations.accepted.Count > 0) {//reinswap
                    int index = rnd.Next(registrations.accepted.Count);
                    registrations.rejected.Add(registrations.accepted[index]);
                    registrations.accepted.RemoveAt(index);
                    unoccupiedFields = setRegUnoccupiedFields(unoccupiedFields, registrations.accepted[index], true);
                    logToSave.Add("swap->rejected");
                }
                else {//rausswap
                    int index = rnd.Next(registrations.rejected.Count);
                    List<int> freePositions = findFreePositions5(unoccupiedFields, registrations.accepted[index]);
                    if (freePositions.Count > 0) {
                        registrations.rejected[index].position = freePositions[rnd.Next(freePositions.Count)];
                        registrations.accepted.Add(registrations.rejected[index]);
                        registrations.rejected.RemoveAt(index);
                        unoccupiedFields = setRegUnoccupiedFields(unoccupiedFields, registrations.accepted[index], false);
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
        private int sumAllRent((List<Registration> accepted, List<Registration> rejected) registrationsLoc) {
            List<Registration> mergedRegistrations = new List<Registration>();
            foreach (Registration reg in registrationsLoc.accepted) { mergedRegistrations.Add(reg.clone()); }
            mergedRegistrations.AddRange(registrationsLoc.rejected);
            return sumRent(mergedRegistrations);
        }

        //O(registrations.Count^2)
        /// <summary>
        /// summiert die Überschneidung aller Anmeldungen / zählt die Anzahl der Überschneidungen
        /// </summary>
        /// <param name="registrationsLoc">Anmeldungen die überprüft werden sollen</param>
        /// <returns>Tuple (anzahl der Überschneidungen, summe der Überschneidungen)</returns>
        private (int anzahl, int summe) sumOverlap(List<Registration> registrationsLoc) {
            int summe = 0; int anzahl = 0;
            for (int i = 0; i < registrationsLoc.Count; i++) {
                for (int j = 0; j < i; j++) {
                    if (registrationsLoc[i].id != registrationsLoc[j].id) {
                        int overlap = registrationsLoc[i].overlap(registrationsLoc[j]);
                        summe += overlap;
                        anzahl += (overlap > 0 ? 1 : 0);
                    }
                }
            }
            return (anzahl, summe);
        }

        //O(sumOverlap+sumRent=registrations.Count^2)
        /// <summary>
        /// -rent + n*overlap
        /// </summary>
        /// <param name="registrationsLoc">nur auf dem Feld plazierte Anmeldungen, keine auf Warteliste</param>
        /// <returns>-rent + n*overlap</returns>
        public int energy(List<Registration> registrationsLoc, double temperature) {
            int energy = sumRent(registrationsLoc);
            energy += sumOverlap(registrationsLoc).summe * (int)((1-(temperature / startTemperature) * 20) + 1);// * (int)(((temperature / startTemperature) * 20) + 1); //Überschneidung wird "wichtiger", je größer die Temperatur wird
            foreach (Registration reg in registrationsLoc) { //border überschreitung prüfen
                foreach (int border in borderPos) {
                    if (reg.position < border && reg.position + reg.rentLength > border) {
                        energy += 2 * reg.rentLength * reg.rentDuration;
                    }
                }
            }
            return energy;
        }

        //O(registrations.Count*checkifOverlap = registrations.Count^2)
        /// <summary>
        /// - (rent aller Anmeldungen die sich nicht überschneiden)
        /// </summary>
        /// <param name="registrationsLoc"></param>
        /// <returns></returns>
        public int energy2(List<Registration> registrationsLoc, double temperature = 0) {
            int energy = 0;
            foreach (Registration reg in registrationsLoc) {
                if (checkIfOverlap3(reg, registrationsLoc) == false) {
                    energy -= reg.rentLength * reg.rentDuration;
                }
                foreach (int border in borderPos) {
                    if (reg.position < border && reg.position + reg.rentLength > border) {
                        energy += 2 * reg.rentLength * reg.rentDuration;
                    }
                }
            }
            return energy;
        }

        //O(registrations.Count+sumOverlap = registrations.Count^2+registrations.Count)
        /// <summary>
        /// -rent+overlap; berechnet Kosten, die für das Diagramm accepted werden
        /// </summary>
        /// <param name="registrationsLoc"></param>
        /// <returns></returns>
        private int energyChart(List<Registration> registrationsLoc) {
            int energy = 0;
            for (int i = 0; i < registrationsLoc.Count; i++) {
                energy -= registrationsLoc[i].rentLength * registrationsLoc[i].rentDuration;
            }
            energy += sumOverlap(registrationsLoc).summe;
            return energy;
        }

        #endregion

        #region ending


        /**<summary>determines further actions (plot energy distribution, save data)</summary>
         **/
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
        /// zeichnet gegebene Energien als Graph; x=zeitpunkt y=energie
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
        /// prints infos (sumOverlap.Anzahl)
        /// </summary>
        /// <param name="registrationsLoc"></param>
        public void printEnding((List<Registration> accepted, List<Registration> rejected) registrationsLoc) {
            Console.WriteLine(registrationsLoc.accepted.Count + " accepted;  " + registrationsLoc.rejected.Count + " rejected");
            Console.WriteLine(sumOverlap(registrationsLoc.accepted).anzahl + " Überschneidungen");
            Console.WriteLine("Mietsumme aller Verwendeten: " + sumRent(registrationsLoc.accepted) + ";   Mietsumme - Überschneidungen: " + energyChart(registrationsLoc.accepted)); ;
        }

        /// <summary>
        /// speichert ergebniss (Positionen der Anmeldungen)
        /// </summary>
        /// <param name="registrations"></param>
        public void saveResult((List<Registration> accepted, List<Registration> rejected) registrations) {
            string filename = dataSetNumber + " savedResult " + programStartTime + ".csv";
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
        /// speichert meta daten (durchläufe, starttemperatur, ... und die energien vom simAnn im Zeitverlauf) 
        /// </summary>
        /// <param name="energies"></param>
        public void saveMeta(List<int> energies, List<int> overlaps, (List<Registration> accepted, List<Registration> rejected) bestDistribution, int bestEnergy) {
            string filename = dataSetNumber + " savedMeta " + programStartTime + ".csv";
            StreamWriter txt = new StreamWriter(filename);
            txt.WriteLine("SIMANN META");
            txt.WriteLine("Anzahl Durchläufe: " + runs);
            txt.WriteLine("Starttemperatur: " + startTemperature);
            txt.WriteLine("Verkleinerungsrate: " + tempDecreaseRate);
            txt.WriteLine();
            txt.WriteLine("BESTE VERTEILUNG");
            txt.WriteLine("Anzahl Anmeldungen: " + (bestDistribution.accepted.Count + bestDistribution.rejected.Count));
            txt.WriteLine("   davon rejected: " + bestDistribution.rejected.Count);
            txt.WriteLine("Anzahl Überschneidungen: " + sumOverlap(bestDistribution.accepted).anzahl);
            txt.WriteLine("Energie: " + bestEnergy);
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
		/// speichert log daten
		/// </summary>
		public void saveLog() {
            string filename = dataSetNumber + " savedLog " + programStartTime + ".csv";
            StreamWriter txt = new StreamWriter(filename);
            foreach (string s in logToSave) {
                txt.WriteLine(s);
            }
            txt.Close(); txt.Dispose();
        }

        #endregion

        #region specialFunctions

        //O(registrations.Count
        /// <summary>
        /// gibt ein geklontes (Wert-kopiertes) Anmeldungen Tuple (accepted, rejected) zurück.
        /// </summary>
        /// <param name="registrationsLoc">zu klonendes Tuple</param>
        /// <returns></returns>
        public (List<Registration> accepted, List<Registration> rejected) cloneLists((List<Registration> accepted, List<Registration> rejected) registrationsLoc) {
            (List<Registration> accepted, List<Registration> rejected) returnRegistrations; returnRegistrations.accepted = new List<Registration>(); returnRegistrations.rejected = new List<Registration>();
            foreach (Registration reg in registrationsLoc.accepted) { returnRegistrations.accepted.Add(reg.clone()); }
            foreach (Registration reg in registrationsLoc.rejected) { returnRegistrations.rejected.Add(reg.clone()); }
            return returnRegistrations;
        }

        //O(registrations.Count
        /// <summary>
        /// prüft, ob die gegebene Anmeldung eine andere in der Liste überschneidet
        /// </summary>
        /// <param name="reg">zu prüfende Anmeldung</param>
        /// <param name="registrationsLoc">Liste mit allen Anmeldungen, mit denen geprüft werden soll</param>
        /// <returns>true: Überschneidung; false: keine Überschneidung</returns>
        private bool checkIfOverlap3(Registration reg, List<Registration> registrationsLoc) {
            for (int i = 0; i < registrationsLoc.Count; i++) {
                if (registrationsLoc[i].overlap(reg) > 0 && reg.id != registrationsLoc[i].id) {
                    return true;
                }
            }
            return false;
        }

        //O( 10^3*reg.länge*reg.dauer)
        /// <summary>
        /// findet alle möglichen Positionen für eine gegebene Anmeldung, diekeine Grenze überschreitet, an denen keine Überschneidung auftritt auf Basis von occupiedFields[]
        /// </summary>
        /// <param name="reg">Anmeldung</param>
        /// <param name="unoccupiedFieldsLoc">2D Array das die Ort-Zeit-tafel darstellt. true: frei; false: besetzt</param>
        /// <returns>Liste mit allen Positionen in der Straße bei denen für die Anmeldung keine Überschneidung auftritt</returns>
        private List<int> findFreePositions5(bool[,] unoccupiedFieldsLoc, Registration reg) {
            List<int> positions = new List<int>();
            for (int x = 0; x <= unoccupiedFieldsLoc.GetLength(0) - reg.rentLength; x++) {
                bool crossBorder = false;
                foreach (int border in borderPos) { //check if current position would cross any border (Erweiterung)
                    if (x < border && x + reg.rentLength > border) { crossBorder = true; break; }
                }
                if (crossBorder == false) {
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

        //O(log(list.count)
        /// <summary>
        /// finds closest value to targetvalue in int list
        /// </summary>
        /// <param name="target">value to whom the nearest should be found</param>
        /// <param name="list"></param>
        /// <returns>integer with index of closest value in list</returns>
        private int findClosestValue4(int target, List<int> list) {
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


        /// <summary>
        /// compares two Registrations by rent; x>y -> -1  x<y ->1
        /// </summary>
        /// <param name="x">first Anmeldung to compare</param>
        /// <param name="y">second Anmeldung to compare</param>
        /// <returns></returns>
        private static int compareByRent4(Registration x, Registration y) {
            int rentX = x.rentDuration * x.rentLength;
            int rentY = y.rentDuration * y.rentLength;
            if (rentX > rentY) { return -1; } else if (rentX < rentY) { return +1; }
            return 0;
        }

        private static int compareByLength5(Registration x, Registration y) {
            int rentX = x.rentDuration * x.rentLength;
            int rentY = y.rentDuration * y.rentLength;
            if (x.rentDuration > y.rentDuration) { return -1; } else if (x.rentDuration < y.rentDuration) { return +1; } else if (x.rentLength > y.rentLength) { return -1; } else if (x.rentLength < y.rentLength) { return +1; }
            return 0;
        }


        //O( dauer*(streetlength-länge)+länge*(gesamtdauer-dauer); worst: dauer=10, länge=1 -> ca 10000 bzw 10^4 = dauer*10^3+länge*10
        /// <summary>
        /// berechnet an eine Anmeldung grenzende Fläche auf der Zeit-Ort-Tafel bis zur nächsten Anmeldung; 
        /// </summary>
        /// <param name="unoccupiedFieldsLoc">bool array mit unbelegten Feldern</param>
        /// <param name="reg"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private (int links, int rechts, int oben, int unten) getSpaceAround5(bool[,] unoccupiedFieldsLoc, Registration reg, int position) {
            int links = 0;
            for (int i = reg.rentStart - starttime; i < reg.rentEnd - starttime; i++) {
                for (int j = position - 1; j >= 0; j--) {
                    if (unoccupiedFieldsLoc[j, i] == false) { break; }
                    links++;
                }
            }

            int rechts = 0;
            for (int i = reg.rentStart - starttime; i < reg.rentEnd - starttime; i++) {
                for (int j = position + reg.rentLength; j < streetLength; j++) {
                    if (unoccupiedFieldsLoc[j, i] == false) { break; }
                    rechts++;
                }
            }

            int oben = 0;
            for (int i = position; i < position + reg.rentLength; i++) {
                for (int j = reg.rentStart - starttime - 1; j >= 0; j--) {
                    if (unoccupiedFieldsLoc[i, j] == false) { break; }
                    oben++;
                }
            }

            int unten = 0;
            for (int i = position; i < position + reg.rentLength; i++) {
                for (int j = reg.rentEnd - starttime; j < duration; j++) {
                    if (unoccupiedFieldsLoc[i, j] == false) { break; }
                    unten++;
                }
            }

            return (links, rechts, oben, unten);
        }

        //ist uU nicht der optimale Algo dafür
        //O(freePositions.Count*getSpaceAround5 = 10^7 = 10^3*(dauer*10^3+länge*10)
        /// <summary>
        /// findet beste Position für gegebene Anmeldung; 
        /// </summary>
        /// <param name="unoccupiedFieldsLoc"></param>
        /// <param name="reg"></param>
        /// <param name="freePositions"></param>
        /// <returns></returns>
        private List<int> findBestPosition5(bool[,] unoccupiedFieldsLoc, Registration reg, List<int> freePositions) {
            (int smallestArea, List<int> positions) best = (int.MaxValue, new List<int>() { -2 });
            for (int i = 0; i < freePositions.Count; i++) {
                (int links, int rechts, int oben, int unten) = getSpaceAround5(unoccupiedFieldsLoc, reg, freePositions[i]);
                int area = Math.Min(links, rechts) + Math.Min(oben, unten); //Summe von (kleinste Außenfläche rechts links) und (kleinste Außenfläche oben unten)
                if (area < best.smallestArea) {
                    best = (area, new List<int>() { freePositions[i] });
                }
                else if (area == best.smallestArea) {
                    best.positions.Add(freePositions[i]);
                }
            }
            return best.positions;
        }


        public void findFreePositionsInRange(int minStartTime, int maxEndTime, int minDauer, int maxDauer, int minLänge, int maxLänge, int maxKosten = int.MaxValue) {
            if (minStartTime >= maxEndTime) { Console.WriteLine("invalid input (minStartTime >= maxEndTime)"); return; }
            if (minDauer >= maxDauer) { Console.WriteLine("invalid input (minDauer >= maxDauer)"); return; }
            if (minLänge >= maxLänge) { Console.WriteLine("invalid input (minLänge >= maxLänge)"); return; }
            if (minStartTime + maxDauer > maxEndTime) { Console.WriteLine("invalid input (minStartTime+dauer > maxEndTime)"); return; }
            if (maxDauer * maxLänge > maxKosten) { Console.WriteLine("invalid input (länge+dauer > maxKosten)"); return; }
            if (maxLänge > streetLength) { Console.WriteLine("invalid input (maxLänge > streetLength)"); return; }
            if (maxEndTime > starttime + duration) { Console.WriteLine("invalid input (maxEndTime> startzeit + duration)"); return; }

            List<(List<int> positions, Registration reg)> positions = new List<(List<int>, Registration)>();
            for (int länge = minLänge; länge < maxLänge; länge++) {
                for (int dauer = minDauer; dauer <= maxDauer; dauer++) {
                    for (int starttime = minStartTime; starttime <= maxEndTime - dauer; starttime++) {
                        if (länge * dauer <= maxKosten) {
                            Registration thisreg = new Registration(-1, starttime, starttime + dauer, länge, 0);
                            List<int> thisPositions = findFreePositions5(unoccupiedFields, thisreg);
                            if (thisPositions.Count != 0) {
                                positions.Add((thisPositions, thisreg));
                            }
                        }
                    }
                }
            }

            foreach ((List<int> positions, Registration reg) pos in positions) {
                pos.reg.print();
                List<int> bestPositions = findBestPosition5(unoccupiedFields, pos.reg, pos.positions);
                foreach (int pos2 in bestPositions) {
                    Console.Write("  " + pos2);
                }

                Console.WriteLine();
            }
        }

        //vereinfachen (Schleifen mergen)
        public void analyseResults() {
            Console.WriteLine("\nANALYSE RESULT:");
            double carThreshold = 5;//ab wievielen Metern Stand braucht man ein zusätzlichen Autostellplatz?
            int hoursBetweenToiletUse = 2;// alle wieviel Stunden geht man durchschnittlich auf Toilette? (abhängig von Wetter, Essensangebot, Geschlechterverteilung,...)
            int[] parkingSpots = new int[duration];
            int[] registrationsNum = new int[duration];
            int[] toiletUsesPerHour = new int[duration];

            for (int t = starttime; t < starttime + duration; t++) {
                foreach (Registration reg in registrations.accepted) {
                    if (reg.rentStart <= t && reg.rentEnd > t) {
                        parkingSpots[t - starttime] += (int)Math.Ceiling((double)reg.rentLength / (double)carThreshold);
                        registrationsNum[t - starttime]++;
                    }

                    if (t - reg.rentStart >= 0 && (t - reg.rentStart) % hoursBetweenToiletUse == 0) {
                        toiletUsesPerHour[t - starttime]++;
                    }
                }
            }
            printArray(registrationsNum, "registrations per hour");
            printArray(parkingSpots, "parking spots per hour");

            int[] newCarsPerHour = new int[duration];
            for (int t = starttime; t < starttime + duration; t++) {
                foreach (Registration reg in registrations.accepted) {
                    if (reg.rentStart == t) {
                        newCarsPerHour[t - starttime] += (int)Math.Ceiling((double)reg.rentLength / (double)carThreshold);
                    }
                }
            }
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
    }
}

