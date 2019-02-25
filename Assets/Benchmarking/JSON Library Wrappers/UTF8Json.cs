using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Text;

public class UTF8Json  : IJsonLibrary
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
        timer.Start();
        string s = Utf8Json.JsonSerializer.ToJsonString<Holder>(jsonData);
        return "Resulting json is " + s.Length.ToString("n0") + " characters";
    }

    public string Deserialize(Stopwatch timer)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(jsonText);
        timer.Start();
        Holder list = Utf8Json.JsonSerializer.Deserialize<Holder>(bytes);
        return "Parsed list is " + list.junkList.Length + " entries long";
    }
}
