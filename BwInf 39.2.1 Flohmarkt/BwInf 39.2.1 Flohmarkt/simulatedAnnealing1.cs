using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BwInf_39._2._1_Flohmarkt {
    class simulatedAnnealing {
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

        public void setRandomPos() {
            foreach (Anfrage a in anfragen) {
                a.position = rnd.Next(streetLength-a.länge);
            }
        }

        public void simulate1() {
            double temp = startTemperature;
            int currentEnergy = energy1(anfragen);
            int bestEnergy = currentEnergy;
            List<Anfrage> besteVerteilung = new List<Anfrage>();
            List<Anfrage> currentAnfragen = new List<Anfrage>();
            foreach (Anfrage afr in anfragen) { currentAnfragen.Add(afr.clone());}
            List<Anfrage> newAnfragen = new List<Anfrage>();
            foreach (Anfrage afr in anfragen) { newAnfragen.Add(afr.clone()); }
            Console.WriteLine(newAnfragen.Count);
            besteVerteilung = currentAnfragen;

            for (int i = 0; i < 10000; i++) {
                newAnfragen = makeMove1(newAnfragen, temp);
                int newEnergy = energy1(newAnfragen);
                if (i % 20 == 0) { Console.WriteLine(i + " : " + currentEnergy); }
                if (newEnergy <= currentEnergy || rnd.NextDouble() < Math.Exp(-(newEnergy - currentEnergy) / temp)) {
                    currentEnergy = newEnergy;
                    currentAnfragen.Clear();
                    foreach (Anfrage afr in newAnfragen) { currentAnfragen.Add(afr.clone()); }
                    if (newEnergy < bestEnergy) {
                        bestEnergy = newEnergy;
                        Console.WriteLine("  "+bestEnergy);
                        besteVerteilung.Clear();
                        foreach (Anfrage afr in newAnfragen) { besteVerteilung.Add(afr.clone()); }
                    }
                }
                else {
                    newAnfragen.Clear();
                    foreach (Anfrage afr in currentAnfragen) { newAnfragen.Add(afr.clone()); }
                }
                temp *= 0.9995;
            }
            Console.WriteLine("done");

        }

        //move der weiter weg geht wird untewahrscheinlicher je kleiner temp wird (rnd wird als koordinatenursprung angenommen; move=rnd*(temp/startTemp)
        List<Anfrage> makeMove1(List<Anfrage> anfragen, double temp) {
            int afr = rnd.Next(anfragen.Count());
            int x = rnd.Next(streetLength - anfragen[afr].länge) - anfragen[afr].position;
            int move = (int)(x * (temp / startTemperature));
            anfragen[afr].position += move;
            return anfragen;
        }

        int energy1(List<Anfrage> anfragenLocal) {
            int energy = 0;
            foreach (Anfrage anfrage1 in anfragenLocal) {
                foreach (Anfrage anfrage2 in anfragenLocal) {
                    energy += anfrage1.überschneidung(anfrage2);
                }
            }
            return energy;
        }
    }
}

