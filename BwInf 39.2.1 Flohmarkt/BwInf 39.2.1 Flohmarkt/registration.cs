using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BwInf_39._2._1_Flohmarkt {
    class Registration {
        public int id;
        public int rentStart;
        public int rentEnd;
        public int rentDuration;
        public int rentLength;
        public int position;

        public Registration(int id, int rentStart, int rentEnd, int rentLength, int position) {
            this.id = id;
            this.rentStart = rentStart;
            this.rentEnd = rentEnd;
            this.rentLength = rentLength;
            rentDuration = rentEnd - rentStart;
            this.position = position;
        }

        /// <summary>
        /// prüft Überschneidung zweier Anmeldungen
        /// </summary>
        /// <param name="reg2"></param>
        /// <returns></returns>
        public int overlap(Registration reg2) {
            int xOverlap = Math.Min(position + rentLength, reg2.position + reg2.rentLength) - Math.Max(position, reg2.position);
            int yOverlap = Math.Min(rentEnd, reg2.rentEnd) - Math.Max(rentStart, reg2.rentStart);
            if (xOverlap > 0 && yOverlap > 0) {
                return xOverlap * yOverlap;
            }
            else { return 0; }
        }

        public Registration clone() { return new Registration(this.id, this.rentStart, this.rentEnd, this.rentLength, this.position); }

        public void print() {
            Console.WriteLine("{0}: from {1} to {2} at position {3} for {4} tables", id, rentStart, rentEnd, position, rentLength);
        }
    }
}
