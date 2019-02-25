FastJson is fast json serializer and deserializer.
FastJson can interconvert an object of C# json text.

---------------------------
- API

-Serialize

string FastJson.Serialize( object obj )

C# object is serialized to json text.

-Deserialize

T      FastJson.Deserialize<T>  ( string jsonText )
object FastJson.Deserialize     ( string jsonText, System.Type type )

C# object is deserialize from json text.
Information on the type of the object is needed.
When there is an error in json text, an exception occurs.


---------------------------
- How to custom serialization 1

When ToJson or FromJson are defined in type of a target, it's used automatically. It's necessary to have static and following arguments.
- static void ToJson( StringBuilder sb, object obj )
- static object FromJson( System.Type type, FastJson.JsonParser parser )

There is a sample in CustomExample.cs.


---------------------------
- How to custom serialization 2

Serializer/Deserializer can be added at FastJson.RegisterSerializer() and FastJson.RegisterDeserializer().

There is a sample in CustomExample2.cs.


---------------------------
- The countermeasure when a AOT error occurred for iOS

When occurring in CreateInstance(), when the type of new() is written specifically, it can be avoided.
When writing it, it can be avoided. It isn't necessary to call.

ex) When an error occurred in CreateInstance in UserClass<Vector3>.
-----------
#pragma warning disable 219

void AOTDummy()
{
	UserClass<Vector3> dummy = new UserClass<Vector3>();
}

#pragma warning restore 219
-----------

In case of generic container, Add() is sometimes also needed.
ex) When an error occurred in List<Vector3>.
-----------
List<Vector3> dummy = new List<Vector3>();
dummy.Add( new Vector3() );
-----------

When the following is defined, it can be avoided in case of HashSet<T>.
-----------
HashSet<Vector3> dummy = new HashSet<Vector3>( new FastJson.DeserializeEnumerator<Vector3>(null,null) );	
dummy.Add(new Vector3());
-----------


---------------------------
- Option

-FastJson.enumSerializationMethod
The type of the serialize of enum is chosen.
Default value is 'EnumSerializationMethod.Name'.

ex)
enum Enum{ AAA }

EnumSerializationMethod.Name:
	Enum.AAA => "AAA"

EnumSerializationMethod.UnderlyingValue
	Enum.AAA => 0

---------------------------
-Example
Please watch Examle.cs

