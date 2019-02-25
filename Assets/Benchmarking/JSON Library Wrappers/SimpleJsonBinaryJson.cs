using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Text;

public class SimpleJsonBinaryJson : IJsonLibrary, ICustomDataJsonLibrary
{
    private string jsonText;
    private Holder jsonData;
    private string jsonPath;

    public void SetJsonPath(string defaultPath)
    {
        jsonPath = defaultPath + ".simpleJsonBin";
    }

    // Optionally write the file this library will read (binary formats, for example)
    public string WriteDataInCustomFormat(string jsonText)
    {
        SimpleJSON.JSONNode n = SimpleJSON.JSON.Parse(jsonText);

        n.SaveToBinaryFile(jsonPath);

        return string.Format("{0} wrote additional json file {1}", this.GetType().ToString(), jsonPath);
    }

    public void Setup(string t, Holder sourceData)
    {
        jsonText = t;
        jsonData = sourceData;
    }

    public string Serialize(Stopwatch timer)
    {
        string notes;
        using (System.IO.MemoryStream outStream = new System.IO.MemoryStream(jsonText.Length))
        {
            timer.Start();

            jsonData.SimpleJSONPopulateNode().SaveToBinaryStream(outStream);

            notes = "Resulting stream is " + outStream.Length.ToString("n0") + " bytes";
        }
        return notes;
    }

    public string Deserialize(Stopwatch timer)
    {
        string notes;
        using (System.IO.FileStream inStream = new System.IO.FileStream(jsonPath, System.IO.FileMode.Open))
        {
            using (System.IO.BinaryReader reader = new System.IO.BinaryReader(inStream))
            {
                timer.Start();
                SimpleJSON.JSONNode node = SimpleJSON.JSONNode.DeserializeBinary(reader);
                SimpleJSON.JSONArray arr = node["junkList"].AsArray;
                Holder list = new Holder();
                list.junkList = new Junk[arr.Count];

                int i = 0;
                foreach (var n in arr)
                {
                    list.junkList[i] = new Junk();
                    list.junkList[i].SimpleJSONParse(n);
                    i++;
                }

                notes = "Parsed list is " + list.junkList.Length + " entries long, and we parsed " + i + " entries";
            }
        }
        return notes;
    }
}
