using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JsonWrapperButton : MonoBehaviour {

    public Button button;
    public Text buttonText;
    public Controller controller;
    public string lib;
    public Controller.JsonAction action;

    public void OnClick()
    {
        controller.Lib = lib;
        controller.Action = action;
    }
}
