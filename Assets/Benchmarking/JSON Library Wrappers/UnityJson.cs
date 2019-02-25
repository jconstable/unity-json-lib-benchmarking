using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class UnityJson : IJsonLibrary {
    private string jsonText;
    private Holder jsonData;

    public void Setup(string t, Holder sourceData)
    {
        jsonText = t;
        jsonData = sourceData;
    }

    public string Serialize( Stopwatch timer )
    {
        timer.Start();
        string s = JsonUtility.ToJson(jsonData);
        return "Resulting json is " + s.Length.ToString("n0") + " characters";
    }

    public string Deserialize( Stopwatch timer )
    {
        timer.Start();
        Holder list = JsonUtility.FromJson<Holder>(jsonText);
        return "Parsed list is " + list.junkList.Length + " entries long";
    }
}
