using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BwInf_39._2._1_Flohmarkt {
    class Anfrage {
        int id;
        int mietbeginn;
        int mietende;
        int mietdauer;
        int länge;

        public Anfrage(int id, int mietbeginn, int mietende, int länge) {
            this.id = id;
            this.mietbeginn = mietbeginn;
            this.mietende = mietende;
            this.länge = länge;
            mietdauer = mietende - mietbeginn;
        }
    }
}
