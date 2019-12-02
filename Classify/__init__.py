import logging

import azure.functions as func
#import json
#import numpy as np
#import pickle
#from sklearn import tree
    
def main(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')
    
   # with open('DecisionTreeModel.pickle', 'rb') as handle:
   #     clf = pickle.load(handle)

    name = req.params.get('name')
    if not name:
        try:
            req_body = req.get_json()
        exceptdf ValueError:
            pass
        else:
            name = req_body.get('name')

    if name:
        return func.HttpResponse(f"Bye {name}!")
    else:
        return func.HttpResponse(
             "Please pass a name on the query string or in the request body",
             status_code=400
        )
