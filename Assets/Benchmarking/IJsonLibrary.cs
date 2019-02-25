using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public interface IJsonLibrary
{
    // Create the resources the library will need for serializing/deserializing
    void Setup(string jsonText, Holder sourceData);
    
    // Serialize the json to string. Must call timer.Start()
    string Serialize(Stopwatch timer);

    // Deserialize the json to classes. Must call timer.Start()
    string Deserialize(Stopwatch timer);
}
