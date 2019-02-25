using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public interface ICrap
{
    // Generate random data for this class
    void RandomPopulate();

    // Helper for the SimpleJSON lib
    void SimpleJSONParse(SimpleJSON.JSONNode node);

    SimpleJSON.JSONNode SimpleJSONPopulateNode();
}
