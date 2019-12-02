from sklearn import tree
import json
import numpy as np
from sklearn.model_selection import train_test_split
from sklearn.model_selection import KFold
import pickle

dataFileName = r"C:\Source\WearableActivityClassifier\Classify\Data\v2.json"
with open(dataFileName) as json_file:
    data = json.load(json_file)
    dataInArray = []
    for datapoint in data:
        steps = int(datapoint["steps"])
        heartRate = int(datapoint["heartRate"])
        activityType = datapoint["activityType"]
        dataInArray.append([steps,heartRate,activityType])
    npData = np.array(dataInArray)
    np.random.shuffle(npData)

trainFeatures, testFeatures, trainActivity, testActivity = train_test_split(
    npData[:,0:2], npData[:,2], test_size=0.33, random_state=42)

clf = tree.DecisionTreeClassifier(max_depth=3)
clf = clf.fit(trainFeatures,trainActivity)
score = clf.score(testFeatures,testActivity)
if(score < .99):
    print("ERROR TRAINING MODEL")
else:
    savedPickle = pickle.dumps(clf)
    with open('DecisionTreeModel.pickle', 'wb') as handle:
        pickle.dump(savedPickle, handle)



