using UnityEngine;

namespace ExampleNS
{
	public class Example : MonoBehaviour
	{
		class ExampleClass
		{
			public int		id;
			public string	name;
		}

		void Start ()
		{
			ExampleClass foo = new ExampleClass();
			foo.id = 1234;
			foo.name = "SampleClass";

			string jsonText = FastJson.Serialize(foo);
			Debug.Log(jsonText);

			ExampleClass fooDecode = FastJson.Deserialize<ExampleClass>(jsonText);
			Debug.Log("id=" + fooDecode.id + " name=" + fooDecode.name);


			int[][][] bar = new int[][][]{
				new int[][]
				{
					new int[]{1},
					new int[]{2,3}
				},
				new int[][]{
					new int[]{4,5,6}
				},
				null
			};

			jsonText = FastJson.Serialize(bar);
			Debug.Log(jsonText);

			int[][][] barDecode = (int[][][])FastJson.Deserialize(jsonText, typeof(int[][][]) );
			Debug.Log( ToString(barDecode) );

		}

		string ToString( object a )
		{
			if( a == null )
			{
				return "null";
			}
			
			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			if( a.GetType().IsArray )
			{
				System.Array array = (System.Array)a;

				sb.Append('{');

				for( int i = 0; i < array.Length; ++i )
				{
					if( i != 0 ) sb.Append(',');
					sb.Append( ToString( array.GetValue(i) ) );
				}

				sb.Append('}');
			}
			else
			{
				sb.Append(a.ToString());
			}

			return sb.ToString();
		}
	}
}
