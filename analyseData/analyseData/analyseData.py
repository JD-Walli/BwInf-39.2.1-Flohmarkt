
import pandas as pd
from matplotlib import pyplot as plt
import numpy as np
#import pandas_profiling as pp
df1 =pd.read_csv("C:/Users/Jakov/Desktop/git/BWInf 20/BwInf 39.2.1 Flohmarkt/BwInf 39.2.1 Flohmarkt/flohmarkt 1.csv",delim_whitespace=True)
#pp.ProfileReport(df1)
#print(df1.Mietbeginn)

preisProStand = (df1.Mietende-df1.Mietbeginn)*df1.Länge
einzelPreisProStand= np.unique(preisProStand)
menge=np.zeros((len(einzelPreisProStand)))
for i in range(len(einzelPreisProStand)):
    menge[i]=sum(map(lambda j: j==einzelPreisProStand[i],preisProStand))

print(sum(menge))

dauerProStand = (df1.Mietende-df1.Mietbeginn)
einzelDauerProStand=np.unique(dauerProStand)
mengeDauer=np.zeros((len(einzelDauerProStand)))
avLänge=np.zeros((len(einzelDauerProStand)))
for i in range(len(einzelDauerProStand)):
    mengeDauer[i]=sum(map(lambda j: j==einzelDauerProStand[i],dauerProStand)) #anzahl an Einträgen mit der gleichen Dauer wie betrachetetes Element
    ind=np.where(dauerProStand==einzelDauerProStand[i]) #indices von allen Einträgen mit entsprechender Dauer
    avLänge[i]=np.average(df1.Länge[dauerProStand==einzelDauerProStand[i]]) #Durchschnittslänge dieser Einträge

print(df1.Mietbeginn.shape)
#xes=np.concatenate([np.reshape(df1.Mietbeginn,(1,len(df1.Mietbeginn))),np.reshape(df1.Mietende,(1,len(df1.Mietende)))],axis=0)
xes=np.concatenate([df1.Mietbeginn,df1.Mietende])
xes=xes.reshape(2,len(df1.Mietbeginn))
xes=np.rot90(xes,3)
yes=np.concatenate([df1.Länge,df1.Länge])
yes=yes.reshape(2,len(df1.Länge))
yes=np.rot90(yes,3)


#fig = plt.figure() 
#ax = plt.axes(projection ='3d') 
#ax.plot3D(einzelDauerProStand,mengeDauer,avLänge) 
#for i in range(len(xes)):
#    ax.plot3D(xes[i],yes[i],[i,i])
plt.bar(einzelPreisProStand,menge)
plt.show()
