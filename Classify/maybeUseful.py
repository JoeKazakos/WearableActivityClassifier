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