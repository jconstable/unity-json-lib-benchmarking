using UnityEngine;
using System.Collections;
using System.Text;

public class CustomExample : MonoBehaviour 
{
	struct CipherInt
	{
		public int		value;

		//When the name is 'ToJson' and argument is identical, it's used automatically.
		static void ToJson( StringBuilder sb, object obj )
		{
			CipherInt i = (CipherInt)obj;
			uint n = (uint)i.value ^ 0x12345678U;
			sb.Append(n.ToString());
		}

		//When the name is 'FromJson' and argument is identical, it's used automatically.
		static object FromJson( System.Type type, FastJson.JsonParser parser )
		{
			CipherInt i = new CipherInt();
			i.value = (int)(parser.ReadUInt32() ^ 0x12345678U);
			return i;
		}
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
