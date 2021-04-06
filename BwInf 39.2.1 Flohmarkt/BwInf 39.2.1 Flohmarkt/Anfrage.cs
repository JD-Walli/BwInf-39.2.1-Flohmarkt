using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BwInf_39._2._1_Flohmarkt {
    class Anfrage {
        public int id;
        public int mietbeginn;
        public int mietende;
        public int mietdauer;
        public int länge;
        public int position;

        public Anfrage(int id, int mietbeginn, int mietende, int länge, int position) {
            this.id = id;
            this.mietbeginn = mietbeginn;
            this.mietende = mietende;
            this.länge = länge;
            mietdauer = mietende - mietbeginn;
            this.position = position;
        }

        public int overlap(Anfrage afr2) {
            int xOverlap = Math.Min(position + länge, afr2.position + afr2.länge)-Math.Max(position, afr2.position);
            int yOverlap = Math.Min(mietende, afr2.mietende)-Math.Max(mietbeginn, afr2.mietbeginn);
            if (xOverlap > 0 && yOverlap > 0) {
                return xOverlap * yOverlap;
            }
            else { return 0; }
        }

        public Anfrage clone() { return new Anfrage(this.id, this.mietbeginn, this.mietende, this.länge, this.position); }

        public void print() {
            Console.WriteLine("{0}: from {1} to {2} at position {3} for {4} tables",id, mietbeginn, mietende, position, länge);
        }
    }
}
