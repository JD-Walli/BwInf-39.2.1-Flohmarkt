using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BwInf_39._2._1_Flohmarkt {
	class simulatedAnnealing {
		int startTemperature;
		public (List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen;
		int streetLength;
		int number;
		int duration;
		int durchläufe;
		int startzeit;
		double verkleinerungsrate;
		Random rnd = new Random();

		#region constructors

		public simulatedAnnealing((List<Anfrage> anfragen, int number) data, int streetLength, int duration, int startzeit, int startTemperature, int durchläufe, double verkleinerungsrate) {
			this.anfragen.verwendet = data.anfragen;
			this.anfragen.abgelehnt = new List<Anfrage>();
			this.number = data.number;
			this.streetLength = streetLength;
			this.duration = duration;
			this.startzeit = startzeit;
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
				a.position = rnd.Next(streetLength - a.länge + 1);
			}
		}

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
		}

		/// <summary>
		/// setzt zufällige Positionen, auch auf abgelehnt Liste; keine Überschneidungen zugelassen
		/// </summary>
		public void setRandomPositions3() {
			foreach (Anfrage a in anfragen.verwendet) {
				a.position = rnd.Next(streetLength - a.länge + 1);
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
			(List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragenLoc = cloneLists(anfragen);
			anfragen.verwendet.Clear(); anfragen.abgelehnt.Clear();

			//ungetestete bessere var: bool[,] unoccupiedFields = new bool[streetLength, duration];
			//for (int i = 0; i < unoccupiedFields.GetLength(0); i++) { for (int j = 0; j < unoccupiedFields.GetLength(1); j++) { unoccupiedFields[i, j] = true; } }
			anfragenLoc.verwendet.Sort(compareByRent4);
			foreach (Anfrage a in anfragenLoc.verwendet) {
				List<int> freePosition = findFreePositions4(a, anfragen.verwendet); //ungetestete bessere var: ...=findFreePositions5(unoccupiedFields, a);
				if (freePosition.Count > 0) {
					a.position = freePosition[rnd.Next(freePosition.Count)];
					anfragen.verwendet.Add(a);
					/*ungetestete bessere var: 
					 for (int i = a.mietbeginn - startzeit; i < a.mietende - startzeit; i++) {
						for (int j = a.position; j < a.position + a.länge; j++) {
							unoccupiedFields[j, i] = false;
						}
					 }
					 */
				} else {
					a.position = -1;
					anfragen.abgelehnt.Add(a);
				}
			}
		}

		/// <summary>
		/// setzt Positionen, auch auf abgelehnt Liste; keine Überschneidungen zugelassen; fängt bei sperrigen Anfragen an; immer an Position, die am wenigsten freie Positionen einschließt
		/// </summary>
		public void setPositions5() {
			(List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragenLoc = cloneLists(anfragen);
			anfragen.verwendet.Clear(); anfragen.abgelehnt.Clear();

			bool[,] unoccupiedFields = new bool[streetLength, duration];
			for (int i = 0; i < unoccupiedFields.GetLength(0); i++) { for (int j = 0; j < unoccupiedFields.GetLength(1); j++) { unoccupiedFields[i, j] = true; } }
			anfragenLoc.verwendet.Sort(compareByRent4);
			foreach (Anfrage a in anfragenLoc.verwendet) {
				List<int> freePositions = findFreePositions5(unoccupiedFields, a);
				if (freePositions.Count > 0) {
					a.position = findBestPosition5(unoccupiedFields, a, freePositions);
					anfragen.verwendet.Add(a);
					for (int i = a.mietbeginn - startzeit; i < a.mietende - startzeit; i++) {
						for (int j = a.position; j < a.position + a.länge; j++) {
							unoccupiedFields[j, i] = false;
						}
					}
				} else {
					a.position = -1;
					anfragen.abgelehnt.Add(a);
				}
			}
		}

		#endregion

		public void simulate() {
			double temp = startTemperature;
			//int currentEnergy = energy(anfragen.verwendet, startTemperature);//variabel
			int currentEnergy = energy2(anfragen.verwendet);//variabel
			List<int> energies = new List<int>();
			(List<Anfrage> verwendet, List<Anfrage> abgelehnt) besteVerteilung; besteVerteilung.verwendet = new List<Anfrage>(); besteVerteilung.abgelehnt = new List<Anfrage>();
			(List<Anfrage> verwendet, List<Anfrage> abgelehnt) currentAnfragen; currentAnfragen.verwendet = new List<Anfrage>(); currentAnfragen.abgelehnt = new List<Anfrage>();
			(List<Anfrage> verwendet, List<Anfrage> abgelehnt) newAnfragen; newAnfragen.verwendet = new List<Anfrage>(); newAnfragen.abgelehnt = new List<Anfrage>();
			newAnfragen = cloneLists(anfragen); currentAnfragen = cloneLists(anfragen);
			int bestEnergy = energyChart(currentAnfragen.verwendet);
			besteVerteilung = currentAnfragen;

			for (int i = 0; i < durchläufe; i++) {
				newAnfragen = move2(newAnfragen, temp); //variabel
														//int newEnergy = energy(newAnfragen.verwendet, temp); //variabel
				int newEnergy = energy2(newAnfragen.verwendet);//variabel

				if (newEnergy <= currentEnergy || rnd.NextDouble() < Math.Exp(-(newEnergy - currentEnergy) / temp)) {//|| rnd.NextDouble() < Math.Exp(-(newEnergy - currentEnergy) / temp)
					currentEnergy = newEnergy;
					Console.WriteLine(i + " : " + currentEnergy + " \t" + newAnfragen.verwendet.Count + " " + newAnfragen.abgelehnt.Count + "  " + sumOverlap(newAnfragen.verwendet).anzahl + " Üs");
					currentAnfragen = cloneLists(newAnfragen);
					if (sumOverlap(currentAnfragen.verwendet).anzahl == 0 && energyChart(currentAnfragen.verwendet) < bestEnergy) {
						bestEnergy = energyChart(currentAnfragen.verwendet);
						besteVerteilung = cloneLists(newAnfragen);
					}
				} else {
					newAnfragen = cloneLists(currentAnfragen);
				}
				energies.Add(energyChart(currentAnfragen.verwendet));
				temp *= verkleinerungsrate;//200000, 130, 0.999972 //70000,74,0.99997 //70000,23,0.99994
			}

			Console.WriteLine("done");
			//plotEnergy(energies); //nur unter Windows
			Console.WriteLine("beste vorgekommene energie: {0} mit {1} abgelehnten und {2} Üs", bestEnergy, besteVerteilung.abgelehnt.Count, sumOverlap(besteVerteilung.verwendet).anzahl);

			anfragen = cloneLists(currentAnfragen);

			printEnding(currentAnfragen);
			saveResult(currentAnfragen);
			saveMeta(energies);
		}

		#region moves

		//verschieben: weiter weg wird unwahrscheinlicher je kleiner temp wird (afr.pos wird als koordinatenursprung angenommen; move=rnd*(temp/startTemp))
		private List<Anfrage> move1(List<Anfrage> anfragen, double temp) {
			int index = rnd.Next(anfragen.Count());
			int x = rnd.Next(streetLength - anfragen[index].länge + 1) - anfragen[index].position;
			int move = (int)(x * (1 - temp / startTemperature));
			move = (move == 0) ? ((x > 0) ? +1 : -1) : move;
			anfragen[index].position += move;
			return anfragen;
		}

		//verschieben (wie move1) und swappen
		private (List<Anfrage> verwendet, List<Anfrage> abgelehnt) move2((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen, double temp) {
			int rnd1 = rnd.Next(100);
			if (rnd1 < 60) { //verschiebe
				anfragen.verwendet = move1(anfragen.verwendet, temp);
			} else {// swappe
				int rnd2 = rnd.Next(100);
				if ((rnd2 < 50 || anfragen.abgelehnt.Count == 0) && anfragen.verwendet.Count > 0) {//reinswap
					int index = rnd.Next(anfragen.verwendet.Count);
					anfragen.abgelehnt.Add(anfragen.verwendet[index]);
					anfragen.verwendet.RemoveAt(index);
				} else {//rausswap
					int index = rnd.Next(anfragen.abgelehnt.Count);
					anfragen.abgelehnt[index].position = rnd.Next(streetLength - anfragen.abgelehnt[index].länge + 1);
					anfragen.verwendet.Add(anfragen.abgelehnt[index]);
					anfragen.abgelehnt.RemoveAt(index);
				}
			}
			return (anfragen.verwendet, anfragen.abgelehnt);
		}

		//verschieben und swappen ohne Überschneidungen (1000 mal probieren ob Platz gefunden wird)
		private (List<Anfrage> verwendet, List<Anfrage> abgelehnt) move3((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen, double temp) {
			int rnd1 = rnd.Next(100);
			if (rnd1 < 60 && anfragen.verwendet.Count > 0) { //verschiebe
				int index = rnd.Next(anfragen.verwendet.Count());
				for (int i = 0; i < 1000; i++) {//versuche 100 mal einen move zu finden, bei dem keine überschneidung rauskommt
					int x = rnd.Next(streetLength - anfragen.verwendet[index].länge + 1) - anfragen.verwendet[index].position;
					int move = x;
					//move = (int)(move * (1 - temp / startTemperature)); 
					move = (move == 0) ? ((x > 0) ? +1 : -1) : move;
					if (!checkIfOverlap3(new Anfrage(anfragen.verwendet[index].id, anfragen.verwendet[index].mietbeginn, anfragen.verwendet[index].mietende, anfragen.verwendet[index].länge, anfragen.verwendet[index].position + move), anfragen.verwendet)) {
						anfragen.verwendet[index].position += move;
						break;
					}
				}
			} else {// swappe
				int rnd2 = rnd.Next(100);
				if ((rnd2 < 50 || anfragen.abgelehnt.Count == 0) && anfragen.verwendet.Count > 0) {//reinswap
					int index = rnd.Next(anfragen.verwendet.Count);
					anfragen.abgelehnt.Add(anfragen.verwendet[index]);
					anfragen.verwendet.RemoveAt(index);
				} else {//rausswap
					int index = rnd.Next(anfragen.abgelehnt.Count);
					for (int i = 0; i < 1000; i++) {
						int newPos = rnd.Next(streetLength - anfragen.abgelehnt[index].länge + 1);
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
		private (List<Anfrage> verwendet, List<Anfrage> abgelehnt) move4((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen, double temp) {
			int rnd1 = rnd.Next(100);
			if (rnd1 < 50 && anfragen.verwendet.Count > 0) { //verschiebe //variabel
				int index = rnd.Next(anfragen.verwendet.Count());
				int x = rnd.Next(streetLength - anfragen.verwendet[index].länge + 1) - anfragen.verwendet[index].position;
				int move = x;
				//move = (int)(move * (1 - temp / startTemperature)); 
				move = (move == 0) ? ((x > 0) ? +1 : -1) : move;
				List<int> freePositions = findFreePositions4(anfragen.verwendet[index], anfragen.verwendet);
				if (freePositions.Count > 0) {
					anfragen.verwendet[index].position = freePositions[findClosestValue4(anfragen.verwendet[index].position + move, freePositions)];
				}
			} else {// swappe
				int rnd2 = rnd.Next(100);
				if ((rnd2 < 50 || anfragen.abgelehnt.Count == 0) && anfragen.verwendet.Count > 0) {//reinswap
					int index = rnd.Next(anfragen.verwendet.Count);
					anfragen.abgelehnt.Add(anfragen.verwendet[index]);
					anfragen.verwendet.RemoveAt(index);
				} else {//rausswap
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
		private int sumRent(List<Anfrage> anfragenLocal) {
			int energy = 0;
			for (int i = 0; i < anfragenLocal.Count; i++) {
				energy -= anfragenLocal[i].länge * anfragenLocal[i].mietdauer;
			}
			return energy;
		}

		private int sumAllRent((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragenLocal) {
			List<Anfrage> mergedAnfragen = new List<Anfrage>();
			foreach (Anfrage afr in anfragenLocal.verwendet) { mergedAnfragen.Add(afr.clone()); }
			mergedAnfragen.AddRange(anfragenLocal.abgelehnt);
			return sumRent(mergedAnfragen);
		}

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

		/// <summary>
		/// -rent + n*overlap
		/// </summary>
		/// <param name="anfragenLocal">nur auf dem Feld plazierte Anfragen, keine auf Warteliste</param>
		/// <returns>-rent + n*overlap</returns>
		private int energy(List<Anfrage> anfragenLocal, double temperatur) {
			int energy = sumRent(anfragenLocal);
			energy += sumOverlap(anfragenLocal).summe * (int)(((temperatur / startTemperature) * 20) + 1);// * (int)(((temperatur / startTemperature) * 20) + 1); //Überschneidung wird "wichtiger", je größer die Temperatur wird
			return energy;
		}

		/// <summary>
		/// -rent + (alle Anfragen die sich Überschneiden)
		/// </summary>
		/// <param name="anfragenLocal"></param>
		/// <returns></returns>
		private int energy2(List<Anfrage> anfragenLocal) {
			int energy = 0;
			for (int i = 0; i < anfragenLocal.Count; i++) {
				if (checkIfOverlap3(anfragenLocal[i], anfragenLocal) == false) {
					energy -= anfragenLocal[i].länge * anfragenLocal[i].mietdauer;
				}
			}
			return energy;
		}

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
		/// prints infos (sumOverlap.Anzahl, energies)
		/// </summary>
		/// <param name="anfragenLocal"></param>
		public void printEnding((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragenLocal) {
			Console.WriteLine("beste mögliche energie: " + sumAllRent(anfragenLocal));
			Console.WriteLine(sumOverlap(anfragenLocal.verwendet).anzahl + " überschneidungen");
			Console.WriteLine("bei einer Energie von " + sumRent(anfragenLocal.verwendet) + "; einer effektiven energie von " + energyChart(anfragenLocal.verwendet)); ;
		}

		/// <summary>
		/// speichert ergebniss (Positionen der Anfragen)
		/// </summary>
		/// <param name="anfragen"></param>
		public void saveResult((List<Anfrage> verwendet, List<Anfrage> abgelehnt) anfragen) {
			string filename = number + " savedResult " + DateTime.Now.ToString("yyMMdd HHmmss") + ".csv";
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
		public void saveMeta(List<int> energies) {
			string filename = number + " savedMeta " + DateTime.Now.ToString("yyMMdd HHmmss") + ".csv";
			StreamWriter txt = new StreamWriter(filename);
			txt.WriteLine("Durchläufe: " + durchläufe);
			txt.WriteLine("Starttemperatur: " + startTemperature);
			txt.WriteLine("verkleinerungsrate: " + verkleinerungsrate);
			txt.WriteLine("/n Energie im Zeitverlauf");
			foreach (var energ in energies) {
				txt.WriteLine(energ);
			}
			txt.Close(); txt.Dispose();

		}

		#endregion

		#region specialFunctions

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

		/// <summary>
		/// findet alle möglichen Positionen für eine gegebene Anfrage, an denen keine Überschneidung auftritt auf Basis von occupiedFields[] -> skaliert besser
		/// </summary>
		/// <param name="afr">Anfrage</param>
		/// <param name="unoccupiedFields">2D Array das die Ort-Zeit-tafel darstellt. true: frei; false: besetzt</param>
		/// <returns>Liste mit allen Positionen in der Straße bei denen für die Anfrage keine Überschneidung auftritt</returns>
		private List<int> findFreePositions5(bool[,] unoccupiedFields, Anfrage afr) {
			List<int> positions = new List<int>();
			for (int x = 0; x <= unoccupiedFields.GetLength(0) - afr.länge; x++) {
				bool horizontalPosition = true;
				for (int j = 0; j < afr.länge; j++) {
					bool vertikalPosition = true;
					for (int y = afr.mietbeginn - startzeit; y < afr.mietende - startzeit; y++) {
						if (unoccupiedFields[x + j, y] == false) { vertikalPosition = false; break; }
					}
					if (vertikalPosition == false) { horizontalPosition = false; x += j; break; }
				}
				if (horizontalPosition) {
					positions.Add(x);
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
		private int findClosestValue4(int target, List<int> list) {
			int index;
			if (target >= list[list.Count - 1]) { index = list.Count - 1; } else if (target <= list[0]) { index = 0; } else {
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
			if (rentX > rentY) { return -1; } else if (rentX < rentY) { return 1; }
			return 0;
		}


		/// <summary>
		/// berechnet an eine Anfrage grenzende Fläche auf der Zeit-Ort-Tafel bis zur nächsten Anfrage
		/// </summary>
		/// <param name="unoccupiedFields">bool array mit unbelegten Feldern</param>
		/// <param name="afr"></param>
		/// <param name="position"></param>
		/// <returns></returns>
		private (int links, int rechts, int oben, int unten) getSpaceAround5(bool[,] unoccupiedFields, Anfrage afr, int position) {
			int links = 0;
			for (int i = afr.mietbeginn - startzeit; i < afr.mietende - startzeit; i++) {
				for (int j = position - 1; j >= 0; j--) {
					if (unoccupiedFields[j, i] == false) { break; }
					links++;
				}
			}

			int rechts = 0;
			for (int i = afr.mietbeginn - startzeit; i < afr.mietende - startzeit; i++) {
				for (int j = position + afr.länge; j < streetLength; j++) {
					if (unoccupiedFields[j, i] == false) { break; }
					rechts++;
				}
			}

			int oben = 0;
			for (int i = position; i < position + afr.länge; i++) {
				for (int j = afr.mietbeginn - startzeit - 1; j >= 0; j--) {
					if (unoccupiedFields[i, j] == false) { break; }
					oben++;
				}
			}

			int unten = 0;
			for (int i = position; i < position + afr.länge; i++) {
				for (int j = afr.mietende - startzeit; j < duration; j++) {
					if (unoccupiedFields[i, j] == false) { break; }
					unten++;
				}
			}

			return (links, rechts, oben, unten);
		}

		//ist uU nicht der optimale Algo dafür
		/// <summary>
		/// findet beste Position für gegebene Anfrage
		/// </summary>
		/// <param name="unoccupiedFields"></param>
		/// <param name="afr"></param>
		/// <param name="freePositions"></param>
		/// <returns></returns>
		private int findBestPosition5(bool[,] unoccupiedFields, Anfrage afr, List<int> freePositions) {
			(int smallestArea, int position) best = (int.MaxValue, -2);
			for (int i = 0; i < freePositions.Count; i++) {
				(int links, int rechts, int oben, int unten) spaceAround = getSpaceAround5(unoccupiedFields, afr, freePositions[i]);
				int area = Math.Min(spaceAround.links, spaceAround.rechts) + Math.Min(spaceAround.oben, spaceAround.unten); //Summe von (kleinste Außenfläche rechts links) und (kleinste Außenfläche oben unten)
				if (area < best.smallestArea) {
					best = (area, freePositions[i]);
				}
			}
			return best.position;
		}

		#endregion
	}
}

