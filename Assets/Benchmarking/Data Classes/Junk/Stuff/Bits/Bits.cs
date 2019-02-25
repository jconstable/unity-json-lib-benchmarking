using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Bits : ICrap
{
    public string BitsName;
    public string[] SomeBits;

    public void RandomPopulate()
    {
        BitsName = RandomTextGenerator.GetRandom();

        SomeBits = new string[Random.Range(1, 32)];

        for( int i =0; i < SomeBits.Length; i++)
        {
            SomeBits[i] = RandomTextGenerator.GetRandom();
        }
    }

    public void SimpleJSONParse(SimpleJSON.JSONNode node)
    {
        BitsName = node["BitsName"];
        SimpleJSON.JSONNode arr = node["SomePieces"];
        int count = arr.Count;
        SomeBits = new string[count];
        for (int i = 0; i < count; i++)
        {
            SomeBits[i] = arr[i].ToString();
        }
    }

    public SimpleJSON.JSONNode SimpleJSONPopulateNode()
    {
        SimpleJSON.JSONNode n = new SimpleJSON.JSONObject();
        n["BitsName"] = BitsName;

        SimpleJSON.JSONArray arr = new SimpleJSON.JSONArray();
        for (int i = 0; i < SomeBits.Length; i++)
        {
            arr[i] = SomeBits[i];
        }
        n["SomeBits"] = arr;

        return n;
    }
}
