using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

[System.Serializable]
public class Junk : ICrap {
    public string JunkName;
    public string JunkData;
    public float FloatA;
    public float FloatD;
    public float FloatC;
    public float FloatB;

    public int IntA;
    public int IntB;

    public Stuff MyStuff;
    public Thing MyThing;

    public void RandomPopulate()
    {
        JunkName = RandomTextGenerator.GetRandom();
        JunkData = RandomTextGenerator.GetRandom();

        FloatA = Random.Range(0f, float.MaxValue);
        FloatB = Random.Range(0f, float.MaxValue);
        FloatC = Random.Range(0f, float.MaxValue);
        FloatD = Random.Range(0f, float.MaxValue);

        IntA = Random.Range(0, int.MaxValue);
        IntB = Random.Range(0, int.MaxValue);

        MyStuff = new Stuff();
        MyThing = new Thing();

        MyStuff.RandomPopulate();
        MyThing.RandomPopulate();
    }

    public void SimpleJSONParse(SimpleJSON.JSONNode node)
    {
        JunkName = node["JunkName"].ToString();
        JunkData = node["JunkData"].ToString();

        FloatA = node["FloatA"].AsFloat;
        FloatB = node["FloatB"].AsFloat;
        FloatC = node["FloatC"].AsFloat;
        FloatD = node["FloatD"].AsFloat;

        IntA = node["IntA"].AsInt;
        IntB = node["IntB"].AsInt;

        MyStuff = new Stuff();
        MyThing = new Thing();

        MyStuff.SimpleJSONParse(node["MyStuff"]);
        MyThing.SimpleJSONParse(node["MyThing"]);
    }

    public SimpleJSON.JSONNode SimpleJSONPopulateNode()
    {
        SimpleJSON.JSONNode n = new SimpleJSON.JSONObject();
        n["JunkName"] = JunkName;
        n["JunkData"] = JunkData;

        n["FloatA"] = FloatA;
        n["FloatB"] = FloatB;
        n["FloatC"] = FloatC;
        n["FloatD"] = FloatD;

        n["IntA"] = IntA;
        n["IntB"] = IntB;

        n["MyStuff"] = MyStuff.SimpleJSONPopulateNode();
        n["MyThing"] = MyThing.SimpleJSONPopulateNode();

        return n;
    }
}
