import matplotlib.pyplot as plt
from matplotlib.patches import Rectangle
import csv

fig, ax = plt.subplots()
plot_abgelehnt=True
pos_abgelehnt=0
data_name="6 savedResult 210328 151826"

with open("C://Users//Jakov//Desktop//git//BWInf 20//BwInf 39.2.1 Flohmarkt//BwInf 39.2.1 Flohmarkt//BwInf 39.2.1 Flohmarkt//bin//Debug//"+data_name+".csv") as csvfile:
  csvReader = csv.reader(csvfile, delimiter=' ')
  print(type(csvReader))
  for row in csvReader:
      print(row)
      if(list(row[0])[0] in ["0","1","2","3","4","5","6","7","8","9","-","+"]):
          if(int(row[4]) is not -1):
              ax.add_patch(Rectangle((int(row[4]), int(row[0])), int(row[2]), int(row[1])-int(row[0]), edgecolor="black", alpha=0.5, label=int(row[3])))
          elif(plot_abgelehnt):
              pos_abgelehnt=pos_abgelehnt-int(row[2])
              ax.add_patch(Rectangle((pos_abgelehnt, int(row[0])), int(row[2]), int(row[1])-int(row[0]), edgecolor="purple",facecolor ="red", alpha=0.5))
        

ax.set_xlim([pos_abgelehnt,1000])
ax.set_ylim([8,18])
plt.show()
