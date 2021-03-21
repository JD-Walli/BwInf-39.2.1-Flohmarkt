using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;

namespace BwInf_39._2._1_Flohmarkt {
    class simulatedAnnealing {
        int startTemperature;
        public (List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen;
        int streetLength;
        int duration;
        int durchläufe;
        double verkleinerungsrate;
        Random rnd = new Random();

        #region constructors

        public simulatedAnnealing(List<Anfrage> anfragen, int streetLength, int duration, int startTemperature, int durchläufe, double verkleinerungsrate) {
            this.anfragen.verwendet = anfragen;
            this.anfragen.abgelehnt = new List<Anfrage>();
            this.streetLength = streetLength;
            this.duration = duration;
            this.startTemperature = startTemperature;
            this.durchläufe = durchläufe;
            this.verkleinerungsrate = verkleinerungsrate;
        }

        public simulatedAnnealing() { }

        #endregion

        #region setPositions

        /// <summary>
        /// setzt zufällige Positionen
        /// </summary>
        public void setRandomPositions1() {
            foreach (Anfrage a in anfragen.verwendet) {
                a.position = rnd.Next(streetLength - a.länge);
            }
        }

        /// <summary>
        /// setzt zufällige Positionen, auch auf abgelehnt Liste
        /// </summary>
        /// <param name="anteilAbgelehnt">anteil in Prozent der auf die Warteliste gesetzten</param>
        public void setRandomPositions2(int anteilAbgelehnt) {
            foreach (Anfrage a in anfragen.verwendet) {
                a.position = rnd.Next(streetLength - a.länge);
            }
            for (int i = 0; i < (((float)anteilAbgelehnt / 100) * (anfragen.verwendet.Count + anfragen.abgelehnt.Count)); i++) {
                int rnd1 = rnd.Next(anfragen.verwendet.Count);
                anfragen.abgelehnt.Add(anfragen.verwendet[rnd1]);
                anfragen.verwendet.RemoveAt(rnd1);
            }
        }

        /// <summary>
        /// setzt zufällige Positionen, auch auf abgelehnt Liste; keine Überschneidungen zugelassen
        /// </summary>
        public void setRandomPositions3() {
            foreach (Anfrage a in anfragen.verwendet) {
                a.position = rnd.Next(streetLength - a.länge);
            }
            int countIDsToRemove = 0;
            for (int i = 0; i < anfragen.verwendet.Count; i++) {
                List<int> idToRemove = new List<int>();
                for (int j = anfragen.verwendet.Count - 1; j > i; j--) {
                    if (anfragen.verwendet[i].overlap(anfragen.verwendet[j]) > 0) {
                        anfragen.abgelehnt.Add(anfragen.verwendet[j]);
                        idToRemove.Add(anfragen.verwendet[j].id);
                    }
                }
                anfragen.verwendet.RemoveAll(x => idToRemove.Contains(x.id));

                countIDsToRemove += idToRemove.Count;
            }
        }

        /// <summary>
        /// setzt zufällige Positionen, auch auf abgelehnt Liste; keine Überschneidungen zugelassen; fängt bei sperrigen Anfragen an
        /// </summary>
        public void setRandomPositions4() {
            (List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragenLoc; anfragenLoc.verwendet = new List<Anfrage>(); anfragenLoc.abgelehnt = new List<Anfrage>();
            foreach (Anfrage afr in anfragen.verwendet) { anfragenLoc.verwendet.Add(afr.clone()); }
            foreach (Anfrage afr in anfragen.abgelehnt) { anfragenLoc.abgelehnt.Add(afr.clone()); }
            anfragen.verwendet.Clear(); anfragen.abgelehnt.Clear();
            anfragenLoc.verwendet.Sort(compareByRent4);
            foreach (Anfrage a in anfragenLoc.verwendet) {
                List<int> freePos = findFreePositions4(a, anfragen.verwendet);
                if (freePos.Count > 0) {
                    a.position = freePos[rnd.Next(freePos.Count)];
                    anfragen.verwendet.Add(a);
                }
                else {
                    a.position = 0;
                    anfragen.abgelehnt.Add(a);
                }
            }
        }

        #endregion

        public void simulate() {
            double temp = startTemperature;
            int currentEnergy = energy(anfragen.verwendet, startTemperature);//variabel
            int bestEnergy = currentEnergy;
            List<int> energies = new List<int>();
            (List<Anfrage> verwendet, List<Anfrage> abgelehnt) besteVerteilung; besteVerteilung.verwendet = new List<Anfrage>(); besteVerteilung.abgelehnt = new List<Anfrage>();
            (List<Anfrage> verwendet, List<Anfrage> abgelehnt) currentAnfragen; currentAnfragen.verwendet = new List<Anfrage>(); currentAnfragen.abgelehnt = new List<Anfrage>();
            (List<Anfrage> verwendet, List<Anfrage> abgelehnt) newAnfragen; newAnfragen.verwendet = new List<Anfrage>(); newAnfragen.abgelehnt = new List<Anfrage>();
            foreach (Anfrage afr in anfragen.verwendet) { newAnfragen.verwendet.Add(afr.clone()); currentAnfragen.verwendet.Add(afr.clone()); }
            foreach (Anfrage afr in anfragen.abgelehnt) { newAnfragen.abgelehnt.Add(afr.clone()); currentAnfragen.abgelehnt.Add(afr.clone()); }
            besteVerteilung = currentAnfragen;

            for (int i = 0; i < durchläufe; i++) {
                newAnfragen = move2(newAnfragen, temp); //variabel
                int newEnergy = energy(newAnfragen.verwendet, temp);//variabel

                if (newEnergy <= currentEnergy || rnd.NextDouble() < Math.Exp(-(newEnergy - currentEnergy) / temp)) {//|| rnd.NextDouble() < Math.Exp(-(newEnergy - currentEnergy) / temp)
                    currentEnergy = newEnergy;
                    Console.WriteLine(i + " : " + currentEnergy + " \t" + newAnfragen.verwendet.Count + " " + newAnfragen.abgelehnt.Count + "  " + sumOverlap(newAnfragen.verwendet).anzahl + " Üs");
                    currentAnfragen.verwendet.Clear(); currentAnfragen.abgelehnt.Clear();
                    foreach (Anfrage afr in newAnfragen.verwendet) { currentAnfragen.verwendet.Add(afr.clone()); }
                    foreach (Anfrage afr in newAnfragen.abgelehnt) { currentAnfragen.abgelehnt.Add(afr.clone()); }
                    if (newEnergy < bestEnergy) {
                        bestEnergy = newEnergy;
                        besteVerteilung.verwendet.Clear(); besteVerteilung.abgelehnt.Clear();
                        foreach (Anfrage afr in newAnfragen.verwendet) { besteVerteilung.verwendet.Add(afr.clone()); }
                        foreach (Anfrage afr in newAnfragen.abgelehnt) { besteVerteilung.abgelehnt.Add(afr.clone()); }
                    }
                }
                else {
                    newAnfragen.verwendet.Clear(); newAnfragen.abgelehnt.Clear();
                    foreach (Anfrage afr in currentAnfragen.verwendet) { newAnfragen.verwendet.Add(afr.clone()); }
                    foreach (Anfrage afr in currentAnfragen.abgelehnt) { newAnfragen.abgelehnt.Add(afr.clone()); }
                }
                energies.Add(energyChart(currentAnfragen.verwendet));
                temp *= verkleinerungsrate;//200000, 130, 0.999972 //70000,74,0.99997 //70000,23,0.99994
            }

            Console.WriteLine("done");
            plotEnergy(energies);
            Console.WriteLine("beste vorgekommene energie: " + bestEnergy);

            anfragen.verwendet.Clear(); anfragen.abgelehnt.Clear();
            foreach (Anfrage afr in currentAnfragen.verwendet) { anfragen.verwendet.Add(afr.clone()); }
            foreach (Anfrage afr in currentAnfragen.abgelehnt) { anfragen.abgelehnt.Add(afr.clone()); }

            printEnding(currentAnfragen);
        }

        #region moves

        //verschieben: weiter weg wird unwahrscheinlicher je kleiner temp wird (afr.pos wird als koordinatenursprung angenommen; move=rnd*(temp/startTemp))
        List<Anfrage> move1(List<Anfrage> anfragen, double temp) {
            int index = rnd.Next(anfragen.Count());
            int x = rnd.Next(streetLength - anfragen[index].länge) - anfragen[index].position;
            int move = (int)(x * (1 - temp / startTemperature));
            move = (move == 0) ? ((x > 0) ? +1 : -1) : move;
            anfragen[index].position += move;
            return anfragen;
        }

        //verschieben (wie move1) und swappen
        public (List<Anfrage> verwendet, List<Anfrage> abgelehnt) move2((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen, double temp) {
            int rnd1 = rnd.Next(100);
            if (rnd1 < 60) { //verschiebe
                anfragen.verwendet = move1(anfragen.verwendet, temp);
            }
            else {// swappe
                int rnd2 = rnd.Next(100);
                if (rnd2 < 50 || anfragen.abgelehnt.Count == 0) {//reinswap
                    int index = rnd.Next(anfragen.verwendet.Count);
                    anfragen.abgelehnt.Add(anfragen.verwendet[index]);
                    anfragen.verwendet.RemoveAt(index);
                }
                else {//rausswap
                    int index = rnd.Next(anfragen.abgelehnt.Count);
                    anfragen.abgelehnt[index].position = rnd.Next(streetLength - anfragen.abgelehnt[index].länge);
                    anfragen.verwendet.Add(anfragen.abgelehnt[index]);
                    anfragen.abgelehnt.RemoveAt(index);
                }
            }
            return (anfragen.verwendet, anfragen.abgelehnt);
        }

        //verschieben und swappen ohne Überschneidungen (1000 mal probieren ob Platz gefunden wird)
        public (List<Anfrage> verwendet, List<Anfrage> abgelehnt) move3((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen, double temp) {
            int rnd1 = rnd.Next(100);
            if (rnd1 < 60) { //verschiebe
                int index = rnd.Next(anfragen.verwendet.Count());
                for (int i = 0; i < 1000; i++) {//versuche 100 mal einen move zu finden, bei dem keine überschneidung rauskommt
                    int x = rnd.Next(streetLength - anfragen.verwendet[index].länge) - anfragen.verwendet[index].position;
                    int move = x;
                    //move = (int)(move * (1 - temp / startTemperature)); 
                    move = (move == 0) ? ((x > 0) ? +1 : -1) : move;
                    if (!checkIfOverlap3(new Anfrage(anfragen.verwendet[index].id, anfragen.verwendet[index].mietbeginn, anfragen.verwendet[index].mietende, anfragen.verwendet[index].länge, anfragen.verwendet[index].position + move), anfragen.verwendet)) {
                        anfragen.verwendet[index].position += move;
                        break;
                    }
                }
            }
            else {// swappe
                int rnd2 = rnd.Next(100);
                if ((rnd2 < 50 || anfragen.abgelehnt.Count == 0) && anfragen.verwendet.Count > 0) {//reinswap
                    int index = rnd.Next(anfragen.verwendet.Count);
                    anfragen.abgelehnt.Add(anfragen.verwendet[index]);
                    anfragen.verwendet.RemoveAt(index);
                }
                else {//rausswap
                    int index = rnd.Next(anfragen.abgelehnt.Count);
                    for (int i = 0; i < 1000; i++) {
                        int newPos = rnd.Next(streetLength - anfragen.abgelehnt[index].länge);
                        if (!checkIfOverlap3(new Anfrage(anfragen.abgelehnt[index].id, anfragen.abgelehnt[index].mietbeginn, anfragen.abgelehnt[index].mietende, anfragen.abgelehnt[index].länge, newPos), anfragen.verwendet)) {
                            anfragen.abgelehnt[index].position = newPos;
                            anfragen.verwendet.Add(anfragen.abgelehnt[index]);
                            anfragen.abgelehnt.RemoveAt(index);
                            break;
                        }
                    }
                }
            }
            return (anfragen.verwendet, anfragen.abgelehnt);
        }

        //verschieben und swappen ohne Überschneidungen (nur an Position wo sicher keine Überschneidungen auftreten)
        public (List<Anfrage> verwendet, List<Anfrage> abgelehnt) move4((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen, double temp) {
            int rnd1 = rnd.Next(100);
            if (rnd1 < 40 && anfragen.verwendet.Count > 0) { //verschiebe
                int index = rnd.Next(anfragen.verwendet.Count());
                int x = rnd.Next(streetLength - anfragen.verwendet[index].länge) - anfragen.verwendet[index].position;
                int move = x;
                //move = (int)(move * (1 - temp / startTemperature)); 
                move = (move == 0) ? ((x > 0) ? +1 : -1) : move;
                List<int> freePositions = findFreePositions4(anfragen.verwendet[index], anfragen.verwendet);
                if (freePositions.Count > 0) {
                    anfragen.verwendet[index].position = freePositions[findClosestValue4(anfragen.verwendet[index].position + move, freePositions)];
                }
            }
            else {// swappe
                int rnd2 = rnd.Next(100);
                if ((rnd2 < 50 || anfragen.abgelehnt.Count == 0) && anfragen.verwendet.Count > 0) {//reinswap
                    int index = rnd.Next(anfragen.verwendet.Count);
                    anfragen.abgelehnt.Add(anfragen.verwendet[index]);
                    anfragen.verwendet.RemoveAt(index);
                }
                else {//rausswap
                    int index = rnd.Next(anfragen.abgelehnt.Count);
                    List<int> freePositions = findFreePositions4(anfragen.abgelehnt[index], anfragen.verwendet);
                    if (freePositions.Count > 0) {
                        anfragen.abgelehnt[index].position = freePositions[rnd.Next(freePositions.Count)];
                        anfragen.verwendet.Add(anfragen.abgelehnt[index]);
                        anfragen.abgelehnt.RemoveAt(index);
                    }
                }
            }
            return (anfragen.verwendet, anfragen.abgelehnt);
        }

        #endregion

        #region energy

        /// <summary>
        /// summiert die Miete, die der Veranstalter mit gegebener Verteilung einnimmt. Überschneidungen nicht berücksichtigt!
        /// </summary>
        /// <returns>-rent</returns>
        public int sumRent(List<Anfrage> anfragenLocal) {
            int energy = 0;
            for (int i = 0; i < anfragenLocal.Count; i++) {
                energy -= anfragenLocal[i].länge * anfragenLocal[i].mietdauer;
            }
            return energy;
        }

        /// <summary>
        /// summiert die Überschneidung aller Anfragen / zählt die Anzahl der Überschneidungen
        /// </summary>
        /// <param name="anfragenLocal">Anfragen die überprüft werden sollen</param>
        /// <returns>Tuple (anzahl der Überschneidungen, summe der Überschneidungen)</returns>
        public (int anzahl, int summe) sumOverlap(List<Anfrage> anfragenLocal) {
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

        /// <summary>
        /// -rent + n*overlap
        /// </summary>
        /// <param name="anfragenLocal">nur auf dem Feld plazierte Anfragen, keine auf Warteliste</param>
        /// <returns>-rent + n*overlap</returns>
        public int energy(List<Anfrage> anfragenLocal, double temperatur) {
            int energy = sumRent(anfragenLocal);
            energy += sumOverlap(anfragenLocal).summe * (int)(((temperatur / startTemperature) * 50) + 1);// * (int)(((temperatur / startTemperature) * 20) + 1); //Überschneidung wird "wichtiger", je größer die Temperatur wird
            return energy;
        }

        /// <summary>
        /// -rent+overlap; berechnet Kosten, die für das Diagramm verwendet werden
        /// </summary>
        /// <param name="anfragenLocal"></param>
        /// <returns></returns>
        public int energyChart(List<Anfrage> anfragenLocal) {
            int energy = 0;
            for (int i = 0; i < anfragenLocal.Count; i++) {
                energy -= anfragenLocal[i].länge * anfragenLocal[i].mietdauer;
            }
            energy += sumOverlap(anfragenLocal).summe;
            return energy;
        }
        
        #endregion

        #region ending

        /// <summary>
        /// zeichnet gegebene Energien als Graph; x=zeitpunkt y=energie
        /// </summary>
        /// <param name="energies">liste mit Energien</param>
        void plotEnergy(List<int> energies) {
            List<float> xVals = new List<float>();
            List<float> yVals = new List<float>();
            for (int i = 0; i < energies.Count; i++) {
                xVals.Add(i); yVals.Add(energies[i]);
            }
            Application.Run(new chart(xVals.ToArray(), yVals.ToArray()));
        }

        /// <summary>
        /// prints infos (sumOverlap.Anzahl, energies)
        /// </summary>
        /// <param name="anfragenLocal"></param>
        public void printEnding((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragenLocal) {
            List<Anfrage> mergedAnfragen = anfragenLocal.verwendet;
            mergedAnfragen.AddRange(anfragenLocal.abgelehnt);
            Console.WriteLine("beste mögliche energie: " + sumRent(mergedAnfragen));
            Console.WriteLine(sumOverlap(anfragenLocal.verwendet).anzahl + " überschneidungen");
            Console.WriteLine("bei einer Energie von " + sumRent(anfragenLocal.verwendet) + "; einer effektiven energie von " + energyChart(anfragenLocal.verwendet)); ;
        }

        #endregion

        #region specialFunctions

        /// <summary>
        /// prüft, ob die gegebene Anfrage eine andere in der Liste überschneidet
        /// </summary>
        /// <param name="afr">zu prüfende Anfrage</param>
        /// <param name="anfragenLocal">Liste mit allen Anfragen, mit denen geprüft werden soll</param>
        /// <returns>true: Überschneidung; false: keine Überschneidung</returns>
        public bool checkIfOverlap3(Anfrage afr, List<Anfrage> anfragenLocal) {
            for (int i = 0; i < anfragenLocal.Count; i++) {
                if (anfragenLocal[i].overlap(afr) > 0 && afr.id != anfragenLocal[i].id) {
                    return true;
                }
            }
            return false;
        }

        
        /// <summary>
        /// findet alle möglichen Positionen für eine gegebene Anfrage, an denen keine Überschneidung auftritt
        /// </summary>
        /// <param name="afr">Anfrage</param>
        /// <param name="anfragenLocal">liste mit allen Anfragen deren Überschneidung berücksichtigt werden sollen</param>
        /// <returns>Liste mit allen Positionen in der Straße bei denen für die Anfrage keine Überschneidung auftritt</returns>
        public List<int> findFreePositions4(Anfrage afr, List<Anfrage> anfragenLocal) {
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
            for (int i = 0; i < streetLength - afr.länge; i++) {
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

        /// <summary>
        /// finds closest value to targetvalue in int list
        /// </summary>
        /// <param name="target">value to whom the nearest should be found</param>
        /// <param name="list"></param>
        /// <returns>integer with index of closest value in list</returns>
        int findClosestValue4(int target, List<int> list) {
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
        public static int compareByRent4(Anfrage x, Anfrage y) {
            int rentX = x.mietdauer * x.länge;
            int rentY = y.mietdauer * y.länge;
            if (rentX > rentY) { return -1; }
            else if (rentY < rentY) { return 1; }
            return 0;
        }
        
        #endregion
    }
}

