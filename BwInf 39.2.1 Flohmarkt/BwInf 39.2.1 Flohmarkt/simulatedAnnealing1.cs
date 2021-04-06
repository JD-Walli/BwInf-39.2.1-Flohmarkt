using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BwInf_39._2._1_Flohmarkt {
    class simulatedAnnealing {
        public (List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen;

        readonly int streetLength;
        readonly int duration;
        readonly int startzeit;
        public List<int> borderPos;

        private bool[,] unoccupiedFields; //nur für Varianten ohne Überschneidungen erlaubt/brauchbar

        readonly int number;
        readonly string programStartTime;
        public List<string> metaToSave = new List<string>();
        public List<string> logToSave = new List<string>();

        readonly int durchläufe;
        readonly int startTemperature;
        readonly double verkleinerungsrate;
        public (EnDel del, string name) energyType;
        public (moveDel del, string name) moveType;

        public (int bestEnergy, List<int> energies, List<int> overlaps, (List<Anfrage> verwendet, List<Anfrage> abgelehnt) letzteVerteilung, (List<Anfrage> verwendet, List<Anfrage> abgelehnt) besteVerteilung) output;

        Random rnd = new Random();

        #region constructors

        public simulatedAnnealing(int number, List<Anfrage> anfragen, int streetLength = 1000, int startzeit = 8, int duration = 10, int startTemperature = 25, int durchläufe = 70000, double verkleinerungsrate = 0.99995) {
            this.anfragen.verwendet = anfragen;
            this.anfragen.abgelehnt = new List<Anfrage>();
            this.number = number;
            this.streetLength = streetLength;
            this.duration = duration;
            this.startzeit = startzeit;
            this.startTemperature = startTemperature;
            this.durchläufe = durchläufe;
            this.verkleinerungsrate = verkleinerungsrate;
            programStartTime = DateTime.Now.ToString("yyMMdd HHmmss") + "";
            unoccupiedFields = new bool[streetLength, duration];
            for (int i = 0; i < unoccupiedFields.GetLength(0); i++) { for (int j = 0; j < unoccupiedFields.GetLength(1); j++) { unoccupiedFields[i, j] = true; } }
            borderPos = new List<int>();
        }

        public simulatedAnnealing() { }

        #endregion

        #region setPositions

        //O(n)=anfragen.Count
        /// <summary>
        /// setzt zufällige Positionen
        /// </summary>
        public void setRandomPositions1() {
            setRandomPositions2(0);
        }

        //O(n)=anfragen.Count+anfragen.Count^2
        /// <summary>
        /// setzt zufällige Positionen, auch auf abgelehnt Liste
        /// </summary>
        /// <param name="anteilAbgelehnt">anteil in Prozent der auf die Warteliste gesetzten</param>
        public void setRandomPositions2(int anteilAbgelehnt) {
            foreach (Anfrage a in anfragen.verwendet) {
                a.position = rnd.Next(streetLength - a.länge + 1);
            }
            for (int i = 0; i < (((float)anteilAbgelehnt / 100) * (anfragen.verwendet.Count + anfragen.abgelehnt.Count)); i++) {
                int rnd1 = rnd.Next(anfragen.verwendet.Count);
                anfragen.abgelehnt.Add(anfragen.verwendet[rnd1]);
                anfragen.verwendet.RemoveAt(rnd1);
            }
            metaToSave.Add("Positioning: setRandomPositions2(" + anteilAbgelehnt + ")");
        }

        //sorted&&optimalpos O(n)=cloneList + streetLength*gesamtdauer + anfragen.Count*log(anfragen.Count) + anfragen.Count*(findFreePositions5+findBestPosition5+dauer*länge) = 10^4 + anfragen.Count*log(anfragen.Count) + anfragen.Count*(10^7)
        //sorted&&optimalpos afr O(n)=cloneList + streetLength*gesamtdauer + anfragen.Count*log(anfragen.Count) + anfragen.Count*(findFreePositions5+findBestPosition5+dauer*länge) = 10^4 + anfragen.Count*log(anfragen.Count) + anfragen.Count*(afr.dauer*10^6+afr.länge*10^4)
        //sorted O(n)=cloneList + streetLength*gesamtdauer + anfragen.Count*log(anfragen.Count) + anfragen.Count*(findFreePositions5+dauer*länge) =  10^4 + anfragen.Count*log(anfragen.Count) + anfragen.Count*(10^3*afr.länge*afr.dauer)
        //O(n)=cloneList + streetLength*gesamtdauer + anfragen.Count*(findFreePositions5+dauer*länge) = 10^4 + anfragen.Count*(10^3*afr.länge*afr.dauer)
        /// <summary>
        /// setzt Positionen, auch auf abgelehnt Liste; keine Überschneidungen zugelassen; nach Wunsch: - fängt bei sperrigen Anfragen an (nach Wunsch gilt A oder Dauer als sperrig)  - immer an Position, die am wenigsten freie Positionen einschließt
        /// </summary>
        /// <param name="state">Konfiguration; ´sorted: Liste wird vor positionierung sortiert; compare5: compareByRent5 wird als comparer verwendet (standard ist compareByRent4); optimal: Anfrage wird an optimaler Position positioniert, ansonsten an zufälliger Position</param>
        public void setPositions5((bool sorted, bool comparer5, bool optimalPos) state) {
            (List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragenLoc = cloneLists(anfragen);
            anfragen.verwendet.Clear(); anfragen.abgelehnt.Clear();

            if (state.sorted) {
                if (state.comparer5) { anfragenLoc.verwendet.Sort(compareByRent5); }
                else { anfragenLoc.verwendet.Sort(compareByRent4); }
            }
            foreach (Anfrage afr in anfragenLoc.verwendet) {
                List<int> freePositions = findFreePositions5(unoccupiedFields, afr);
                if (freePositions.Count > 0) {
                    if (state.optimalPos) {
                        afr.position = findBestPosition5(unoccupiedFields, afr, freePositions)[0];
                    }
                    else { afr.position = freePositions[rnd.Next(freePositions.Count)]; }
                    anfragen.verwendet.Add(afr);
                    unoccupiedFields = setAfrUnoccupiedFields(unoccupiedFields, afr, false);
                }
                else {
                    afr.position = -1;
                    anfragen.abgelehnt.Add(afr);
                }
            }
            output = (energyChart(anfragen.verwendet), new List<int>(), new List<int>(), anfragen, anfragen);
            metaToSave.Add("Positioning: setPositions5 (" + (state.sorted ? "sorted " + (state.comparer5 ? " by compareByRent5; " : "by compareByRent4; ") : "not Sorted; ") + (state.optimalPos ? "optimalPosition)" : "randomPosition)"));
        }

        #endregion

        // move4 O(n)=cloneList+energy+durchläufe*(move+energy+cloneList+energyChart) = (afr:) anfragen.Count^2+durchläufe*(10^3*afr.länge+anfragen.Count^2) = (nonafr:) anfragen.Count^2+durchläufe*(10^6+anfragen.Count^2)
        // move2 O(n)=anfragen.Count^2+durchläufe*(anfragen.Count+2*anfragen.Count^2) = durchläufe*(anfragen.Count^2)
        public void simulate() {
            double temp = startTemperature;
            int currentEnergy = energyType.del(anfragen.verwendet, startTemperature);//variabel
            List<int> energies = new List<int>();
            List<int> overlaps = new List<int>();
            (List<Anfrage> verwendet, List<Anfrage> abgelehnt) besteVerteilung; besteVerteilung.verwendet = new List<Anfrage>(); besteVerteilung.abgelehnt = new List<Anfrage>();
            (List<Anfrage> verwendet, List<Anfrage> abgelehnt) currentAnfragen; currentAnfragen.verwendet = new List<Anfrage>(); currentAnfragen.abgelehnt = new List<Anfrage>();
            (List<Anfrage> verwendet, List<Anfrage> abgelehnt) newAnfragen; newAnfragen.verwendet = new List<Anfrage>(); newAnfragen.abgelehnt = new List<Anfrage>();
            newAnfragen = cloneLists(anfragen); currentAnfragen = cloneLists(anfragen);
            int bestEnergy = energyChart(currentAnfragen.verwendet);
            besteVerteilung = currentAnfragen;

            for (int i = 0; i < durchläufe; i++) {
                newAnfragen = moveType.del(newAnfragen, temp); //variabel
                int newEnergy = energyType.del(newAnfragen.verwendet, temp);//variabel
                logToSave[logToSave.Count - 1] += " " + newEnergy;
                if (i % 100 == 0) Console.WriteLine(i + "  " + currentEnergy);
                if (newEnergy <= currentEnergy || rnd.NextDouble() < Math.Exp(-(newEnergy - currentEnergy) / temp)) {//|| rnd.NextDouble() < Math.Exp(-(newEnergy - currentEnergy) / temp)
                    currentEnergy = newEnergy;
                    logToSave[logToSave.Count - 1] += " " + sumOverlap(newAnfragen.verwendet).anzahl; logToSave.Add("");
                    //Console.WriteLine(i + " : " + currentEnergy + " \t" + newAnfragen.verwendet.Count + " " + newAnfragen.abgelehnt.Count + "  " + sumOverlap(newAnfragen.verwendet).anzahl + " Üs");
                    currentAnfragen = cloneLists(newAnfragen);
                    if (sumOverlap(currentAnfragen.verwendet).anzahl == 0 && energyChart(currentAnfragen.verwendet) < bestEnergy) {
                        bestEnergy = energyChart(currentAnfragen.verwendet);
                        besteVerteilung = cloneLists(newAnfragen);
                    }
                }
                else {
                    newAnfragen = cloneLists(currentAnfragen);
                }
                energies.Add(energyChart(currentAnfragen.verwendet));
                overlaps.Add(sumOverlap(currentAnfragen.verwendet).anzahl);
                temp *= verkleinerungsrate;//200000, 130, 0.999972 //70000,74,0.99997 //70000,23,0.99994
            }

            Console.WriteLine("done");

            output = (bestEnergy, energies, overlaps, currentAnfragen, besteVerteilung);
            anfragen = cloneLists(currentAnfragen);
        }

        #region moves
        /// <summary>
        /// verschieben: weiter weg wird unwahrscheinlicher je kleiner temp wird (afr.pos wird als koordinatenursprung angenommen; move=rnd*(temp/startTemp))
        /// </summary>
        public (List<Anfrage> verwendet, List<Anfrage> abgelehnt) move1((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen, double temp) {
            int index = rnd.Next(anfragen.verwendet.Count());
            int x = rnd.Next(streetLength - anfragen.verwendet[index].länge + 1) - anfragen.verwendet[index].position;
            int move = (int)(x * (1 - temp / startTemperature));
            move = (move == 0) ? ((x > 0) ? +1 : -1) : move;
            anfragen.verwendet[index].position += move;
            return (anfragen.verwendet, anfragen.abgelehnt);
        }

        //O(n)= anfragen.count
        /// <summary>
        ///verschieben (wie move1) und swappen
        /// </summary>
        public (List<Anfrage> verwendet, List<Anfrage> abgelehnt) move2((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen, double temp) {
            int rnd1 = rnd.Next(100);
            if (rnd1 < 50 && anfragen.verwendet.Count > 0) { //verschiebe
                anfragen = move1(anfragen, temp);
                logToSave.Add("verschiebe");
            }
            else {// swappe
                int rnd2 = rnd.Next(100);
                if ((rnd2 < 50 || anfragen.abgelehnt.Count == 0) && anfragen.verwendet.Count > 0) {//reinswap
                    int index = rnd.Next(anfragen.verwendet.Count);
                    anfragen.abgelehnt.Add(anfragen.verwendet[index]);
                    anfragen.verwendet.RemoveAt(index);
                    logToSave.Add("swap->abgelehnt");
                }
                else {//rausswap
                    int index = rnd.Next(anfragen.abgelehnt.Count);
                    anfragen.abgelehnt[index].position = rnd.Next(streetLength - anfragen.abgelehnt[index].länge + 1);
                    anfragen.verwendet.Add(anfragen.abgelehnt[index]);
                    anfragen.abgelehnt.RemoveAt(index);
                    logToSave.Add("swap->verwendet");
                }
            }
            return (anfragen.verwendet, anfragen.abgelehnt);
        }

        //O(n)=findFreePositions4+anfragen.count= 10^6+10^3*anfragen.Count = anfragen.count*afr.länge+10^3*afr.länge
        /// <summary>
        ///verschieben und swappen ohne Überschneidungen (nur an Position wo sicher keine Überschneidungen auftreten)
        /// </summary>
        public (List<Anfrage> verwendet, List<Anfrage> abgelehnt) move4((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen, double temp) {
            int rnd1 = rnd.Next(100);
            if (rnd1 < 50 && anfragen.verwendet.Count > 0) { //verschiebe //variabel
                int index = rnd.Next(anfragen.verwendet.Count());
                int x = rnd.Next(streetLength - anfragen.verwendet[index].länge + 1) - anfragen.verwendet[index].position;
                int move = x;
                //move = (int)(move * (1 - temp / startTemperature)); 
                move = (move == 0) ? ((x > 0) ? +1 : -1) : move;
                List<int> freePositions = findFreePositions5(unoccupiedFields, anfragen.verwendet[index]);
                if (freePositions.Count > 0) {
                    unoccupiedFields = setAfrUnoccupiedFields(unoccupiedFields, anfragen.verwendet[index], true);
                    anfragen.verwendet[index].position = freePositions[findClosestValue4(anfragen.verwendet[index].position + move, freePositions)];
                    unoccupiedFields = setAfrUnoccupiedFields(unoccupiedFields, anfragen.verwendet[index], false);
                }
                logToSave.Add("verschiebe");
            }
            else {// swappe
                int rnd2 = rnd.Next(100);
                if ((rnd2 < 50 || anfragen.abgelehnt.Count == 0) && anfragen.verwendet.Count > 0) {//reinswap
                    int index = rnd.Next(anfragen.verwendet.Count);
                    anfragen.abgelehnt.Add(anfragen.verwendet[index]);
                    anfragen.verwendet.RemoveAt(index);
                    unoccupiedFields = setAfrUnoccupiedFields(unoccupiedFields, anfragen.verwendet[index], true);
                    logToSave.Add("swap->abgelehnt");
                }
                else {//rausswap
                    int index = rnd.Next(anfragen.abgelehnt.Count);
                    List<int> freePositions = findFreePositions5(unoccupiedFields, anfragen.verwendet[index]);
                    if (freePositions.Count > 0) {
                        anfragen.abgelehnt[index].position = freePositions[rnd.Next(freePositions.Count)];
                        anfragen.verwendet.Add(anfragen.abgelehnt[index]);
                        anfragen.abgelehnt.RemoveAt(index);
                        unoccupiedFields = setAfrUnoccupiedFields(unoccupiedFields, anfragen.verwendet[index], false);
                        logToSave.Add("swap->verwendet");
                    }
                }
            }
            return (anfragen.verwendet, anfragen.abgelehnt);
        }

        #endregion

        #region energy

        //O(n)=anfragen.Count
        /// <summary>
        /// summiert die Miete, die der Veranstalter mit gegebener Verteilung einnimmt. Überschneidungen nicht berücksichtigt!
        /// </summary>
        /// <returns>-rent</returns>
        private int sumRent(List<Anfrage> anfragenLocal) {
            int energy = 0;
            for (int i = 0; i < anfragenLocal.Count; i++) {
                energy -= anfragenLocal[i].länge * anfragenLocal[i].mietdauer;
            }
            return energy;
        }

        //O(n)=anfragen.Count
        private int sumAllRent((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragenLocal) {
            List<Anfrage> mergedAnfragen = new List<Anfrage>();
            foreach (Anfrage afr in anfragenLocal.verwendet) { mergedAnfragen.Add(afr.clone()); }
            mergedAnfragen.AddRange(anfragenLocal.abgelehnt);
            return sumRent(mergedAnfragen);
        }

        //O(n)=anfragen.Count^2
        /// <summary>
        /// summiert die Überschneidung aller Anfragen / zählt die Anzahl der Überschneidungen
        /// </summary>
        /// <param name="anfragenLocal">Anfragen die überprüft werden sollen</param>
        /// <returns>Tuple (anzahl der Überschneidungen, summe der Überschneidungen)</returns>
        private (int anzahl, int summe) sumOverlap(List<Anfrage> anfragenLocal) {
            int summe = 0; int anzahl = 0;
            for (int i = 0; i < anfragenLocal.Count; i++) {
                for (int j = 0; j < i; j++) {
                    if (anfragenLocal[i].id != anfragenLocal[j].id) {
                        int overlap = anfragenLocal[i].overlap(anfragenLocal[j]);
                        summe += overlap;
                        anzahl += (overlap > 0 ? 1 : 0);
                    }
                }
            }
            return (anzahl, summe);
        }

        //O(n)=sumOverlap+sumRent=anfragen.Count^2
        /// <summary>
        /// -rent + n*overlap
        /// </summary>
        /// <param name="anfragenLocal">nur auf dem Feld plazierte Anfragen, keine auf Warteliste</param>
        /// <returns>-rent + n*overlap</returns>
        public int energy(List<Anfrage> anfragenLocal, double temperatur) {
            int energy = sumRent(anfragenLocal);
            energy += sumOverlap(anfragenLocal).summe * (int)(((temperatur / startTemperature) * 20) + 1);// * (int)(((temperatur / startTemperature) * 20) + 1); //Überschneidung wird "wichtiger", je größer die Temperatur wird
            return energy;
        }

        //O(n)=anfragen.Count*checkifOverlap = anfragen.Count^2
        /// <summary>
        /// - (rent aller Umfragen die sich nicht überschneiden)
        /// </summary>
        /// <param name="anfragenLocal"></param>
        /// <returns></returns>
        public int energy2(List<Anfrage> anfragenLocal, double temperatur = 0) {
            int energy = 0;
            foreach (Anfrage afr in anfragenLocal) {
                if (checkIfOverlap3(afr, anfragenLocal) == false) {
                    energy -= afr.länge * afr.mietdauer;
                }
                foreach (int border in borderPos) {
                    if (afr.position < border && afr.position + afr.länge > border) {
                        energy += 2 * afr.länge * afr.mietdauer;
                    }
                }
            }
            return energy;
        }

        //O(n)=anfragen.Count+sumOverlap = anfragen.Count^2+anfragen.Count
        /// <summary>
        /// -rent+overlap; berechnet Kosten, die für das Diagramm verwendet werden
        /// </summary>
        /// <param name="anfragenLocal"></param>
        /// <returns></returns>
        private int energyChart(List<Anfrage> anfragenLocal) {
            int energy = 0;
            for (int i = 0; i < anfragenLocal.Count; i++) {
                energy -= anfragenLocal[i].länge * anfragenLocal[i].mietdauer;
            }
            energy += sumOverlap(anfragenLocal).summe;
            return energy;
        }

        #endregion

        #region ending


        /**<summary>determines further actions (plot energy distribution, save data)</summary>
         **/
        public void finish() {
            Console.WriteLine("\nLETZTE VERTEILUNG");
            printEnding(output.letzteVerteilung);
            Console.WriteLine("\nBESTE VERTEILUNG");
            printEnding(output.besteVerteilung);
            Console.Write("\nplot energies (y/n): ");
            if (Console.ReadLine() == "y") {
                plotEnergy(output.energies);
            }
            try {
                Console.Write("save result, meta, log (y/n y/n y/n): ");
                string[] input = Console.ReadLine().Split(' ');
                if (input[0] == "y") {
                    saveResult(output.besteVerteilung);
                    Console.WriteLine("saved result");
                }
                if (input[1] == "y") {
                    saveMeta(output.energies, output.overlaps, output.besteVerteilung, output.bestEnergy);
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
        /// <param name="anfragenLocal"></param>
        public void printEnding((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragenLocal) {
            Console.WriteLine(anfragenLocal.verwendet.Count + " verwendet;  " + anfragenLocal.abgelehnt.Count + " abgelehnt");
            Console.WriteLine(sumOverlap(anfragenLocal.verwendet).anzahl + " Überschneidungen");
            Console.WriteLine("Mietsumme aller Verwendeten: " + sumRent(anfragenLocal.verwendet) + ";   Mietsumme - Überschneidungen: " + energyChart(anfragenLocal.verwendet)); ;
        }

        /// <summary>
        /// speichert ergebniss (Positionen der Anfragen)
        /// </summary>
        /// <param name="anfragen"></param>
        public void saveResult((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen) {
            string filename = number + " savedResult " + programStartTime + ".csv";
            StreamWriter txt = new StreamWriter(filename);
            txt.WriteLine("Mietbegin Mietende Länge ID position");
            foreach (var afr in anfragen.verwendet) {
                txt.WriteLine("{0} {1} {2} {3} {4}", afr.mietbeginn, afr.mietende, afr.länge, afr.id, afr.position);
            }
            foreach (var afr in anfragen.abgelehnt) {
                txt.WriteLine("{0} {1} {2} {3} -1", afr.mietbeginn, afr.mietende, afr.länge, afr.id);
            }
            txt.Close(); txt.Dispose();
        }

        /// <summary>
        /// speichert meta daten (durchläufe, starttemperatur, ... und die energien vom simAnn im Zeitverlauf) 
        /// </summary>
        /// <param name="energies"></param>
        public void saveMeta(List<int> energies, List<int> overlaps, (List<Anfrage> verwendet, List<Anfrage> abgelehnt) besteVerteilung, int bestEnergy) {
            string filename = number + " savedMeta " + programStartTime + ".csv";
            StreamWriter txt = new StreamWriter(filename);
            txt.WriteLine("SIMANN META");
            txt.WriteLine("Anzahl Durchläufe: " + durchläufe);
            txt.WriteLine("Starttemperatur: " + startTemperature);
            txt.WriteLine("Verkleinerungsrate: " + verkleinerungsrate);
            txt.WriteLine();
            txt.WriteLine("BESTE VERTEILUNG");
            txt.WriteLine("Anzahl Anfragen: " + (besteVerteilung.verwendet.Count + besteVerteilung.abgelehnt.Count));
            txt.WriteLine("   davon abgelehnt: " + besteVerteilung.abgelehnt.Count);
            txt.WriteLine("Anzahl Überschneidungen: " + sumOverlap(besteVerteilung.verwendet).anzahl);
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
            string filename = number + " savedLog " + programStartTime + ".csv";
            StreamWriter txt = new StreamWriter(filename);
            foreach (string s in logToSave) {
                txt.WriteLine(s);
            }
            txt.Close(); txt.Dispose();
        }

        #endregion

        #region specialFunctions

        //O(n)=anfragen.Count
        /// <summary>
        /// gibt ein geklontes (Wert-kopiertes) anfragen Tuple (verwendet, abgelehnt) zurück.
        /// </summary>
        /// <param name="anfragenLocal">zu klonendes Tuple</param>
        /// <returns></returns>
        public (List<Anfrage> verwendet, List<Anfrage> abgelehnt) cloneLists((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragenLocal) {
            (List<Anfrage> verwendet, List<Anfrage> abgelehnt) returnAnfragen; returnAnfragen.verwendet = new List<Anfrage>(); returnAnfragen.abgelehnt = new List<Anfrage>();
            foreach (Anfrage afr in anfragenLocal.verwendet) { returnAnfragen.verwendet.Add(afr.clone()); }
            foreach (Anfrage afr in anfragenLocal.abgelehnt) { returnAnfragen.abgelehnt.Add(afr.clone()); }
            return returnAnfragen;
        }

        //O(N)=anfragen.Count
        /// <summary>
        /// prüft, ob die gegebene Anfrage eine andere in der Liste überschneidet
        /// </summary>
        /// <param name="afr">zu prüfende Anfrage</param>
        /// <param name="anfragenLocal">Liste mit allen Anfragen, mit denen geprüft werden soll</param>
        /// <returns>true: Überschneidung; false: keine Überschneidung</returns>
        private bool checkIfOverlap3(Anfrage afr, List<Anfrage> anfragenLocal) {
            for (int i = 0; i < anfragenLocal.Count; i++) {
                if (anfragenLocal[i].overlap(afr) > 0 && afr.id != anfragenLocal[i].id) {
                    return true;
                }
            }
            return false;
        }

        //O(n)=10^3 + anfragen.count*afr.länge+10^3*afr.länge = 10^6+10^3*anfragen.Count
        /// <summary>
        /// findet alle möglichen Positionen für eine gegebene Anfrage, an denen keine Überschneidung auftritt
        /// </summary>
        /// <param name="afr">Anfrage</param>
        /// <param name="anfragenLocal">liste mit allen Anfragen deren Überschneidung berücksichtigt werden sollen</param>
        /// <returns>Liste mit allen Positionen in der Straße bei denen für die Anfrage keine Überschneidung auftritt</returns>
        private List<int> findFreePositions4(Anfrage afr, List<Anfrage> anfragenLocal) {
            bool[] verticalPositions = new bool[streetLength];
            for (int e = 0; e < verticalPositions.Length; e++) { verticalPositions[e] = true; }
            for (int i = 0; i < anfragenLocal.Count; i++) {
                if (anfragenLocal[i].overlap(new Anfrage(-1, afr.mietbeginn, afr.mietende, streetLength, 0)) > 0) { //wenn aktuell betrachtete Anfrage sich mit betrachtetem Querstreifen überschneidet
                    for (int j = anfragenLocal[i].position; j < anfragenLocal[i].position + anfragenLocal[i].länge; j++) { //gehe Bereich ab, in dem sich betrachtete Anfrage befindet und setze alle spotsvertikal Einträge an dieser Stelle auf false
                        verticalPositions[j] = false;
                    }
                }
            }
            List<int> positions = new List<int>();
            for (int i = 0; i <= streetLength - afr.länge; i++) {
                bool horizontalPosition = true;
                for (int j = 0; j < afr.länge; j++) {
                    if (verticalPositions[i + j] == false) {
                        horizontalPosition = false;
                        i += j; // wenn an Stelle i+j false ist, kann man die Stellen zwischen i und i+j überspringen, da auf jeden Fall keine ganze afr.länge mher reinpasst
                        break;
                    }
                }
                if (horizontalPosition) {
                    positions.Add(i);
                }
            }
            return positions;
        }

        //O(n)= 10^3*afr.länge*afr.dauer
        /// <summary>
        /// findet alle möglichen Positionen für eine gegebene Anfrage, diekeine Grenze überschreitet, an denen keine Überschneidung auftritt auf Basis von occupiedFields[] -> skaliert besser
        /// </summary>
        /// <param name="afr">Anfrage</param>
        /// <param name="unoccupiedFieldsLoc">2D Array das die Ort-Zeit-tafel darstellt. true: frei; false: besetzt</param>
        /// <returns>Liste mit allen Positionen in der Straße bei denen für die Anfrage keine Überschneidung auftritt</returns>
        private List<int> findFreePositions5(bool[,] unoccupiedFieldsLoc, Anfrage afr) {
            List<int> positions = new List<int>();
            for (int x = 0; x <= unoccupiedFieldsLoc.GetLength(0) - afr.länge; x++) {
                bool crossBorder = false;
                foreach (int border in borderPos) { //check if current position would cross any border (Erweiterung)
                    if (x < border && x + afr.länge > border) { crossBorder = true; break; }
                }
                if (crossBorder == false) {
                    bool horizontalPosition = true;
                    for (int j = 0; j < afr.länge; j++) {
                        bool vertikalPosition = true;
                        for (int y = afr.mietbeginn - startzeit; y < afr.mietende - startzeit; y++) {
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

        //O(n)=log(list.count)
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
        /// compares two Anfragen by rent; x>y -> -1  x<y ->1
        /// </summary>
        /// <param name="x">first Anfrage to compare</param>
        /// <param name="y">second Anfrage to compare</param>
        /// <returns></returns>
        private static int compareByRent4(Anfrage x, Anfrage y) {
            int rentX = x.mietdauer * x.länge;
            int rentY = y.mietdauer * y.länge;
            if (rentX > rentY) { return -1; } else if (rentX < rentY) { return +1; }
            return 0;
        }

        private static int compareByRent5(Anfrage x, Anfrage y) {
            int rentX = x.mietdauer * x.länge;
            int rentY = y.mietdauer * y.länge;
            if (x.mietdauer > y.mietdauer) { return -1; } else if (x.mietdauer < y.mietdauer) { return +1; } else if (x.länge > y.länge) { return -1; } else if (x.länge < y.länge) { return +1; }
            return 0;
        }


        //O(n)= dauer*(streetlength-länge)+länge*(gesamtdauer-dauer); worst: dauer=10, länge=1 -> ca 10000 bzw 10^4 = dauer*10^3+länge*10
        /// <summary>
        /// berechnet an eine Anfrage grenzende Fläche auf der Zeit-Ort-Tafel bis zur nächsten Anfrage; 
        /// </summary>
        /// <param name="unoccupiedFieldsLoc">bool array mit unbelegten Feldern</param>
        /// <param name="afr"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private (int links, int rechts, int oben, int unten) getSpaceAround5(bool[,] unoccupiedFieldsLoc, Anfrage afr, int position) {
            int links = 0;
            for (int i = afr.mietbeginn - startzeit; i < afr.mietende - startzeit; i++) {
                for (int j = position - 1; j >= 0; j--) {
                    if (unoccupiedFieldsLoc[j, i] == false) { break; }
                    links++;
                }
            }

            int rechts = 0;
            for (int i = afr.mietbeginn - startzeit; i < afr.mietende - startzeit; i++) {
                for (int j = position + afr.länge; j < streetLength; j++) {
                    if (unoccupiedFieldsLoc[j, i] == false) { break; }
                    rechts++;
                }
            }

            int oben = 0;
            for (int i = position; i < position + afr.länge; i++) {
                for (int j = afr.mietbeginn - startzeit - 1; j >= 0; j--) {
                    if (unoccupiedFieldsLoc[i, j] == false) { break; }
                    oben++;
                }
            }

            int unten = 0;
            for (int i = position; i < position + afr.länge; i++) {
                for (int j = afr.mietende - startzeit; j < duration; j++) {
                    if (unoccupiedFieldsLoc[i, j] == false) { break; }
                    unten++;
                }
            }

            return (links, rechts, oben, unten);
        }

        //ist uU nicht der optimale Algo dafür
        //O(n)=freePositions.Count*getSpaceAround5 = 10^7 = 10^3*(dauer*10^3+länge*10)
        /// <summary>
        /// findet beste Position für gegebene Anfrage; 
        /// </summary>
        /// <param name="unoccupiedFieldsLoc"></param>
        /// <param name="afr"></param>
        /// <param name="freePositions"></param>
        /// <returns></returns>
        private List<int> findBestPosition5(bool[,] unoccupiedFieldsLoc, Anfrage afr, List<int> freePositions) {
            (int smallestArea, List<int> positions) best = (int.MaxValue, new List<int>() { -2 });
            for (int i = 0; i < freePositions.Count; i++) {
                (int links, int rechts, int oben, int unten) = getSpaceAround5(unoccupiedFieldsLoc, afr, freePositions[i]);
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
            if (maxEndTime > startzeit + duration) { Console.WriteLine("invalid input (maxEndTime> startzeit + duration)"); return; }

            List<(List<int> positions, Anfrage afr)> positions = new List<(List<int>, Anfrage)>();
            for (int länge = minLänge; länge < maxLänge; länge++) {
                for (int dauer = minDauer; dauer <= maxDauer; dauer++) {
                    for (int starttime = minStartTime; starttime <= maxEndTime - dauer; starttime++) {
                        if (länge * dauer <= maxKosten) {
                            Anfrage thisafr = new Anfrage(-1, starttime, starttime + dauer, länge, 0);
                            List<int> thisPositions = findFreePositions5(unoccupiedFields, thisafr);
                            if (thisPositions.Count != 0) {
                                positions.Add((thisPositions, thisafr));
                            }
                        }
                    }
                }
            }

            foreach ((List<int> positions, Anfrage afr) pos in positions) {
                pos.afr.print();
                List<int> bestPositions = findBestPosition5(unoccupiedFields, pos.afr, pos.positions);
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
            int hoursBetweenToiletUse = 2;// alle wieviel Stunden ghet man durchschnittlich auf Toilette? (abhängig von Wetter, Essensangebot,...)
            int[] parkingSpots = new int[duration];
            int[] anfragenNum = new int[duration];
            int[] toiletUsesPerHour = new int[duration];

            for (int t = startzeit; t < startzeit + duration; t++) {
                foreach (Anfrage afr in anfragen.verwendet) {
                    if (afr.mietbeginn <= t && afr.mietende > t) {
                        parkingSpots[t - startzeit] += (int)Math.Ceiling((double)afr.länge / (double)carThreshold);
                        anfragenNum[t - startzeit]++;
                    }

                    if (t - afr.mietbeginn >= 0 && (t - afr.mietbeginn) % hoursBetweenToiletUse == 0) {
                        toiletUsesPerHour[t - startzeit]++;
                    }
                }
            }
            printArray(anfragenNum, "anfragen per hour");
            printArray(parkingSpots, "parking spots per hour");

            int[] newCarsPerHour = new int[duration];
            for (int t = startzeit; t < startzeit + duration; t++) {
                foreach (Anfrage afr in anfragen.verwendet) {
                    if (afr.mietbeginn == t) {
                        newCarsPerHour[t - startzeit] += (int)Math.Ceiling((double)afr.länge / (double)carThreshold);
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
                firstLine += "\t" + (i + startzeit);
                secondLine += "\t" + array[i];
            }
            Console.WriteLine(firstLine);
            Console.WriteLine(secondLine);
        }

        private bool[,] setAfrUnoccupiedFields(bool[,] unoccupiedFieldsLoc, Anfrage afr, bool boolVal) {
            for (int i = afr.mietbeginn - startzeit; i < afr.mietende - startzeit; i++) {
                for (int j = afr.position; j < afr.position + afr.länge; j++) {
                    unoccupiedFieldsLoc[j, i] = boolVal;
                }
            }
            return unoccupiedFieldsLoc;
        }

        public delegate int EnDel(List<Anfrage> anfragenLocal, double temperatur);
        public delegate (List<Anfrage> verwendet, List<Anfrage> abgelehnt) moveDel((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen, double temp);

        #endregion
    }
}

