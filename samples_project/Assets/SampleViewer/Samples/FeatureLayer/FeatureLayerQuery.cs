// Copyright 2022 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Esri.ArcGISMapsSDK.Components;
using Esri.GameEngine.Geometry;

// The follow System.Serializable classes are used to define the REST API response
// in order to leverage Unity's JsonUtility.
// When implementing your own version of this the Baseball Properties would need to 
// be updated.
[System.Serializable]
public class FeatureCollectionData
{
    public string type;
    public Feature[] features;
}

[System.Serializable]
public class Feature
{
    public string type;
    public Geometry geometry;
    public TreeProperties properties;
}

[System.Serializable]
public class TreeProperties
{
    public string genus;
    public string crown;
    public string height;
}

[System.Serializable]
public class Geometry
{
    public string type;
    public double[] coordinates;
}

// This class issues a query request to a Feature Layer which it then parses to create GameObjects at accurate locations
// with correct property values. This is a good starting point if you are looking to parse your own feature layer into Unity.
public class FeatureLayerQuery : MonoBehaviour
{
    // The feature layer we are going to query
    public string FeatureLayerURL = "https://services2.arcgis.com/jUpNdisbWqRpMo35/arcgis/rest/services/Baumkataster_Berlin/FeatureServer/0";

    // This prefab will be instatiated for each feature we parse
    public GameObject TreePrefab;

    private int TreeSpawnHeight = 10000;

    // This will hold a reference to each feature we created
    public List<GameObject> Trees = new List<GameObject>();

    // In the query request we can denote the Spatial Reference we want the return geometries in.
    // It is important that we create the GameObjects with the same Spatial Reference
    private int FeatureSRWKID = 4326;

    public ArcGISCameraComponent ArcGISCamera;
    public Dropdown TreeSelector;

    // Get all the features when the script starts
    void Start()
    {
        StartCoroutine(GetFeatures());

        TreeSelector.onValueChanged.AddListener(delegate
        {
            TreeSelected();
        });
    }

    // Sends the Request to get features from the service
    private IEnumerator GetFeatures()
    {
        // To learn more about the Feature Layer rest API and all the things that are possible checkout
        // https://developers.arcgis.com/rest/services-reference/enterprise/query-feature-service-layer-.htm

        string QueryRequestURL = FeatureLayerURL + "/Query?" + MakeRequestHeaders();
        UnityWebRequest Request = UnityWebRequest.Get(QueryRequestURL);
        yield return Request.SendWebRequest();

        if (Request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(Request.error);
        }
        else
        {
           
            CreateGameObjectsFromResponse(Request.downloadHandler.text);
            PopulateTreeDropdown();
        }
    }

    // Creates the Request Headers to be used in our HTTP Request
    // f=geojson is the output format
    // where=1=1 gets every feature. geometry based or more intelligent where clauses should be used
    //     with larger datasets
    // outSR=4326 gets the return geometries in the SR 4326
    // outFields specifies the fields we want in the response
    // only getting the first 500 of the trees for the performance purpose
    private string MakeRequestHeaders()
    {
        string[] OutFields =
        {
             //genus
            "gattung",
            //crown size
            "kronedurch",
            //height
            "baumhoehe"
        };

        string OutFieldHeader = "outFields=";
        for (int i = 0; i < OutFields.Length; i++)
        {
            OutFieldHeader += OutFields[i];

            if (i < OutFields.Length - 1)
            {
                OutFieldHeader += ",";
            }
        }
        string district = "Mitte";
        string[] RequestHeaders =
        {
            "f=geojson",
            $"where=bezirk%3D%27{district}%27",
            "outSR=" + FeatureSRWKID.ToString(),
            OutFieldHeader,
            "resultRecordCount=500"
        };

        string ReturnValue = "";
        for (int i = 0; i < RequestHeaders.Length; i++)
        {
            ReturnValue += RequestHeaders[i];

            if (i < RequestHeaders.Length - 1)
            {
                ReturnValue += "&";
            }
        }

        return ReturnValue;
    }

    // Given a valid response from our query request to the feature layer, this method will parse the response text
    // into geometries and properties which it will use to create new GameObjects and locate them correctly in the world.
    // This logic will differ based on the properties you are trying to parse out of the response.
    private void CreateGameObjectsFromResponse(string Response)
    {
        // Deserialize the JSON response from the query.
        var deserialized = JsonUtility.FromJson<FeatureCollectionData>(Response);
        int counter = 0;
        foreach (Feature feature in deserialized.features)
        {
            double Longitude = feature.geometry.coordinates[0];
            double Latitude = feature.geometry.coordinates[1];

            ArcGISPoint Position = new ArcGISPoint(Longitude, Latitude, TreeSpawnHeight, new ArcGISSpatialReference(FeatureSRWKID));

            var NewTree = Instantiate(TreePrefab, this.transform);
            
            NewTree.name = feature.properties.genus +"_"+counter.ToString();
            counter++;
            Trees.Add(NewTree);
            NewTree.SetActive(true);

            var LocationComponent = NewTree.GetComponent<ArcGISLocationComponent>();
            LocationComponent.enabled = true;
            LocationComponent.Position = Position;

            var TreeInfo = NewTree.GetComponent<Tree>();

            TreeInfo.SetInfo(feature.properties.genus);
            TreeInfo.SetInfo(feature.properties.crown);
            TreeInfo.SetInfo(feature.properties.height);

            TreeInfo.ArcGISCamera = ArcGISCamera;
            TreeInfo.SetSpawnHeight(TreeSpawnHeight);
        }

    }


    private void PopulateTreeDropdown()
    {

        List<string> TreeNames = new List<string>();
        foreach (GameObject tree in Trees)
        {
            TreeNames.Add(tree.name);
        }
        TreeNames.Sort();
        TreeSelector.AddOptions(TreeNames);
    }


    private void TreeSelected()
    {
        var TreeName = TreeSelector.options[TreeSelector.value].text;
        foreach (GameObject tree in Trees)
        {
            if (TreeName == tree.name)
            {
                var TreeLocation = tree.GetComponent<ArcGISLocationComponent>();
                if (TreeLocation == null)
                {
                    return;
                }
                var CameraLocation = ArcGISCamera.GetComponent<ArcGISLocationComponent>();
                double Longitude = TreeLocation.Position.X;
                double Latitude = TreeLocation.Position.Y;
                double CameraAltitude = TreeLocation.Position.Z + 20;
                ArcGISPoint NewPosition = new ArcGISPoint(Longitude, Latitude, CameraAltitude, TreeLocation.Position.SpatialReference);

                CameraLocation.Position = NewPosition;
                CameraLocation.Rotation = TreeLocation.Rotation;
            }
        }
    }
}
