import logging

import azure.functions as func
from sklearn import tree
import pickle
from os import path

def main(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')

    decisionTreeModelFilename = 'DecisionTreeModel.pkl'
    if path.exists(decisionTreeModelFilename):
        logging.info("file exists")
        with open(decisionTreeModelFilename, 'rb') as handle:
            clf = pickle.load(handle)
            logging.info("Loaded decision tree")
    
    logging.info("AFTER READ")

    req_body = req.get_json()
    heartRate = req_body.get("heartRate")
    steps = req_body.get("steps")
    
    logging.info("HeartRate:"+str(heartRate))
    logging.info("Steps:"+str(steps))
    
    classifications = clf.predict([[steps,heartRate]])
    activityClassification = classifications[0]

    output = '{"activityType":"' + activityClassification + '"}'
    return func.HttpResponse(output)

