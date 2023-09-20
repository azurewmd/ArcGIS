using System;
using System.Collections;
using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Components;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public struct Data
{
    public string name;
    public bool enabled;
}

public class ScrollViewItem : MonoBehaviour, IPointerClickHandler
{
    private FeatureLayer featureLayer;
    public Data data;

    private void Start()
    {
        featureLayer = FindObjectOfType<ArcGISMapComponent>().GetComponentInChildren<FeatureLayer>();
        data.name = GetComponentInChildren<TextMeshProUGUI>().text;
    }

    private void Update()
    {
        if (featureLayer.GetAllOutfields && data.name == "Get All Features")
        {
            data.enabled = true;
        }

        GetComponentInChildren<Toggle>().isOn = data.enabled;
        featureLayer.SelectItems();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!featureLayer.outfieldsToGet.Contains(data.name))
        {
            if (data.name == "Get All Features" && !featureLayer.GetAllOutfields)
            {
                featureLayer.GetAllOutfields = true;
                featureLayer.outfieldsToGet.Clear();
            }
            else
            {
                featureLayer.GetAllOutfields = false;
                featureLayer.outfieldsToGet.Remove("Get All Features");
            }

            featureLayer.outfieldsToGet.Add(data.name);
            data.enabled = true;
        }
        else
        {
            if (data.name == "Get All Features" && featureLayer.GetAllOutfields)
            {
                featureLayer.GetAllOutfields = false;
            }

            featureLayer.outfieldsToGet.Remove(data.name);
            data.enabled = false;
        }
    }
}