import datetime
import logging


import azure.functions as func
from os import path
import os
import requests
import numpy as np
import json
from sklearn.model_selection import train_test_split
from sklearn import tree
import pickle


def main(mytimer: func.TimerRequest) -> None:
    utc_timestamp = datetime.datetime.utcnow().replace(
        tzinfo=datetime.timezone.utc).isoformat()

    logging.info('TIMER HIT')
    url=os.environ.get('getDataURL')
    logging.info("url"+ str(url))

    dataResponseString = requests.post(url=url)
    logging.info(dataResponseString)

    data = json.loads(dataResponseString.text)
    logging.info(data)

    dataInArray = []
    for datapoint in data:
        steps = int(datapoint["steps"])
        heartRate = int(datapoint["heartRate"])
        activityType = datapoint["activityType"]
        if steps > 0 or heartRate > 0:
            dataInArray.append([steps,heartRate,activityType])
    npData = np.array(dataInArray)
    np.random.shuffle(npData)
    
    logging.info(npData)

    trainFeatures, testFeatures, trainActivity, testActivity = train_test_split(
        npData[:,0:2], npData[:,2], test_size=0.33, random_state=42)

    clf = tree.DecisionTreeClassifier(max_depth=3)
    clf = clf.fit(trainFeatures,trainActivity)
    score = clf.score(testFeatures,testActivity)
    logging.info("Score:"+str(score))
    if(score < .99):
        logging.info("ERROR TRAINING MODEL")
    else:
        logging.info("Model successfully trained")
        with open('DecisionTreeModel.pkl', 'wb') as handle:
            pickle.dump(clf, handle)

    logging.info("AFTER READ")

    if mytimer.past_due:
        logging.info('The timer is past due!')

    logging.info('Python timer trigger function ran at %s', utc_timestamp)
