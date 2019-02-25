using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Text;

public class SimpleJson : IJsonLibrary
{
    private string jsonText;
    private Holder jsonData;

    public void Setup(string t, Holder sourceData)
    {
        jsonText = t;
        jsonData = sourceData;
    }

    public string Serialize(Stopwatch timer)
    {
        StringBuilder b = new StringBuilder();
        timer.Start();
        jsonData.SimpleJSONPopulateNode().WriteToStringBuilder(b, 0, 0, SimpleJSON.JSONTextMode.Compact);
        string s = b.ToString();
        return "Resulting json is " + s.Length.ToString("n0") + " characters";
    }

    public string Deserialize(Stopwatch timer)
    {
        timer.Start();

        SimpleJSON.JSONNode node = SimpleJSON.JSON.Parse(jsonText);
        Holder list = new Holder();
        SimpleJSON.JSONArray arr = node["junkList"].AsArray;
        list.junkList = new Junk[arr.Count];

        int i = 0;
        foreach (var n in arr)
        {
            list.junkList[i] = new Junk();
            list.junkList[i].SimpleJSONParse(n);
            i++;
        }

        return "Parsed list is " + list.junkList.Length + " entries long, and we parsed " + i + " entries";
    }
}
