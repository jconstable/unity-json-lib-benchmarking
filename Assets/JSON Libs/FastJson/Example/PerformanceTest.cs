using UnityEngine;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class PerformanceTest : MonoBehaviour
{

	void OnGUI()
	{
		float scale = Camera.main.pixelHeight / 240;
		GUI.matrix = Matrix4x4.Scale(new Vector3(scale,scale,scale));

		if( GUILayout.Button("test") )
		{
			TestPerformance();
		}
	}

	StringBuilder resultText = new StringBuilder();

	void TestPerformance()
	{
		//Random.seed = 12345;

#if true
		Test1<IntClass,int>			("int", 				() => Random.Range(-10000,10000) );
		Test1<FloatClass,float>		("float",				() => Random.value );
		Test1<BoolClass,bool>		("bool",				() => (Random.Range(0,2) == 0) );
		Test1<Vector3Class,Vector3>	("Vector3",				() => new Vector3(Random.value,Random.value,Random.value) );
		Test1<StringClass,string>	("string",				() => GenerateRandomText(100) );
		Test1<EnumClass,EnumA>		("enum",			 	() => (EnumA)Random.Range(0,6) );
		Test1<ClassAClass,ClassA>	("UserDefineClassA", 	() => ClassA.Generate());
#endif
		
#if false
		Test2("int", 				() => Random.Range(-10000,10000) );
		Test2("float",				() => Random.value );
		Test2("bool",				() => (Random.Range(0,2) == 0) );
		Test2("Vector3",			() => new Vector3(Random.value,Random.value,Random.value) );
		Test2("string",				() => GenerateRandomText(100) );
		Test2("enum",			 	() => (EnumA)Random.Range(0,6) );
		Test2("UserDefineClassA", 	() => ClassA.Generate());
#endif
	}


	class IntClass{ public int value = 0; }
	class FloatClass{ public float value = 0; }
	class BoolClass{ public bool value = false; }
	class Vector3Class{ public Vector3 value = Vector3.zero; }
	class StringClass{ public string value = ""; }
	class ClassAClass{ public ClassA value= null; }
	class EnumClass{ public EnumA value = EnumA.ABC; }

	delegate T GenerateFunc<T>();

	T GenerateTestData2<T,U>( GenerateFunc<U> generateValue ) where T : new()
	{
		T data = new T();
		typeof(T).GetField("value").SetValue( data, generateValue() );		
		return data;
	}
	
	void Test1<T,U>( string name, GenerateFunc<U> generate ) where T : new()
	{
		resultText = new StringBuilder();

		T obj = GenerateTestData2<T,U>(generate);
		
		Stopwatch( "FastJson,s,"+name, 	()=>{ FastJson.Serialize(obj); } );
		//Stopwatch( "EasyJson,s,"+name,	()=>{ EasyJSON.Serializer.Serialize(obj); } );
		//Stopwatch( "XML-JSON,s,"+name,	()=>{ JSONSerializer.Serialize(obj); } );
		//Stopwatch( "JSON.NET,s,"+name, 	()=>{ Newtonsoft.Json.JsonConvert.SerializeObject(obj); } );

		string fastJsonText = FastJson.Serialize(obj);
		//string jsonDotNetText = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
		//string easyJsonText = EasyJSON.Serializer.Serialize(obj);
		//string xmlJsonText = JSONSerializer.Serialize(obj);
		
		Stopwatch( "FastJson,d,"+name, 	()=>{ FastJson.Deserialize<T>(fastJsonText); } );
		//Stopwatch( "EasyJson,d,"+name,	()=>{ EasyJSON.Serializer.Deserialize<T>(easyJsonText); } );
		//Stopwatch( "XML-JSON,d,"+name,	()=>{ JSONSerializer.Deserialize<T>(xmlJsonText); } );
		//Stopwatch( "JSON.NET,d,"+name, 	()=>{ Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonDotNetText); } );
		
		Debug.Log(resultText.ToString());
	}
	
	class ArrayClass<T>
	{
		public T[]	array;
	}

	ArrayClass<T> GenerateTestData2<T>( int count, GenerateFunc<T> generate )
	{
		ArrayClass<T> data = new ArrayClass<T>();
		data.array = new T[count];
		
		for( int i = 0; i < count; ++i )
		{
			data.array[i] = generate();
		}
		
		return data;
	}
	
	void Test2<T>( string name, GenerateFunc<T> generate )
	{
		resultText = new StringBuilder();

		ArrayClass<T> obj = GenerateTestData2<T>(1000,generate);

		Stopwatch( "FastJson,s,"+name, 	()=>{ FastJson.Serialize(obj); } );
		//Stopwatch( "JSON.NET,s,"+name, 	()=>{ Newtonsoft.Json.JsonConvert.SerializeObject(obj); } );
		//Stopwatch( "EasyJson,s,"+name,	()=>{ EasyJSON.Serializer.Serialize(obj); } );
		//Stopwatch( "XML-JSON,s,"+name,	()=>{ JSONSerializer.Serialize(obj); } );

		string fastJsonText = FastJson.Serialize(obj);
		//string jsonDotNetText = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
		//string easyJsonText = EasyJSON.Serializer.Serialize(obj);
		//string xmlJsonText = JSONSerializer.Serialize(obj);
		
		Stopwatch( "FastJson,d,"+name, 	()=>{ FastJson.Deserialize<ArrayClass<T>>(fastJsonText); } );
		//Stopwatch( "JSON.NET,d,"+name, 	()=>{ Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonDotNetText); } );
		//Stopwatch( "EasyJson,d,"+name,	()=>{ EasyJSON.Serializer.Deserialize<ArrayClass<T>>(easyJsonText); } );
		//Stopwatch( "XML-JSON,d,"+name,	()=>{ JSONSerializer.Deserialize<ArrayClass<T>>(xmlJsonText); } );
		
		Debug.Log(resultText.ToString());
	}

	void Stopwatch( string text, System.Action action )
	{
#if UNITY_ANDROID || UNITY_IPHONE
		int loop = 5000;
#else
		int loop = 100000;
#endif
	
		string result = "unsupported";

		try
		{
			System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
			
			for( int i = 0; i < loop; ++i )
			{
				action();
			}
			
			stopwatch.Stop();
			result = stopwatch.ElapsedMilliseconds.ToString();
		}
		catch( System.Exception ){}

		resultText.AppendLine( string.Format("{0},{1}",text, result) );
	}

	enum EnumA
	{
		ABC,
		DEF,
		GHI,
		JKLMN,
		OPQRST,
		UVWXWZ,
	}
	
	class ClassA
	{
		public int		intValueA;
		public int		intValueB;
		public bool		boolValue;
		public string	stringValueA;
		public string	stringValueB;
		public float	floatValue;
		public List<string>	stringList;
		public List<int>	intList;
		
		public static ClassA Generate()
		{
			ClassA obj = new ClassA();
			obj.intValueA = Random.Range(-100,100);
			obj.intValueB = Random.Range(-1000,1000);
			obj.boolValue = Random.Range(0,2) == 0;
			obj.stringValueA = GenerateRandomText(10);
			obj.stringValueB = GenerateRandomText(100);
			obj.floatValue = Random.value;
			obj.stringList = new List<string>();
			for( int i = 0; i < 10; ++i )
			{
				obj.stringList.Add(GenerateRandomText(10));
			}
			obj.intList = new List<int>();
			for( int i = 0; i < 10; ++i )
			{
				obj.intList.Add(Random.Range(-10,10));
			}
			
			return obj;
		}
	}
	
	static string GenerateRandomText( int length )
	{
		char[] buff = new char[length];
		
		for( int i = 0; i < length; ++i )
		{
			switch(Random.Range(0,3))
			{
			case 0: buff[i] = (char)Random.Range('a','z'+1); break;
			case 1: buff[i] = (char)Random.Range('A','Z'+1); break;
			case 2: buff[i] = (char)Random.Range('0','9'+1); break;
			}
		}
		
		return new string(buff);
	}

}

