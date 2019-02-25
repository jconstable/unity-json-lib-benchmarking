using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICustomDataJsonLibrary {

    // In case this type of json needs a custom data file (binary formats, for example)
    void SetJsonPath(string defaultPath);

    // Optionally write the file this library will read (binary formats, for example)
    string WriteDataInCustomFormat(string jsonText);
}