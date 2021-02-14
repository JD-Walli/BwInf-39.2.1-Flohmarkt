using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;

namespace BwInf_39._2._1_Flohmarkt {
    class simulatedAnnealing {//TODO: chart mit energiekurve
        int startTemperature;
        List<Anfrage> anfragen = new List<Anfrage>();
        int streetLength;
        int duration;
        Random rnd = new Random();

        public simulatedAnnealing(List<Anfrage> anfragen, int streetLength, int duration, int startTemperature) {
            this.anfragen = anfragen;
            this.streetLength = streetLength;
            this.duration = duration;
            this.startTemperature = startTemperature;
        }

        public simulatedAnnealing() { }

        public void setRandomPos() {
            foreach (Anfrage a in anfragen) {
                a.position = rnd.Next(streetLength - a.länge);
            }
        }

        public void simulate1() {
            double temp = startTemperature;
            int currentEnergy = energy1(anfragen);
            int bestEnergy = currentEnergy;
            List<Anfrage> besteVerteilung = new List<Anfrage>();
            List<Anfrage> currentAnfragen = new List<Anfrage>();
            foreach (Anfrage afr in anfragen) { currentAnfragen.Add(afr.clone()); }
            List<Anfrage> newAnfragen = new List<Anfrage>();
            foreach (Anfrage afr in anfragen) { newAnfragen.Add(afr.clone()); }
            besteVerteilung = currentAnfragen;
            List<int> posEnergyDiffs = new List<int>();
            for (int i = 0; i < 70000; i++) {
                newAnfragen = makeMove1(newAnfragen, temp);
                int newEnergy = energy12(newAnfragen);
                if (newEnergy > currentEnergy) {
                    posEnergyDiffs.Add(newEnergy - currentEnergy);
                }
                if (newEnergy <= currentEnergy ) {//|| rnd.NextDouble() < Math.Exp(-(newEnergy - currentEnergy) / temp)
                    currentEnergy = newEnergy;
                    currentAnfragen.Clear();
                    Console.WriteLine(i + " : " + currentEnergy);
                    foreach (Anfrage afr in newAnfragen) { currentAnfragen.Add(afr.clone()); }
                    if (newEnergy < bestEnergy) {
                        bestEnergy = newEnergy;
                        besteVerteilung.Clear();
                        foreach (Anfrage afr in newAnfragen) { besteVerteilung.Add(afr.clone()); }
                    }
                }
                else {
                    newAnfragen.Clear();
                    foreach (Anfrage afr in currentAnfragen) { newAnfragen.Add(afr.clone()); }
                }
                temp *= 0.99994;//200000, 130, 0.999972 //70000,74,0.99997 //70000,23,0.99994
            }
            Console.WriteLine("done");
            plotPosEnergyDiffs(posEnergyDiffs);
        }

        //move der weiter weg geht wird untewahrscheinlicher je kleiner temp wird (rnd wird als koordinatenursprung angenommen; move=rnd*(temp/startTemp)
        List<Anfrage> makeMove1(List<Anfrage> anfragen, double temp) {
            int afr = rnd.Next(anfragen.Count());
            int x = rnd.Next(streetLength - anfragen[afr].länge) - anfragen[afr].position;
            int move = (int)(x * (temp / startTemperature ));
            move = (move == 0) ? ((x > 0) ? +1 : -1) : move;
            //Console.WriteLine("                      " + move + "  " + (int)temp);
            anfragen[afr].position += move;
            return anfragen;
        }

        //plots all currentenergy-newenergy elements which making the energy higher
        void plotPosEnergyDiffs(List<int> energyDiffs) {
            List<float> xVals = new List<float>();
            List<float> yVals = new List<float>();
            foreach (int en in energyDiffs) {
                if (en > 0) {
                    bool foundsame = false;
                    for(int i = 0; i < xVals.Count; i++) {
                        if (en == xVals[i]) {
                            yVals[i]++;
                            foundsame = true;
                            break;
                        }
                    }
                    if (foundsame == false) {
                        xVals.Add(en);yVals.Add(1);
                    }
                }
            }
            Application.Run(new chart(xVals.ToArray(), yVals.ToArray()));

        }

        public int energy1(List<Anfrage> anfragenLocal) {
            int energy = 0;
            for (int i= 0;i < anfragenLocal.Count;i++) {
                for (int j = 0; j < anfragenLocal.Count; j++) {
                    if (i != j) {
                        energy += anfragenLocal[i].überschneidung(anfragenLocal[j]);
                        //Console.WriteLine("  " + anfragenLocal[i].id + ", " + anfragenLocal[j].id + " -> " + anfragenLocal[i].überschneidung(anfragenLocal[j]) + "  " + energy);
                    }
                }
            }
            return energy/2;
        }

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
    }
}

