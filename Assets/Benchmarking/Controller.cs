using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using UnityEngine.UI;

public class Controller : MonoBehaviour {
    public int Size = 500;
    public string Lib;
    public JsonAction Action = JsonAction.None;

    public GameObject ButtonLayout;
    public Text LastLibName;
    public Text LastActionName;
    public Text LastTimeValue;
    public Text Notes;
    public Text LoadedStatus;

    public InputField NumEntriesToCreate;
    public InputField NumSamplesToRun;

    private List<IJsonLibrary> m_knownJsonLibraryWrappers = null;
    private int m_working = -1;

    private string m_jsonText;
    private Holder m_holder;

    public enum JsonAction
    {
        None,
        Serialize,
        Deserialize
    }

    void LocateJsonWrapperClasses()
    {
        m_knownJsonLibraryWrappers = new List<IJsonLibrary>();
        var assembly = System.Reflection.Assembly.GetAssembly(typeof(IJsonLibrary));
        foreach( var t in assembly.GetExportedTypes())
        {
            if (!t.IsAbstract)
            {
                var interfaces = new List<System.Type>(t.GetInterfaces());
                if (interfaces.Contains(typeof(IJsonLibrary))){
                    IJsonLibrary wrapper = System.Activator.CreateInstance(t) as IJsonLibrary;
                    if (wrapper != null)
                    {
                        m_knownJsonLibraryWrappers.Add(wrapper);
                    }
                }
            }
        }

        UnityEngine.Debug.Log(string.Format("Found {0} json library wrappers", m_knownJsonLibraryWrappers.Count));
    }

	// Use this for initialization
	void Start () {
        LocateJsonWrapperClasses();

        CreateActionButtonsForWrappers();

        LoadJson();
    }

    public void UnloadJson()
    {
        m_jsonText = null;
        m_holder = null;
        LoadedStatus.text = "Not Loaded";
        LoadedStatus.color = Color.red;
    }

    public void LoadJson()
    {
        try
        {
            m_jsonText = System.IO.File.ReadAllText(JsonPath());
            m_holder = JsonUtility.FromJson<Holder>(m_jsonText);

            foreach (var wrapper in m_knownJsonLibraryWrappers)
            {
                wrapper.Setup(m_jsonText, m_holder);
                var customDataWrapper = wrapper as ICustomDataJsonLibrary;
                if (customDataWrapper != null)
                {
                    customDataWrapper.SetJsonPath(JsonPath());
                }
            }

            long size = m_jsonText.Length;
            long mb = size / (1024 * 1024);
            Notes.text = string.Format("Loaded JSON with {0} characters ({1}MB)", size, mb);

            LoadedStatus.text = "Loaded!";
            LoadedStatus.color = Color.green;
        } catch (System.Exception e )
        {
            UnityEngine.Debug.Log(e.Message);
            UnloadJson();
        }
    }

    void CreateActionButtonsForWrappers()
    {
        foreach (var wrapper in m_knownJsonLibraryWrappers)
        {
            string lib = wrapper.GetType().ToString();
            var horizontalLayout = new GameObject(lib + " buttons").AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            horizontalLayout.childForceExpandHeight = true;
            horizontalLayout.childForceExpandWidth = true;
            horizontalLayout.childControlHeight = true;
            horizontalLayout.childControlWidth = true;

            var actions = new List<JsonAction>() { JsonAction.Serialize, JsonAction.Deserialize };
            foreach (var action in actions)
            {
                GameObject buttonGO = Instantiate(Resources.Load<GameObject>("UI/JsonWrapperButton"));
                buttonGO.name = lib + "+" + action.ToString();
                JsonWrapperButton button = buttonGO.GetComponent<JsonWrapperButton>();
                button.action = action;
                button.lib = lib;
                button.buttonText.text = lib + "\n" + action.ToString();
                button.controller = this;
                buttonGO.transform.SetParent(horizontalLayout.transform);
            }

            horizontalLayout.transform.SetParent(ButtonLayout.transform);
        }
    }

    string JsonPath()
    {
        return Application.persistentDataPath + "/../" + "Sample.json";
    }

    // Update is called once per frame
    void Update() {
        if (m_working == -1)
        {
            if (Action != JsonAction.None)
            {
                if( m_holder != null )
                {
                    m_working = 0;
                    StartCoroutine(DoBenchmark());
                } else
                {
                    Notes.text = "You must load the json first. Click 'Load JSON'";
                }
                
            }
        } else
        {
            string workingText = "Working.";
            for( int i = 0; i < m_working; i++ )
            {
                workingText += ".";
            }

            Notes.text = workingText + "\n" ;
            LastTimeValue.text = workingText;
            m_working = (m_working + 1) % 3;
        }

    }

    IEnumerator DoBenchmark()
    {
        yield return null;
        
        // Attempt to find a wrapper matching the selection name
        IJsonLibrary wrapper = null;
        foreach (var w in m_knownJsonLibraryWrappers)
        {
            System.Type t = w.GetType();
            if (t.ToString() == Lib)
            {
                wrapper = w;
                break;
            }
        }

        if (wrapper != null)
        {
            yield return null;

            LastLibName.text = Lib;
            LastActionName.text = Action.ToString();

            long sum = 0;
            long samples = long.Parse(NumSamplesToRun.text);
            string notes = string.Empty; // Gets overwritten, only last notes are shown
            long avg = 0;

            if (samples > 0)
            {   
                for (int i = 0; i < samples; i++)
                {
                    Stopwatch timer = new Stopwatch();


                    if (Action == JsonAction.Deserialize)
                    {
                        notes = string.Format("Json being used is {0} char long", m_jsonText.Length);
                        notes += wrapper.Deserialize(timer);
                    }
                    else if (Action == JsonAction.Serialize)
                    {
                        notes = string.Format("Class being serialized has {0} complex elements", m_holder.junkList.Length);
                        notes += wrapper.Serialize(timer);
                    }

                    if (!timer.IsRunning)
                        throw new System.Exception("Stopwatch timer was not started by wrapper");

                    timer.Stop();

                    sum += timer.ElapsedMilliseconds;

                    //UnityEngine.Debug.Log(string.Format("----> {0} using {1} took {2}", Action, Lib, timer.ElapsedMilliseconds));
                    yield return null;
                }

                avg = (sum / samples);
            }

            notes += "\n" + samples.ToString() + " samples taken";
            Notes.text = notes;

            LastTimeValue.text = avg.ToString();
        }
        else
        {
            UnityEngine.Debug.Log(string.Format("Unable to find suitable IJsonWrapper to match selection {0}", Lib));
        }

        Action = JsonAction.None;
        m_working = -1;
	}

    public void CreateJson()
    {
        UnloadJson();

        Notes.text = "Working...";

        Holder h = new Holder();
        h.Capacity = int.Parse(NumEntriesToCreate.text);
        h.RandomPopulate();

        string jsonText = JsonUtility.ToJson(h);
        string notes = string.Format("Randomly generated json file with {0} characters", jsonText.Length.ToString("n0"));

        // Write basic json text data to default json file
        string jsonPath = JsonPath();
        System.IO.File.WriteAllText(jsonPath, jsonText);
        notes += string.Format("\nWrote json text to {0}", jsonPath);

        // Write custom data for wrappers that need it
        foreach( var wrapper in m_knownJsonLibraryWrappers )
        {
            var customDataWrapper = wrapper as ICustomDataJsonLibrary;
            if( customDataWrapper != null )
            {
                customDataWrapper.SetJsonPath(jsonPath);
                notes += "\n"+customDataWrapper.WriteDataInCustomFormat(jsonText);
            }
        }

        UnityEngine.Debug.Log(notes);
        Notes.text = notes;
    }
}
