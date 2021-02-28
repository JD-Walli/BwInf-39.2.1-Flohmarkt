using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;

namespace BwInf_39._2._1_Flohmarkt {
    class simulatedAnnealing {//Ziel: Überschneidungen auf 0 bringen
        int startTemperature;
        public (List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen;
        int streetLength;
        int duration;
        int durchläufe;
        double verkleinerungsrate;
        Random rnd = new Random();

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

        public void setRandomPos1() {
            foreach (Anfrage a in anfragen.verwendet) {
                a.position = rnd.Next(streetLength - a.länge);
            }
        }

        /// <summary>
        /// setzt zufällige Positionen, auch auf abgelehnt Liste
        /// </summary>
        /// <param name="anteilAbgelehnt">anteil in Prozent der auf die Warteliste gesetzten</param>
        public void setRandomPos2(int anteilAbgelehnt) {
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
        public void setRandomPos3() {
            foreach (Anfrage a in anfragen.verwendet) {
                a.position = rnd.Next(streetLength - a.länge);
            }
            for (int i = 0; i < anfragen.verwendet.Count; i++) {
                List<int> idToRemove = new List<int>();
                for (int j = anfragen.verwendet.Count-1; j > i; j--) {
                    if (anfragen.verwendet[i].überschneidung(anfragen.verwendet[j]) > 0) {
                        anfragen.abgelehnt.Add(anfragen.verwendet[j]);
                        idToRemove.Add(anfragen.verwendet[j].id);
                    }
                }
                anfragen.verwendet.RemoveAll(x => idToRemove.Contains(x.id));
            }
            Console.WriteLine(energy12(anfragen.verwendet));
        }

        public void simulate1() {
            double temp = startTemperature;
            int currentEnergy = energy2(anfragen.verwendet, startTemperature);//variabel
            int bestEnergy = currentEnergy;
            List<int> energies = new List<int>();
            (List<Anfrage> verwendet, List<Anfrage> abgelehnt) besteVerteilung; besteVerteilung.verwendet = new List<Anfrage>(); besteVerteilung.abgelehnt = new List<Anfrage>();
            (List<Anfrage> verwendet, List<Anfrage> abgelehnt) currentAnfragen; currentAnfragen.verwendet = new List<Anfrage>(); currentAnfragen.abgelehnt = new List<Anfrage>();
            (List<Anfrage> verwendet, List<Anfrage> abgelehnt) newAnfragen; newAnfragen.verwendet = new List<Anfrage>(); newAnfragen.abgelehnt = new List<Anfrage>();
            foreach (Anfrage afr in anfragen.verwendet) { newAnfragen.verwendet.Add(afr.clone()); currentAnfragen.verwendet.Add(afr.clone()); }
            foreach (Anfrage afr in anfragen.abgelehnt) { newAnfragen.abgelehnt.Add(afr.clone()); currentAnfragen.abgelehnt.Add(afr.clone()); }
            besteVerteilung = currentAnfragen;
            List<int> posEnergyDiffs = new List<int>();
            for (int i = 0; i < durchläufe; i++) {
                newAnfragen = makeMove3(newAnfragen, temp); //variabel
                int newEnergy = energy2(newAnfragen.verwendet, temp);//variabel
                if (newEnergy > currentEnergy) {
                    posEnergyDiffs.Add(newEnergy - currentEnergy);
                }

                if (newEnergy <= currentEnergy || rnd.NextDouble() < Math.Exp(-(newEnergy - currentEnergy) / temp)) {//|| rnd.NextDouble() < Math.Exp(-(newEnergy - currentEnergy) / temp)
                    currentEnergy = newEnergy;
                    Console.WriteLine(i + " : " + currentEnergy + " \t" + currentAnfragen.verwendet.Count + " " + currentAnfragen.abgelehnt.Count);
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
                energies.Add(chartEnergy(currentAnfragen.verwendet));
                temp *= verkleinerungsrate;//200000, 130, 0.999972 //70000,74,0.99997 //70000,23,0.99994
            }
            Console.WriteLine("done");
            anfragen = currentAnfragen;
            //plotPosEnergyDiffs(posEnergyDiffs);
            plotEnergy(energies);
        }

        //move der weiter weg geht wird untewahrscheinlicher je kleiner temp wird (rnd wird als koordinatenursprung angenommen; move=rnd*(temp/startTemp)
        List<Anfrage> makeMove1(List<Anfrage> anfragen, double temp) {
            int afr = rnd.Next(anfragen.Count());
            int x = rnd.Next(streetLength - anfragen[afr].länge) - anfragen[afr].position;
            int move = (int)(x * (temp / startTemperature));
            move = (move == 0) ? ((x > 0) ? +1 : -1) : move;
            //Console.WriteLine("                      " + move + "  " + (int)temp);
            anfragen[afr].position += move;
            return anfragen;
        }


        public (List<Anfrage> verwendet, List<Anfrage> abgelehnt) makeMove2((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen, double temp) {
            int rnd1 = rnd.Next(100);
            if (rnd1 < 60) { //verschiebe
                anfragen.verwendet = makeMove1(anfragen.verwendet, temp);
            }
            else {// swappe
                int rnd2 = rnd.Next(100);
                if (rnd2 < 50 || anfragen.abgelehnt.Count == 0) {//reinswap
                    int rnd3 = rnd.Next(anfragen.verwendet.Count);
                    anfragen.abgelehnt.Add(anfragen.verwendet[rnd3]);
                    anfragen.verwendet.RemoveAt(rnd3);
                }
                else {//rausswap
                    int rnd3 = rnd.Next(anfragen.abgelehnt.Count);
                    anfragen.abgelehnt[rnd3].position = rnd.Next(streetLength - anfragen.abgelehnt[rnd3].länge);
                    anfragen.verwendet.Add(anfragen.abgelehnt[rnd3]);
                    anfragen.abgelehnt.RemoveAt(rnd3);
                }
            }
            return (anfragen.verwendet, anfragen.abgelehnt);
        }

        public (List<Anfrage> verwendet, List<Anfrage> abgelehnt) makeMove3((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen, double temp) {
            int rnd1 = rnd.Next(100);
            if (rnd1 < 60) { //verschiebe
                int afr = rnd.Next(anfragen.verwendet.Count());
                for (int i = 0; i < 1000; i++) {//versuche 100 mal einen move zu finden, bei dem keine überschneidung rauskommt
                    int x = rnd.Next(streetLength - anfragen.verwendet[afr].länge) - anfragen.verwendet[afr].position;
                    //int move = (int)(x * (temp / startTemperature)); 
                    int move=x;
                    move = (move == 0) ? ((x > 0) ? +1 : -1) : move;
                    if (!checkIfÜberschneidung(new Anfrage(anfragen.verwendet[afr].id, anfragen.verwendet[afr].mietbeginn, anfragen.verwendet[afr].mietende, anfragen.verwendet[afr].länge, anfragen.verwendet[afr].position + move))) {
                        anfragen.verwendet[afr].position += move;
                        break;
                    }
                }
            }
            else {// swappe
                int rnd2 = rnd.Next(100);
                if ((rnd2 < 50 || anfragen.abgelehnt.Count == 0) && anfragen.verwendet.Count > 0) {//reinswap
                    int rnd3 = rnd.Next(anfragen.verwendet.Count);
                    anfragen.abgelehnt.Add(anfragen.verwendet[rnd3]);
                    anfragen.verwendet.RemoveAt(rnd3);
                }
                else {//rausswap
                    int rnd3 = rnd.Next(anfragen.abgelehnt.Count);
                    for (int i = 0; i < 1000; i++) {
                        int newPos = rnd.Next(streetLength - anfragen.abgelehnt[rnd3].länge);
                        if (!checkIfÜberschneidung(new Anfrage(anfragen.abgelehnt[rnd3].id, anfragen.abgelehnt[rnd3].mietbeginn, anfragen.abgelehnt[rnd3].mietende, anfragen.abgelehnt[rnd3].länge, newPos))) {
                            anfragen.abgelehnt[rnd3].position = newPos;
                            anfragen.verwendet.Add(anfragen.abgelehnt[rnd3]);
                            anfragen.abgelehnt.RemoveAt(rnd3);
                            break;
                        }
                    }
                }
            }
            return (anfragen.verwendet, anfragen.abgelehnt);
        }


        //plots all currentenergy-newenergy elements which making the energy higher
        void plotPosEnergyDiffs(List<int> energyDiffs) {
            List<float> xVals = new List<float>();
            List<float> yVals = new List<float>();
            foreach (int en in energyDiffs) {
                if (en > 0) {
                    bool foundsame = false;
                    for (int i = 0; i < xVals.Count; i++) {
                        if (en == xVals[i]) {
                            yVals[i]++;
                            foundsame = true;
                            break;
                        }
                    }
                    if (foundsame == false) {
                        xVals.Add(en); yVals.Add(1);
                    }
                }
            }
            Application.Run(new chart(xVals.ToArray(), yVals.ToArray()));

        }

        void plotEnergy(List<int> energies) {
            List<float> xVals = new List<float>();
            List<float> yVals = new List<float>();
            for (int i = 0; i < energies.Count; i++) {
                xVals.Add(i); yVals.Add(energies[i]);

            }
            Application.Run(new chart(xVals.ToArray(), yVals.ToArray()));

        }

        public int energy1(List<Anfrage> anfragenLocal) {
            int energy = 0;
            for (int i = 0; i < anfragenLocal.Count; i++) {
                for (int j = 0; j < anfragenLocal.Count; j++) {
                    if (i != j) {
                        energy += anfragenLocal[i].überschneidung(anfragenLocal[j]);
                        //Console.WriteLine("  " + anfragenLocal[i].id + ", " + anfragenLocal[j].id + " -> " + anfragenLocal[i].überschneidung(anfragenLocal[j]) + "  " + energy);
                    }
                }
            }
            return energy / 2;
        }

        //gleich wie energy1 ohne doppelte zählung -> skaliert besser
        public int energy12(List<Anfrage> anfragenLocal) {
            int energy = 0;

            for (int i = 0; i < anfragenLocal.Count; i++) {
                for (int j = 0; j < i; j++) {
                    if (i != j) {
                        energy += anfragenLocal[i].überschneidung(anfragenLocal[j]);
                    }
                }
            }
            return energy;
        }

        public int bestEnergy() {
            int energy = 0;
            List<Anfrage> anfragenLocalmerged = anfragen.verwendet;
            anfragenLocalmerged.AddRange(anfragen.abgelehnt);
            for (int i = 0; i < anfragenLocalmerged.Count; i++) {
                energy -= anfragenLocalmerged[i].länge * anfragenLocalmerged[i].mietdauer;
            }
            return energy;
        }

        /// <summary>
        /// energieberechnung: - belegteFläche + f * Überschneidung
        /// </summary>
        /// <param name="anfragenLocal">nur auf dem Feld plazierte Anfragen, keine auf Warteliste</param>
        /// <returns>Energie</returns>
        public int energy2(List<Anfrage> anfragenLocal, double temperatur) {
            int energy = 0;
            for (int i = 0; i < anfragenLocal.Count; i++) {
                energy -= anfragenLocal[i].länge * anfragenLocal[i].mietdauer;
            }
            //Console.WriteLine(energy12(anfragenLocal) * (int)(((temperatur / startTemperature) * 5) + 1));
            energy += energy12(anfragenLocal) * 7;// * (int)(((temperatur / startTemperature) * 20) + 1); //Überschneidung wird "wichtiger", je größer die Temperatur wird
            return energy;
        }

        public int chartEnergy(List<Anfrage> anfragenLocal) {
            int energy = 0;
            for (int i = 0; i < anfragenLocal.Count; i++) {
                energy -= anfragenLocal[i].länge * anfragenLocal[i].mietdauer;
            }
            for (int i = 0; i < anfragenLocal.Count; i++) {
                for (int j = 0; j < i; j++) {
                    if (i != j) {
                        energy += anfragenLocal[i].überschneidung(anfragenLocal[j]);
                    }
                }
            }
            return energy;
        }

        public void printFinish(List<Anfrage> anfragenLocal) {
            int anzahlÜberschneidungen = 0;
            for (int i = 0; i < anfragenLocal.Count; i++) {
                for (int j = 0; j < i; j++) {
                    if (i != j) {
                        if (anfragenLocal[i].überschneidung(anfragenLocal[j]) > 0)
                            anzahlÜberschneidungen++;
                    }
                }
            }
            int energy = 0;
            for (int i = 0; i < anfragenLocal.Count; i++) {
                energy -= anfragenLocal[i].länge * anfragenLocal[i].mietdauer;
            }
            Console.WriteLine(anzahlÜberschneidungen + " überschneidungen");
            Console.WriteLine("bei einer Energie von " + energy + "; einer effektiven energie von " + chartEnergy(anfragenLocal) + " und einer arbeitsenergie von " + energy2(anfragenLocal, 3)); ;
        }

        public bool checkIfÜberschneidung(Anfrage afr) {
            for (int i = 0; i < anfragen.verwendet.Count; i++) {
                if (anfragen.verwendet[i].überschneidung(afr) > 0 && afr.id != anfragen.verwendet[i].id) {
                    return true;
                }
            }
            return false;
        }

    }
}

