using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Stuff : ICrap
{
    public Bits MyBits;
    public Pieces MyPieces;

    public string StuffName;
    public string DescA;
    public string DescB;
    public string DescC;
    public string DescD;
    public string DescE;

    public int AttrA;
    public int AttrB;
    public int AttrC;


    public void RandomPopulate()
    {
        MyBits = new Bits();
        MyPieces = new Pieces();
        MyBits.RandomPopulate();
        MyPieces.RandomPopulate();

        StuffName = RandomTextGenerator.GetRandom();
        DescA = RandomTextGenerator.GetRandom();
        DescB = RandomTextGenerator.GetRandom();
        DescC = RandomTextGenerator.GetRandom();
        DescD = RandomTextGenerator.GetRandom();
        DescE = RandomTextGenerator.GetRandom();

        AttrA = Random.Range(0, int.MaxValue);
        AttrB = Random.Range(0, int.MaxValue);
        AttrC = Random.Range(0, int.MaxValue);
    }

    public void SimpleJSONParse(SimpleJSON.JSONNode node)
    {
        StuffName = node["StuffName"].ToString();
        DescA = node["DescA"].ToString();
        DescB = node["DescB"].ToString();
        DescC = node["DescC"].ToString();
        DescD = node["DescD"].ToString();
        DescE = node["DescE"].ToString();

        AttrA = node["AttrA"].AsInt;
        AttrB = node["AttrB"].AsInt;
        AttrC = node["AttrC"].AsInt;

        MyBits = new Bits();
        MyPieces = new Pieces();

        MyBits.SimpleJSONParse(node["MyBits"]);
        MyPieces.SimpleJSONParse(node["MyPieces"]);
    }

    public SimpleJSON.JSONNode SimpleJSONPopulateNode()
    {
        SimpleJSON.JSONNode n = new SimpleJSON.JSONObject();
        n["StuffName"] = StuffName;

        n["DescA"] = DescA;
        n["DescB"] = DescB;
        n["DescC"] = DescC;
        n["DescD"] = DescD;
        n["DescE"] = DescE;

        n["AttrA"] = AttrA;
        n["AttrB"] = AttrB;
        n["AttrC"] = AttrC;

        n["MyBits"] = MyBits.SimpleJSONPopulateNode();
        n["MyPieces"] = MyPieces.SimpleJSONPopulateNode();

        return n;
    }
}