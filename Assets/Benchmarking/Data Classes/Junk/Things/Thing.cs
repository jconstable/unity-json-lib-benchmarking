using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Thing : ICrap {
    public string MyStringA;
    public string MyStringB;
    public string MyStringC;
    public string MyStringD;


    public void RandomPopulate()
    {
        MyStringA = RandomTextGenerator.GetRandom();
        MyStringB = RandomTextGenerator.GetRandom();
        MyStringC = RandomTextGenerator.GetRandom();
        MyStringD = RandomTextGenerator.GetRandom();
    }

    public void SimpleJSONParse(SimpleJSON.JSONNode node)
    {
        MyStringA = node["MyStringA"].ToString();
        MyStringB = node["MyStringB"].ToString();
        MyStringC = node["MyStringC"].ToString();
        MyStringD = node["MyStringD"].ToString();
    }

    public SimpleJSON.JSONNode SimpleJSONPopulateNode()
    {
        SimpleJSON.JSONNode n = new SimpleJSON.JSONObject();

        n["MyStringA"] = MyStringA;
        n["MyStringB"] = MyStringB;
        n["MyStringC"] = MyStringC;
        n["MyStringD"] = MyStringD;

        return n;
    }
}
