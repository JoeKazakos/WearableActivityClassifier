   kf = KFold(n_splits=5)
    for trainIndex, testIndex in kf.split(inputFeatures):
        trainInputFeatures = npData[trainIndex,0:2]
        trainClasses = npData[trainIndex,2]
        testInputFeatures = npData[testIndex,0:2]
        testClasses = npData[testIndex,2]
        for depth in range(1,7):
            clf = tree.DecisionTreeClassifier(max_depth=depth)
            clf = clf.fit(trainInputFeatures,trainClasses)
            score = clf.score(testInputFeatures,testClasses)
            print(str(depth) + "," + str(score))
        
    for depth in range(1,7):
        clf = tree.DecisionTreeClassifier(max_depth=depth)
        clf = clf.fit(npData[:,0:2],npData[:,2])
        score = clf.score(npData[:,0:2],npData[:,2])
        print("d:"+str(depth) + " s:" + str(score))   
        dot_data = tree.export_graphviz(clf,
            feature_names=['steps','heartRate'],
            class_names=['Other','Run'],
            filled=True, rounded=True,  
            special_characters=True)
        #g = graphviz.Source(dot_data)
        #g.render("classifier" + str(depth))









    name = req.params.get('name')
    if not name:
        try:
            req_body = req.get_json()
        except ValueError:
            pass
        else:
            name = req_body.get('name')

    if name:
        return func.HttpResponse(f"Bye 5 {name}!")
    else:
        return func.HttpResponse(
             "Please pass a name on the query string or in the request body",
             status_code=400
        )

c = 0

bball = 0
r = 0
run = 0

for d in data:
    if d["activityType"]=="Run":
        time = d["time"][0:10]
        if time == "11/28/2019":
            run += 1
        if time == "11/27/2019":
            r += 1
        if time == "12/05/2019":
            bball += 1













# create candlestick plot
overallArray = []
runActualArray = []
otherActualArray = []
runPredictArray = []
otherPredictArray = []
for counter in range(1000):
    trainFeatures, testFeatures, trainActivity, testActivity = train_test_split(
            npData[:,0:2], npData[:,2], test_size=0.33)
    clf = tree.DecisionTreeClassifier(max_depth=3)
    clf = clf.fit(trainFeatures,trainActivity)
    predictions = clf.predict(testFeatures)
    t = [["Other","Other",0],["Other","Run",0],["Run","Run",0],["Run","Other",0]]
    for i in range(len(testActivity)):
        predict = predictions[i]
        actual = testActivity[i]
        for x in t:
            if x[0] == predict and x[1] == actual:
                x[2] += 1
    # stats
    otherOther = t[0][2]
    otherRun = t[1][2]
    runRun = t[2][2]
    runOther = t[3][2]
    overall = (otherOther + runRun)/(otherOther+otherRun+runRun+runOther)
    runActual = (runRun)/(runRun + otherRun)
    otherActual = (otherOther)/(otherOther+runOther)
    runPredict = (runRun)/(runRun+runOther)
    otherPredict = (otherOther)/(otherOther+otherRun)
    overallArray.append(overall)
    runActualArray.append(runActual)
    otherActualArray.append(otherActual)
    runPredictArray.append(runPredict)
    otherPredictArray.append(otherPredict)



#make scatter plot of dataset
with open("datapoints.txt", 'w') as handle:
    for a in npData:
        steps=int(a[0])
        hr = int(a[1])
        activity = a[2]
        if(activity == "Run"):
            line = str(steps)+", ,"+str(hr)+"\n"
        else:
            line = str(steps)+","+str(hr)+", \n"
        handle.write(line)

# make scatter plot of predictions
if 1==1:
    trainFeatures, testFeatures, trainActivity, testActivity = train_test_split(
            npData[:,0:2], npData[:,2], test_size=0.33)
    clf = tree.DecisionTreeClassifier(max_depth=3)
    clf = clf.fit(trainFeatures,trainActivity)
    testPoints = []
    for s in range(0,200,5):
        for h in range(0,160,5):
            testPoint = [s,h]
            testPoints.append(testPoint)
    testPointsPredicts = clf.predict(testPoints)
    with open("predicts.txt", 'w') as handle:
        for a in range(len(testPoints)):
            steps=int(testPoints[a][0])
            hr = int(testPoints[a][1])
            activity = testPointsPredicts[a]
            if(activity == "Run"):
                line = str(steps)+", ,"+str(hr)+"\n"
            else:
                line = str(steps)+","+str(hr)+", \n"
            handle.write(line)

# png decision tree
if 1==1:
    dot_data = tree.export_graphviz(clf, out_file=None, 
        feature_names=["Steps","Heart Rate"],  
        class_names=["Other","Run"],  
        filled=True, rounded=True,  
        special_characters=True)  
    graph = graphviz.Source(dot_data)
    graph.render("ActivityClassifier")