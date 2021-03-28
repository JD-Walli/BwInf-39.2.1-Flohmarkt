using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BwInf_39._2._1_Flohmarkt {
	class Program {
		static void Main(string[] args) {
			simulatedAnnealing simAnn = new simulatedAnnealing(readData(5), 1000, 10, 8, 25, 200000, 0.99995);
            simAnn.setRandomPositions2(0);
            //simAnn.setPositions5("sorted optimal compare5");
            simulatedAnnealing.EnDel energyDelegate = simAnn.energy2;
            simulatedAnnealing.moveDel moveDelegate = simAnn.move2;
            simAnn.metaToSave.Add("Positioning: setRandomPositions2(0)");
            simAnn.metaToSave.Add("energy: energy2");
            simAnn.metaToSave.Add("move: move2");
            simAnn.simulate(energyDelegate,moveDelegate);

			Console.ReadLine();
		}

		private static (List<Anfrage> anfragen, int number) readData(int number) {
			string[] lines = System.IO.File.ReadAllLines(System.IO.Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + "/flohmarkt " + number + ".txt");
			List<Anfrage> anfragen = new List<Anfrage>();
			for (int i = 1; i < lines.Length; i++) {
				string[] line = lines[i].Split(' ');
				anfragen.Add(new Anfrage(i - 1, int.Parse(line[0].Trim()), int.Parse(line[1].Trim()), int.Parse(line[2].Trim()), 0));
			}
			Console.WriteLine(anfragen.Count + "  " + lines.Length);
			return (anfragen, number);
		}


    }
}
