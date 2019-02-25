using UnityEngine;
using System.Text;
using System.Collections;

public class CustomExample2 : MonoBehaviour
{
	struct CipherInt
	{
		public int		value;

		public static void Serialize( StringBuilder sb, object obj )
		{
			CipherInt i = (CipherInt)obj;
			uint n = (uint)i.value ^ 0x12345678U;
			sb.Append(n.ToString());
		}

		public static object Deserialize( System.Type type, FastJson.JsonParser parser )
		{
			CipherInt i = new CipherInt();
			i.value = (int)(parser.ReadUInt32() ^ 0x12345678U);
			return i;
		}
	}

	void Awake()
	{
		//registered serializer/deserializer is called.
		FastJson.RegisterSerializer( typeof(CipherInt), CipherInt.Serialize );
		FastJson.RegisterDeserializer( typeof(CipherInt), CipherInt.Deserialize );
	}

	void Start () 
	{
		CipherInt i = new CipherInt();
		i.value = 123;

		string json = FastJson.Serialize( i );
		Debug.Log( json );

		CipherInt d = FastJson.Deserialize<CipherInt>(json);
		Debug.Log( d.value );
	}
}
