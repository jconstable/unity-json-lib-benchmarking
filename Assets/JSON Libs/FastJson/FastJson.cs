#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5
#define UNITY_3
#endif

#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6
#define UNITY_4
#endif

#if !UNITY_3 && !UNITY_4
#define ENABLED_HASH128
#endif

using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

static public class FastJson
{
	//--------------------------------------------------
	//option

	/// <summary>
	/// enum serialization method.
	/// Name:              Enum.A => "A"
	/// UnderlyingValue :  Enum.A => 0
	/// 
	/// Deserialize is available to both of Name and UnderlyingValue.
	/// </summary>
	public static EnumSerializationMethod		enumSerializationMethod = EnumSerializationMethod.Name;

	//option end
	//--------------------------------------------------

	public enum EnumSerializationMethod
	{
		Name,					//string	"AAA","BBB","CCC"
		UnderlyingValue,		//int		0,1,2
	}

	#region Custom serialize/deserialize

	public delegate void Serializer( StringBuilder sb, object obj );
	public delegate object Deserializer( System.Type type, JsonParser parser );

	public static void RegisterSerializer( System.Type type, Serializer serializer )
	{
		if( type != null &&serializer != null )
		{
			serializerCache[type] = serializer;
		}
	}

	public static void RegisterDeserializer( System.Type type, Deserializer deserializer )
	{
		if( type != null && deserializer != null )
		{
			deserializerCache[type] = deserializer;
		}
	}

	#endregion

	static System.Type[]	serializerrArgs = new System.Type[]{
		typeof(StringBuilder), typeof(object)
	};
	
	static System.Type[]	deserializerArgs = new System.Type[]{
		typeof(System.Type), typeof(FastJson.JsonParser)
	};

	public static string Serialize( object obj )
	{
		StringBuilder sb = new StringBuilder();
		_Serialize(sb,obj);
		return sb.ToString();
	}
	
	static void _Serialize( StringBuilder sb, object obj )
	{
		if( _SerializeIfNull(sb,obj) )
		{
			return;
		}
		
		Serializer serializer = _GetSerializer(obj.GetType());
		
		if( serializer != null )
		{
			serializer(sb,obj);
		}
	}

	static Serializer _GetSerializer( System.Type type )
	{		
		Serializer serializer;
		
		if( serializerCache.TryGetValue(type,out serializer) )
		{
			return serializer;
		}

		MethodInfo method = type.GetMethod("ToJson", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase, null, serializerrArgs, null );
		
		if( method != null )
		{
			serializer = (Serializer)System.Delegate.CreateDelegate( typeof(Serializer), method );
		}
		else if( !type.IsAbstract )
		{
			if( type.IsGenericType )
			{
				System.Type genericType = type.GetGenericTypeDefinition();
				System.Type[] args = genericType.GetGenericArguments();

				if( genericType == typeof(List<>) )
				{
					if( _GetSerializer(args[0]) != null )
					{
						serializer = _SerializeList;
					}
				}
				else if( genericType == typeof(HashSet<>) )
				{
					if( _GetSerializer(args[0]) != null )
					{
						serializer = _SerializeHashSet;
					}
				}
				else if( genericType == typeof(Dictionary<,>) || genericType == typeof(SortedDictionary<,>) )
				{
					if( _GetSerializer(args[0]) != null && _GetSerializer(args[1]) != null )
					{
						serializer = _SerializeDictionary;
					}
				}
				else
				{
					serializer = _SerializeObject;
				}
			}
			else if( type.IsArray )
			{
				if( _GetSerializer(type.GetElementType()) != null )
				{
					serializer = _SerializeArray;
				}
			}
			else if( type.IsEnum )
			{
				switch( enumSerializationMethod )
				{
				case EnumSerializationMethod.Name:
					serializer = _SerializeEnumName;
					break;

				case EnumSerializationMethod.UnderlyingValue:
					serializer = _GetSerializer(System.Enum.GetUnderlyingType(type));
					break;
				}

			}
			else if( type.IsClass || type.IsValueType )
			{
				serializer = _SerializeObject;
			}
		}

		serializerCache.Add(type,serializer);
		return serializer;
	}

	static void _SerializeArray( StringBuilder sb, object obj )
	{
		if( _SerializeIfNull(sb,obj) )
		{
			return;
		}

		System.Type type = obj.GetType();
		System.Array array = (System.Array) obj;
		Serializer serializer = _GetSerializer(type.GetElementType());

		if( array.Rank == 1 )
		{
			sb.Append('[');

			for( int i = 0, count = array.Length; i < count; ++i )
			{
				if( i > 0 ) sb.Append(',');
				serializer(sb,array.GetValue(i));
			}

			sb.Append(']');
			return;
		}
		else if( array.Rank == 2 )
		{
			int w = array.GetLength(0);
			int h = array.GetLength(1);

			sb.Append("[[");

			for( int x = 0; x < w; ++x )
			{
				if( x > 0 )
				{
					sb.Append("],[");
				}

				for( int y = 0; y < h; ++y )
				{
					if( y > 0 ) sb.Append(',');
					serializer(sb,array.GetValue(x,y));
				}
			}

			sb.Append("]]");
			return;
		}
		else
		{
			int rank = array.Rank;
			int[] indices = new int[rank];
			int[] lengths = new int[rank];

			for( int i = 0; i < rank; ++i )
			{
				lengths[i] = array.GetLength(i);

				if( lengths[i] == 0 )
				{
					sb.Append("null");
					return;
				}
			}

			for( int i = 0; i < rank; ++i )
			{
				sb.Append('[');
			}

			while(true)
			{
			next:
				
				serializer(sb,array.GetValue(indices));

				int i;

				for( i = rank-1; i >= 0; --i )
				{
					if( ++indices[i] < lengths[i] )
					{
						sb.Append(',');

						for( ; i < rank-1; ++i )
						{
							sb.Append('[');	
						}

						goto next;
					}

					indices[i] = 0;
					sb.Append(']');
				}

				break;
			}

			return;
		}
	}

	static void _SerializeList( StringBuilder sb, object obj )
	{
		if( _SerializeIfNull(sb,obj) )
		{
			return;
		}

		System.Type type = obj.GetType();
		Serializer elementSerializer = _GetSerializer(type.GetGenericArguments()[0]);

		IList list = (IList)obj;

		if( list.Count > 0 )
		{
			sb.Append('[');

			elementSerializer(sb,list[0]);

			for( int i = 1, count = list.Count; i < count; ++i )
			{
				sb.Append(',');
				elementSerializer(sb,list[i]);
			}
			
			sb.Append(']');
		}
		else
		{
			sb.Append("[]");
		}
	}

	static void _SerializeDictionary( StringBuilder sb, object obj )
	{
		if( _SerializeIfNull(sb,obj) )
		{
			return;
		}

		System.Type type = obj.GetType();
		System.Type[] genericArg = type.GetGenericArguments();

		Serializer keySerializer = _GetSerializer(genericArg[0]);
		Serializer valueSerializer = _GetSerializer(genericArg[1]);

		IDictionary dictionary = (IDictionary)obj;

		if( dictionary.Count > 0 )
		{
			sb.Append('{');

			IDictionaryEnumerator enumerator = dictionary.GetEnumerator();

			enumerator.MoveNext();
			keySerializer( sb, enumerator.Key );
			sb.Append(':');
			valueSerializer( sb, enumerator.Value );

			while( enumerator.MoveNext() )
			{
				sb.Append(',');
				keySerializer( sb, enumerator.Key );
				sb.Append(':');
				valueSerializer( sb, enumerator.Value );
			}

			sb.Append('}');
		}
		else
		{
			sb.Append("{}");
		}
	}

	static void _SerializeHashSet( StringBuilder sb, object obj )
	{
		if( _SerializeIfNull(sb,obj) )
		{
			return;
		}
		
		System.Type type = obj.GetType();
		Serializer elementSerializer = _GetSerializer(type.GetGenericArguments()[0]);

		IEnumerator enumerator = ((IEnumerable)obj).GetEnumerator();

		if( enumerator.MoveNext() )
		{
			sb.Append('[');

			elementSerializer(sb,enumerator.Current);

			while( enumerator.MoveNext() )
			{
				sb.Append(',');
				elementSerializer(sb,enumerator.Current);
			}

			sb.Append(']');
		}
		else
		{
			sb.Append("[]");
		}
	}

	class FieldSerializer
	{
		public string		nameAndColon;
		public FieldInfo	fieldInfo;
		public Serializer	serializer;

		public FieldSerializer( string nameAndColon, FieldInfo fieldInfo, Serializer serializer )
		{
			this.nameAndColon = nameAndColon;
			this.fieldInfo = fieldInfo;
			this.serializer = serializer;
		}
	}

	static Dictionary<System.Type,List<FieldSerializer>>		fieldSerializerCache;

	static void _SerializeObject( StringBuilder sb, object obj )
	{
		if( _SerializeIfNull(sb,obj) )
		{
			return;
		}

		System.Type type = obj.GetType();
		List<FieldSerializer> fieldSerializerList;

		sb.Append('{');

		if( !fieldSerializerCache.TryGetValue(type, out fieldSerializerList) )
		{
			FieldInfo[] fields = obj.GetType().GetFields(BINDING_FLAGS);
			fieldSerializerList = new List<FieldSerializer>();
			
			for( int fieldIndex = 0; fieldIndex < fields.Length; ++fieldIndex )
			{
				FieldInfo info = fields[fieldIndex];
				
				if( info.IsNotSerialized )
				{
					continue;
				}
				
				Serializer serializer = _GetSerializer( info.FieldType );
				
				if( serializer == null )
				{
					continue;
				}
				
				string nameAndColon;
				
				if( fieldSerializerList.Count > 0 )
				{
					nameAndColon = ",\"" + info.Name + "\":";
				}
				else
				{
					nameAndColon = '"' + info.Name + "\":";
				}

				fieldSerializerList.Add( new FieldSerializer(nameAndColon,info,serializer) );
			}
			
			fieldSerializerCache.Add( type, fieldSerializerList );
		}

		foreach( FieldSerializer fieldSerializer in fieldSerializerList )
		{
			sb.Append(fieldSerializer.nameAndColon);
			fieldSerializer.serializer( sb, fieldSerializer.fieldInfo.GetValue(obj) );
		}

		sb.Append('}');
	}

	static bool _SerializeIfNull( StringBuilder sb, object obj )
	{
		if( obj == null )
		{
			sb.Append("null");
			return true;
		}

		return false;
	}

	static Dictionary<System.Type,Serializer>	serializerCache;
	static Dictionary<System.Type,Deserializer>	deserializerCache;

	static byte[] encodeCharaTable;
	static byte[] decodeCharaTable;
	static byte[] hexDecodeTable;
	static bool[] jsonSeparator;
	static char[] hexBuff;

	public const string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss";
	public const BindingFlags BINDING_FLAGS = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

	static void _Register( System.Type type, Serializer serializer, Deserializer deserializer )
	{
		serializerCache.Add( type, serializer );
		deserializerCache.Add( type, deserializer );
	}

	static FastJson()
	{
		fieldSerializerCache = new Dictionary<System.Type, List<FieldSerializer>>();

		serializerCache = new Dictionary<System.Type, Serializer>();
		deserializerCache = new Dictionary<System.Type, Deserializer>();

		_Register( typeof(System.Int16), 	_SerializeInt16,	_DeserializeInt16 );
		_Register( typeof(System.Int32), 	_SerializeInt32,	_DeserializeInt32 );
		_Register( typeof(System.Int64), 	_SerializeInt64,	_DeserializeInt64 );
		_Register( typeof(System.UInt16), 	_SerializeUInt16,	_DeserializeUInt16 );
		_Register( typeof(System.UInt32), 	_SerializeUInt32,	_DeserializeUInt32 );
		_Register( typeof(System.UInt64), 	_SerializeUInt64,	_DeserializeUInt64 );
		_Register( typeof(System.Byte),		_SerializeByte,		_DeserializeByte );
		_Register( typeof(System.SByte),	_SerializeSByte,	_DeserializeSByte );
		_Register( typeof(System.Single),	_ToString,			_DeserializeFloat );
		_Register( typeof(System.Double),	_ToString,			_DeserializeDouble );
		_Register( typeof(System.Boolean), 	_SerializeBool,		_DeserializeBoolean );
		_Register( typeof(System.Decimal), 	_ToString,			_DeserializeDecimal );
		_Register( typeof(System.Char), 	_SerializeChar,		_DeserializeChar );
		_Register( typeof(string), 			_SerializeString,	_DeserializeString );

		_Register( typeof(UnityEngine.Vector2),			_SerializeVector2,		_DeserializeVector2 );
		_Register( typeof(UnityEngine.Vector3),			_SerializeVector3,		_DeserializeVector3 );
		_Register( typeof(UnityEngine.Vector4),			_SerializeVector4,		_DeserializeVector4 );
		_Register( typeof(UnityEngine.Quaternion),		_SerializeQuaternion,	_DeserializeQuaternion );
		_Register( typeof(UnityEngine.Rect),			_SerializeRect,			_DeserializeRect );
		_Register( typeof(UnityEngine.Color),			_SerializeColor,		_DeserializeColor );
		_Register( typeof(UnityEngine.Color32),			_SerializeColor32,		_DeserializeColor32 );
		
		#if ENABLED_HASH128
		_Register( typeof(UnityEngine.Hash128),			_SerializeHash128,		_DeserializeHash128 );

		hash128Fields = new FieldInfo[4];
		BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.SetField | BindingFlags.GetField;
		hash128Fields[0] = typeof(UnityEngine.Hash128).GetField("m_u32_0", flags);
		hash128Fields[1] = typeof(UnityEngine.Hash128).GetField("m_u32_1", flags);
		hash128Fields[2] = typeof(UnityEngine.Hash128).GetField("m_u32_2", flags);
		hash128Fields[3] = typeof(UnityEngine.Hash128).GetField("m_u32_3", flags);

		#endif

		_Register( typeof(System.DateTime),	_SerializeDate,		_DeserializeDate );

		encodeCharaTable = new byte[0x5D];
		encodeCharaTable[0x22] = (byte)'"';
		encodeCharaTable[0x5C] = (byte)'\\';
		encodeCharaTable[0x2F] = (byte)'/';
		encodeCharaTable[0x08] = (byte)'b';
		encodeCharaTable[0x0C] = (byte)'f';
		encodeCharaTable[0x0A] = (byte)'n';
		encodeCharaTable[0x0D] = (byte)'r';
		encodeCharaTable[0x09] = (byte)'t';

		decodeCharaTable = new byte[0x75];
		decodeCharaTable['"'] = 0x22;
		decodeCharaTable['\\'] = 0x5C;
		decodeCharaTable['/'] = 0x2F;
		decodeCharaTable['b'] = 0x08;
		decodeCharaTable['f'] = 0x0C;
		decodeCharaTable['n'] = 0x0A;
		decodeCharaTable['r'] = 0x0D;
		decodeCharaTable['t'] = 0x09;

		hexDecodeTable = new byte[0x67];
		hexDecodeTable['0'] = 0;
		hexDecodeTable['1'] = 1;
		hexDecodeTable['2'] = 2;
		hexDecodeTable['3'] = 3;
		hexDecodeTable['4'] = 4;
		hexDecodeTable['5'] = 5;
		hexDecodeTable['6'] = 6;
		hexDecodeTable['7'] = 7;
		hexDecodeTable['8'] = 8;
		hexDecodeTable['9'] = 9;
		hexDecodeTable['a'] = hexDecodeTable['A'] = 10;
		hexDecodeTable['b'] = hexDecodeTable['B'] = 11;
		hexDecodeTable['c'] = hexDecodeTable['C'] = 12;
		hexDecodeTable['d'] = hexDecodeTable['D'] = 13;
		hexDecodeTable['e'] = hexDecodeTable['E'] = 14;
		hexDecodeTable['f'] = hexDecodeTable['F'] = 15;

		jsonSeparator = new bool[126];

		foreach( char ch in "]}:," )
		{
			jsonSeparator[ch] = true;
		}

		hexBuff = new char[6];
		hexBuff[0] = '\\';
		hexBuff[1] = 'u';
	}

	static public int HexToInt( char ch )
	{
		int i = (int)ch;
		return i < hexDecodeTable.Length ? (int)hexDecodeTable[i] : 0;
	}

	static public char IntToHex( int i )
	{
		return i <= 9 ? (char)(i + '0') : (char)(i - 10 + 'A');
	}

	static public char IntToHex( uint i )
	{
		return i <= 9 ? (char)(i + '0') : (char)(i - 10 + 'A');
	}

	static public bool IsDigit( char ch )
	{
		uint i = (uint)ch;
		return i - '0' <= 9;
	}

	static public bool IsSpace( char ch )
	{
		return ch <= ' ';
	}

	static public bool IsJsonSeparator( char ch )
	{
		int i = (int)ch;
		return i < jsonSeparator.Length && jsonSeparator[i];
	}

	static public char ToUpper( char ch )
	{
		return (char)(ch | 0x20);
	}

	static void _ToString( StringBuilder sb, object obj )
	{
		sb.Append(obj.ToString());
	}

	static char[] numberBuff = new char[21];	//21 = ceil(log2^64 / log 10) + 1

	static void _SerializeSByte( StringBuilder sb, object obj )
	{
		sbyte value = (sbyte)obj;
		
		if( value != 0 )
		{
			int i = numberBuff.Length;
			
			if( value > 0 )
			{
				while( value != 0 )
				{
					int n = value % 10;
					value /= 10;
					numberBuff[--i] = (char)(n + '0');
				}
			}
			else
			{
				while( value != 0 )
				{
					int n = -(value % 10);
					value /= 10;
					numberBuff[--i] = (char)(n + '0');
				}
				
				numberBuff[--i] = '-';
			}
			
			sb.Append(numberBuff,i,numberBuff.Length-i);
		}
		else
		{
			sb.Append('0');
		}
	}

	static void _SerializeInt16( StringBuilder sb, object obj )
	{
		short value = (short)obj;
		
		if( value != 0 )
		{
			int i = numberBuff.Length;
			
			if( value > 0 )
			{
				while( value != 0 )
				{
					int n = value % 10;
					value /= 10;
					numberBuff[--i] = (char)(n + '0');
				}
			}
			else
			{
				while( value != 0 )
				{
					int n = -(value % 10);
					value /= 10;
					numberBuff[--i] = (char)(n + '0');
				}
				
				numberBuff[--i] = '-';
			}
			
			sb.Append(numberBuff,i,numberBuff.Length-i);
		}
		else
		{
			sb.Append('0');
		}
	}

	static void _SerializeInt32( StringBuilder sb, object obj )
	{
		int value = (int)obj;
		
		if( value != 0 )
		{
			int i = numberBuff.Length;
			
			if( value > 0 )
			{
				while( value != 0 )
				{
					int n = value % 10;
					value /= 10;
					numberBuff[--i] = (char)(n + '0');
				}
			}
			else
			{
				while( value != 0 )
				{
					int n = -(value % 10);
					value /= 10;
					numberBuff[--i] = (char)(n + '0');
				}
				
				numberBuff[--i] = '-';
			}
			
			sb.Append(numberBuff,i,numberBuff.Length-i);
		}
		else
		{
			sb.Append('0');
		}
	}

	static void _SerializeInt64( StringBuilder sb, object obj )
	{
		long value = (long)obj;
		
		if( value != 0 )
		{
			int i = numberBuff.Length;
			
			if( value > 0 )
			{
				while( value != 0 )
				{
					int n = (int)(value % 10);
					value /= 10;
					numberBuff[--i] = (char)(n + '0');
				}
			}
			else
			{
				while( value != 0 )
				{
					int n = -(int)(value % 10);
					value /= 10;
					numberBuff[--i] = (char)(n + '0');
				}
				
				numberBuff[--i] = '-';
			}
			
			sb.Append(numberBuff,i,numberBuff.Length-i);
		}
		else
		{
			sb.Append('0');
		}
	}
	
	static void _SerializeByte( StringBuilder sb, object obj )
	{
		byte value = (byte)obj;
		
		if( value != 0 )
		{
			int i = numberBuff.Length;
			
			while( value != 0 )
			{
				int n = value % 10;
				value /= 10;
				numberBuff[--i] = (char)(n + '0');
			}
			
			sb.Append(numberBuff,i,numberBuff.Length-i);
		}
		else
		{
			sb.Append('0');
		}
	}

	static void _SerializeUInt16( StringBuilder sb, object obj )
	{
		ushort value = (ushort)obj;
		
		if( value != 0 )
		{
			int i = numberBuff.Length;
			
			while( value != 0 )
			{
				int n = value % 10;
				value /= 10;
				numberBuff[--i] = (char)(n + '0');
			}
			
			sb.Append(numberBuff,i,numberBuff.Length-i);
		}
		else
		{
			sb.Append('0');
		}
	}

	static void _SerializeUInt32( StringBuilder sb, object obj )
	{
		uint value = (uint)obj;
		
		if( value != 0 )
		{
			int i = numberBuff.Length;
			
			while( value != 0 )
			{
				int n = (int)(value % 10);
				value /= 10;
				numberBuff[--i] = (char)(n + '0');
			}
			
			sb.Append(numberBuff,i,numberBuff.Length-i);
		}
		else
		{
			sb.Append('0');
		}
	}

	static void _SerializeUInt64( StringBuilder sb, object obj )
	{
		ulong value = (ulong)obj;
		
		if( value != 0 )
		{
			int i = numberBuff.Length;
			
			while( value != 0 )
			{
				int n = (int)(value % 10);
				value /= 10;
				numberBuff[--i] = (char)(n + '0');
			}
			
			sb.Append(numberBuff,i,numberBuff.Length-i);
		}
		else
		{
			sb.Append('0');
		}
	}

	static void _SerializeBool( StringBuilder sb, object obj )
	{
		if( (bool)obj )
		{
			sb.Append("true");
		}
		else
		{
			sb.Append("false");
		}
	}

	static void _SerializeChar( StringBuilder sb, object obj )
	{
		int i = (int)(char)obj;
		sb.Append(i.ToString());
	}

	static void _SerializeString( StringBuilder sb, object obj )
	{
		if( obj != null )
		{
			sb.Append('"');
			
			string tmp = (string)obj;
			
			for( int i = 0; i < tmp.Length; ++i )
			{
				char ch = tmp[i];
				
				if( ch >= encodeCharaTable.Length || encodeCharaTable[ch] == 0 )
				{
					if( ch < 0x7F )
					{
						sb.Append(ch);
					}
					else
					{
						int j = (int)ch;
						hexBuff[2] = IntToHex((j >> 12) & 0x0F);
						hexBuff[3] = IntToHex((j >>  8) & 0x0F);
						hexBuff[4] = IntToHex((j >>  4) & 0x0F);
						hexBuff[5] = IntToHex(j & 0x0F);
						sb.Append(hexBuff);
					}
				}
				else
				{
					sb.Append('\\');
					sb.Append((char)encodeCharaTable[ch]);
				}
			}
			
			sb.Append('"'); 
		}
		else
		{
			sb.Append("null");
		}
	}

	static void _SerializeEnumName( StringBuilder sb, object obj )
	{
		sb.Append('"');
		sb.Append(obj.ToString());
		sb.Append('"');
	}

	static void _SerializeVector2( StringBuilder sb, object vec )
	{
		UnityEngine.Vector2 tmp = (UnityEngine.Vector2)vec;
		sb.Append('[');
		sb.Append(tmp.x);
		sb.Append(',');
		sb.Append(tmp.y);
		sb.Append(']');
	}

	static void _SerializeVector3( StringBuilder sb, object vec )
	{
		UnityEngine.Vector3 tmp = (UnityEngine.Vector3)vec;
		sb.Append('[');
		sb.Append(tmp.x);
		sb.Append(',');
		sb.Append(tmp.y);
		sb.Append(',');
		sb.Append(tmp.z);
		sb.Append(']');
	}

	static void _SerializeVector4( StringBuilder sb, object vec )
	{
		UnityEngine.Vector4 tmp = (UnityEngine.Vector4)vec;
		sb.Append('[');
		sb.Append(tmp.x);
		sb.Append(',');
		sb.Append(tmp.y);
		sb.Append(',');
		sb.Append(tmp.z);
		sb.Append(',');
		sb.Append(tmp.w);
		sb.Append(']');
	}

	static void _SerializeQuaternion( StringBuilder sb, object q )
	{
		UnityEngine.Quaternion tmp = (UnityEngine.Quaternion)q;
		sb.Append('[');
		sb.Append(tmp.x);
		sb.Append(',');
		sb.Append(tmp.y);
		sb.Append(',');
		sb.Append(tmp.z);
		sb.Append(',');
		sb.Append(tmp.w);
		sb.Append(']');
	}

	static void _SerializeRect( StringBuilder sb, object rect )
	{
		UnityEngine.Rect tmp = (UnityEngine.Rect)rect;
		sb.Append('[');
		sb.Append(tmp.x);
		sb.Append(',');
		sb.Append(tmp.y);
		sb.Append(',');
		sb.Append(tmp.width);
		sb.Append(',');
		sb.Append(tmp.height);
		sb.Append(']');
	}

	static void _SerializeColor( StringBuilder sb, object color )
	{
		UnityEngine.Color tmp = (UnityEngine.Color)color;
		sb.Append('[');
		sb.Append(tmp.r);
		sb.Append(',');
		sb.Append(tmp.g);
		sb.Append(',');
		sb.Append(tmp.b);
		sb.Append(',');
		sb.Append(tmp.a);
		sb.Append(']');
	}

	static void _SerializeColor32( StringBuilder sb, object color )
	{
		UnityEngine.Color32 tmp = (UnityEngine.Color32)color;
		char[] code = new char[11];
		code[0] = '"';
		code[1] = '#';
		code[2] = IntToHex(tmp.a>>4);
		code[3] = IntToHex(tmp.a&0xF);
		code[4] = IntToHex(tmp.r>>4);
		code[5] = IntToHex(tmp.r&0xF);
		code[6] = IntToHex(tmp.g>>4);
		code[7] = IntToHex(tmp.g&0xF);
		code[8] = IntToHex(tmp.b>>4);
		code[9] = IntToHex(tmp.b&0xF);
		code[10] = '"';
		sb.Append(code);
	}

#if ENABLED_HASH128

	static FieldInfo[]	hash128Fields;
	static char[] hash128Buff = new char[32];

	public static string Hash128ToString( UnityEngine.Hash128 hash )
	{
		for (int i = 0, count = hash128Fields.Length; i < count; ++i) 
		{
			uint x = (uint)hash128Fields[i].GetValue( hash );
			int baseIndex = i << 3;

			for( int j = 0; j < sizeof(uint) * 2; ++j )
			{
				hash128Buff[baseIndex+j] = IntToHex( x >> 28 );
				x <<= 4;
			}
		}

		return new string( hash128Buff );
	}

	static void _SerializeHash128( StringBuilder sb, object hash )
	{
		// failed ToString() by unity5

		sb.Append('"');

		for (int i = 0, count = hash128Fields.Length; i < count; ++i) 
		{
			uint x = (uint)hash128Fields [i].GetValue (hash);

			for( int j = 0; j < sizeof(uint) * 2; ++j )
			{
				sb.Append( IntToHex( x >> 28 ) );
				x <<= 4;
			}
		}

		sb.Append('"');
	}

#endif

	static void _SerializeDate( StringBuilder sb, object date )
	{
		System.DateTime tmp = (System.DateTime)date;
		sb.Append('"');
		sb.Append(tmp.ToString(DATE_FORMAT));
		sb.Append('"');
	}

	public static T Deserialize<T>( string jsonText )
	{
		JsonParser parser = new JsonParser(jsonText);
		return (T)_Deserialize(typeof(T),parser);
	}

	public static object Deserialize( string jsonText, System.Type type )
	{
		JsonParser parser = new JsonParser(jsonText);
		return _Deserialize(type,parser);
	}

	static object _Deserialize( System.Type type, JsonParser parser )
	{
		Deserializer deserializer = _GetDeserializer(type);
		
		if( deserializer != null )
		{
			return deserializer(type, parser);
		}
		
		return _GetDefault(type);
	}

	static object _GetDefault( System.Type type )
	{
		if( type.IsClass )
		{
			return null;
		}

		return System.Activator.CreateInstance(type);
	}

	public class JsonParser
	{
		int		count;
		readonly int	textLength;
		readonly string	jsonText;

		StringBuilder	subStringBuilder;

		public JsonParser( string jsonText )
		{
			this.jsonText = jsonText;
			count = 0;
			textLength = jsonText.Length;

			subStringBuilder = new StringBuilder();
		}

		public char Peek()
		{
			return jsonText[count];
		}

		public char Read()
		{
			return jsonText[count++];
		}

		public char PeekWithTrim()
		{
			char ch;

			while( (ch = Peek()) <= ' ' )
				++count;

			return ch;
		}

		public char ReadWithTrim()
		{
			char ch;
			while( (ch = Read()) <= ' ' );
			return ch;
		}

		public void Trim()
		{
			while( count < textLength && Peek() <= ' ' ) 
				++count;
		}

		void _Skip()
		{
			while( count < textLength )
			{
				char ch = Peek();
				
				if( IsJsonSeparator(ch) )
				{
					break;
				}
				
				++count;
			}
		}

		public string ReadToken()
		{
			Trim();

			int begin = count;
			_Skip();
			int end = count;
			return jsonText.Substring(begin,end-begin);
		}

		public bool ReadIfNull()
		{
			Trim();
			return _ReadIfNull();
		}

		bool _ReadIfNull()
		{
			if( count + 4 > textLength )
			{
				return false;
			}

			if( jsonText[count+0] != 'n' ||
			  	jsonText[count+1] != 'u' ||
			    jsonText[count+2] != 'l' ||
			   	jsonText[count+3] != 'l' )
			{
				return false;
			}

			count += 4;
			return true;
		}

		public void ReadSpecifyChar( char spec )
		{
			char ch;

			do
			{
				ch = Read();

				if( ch == spec )
				{
					return;
				}
			}
			while( ch <= ' ' && count < textLength );

			int position = UnityEngine.Mathf.Max(0,count - 5);
			int length = UnityEngine.Mathf.Min(10,textLength-position);
			string text = jsonText.Substring( position , length );
			throw new System.FormatException("failed read char '"+ spec +"'. index = " + count + " text = \""+text+"\"" );
		}

		public string ReadString()
		{
			char ch = PeekWithTrim();	// '"' or 'n'ull

			if( ch != '"' && ch != 'n' )
			{
				goto failed;
			}

			if( ch == 'n' )
			{
				if( _ReadIfNull() )
				{
					return null;
				}
				
				goto failed;
			}

			Read();

			subStringBuilder.Length = 0;

			while( true )
			{
				ch = Read();

				if( ch == '"' )
				{
					break;
				}

				if( ch != '\\' )
				{
					subStringBuilder.Append(ch);
					continue;
				}

				if( count >= textLength )
				{
					goto failed;
				}

				ch = Read();

				if( ch == 'u' )
				{
					if( count + 4 > textLength )
					{
						break;
					}

					int code = 
						(HexToInt(jsonText[count+0]) << 12) +
						(HexToInt(jsonText[count+1]) << 8) + 
						(HexToInt(jsonText[count+2]) << 4) + 
						(HexToInt(jsonText[count+3]) << 0);

					subStringBuilder.Append((char)code);
					count += 4;
				}
				else if( ch < decodeCharaTable.Length )
				{
					subStringBuilder.Append((char)decodeCharaTable[ch]);
				}
			}

			return subStringBuilder.ToString();

		failed:
			
			throw CreateFormatException();
		}

		public void SkipValue()
		{
			char ch = PeekWithTrim();

			if( ch == '"' )
			{
				ReadString();
			}
			else if( ch == '[' )
			{
				Read();

				if( PeekWithTrim() != ']' )
				{
					while(true)
					{
						SkipValue();
						ch = ReadWithTrim();

						if( ch == ',' )
						{
							continue;
						}
						else if( ch == ']' )
						{
							break;
						}

						goto failed;
					}
				}
				else
				{
					Read();
				}
			}
			else if( ch == '{' )
			{
				Read();
				
				if( PeekWithTrim() != '}' )
				{
					while(true)
					{
						SkipValue();
						ch = ReadWithTrim();

						if( ch == ':' )
						{
							SkipValue();
							ch = ReadWithTrim();
						}
						
						if( ch == ',' )
						{
							continue;
						}
						else if( ch == '}' )
						{
							break;
						}

						goto failed;
					}
				}
				else
				{
					Read();
				}
			}
			else if( ch == 'n' )
			{
				if( !ReadIfNull() )
				{
					goto failed;
				}
			}
			else if( ch == 't' || ch == 'f' )
			{
				bool dummy;

				if( !System.Boolean.TryParse(ReadToken(),out dummy) )
				{
					goto failed;
				}
			}
			else
			{
				//number
				decimal dummy1;
				double dummy2;
				string token = ReadToken();

				if( !System.Decimal.TryParse(token,out dummy1) && !System.Double.TryParse(token,out dummy2) )
				{
					goto failed;
				}
			}

			return;

		failed:
			
			throw CreateFormatException();
		}

		public bool End
		{
			get
			{
				return count >= textLength;
			}
		}

		public System.FormatException CreateFormatException()
		{
			return new System.FormatException(_GetExceptionText());
		}

		public System.OverflowException CreateOverflowException()
		{
			return new System.OverflowException(_GetExceptionText());
		}

		string _GetExceptionText()
		{
			int start = UnityEngine.Mathf.Max(0,count - 5);
			int length = UnityEngine.Mathf.Min(10,textLength-start);
			string text = jsonText.Substring( start, length );
			return "index = " + count + " text = \""+text+"\"";
		}

		public bool ReadBool()
		{
			char ch = ReadWithTrim();

			if( ch == 't' )
			{
				if( jsonText[count+0] == 'r' &&
				    jsonText[count+1] == 'u' &&
				    jsonText[count+2] == 'e'
				   ) 
				{
					count += 3;
					return true;
				}
			}
			else if( ch == 'f' )
			{
				if( jsonText[count+0] == 'a' &&
				    jsonText[count+1] == 'l' &&
				   	jsonText[count+2] == 's' &&
				   	jsonText[count+3] == 'e'
				   ) 
				{
					count += 4;
					return false;
				}
			}

			throw CreateFormatException();
		}

		bool _ReadNegativeSign()
		{
			char ch = PeekWithTrim();
			bool negative = false;

			if( ch == '-' )
			{
				++count;
				negative = true;
			}
			else if( ch == '+' )
			{
				++count;
				negative = false;
			}

			return negative;
		}

		public sbyte ReadInt8()
		{
			const int digits = 3;
			
			bool negative = _ReadNegativeSign();
			
			char ch = Peek();
			
			if( IsDigit(ch) )
			{
				int n = 0;
				
				if( ch == '0' )
				{
					++count;
					return 0;
				}
				
				for( int i = 0, j = UnityEngine.Mathf.Min(digits-1,textLength-count); i < j; ++i )
				{
					ch = Peek();
					
					if( IsDigit(ch) )
					{
						++count;
						n = n * 10 + (ch - '0');
						continue;
					}
					
					return negative ? (sbyte)-n : (sbyte)n;
				}
				
				if( End || !IsDigit(ch = Peek()) )
				{
					return negative ? (sbyte)-n : (sbyte)n;
				}
				
				++count;
				
				if( End || !IsDigit(Peek()) )
				{
					int s = ch - '0';
					
					if( n < sbyte.MaxValue/10 )
					{
						n = n*10 + s;
						return negative ? (sbyte)-n : (sbyte)n;
					}
					else if( n == sbyte.MaxValue/10 )
					{
						if( !negative )
						{
							if( s > sbyte.MaxValue%10 )
							{
								goto overflow;
							}
							
							return (sbyte)(n*10 + s);
						}
						else
						{
							if( s > -(sbyte.MinValue%10) )
							{
								goto overflow;
							}
							
							return (sbyte)(n*-10 - s);
						}
					}
				}
				
			overflow:
					
					throw CreateOverflowException();
			}
			
			throw CreateFormatException();
		}

		public short ReadInt16()
		{
			const int digits = 5;

			bool negative = _ReadNegativeSign();
			
			char ch = Peek();
			
			if( IsDigit(ch) )
			{
				int n = 0;
				
				if( ch == '0' )
				{
					++count;
					return 0;
				}
				
				for( int i = 0, j = UnityEngine.Mathf.Min(digits-1,textLength-count); i < j; ++i )
				{
					ch = Peek();
					
					if( IsDigit(ch) )
					{
						++count;
						n = n * 10 + (ch - '0');
						continue;
					}
					
					return negative ? (short)-n : (short)n;
				}
				
				if( End || !IsDigit(ch = Peek()) )
				{
					return negative ? (short)-n : (short)n;
				}
				
				++count;
				
				if( End || !IsDigit(Peek()) )
				{
					int s = ch - '0';
					
					if( n < short.MaxValue/10 )
					{
						n = n*10 + s;
						return negative ? (short)-n : (short)n;
					}
					else if( n == short.MaxValue/10 )
					{
						if( !negative )
						{
							if( s > short.MaxValue%10 )
							{
								goto overflow;
							}
							
							return (short)(n*10 + s);
						}
						else
						{
							if( s > -(short.MinValue%10) )
							{
								goto overflow;
							}
							
							return (short)(n*-10 - s);
						}
					}
				}

			overflow:
					
				throw CreateOverflowException();
			}
			
			throw CreateFormatException();
		}

		public int ReadInt32()
		{
			const int digits = 10;

			bool negative = _ReadNegativeSign();

			char ch = Peek();

			if( IsDigit(ch) )
			{
				int n = 0;

				if( ch == '0' )
				{
					++count;
					return 0;
				}
				
				for( int i = 0, j = UnityEngine.Mathf.Min(digits-1,textLength-count); i < j; ++i )
				{
					ch = Peek();
					
					if( IsDigit(ch) )
					{
						++count;
						n = n * 10 + (ch - '0');
						continue;
					}
					
					return negative ? -n : n;
				}
				
				if( End || !IsDigit(ch = Peek()) )
				{
					return negative ? -n : n;
				}
				
				++count;
				
				if( End || !IsDigit(Peek()) )
				{
					int s = ch - '0';
					
					if( n < int.MaxValue/10 )
					{
						n = n*10 + s;
						return negative ? -n : n;
					}
					else if( n == int.MaxValue/10 )
					{
						if( !negative )
						{
							if( s > int.MaxValue%10 )
							{
								goto overflow;
							}
							
							return n*10 + s;
						}
						else
						{
							if( s > -(int.MinValue%10) )
							{
								goto overflow;
							}
							
							return n*-10 - s;
						}
					}
				}
				
			overflow:
					
				throw CreateOverflowException();
			}

			throw CreateFormatException();
		}
		
		public long ReadInt64()
		{
			const int digits = 19;
			
			bool negative = _ReadNegativeSign();
			
			char ch = Peek();
			
			if( IsDigit(ch) )
			{
				long n = 0;
				
				if( ch == '0' )
				{
					++count;
					return 0;
				}
				
				for( int i = 0, j = UnityEngine.Mathf.Min(digits-1,textLength-count); i < j; ++i )
				{
					ch = Peek();
					
					if( IsDigit(ch) )
					{
						++count;
						n = n * 10 + (ch - '0');
						continue;
					}
					
					return negative ? -n : n;
				}
				
				if( End || !IsDigit(ch = Peek()) )
				{
					return negative ? -n : n;
				}
				
				++count;

				if( End || !IsDigit(Peek()) )
				{
					int s = ch - '0';
					
					if( n < long.MaxValue/10 )
					{
						n = n*10 + s;
						return negative ? -n : n;
					}
					else if( n == long.MaxValue/10 )
					{
						if( !negative )
						{
							if( s > long.MaxValue%10 )
							{
								goto overflow;
							}
							
							return n*10 + s;
						}
						else
						{
							if( s > -(long.MinValue%10) )
							{
								goto overflow;
							}
							
							return n*-10 - s;
						}
					}
				}
				
			overflow:
					
				throw CreateOverflowException();
			}
			
			throw CreateFormatException();
		}

		public byte ReadUInt8()
		{
			const int digits = 3;
			char ch;
			
			if( !_ReadNegativeSign() && IsDigit(ch = Peek()) )
			{
				int n = 0;
				
				if( ch == '0') 
				{
					++count;
					return (byte)0;
				}
				
				for( int i = 0, j = UnityEngine.Mathf.Min(digits-1,textLength-count); i < j; ++i )
				{
					ch = Peek();
					
					if( IsDigit(ch) )
					{
						++count;
						n = n * 10 + (ch - '0');
						continue;
					}
					
					return (byte)n;
				}
				
				if( End || !IsDigit(ch = Peek()) )
				{
					return (byte)n;
				}
				
				++count;
				
				if( End || !IsDigit(Peek()) )
				{
					int s = ch - '0';
					
					if( n < byte.MaxValue/10 || n == byte.MaxValue/10 && s <= byte.MaxValue%10 )
					{
						return (byte)(n*10 + s);
					}
				}

				throw CreateOverflowException();
			}
			
			throw CreateFormatException();
		}
		
		public ushort ReadUInt16()
		{
			const int digits = 5;
			char ch;
			
			if( !_ReadNegativeSign() && IsDigit(ch = Peek()) )
			{
				int n = 0;
				
				if( ch == '0') 
				{
					++count;
					return (ushort)0;
				}
				
				for( int i = 0, j = UnityEngine.Mathf.Min(digits-1,textLength-count); i < j; ++i )
				{
					ch = Peek();
					
					if( IsDigit(ch) )
					{
						++count;
						n = n * 10 + (ch - '0');
						continue;
					}
					
					return (ushort)n;
				}
				
				if( End || !IsDigit(ch = Peek()) )
				{
					return (ushort)n;
				}
				
				++count;
				
				if( End || !IsDigit(Peek()) )
				{
					int s = ch - '0';
					
					if( n < ushort.MaxValue/10 || n == ushort.MaxValue/10 && s <= ushort.MaxValue%10 )
					{
						return (ushort)(n*10 + s);
					}
				}

				throw CreateOverflowException();
			}
			
			throw CreateFormatException();
		}

		public uint ReadUInt32()
		{
			const int digits = 10;
			char ch;
			
			if( !_ReadNegativeSign() && IsDigit(ch = Peek()) )
			{
				uint n = 0;
				
				if( ch == '0') 
				{
					++count;
					return 0U;
				}
				
				for( int i = 0, j = UnityEngine.Mathf.Min(digits-1,textLength-count); i < j; ++i )
				{
					ch = Peek();
					
					if( IsDigit(ch) )
					{
						++count;
						n = n * 10 + (uint)(ch - '0');
						continue;
					}
					
					return n;
				}
				
				if( End || !IsDigit(ch = Peek()) )
				{
					return n;
				}
				
				++count;
				
				if( End || !IsDigit(Peek()) )
				{
					uint s = (uint)(ch - '0');
					
					if( n < uint.MaxValue/10 || n == uint.MaxValue/10 && s <= uint.MaxValue%10 )
					{
						return n*10 + s;
					}
				}

				throw CreateOverflowException();
			}
			
			throw CreateFormatException();
		}

		public ulong ReadUInt64()
		{
			const int digits = 20;
			char ch;

			if( !_ReadNegativeSign() && IsDigit(ch = Peek()) )
			{
				ulong n = 0;

				if( ch == '0') 
				{
					++count;
					return 0UL;
				}
				
				for( int i = 0, j = UnityEngine.Mathf.Min(digits-1,textLength-count); i < j; ++i )
				{
					ch = Peek();
					
					if( IsDigit(ch) )
					{
						++count;
						n = n * 10 + (ulong)(ch - '0');
						continue;
					}
					
					return n;
				}
				
				if( End || !IsDigit(ch = Peek()) )
				{
					return n;
				}
				
				++count;
				
				if( End || !IsDigit(Peek()) )
				{
					uint s = (uint)(ch - '0');
					
					if( n < ulong.MaxValue/10 || n == ulong.MaxValue/10 && s <= ulong.MaxValue%10 )
					{
						return n*10 + s;
					}
				}
					
				throw CreateOverflowException();
			}
			
			throw CreateFormatException();
		}
	}
	
	static Deserializer _GetDeserializer( System.Type type )
	{		
		Deserializer deserializer;
		
		if( deserializerCache.TryGetValue(type,out deserializer) )
		{
			return deserializer;
		}

		MethodInfo method = type.GetMethod("FromJson", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase, null, deserializerArgs, null );

		if( method != null )
		{
			deserializer = (Deserializer)System.Delegate.CreateDelegate( typeof(Deserializer), method );
		}
		else if( !type.IsAbstract )
		{
			if( type.IsGenericType )
			{
				System.Type genericType = type.GetGenericTypeDefinition();
				System.Type[] args = genericType.GetGenericArguments();
				
				if( genericType == typeof(List<>) )
				{
					if( _GetDeserializer(args[0]) != null )
					{
						deserializer = _DeserializeList;
					}
				}
				else if( genericType == typeof(HashSet<>) )
				{
					if( _GetDeserializer(args[0]) != null )
					{
						deserializer = _DeserializeHashSet;
					}
				}
				else if( genericType == typeof(Dictionary<,>) || genericType == typeof(SortedDictionary<,>) )
				{
					if( _GetDeserializer(args[0]) != null && _GetDeserializer(args[1]) != null )
					{
						deserializer = _DeserializeDictionary;
					}
				}
				else
				{
					deserializer = _DeserializeObject;
				}
			}
			else if( type.IsArray )
			{
				if( _GetDeserializer(type.GetElementType()) != null )
				{
					deserializer = _DeserializeArray;
				}
			}
			else if( type.IsEnum )
			{
				deserializer = _DeserializeEnum;
			}
			else if( type.IsClass || type.IsValueType )
			{
				deserializer = _DeserializeObject;
			}
		}

		deserializerCache.Add(type,deserializer);
		return deserializer;
	}

	static object _DeserializeArray( System.Type type, JsonParser parser )
	{
		if( parser.ReadIfNull() )
		{
			return null;
		}

		System.Type elementType = type.GetElementType();
		Deserializer elementDeserializer = _GetDeserializer(elementType);

		if( elementDeserializer == null )
		{
			parser.SkipValue();
			return null;
		}

		int rank = type.GetArrayRank();

		if( rank == 1 )
		{
			parser.ReadSpecifyChar('[');

			if( parser.PeekWithTrim() == ']' )
			{
				parser.Read();
				return System.Array.CreateInstance(elementType,0);
			}
			
			List<object> list = new List<object>();
			
			while(true)
			{
				list.Add( elementDeserializer(elementType,parser) );
				
				char ch = parser.ReadWithTrim();
				
				if( ch == ',' )
				{
					continue;
				}
				else if( ch == ']' )
				{
					break;
				}
				
				goto failed;
			}
			
			System.Array array = System.Array.CreateInstance(elementType,list.Count);
			
			for( int i = 0; i < list.Count; ++i )
			{
				array.SetValue( list[i], i );
			}
			
			return array;
		}
		else if( rank >= 2 )
		{
			int[] length = new int[rank];
			List<object> list = _ParseArray( parser, elementType, elementDeserializer, 0, ref length );

			System.Array array = System.Array.CreateInstance(elementType,length);
			int[] indices = new int[rank];
			_SetArray( array, indices, list, 0 );

			return array;
		}
		
	failed:	
		throw parser.CreateFormatException();
	}

	static List<object> _ParseArray( JsonParser parser, System.Type elementType, Deserializer deserializer, int nest, ref int[] length  )
	{
		parser.ReadSpecifyChar('[');

		List<object> list = new List<object>();

		if( parser.PeekWithTrim() != ']' )
		{
			if( nest+1 == length.Length )
			{
				while(true)
				{
					list.Add( deserializer(elementType,parser) );
					char ch = parser.ReadWithTrim();
					
					if( ch == ',' )
					{
						continue;
					}
					else if( ch == ']' )
					{
						break;
					}
					
					goto failed;
				}
			}
			else
			{
				while(true)
				{
					list.Add( _ParseArray(parser,elementType,deserializer,nest+1,ref length) );
					char ch = parser.ReadWithTrim();
					
					if( ch == ',' )
					{
						continue;
					}
					else if( ch == ']' )
					{
						break;
					}
					
					goto failed;
				}
			}

			length[nest] = list.Count;
		}
		else
		{
			parser.Read();
		}

		return list;

	failed:
		throw parser.CreateFormatException();
	}

	static void _SetArray( System.Array array, int[] indices, List<object> listList, int nest )
	{
		if( nest == indices.Length - 1 )
		{
			for( int i = 0; i < listList.Count; ++i )
			{
				indices[indices.Length-1] = i;
				array.SetValue( listList[i], indices );
			}
		}
		else
		{
			for( int i = 0; i < listList.Count; ++i )
			{
				indices[nest] = i;
				_SetArray( array, indices, (List<object>)listList[i], nest+1 );
			}
		}
	}
	
	static object _DeserializeList( System.Type type, JsonParser parser )
	{
		if( parser.ReadIfNull() )
		{
			return null;
		}

		parser.ReadSpecifyChar('[');

		IList list = (IList)System.Activator.CreateInstance(type);

		if( parser.PeekWithTrim() != ']' )
		{
			System.Type elementType = type.GetGenericArguments()[0];
			Deserializer derializer = _GetDeserializer( elementType );

			while( true )
			{
				list.Add( derializer( elementType, parser ) );
				char ch = parser.ReadWithTrim();

				if( ch == ',' )
				{
					continue;
				}
				else if( ch == ']' )
				{
					break;
				}

				goto failed;
			}
		}
		else
		{
			parser.Read();
		}

		return list;
		
	failed:	
		throw parser.CreateFormatException();
	}

	static object _DeserializeHashSet( System.Type type, JsonParser parser )
	{
		if( parser.ReadIfNull() )
		{
			return null;
		}

		System.Type[] arguments = type.GetGenericArguments();
		System.Type enumeratorType = typeof(DeserializeEnumerator<>).MakeGenericType(arguments);
		object enumerator = System.Activator.CreateInstance( enumeratorType, parser, arguments[0] );
		return System.Activator.CreateInstance(type,enumerator);
	}

	#if UNITY_IOS

	#pragma warning disable 219

	static void AOTDummy()
	{
		HashSet<object> 	dummy1 = new HashSet<object> ( new DeserializeEnumerator<object>(null,null) );		dummy1.Add (null);
		HashSet<int>	 	dummy2 = new HashSet<int> ( new DeserializeEnumerator<int>(null,null) );			dummy2.Add (0);

		List<object>		dummy3 = new List<object> ();		dummy3.Add (null);
		List<int>			dummy4 = new List<int> ();			dummy4.Add (0);

		HashSet<object>		dummy5 = new HashSet<object> ();	dummy5.Add (null);
		HashSet<int>		dummy6 = new HashSet<int> ();		dummy6.Add (0);

		Dictionary<object,object>			dummy7 = new Dictionary<object,object> ();				dummy7.Add (null, null);
		Dictionary<object,int>				dummy8 = new Dictionary<object,int> ();					dummy8.Add (null, 0);
		Dictionary<int,int>					dummy9 = new Dictionary<int,int> ();					dummy9.Add (0, 0);
		Dictionary<int,object>				dummy10 = new Dictionary<int,object> ();				dummy10.Add (0, null);

		SortedDictionary<object,object>		dummy11 = new SortedDictionary<object,object> ();		dummy11.Add (null, null);
		SortedDictionary<object,int>		dummy12 = new SortedDictionary<object,int> ();			dummy12.Add (null, 0);
		SortedDictionary<int,int>			dummy13 = new SortedDictionary<int,int> ();				dummy13.Add (0, 0);
		SortedDictionary<int,object>		dummy14 = new SortedDictionary<int,object> ();			dummy14.Add (0, null);
	}

	#pragma warning restore 219

	#endif

	public class DeserializeEnumerator<T> : IEnumerator<T>, System.IDisposable, IEnumerable<T>
	{
		public IEnumerator<T> GetEnumerator(){ return this; }
		IEnumerator IEnumerable.GetEnumerator(){ return this; }

		public T Current
		{
			get{ return current; }
		}

		object IEnumerator.Current
		{
			get{ return current; }
		}

		T current;
		Deserializer deserializer;
		JsonParser parser;
		System.Type type;

		public DeserializeEnumerator( JsonParser parser, System.Type type )
		{
			parser.ReadSpecifyChar('[');

			if( parser.PeekWithTrim() != ']' )
			{
				this.parser = parser;
				this.current = default(T);
				this.deserializer = _GetDeserializer(type);
				this.type = type;
			}
			else
			{
				parser.Read();
				this.parser = null;
			}
		}

		public bool MoveNext()
		{
			if( parser != null )
			{
				current = (T)deserializer(type,parser);

				char ch = parser.ReadWithTrim();

				if( ch == ',' )
				{
					return true;
				}

				if( ch == ']' )
				{
					parser = null;
					return true;
				}
			}
			else
			{
				return false;
			}

			throw parser.CreateFormatException();
		}
		
		public void Dispose ()
		{
			current = default(T);
			deserializer = null;
			type = null;
			parser = null;
		}

		public void Reset(){}
	}
	
	static object _DeserializeDictionary( System.Type type, JsonParser parser )
	{
		if( parser.ReadIfNull() )
		{
			return null;
		}

		parser.ReadSpecifyChar('{');

		IDictionary dictionary = (IDictionary)System.Activator.CreateInstance(type);
		
		if( parser.PeekWithTrim() == '}' )
		{
			parser.Read();
		}
		else
		{
			System.Type[] genericArgumets = type.GetGenericArguments();
			System.Type keyType = genericArgumets[0];
			System.Type valueType = genericArgumets[1];
			Deserializer keyDeserializer = _GetDeserializer( keyType );
			Deserializer valueDeserializer = _GetDeserializer( valueType );

			while( true )
			{
				object key = keyDeserializer( keyType, parser );
				parser.ReadSpecifyChar( ':' );
				object value = valueDeserializer( valueType, parser );
				dictionary.Add( key, value );

				char ch = parser.ReadWithTrim();

				if( ch == ',' )
				{
					continue;
				}
				else if( ch == '}' )
				{
					break;
				}

				goto failed;
			}
		}

		return dictionary;
		
	failed:	
		throw parser.CreateFormatException();
	}
	
	static object _DeserializeEnum( System.Type type, JsonParser parser )
	{
		if( parser.PeekWithTrim() == '"' )
		{
			return System.Enum.Parse(type,parser.ReadString());
		}

		return _Deserialize(System.Enum.GetUnderlyingType(type), parser);
	}
	
	static object _DeserializeObject( System.Type type, JsonParser parser )
	{
		if( parser.ReadIfNull() )
		{
			return null;
		}

		parser.ReadSpecifyChar('{');

		object obj = System.Activator.CreateInstance(type);

		while( true )
		{
			char ch = parser.PeekWithTrim();

			if( ch == '"' )
			{
				string fieldName = parser.ReadString();
				parser.ReadSpecifyChar(':');

				FieldInfo fieldInfo = type.GetField(fieldName, BINDING_FLAGS);

				if( fieldInfo != null )
				{
					fieldInfo.SetValue( obj, _Deserialize(fieldInfo.FieldType,parser) );
				}
				else
				{
					parser.SkipValue();
				}

				ch = parser.ReadWithTrim();

				if( ch == '}' )
				{
					break;
				}
				else if( ch == ',' )
				{
					continue;
				}
			}
			else if( ch == '}' )
			{
				parser.Read();
				break;
			}

			goto failed;
		}

		return obj;

	failed:
		throw parser.CreateFormatException();
	}

	static object _DeserializeInt16( System.Type type, JsonParser parser )
	{
		return parser.ReadInt16();
	}

	static object _DeserializeInt32( System.Type type, JsonParser parser )
	{
		return parser.ReadInt32();
	}

	static object _DeserializeInt64( System.Type type, JsonParser parser )
	{
		return parser.ReadInt64();
	}

	static object _DeserializeUInt16( System.Type type, JsonParser parser )
	{
		return parser.ReadUInt16();
	}
	
	static object _DeserializeUInt32( System.Type type, JsonParser parser )
	{
		return parser.ReadUInt32();
	}
	
	static object _DeserializeUInt64( System.Type type, JsonParser parser )
	{
		return parser.ReadUInt64();
	}

	static object _DeserializeString( System.Type type, JsonParser parser )
	{
		parser.Trim();
		return parser.ReadString();
	}

	static object _DeserializeFloat( System.Type type, JsonParser parser )
	{
		return System.Single.Parse(parser.ReadToken());
	}

	static object _DeserializeDouble( System.Type type, JsonParser parser )
	{
		return System.Double.Parse(parser.ReadToken());
	}

	static object _DeserializeByte( System.Type type, JsonParser parser )
	{
		return parser.ReadUInt8();
	}

	static object _DeserializeSByte( System.Type type, JsonParser parser )
	{
		return parser.ReadInt8();
	}

	static object _DeserializeBoolean( System.Type type, JsonParser parser )
	{
		return parser.ReadBool();
	}

	static object _DeserializeDecimal( System.Type type, JsonParser parser )
	{
		return System.Decimal.Parse(parser.ReadToken());
	}

	static object _DeserializeChar( System.Type type, JsonParser parser )
	{
		return (char)parser.ReadUInt16();
	}

	static object _DeserializeVector2( System.Type type, JsonParser parser )
	{
		parser.ReadSpecifyChar('[');
		float x = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(',');
		float y = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(']');
		return new UnityEngine.Vector2(x,y);
	}

	static object _DeserializeVector3( System.Type type, JsonParser parser )
	{
		parser.ReadSpecifyChar('[');
		float x = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(',');
		float y = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(',');
		float z = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(']');
		return new UnityEngine.Vector3(x,y,z);
	}

	static object _DeserializeVector4( System.Type type, JsonParser parser )
	{
		parser.ReadSpecifyChar('[');
		float x = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(',');
		float y = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(',');
		float z = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(',');
		float w = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(']');
		return new UnityEngine.Vector4(x,y,z,w);
	}

	static object _DeserializeQuaternion( System.Type type, JsonParser parser )
	{
		parser.ReadSpecifyChar('[');
		float x = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(',');
		float y = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(',');
		float z = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(',');
		float w = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(']');
		return new UnityEngine.Quaternion(x,y,z,w);
	}

	static object _DeserializeRect( System.Type type, JsonParser parser )
	{
		parser.ReadSpecifyChar('[');
		float x = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(',');
		float y = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(',');
		float w = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(',');
		float h = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(']');
		return new UnityEngine.Rect(x,y,w,h);
	}

	static object _DeserializeColor( System.Type type, JsonParser parser )
	{
		parser.ReadSpecifyChar('[');
		float r = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(',');
		float g = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(',');
		float b = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(',');
		float a = System.Single.Parse(parser.ReadToken());
		parser.ReadSpecifyChar(']');
		return new UnityEngine.Color(r,g,b,a);
	}

	static object _DeserializeColor32( System.Type type, JsonParser parser )
	{
		string code = parser.ReadString();
		byte a = (byte)((HexToInt(code[1]) << 4) + HexToInt(code[2]));
		byte r = (byte)((HexToInt(code[3]) << 4) + HexToInt(code[4]));
		byte g = (byte)((HexToInt(code[5]) << 4) + HexToInt(code[6]));
		byte b = (byte)((HexToInt(code[7]) << 4) + HexToInt(code[8]));
		return new UnityEngine.Color32(r,g,b,a);
	}

#if ENABLED_HASH128

	static public UnityEngine.Hash128 ParseHash128( string hex )
	{
		object hash = new UnityEngine.Hash128 ();

		for( int i = 0, count = hash128Fields.Length; i < count; ++i )
		{
			uint u = 0;

			for (int j = (i<<3), k = j + sizeof(uint)*2; j < k; ++j) 
			{
				u = (u<<4) + (uint)HexToInt( hex[j] );
			}

			hash128Fields[i].SetValue( hash, u );
		} 

		return (UnityEngine.Hash128)hash;
	}

	static object _DeserializeHash128( System.Type type, JsonParser parser )
	{
		//Failed UnityEngine.Hash128.Parse

		object hash = (object)new UnityEngine.Hash128 ();
		string hex = parser.ReadString();

		for( int i = 0, count = hash128Fields.Length; i < count; ++i )
		{
			uint u = 0;

			for (int j = (i<<3), k = j + sizeof(uint)*2; j < k; ++j) 
			{
				u = (u<<4) + (uint)HexToInt( hex[j] );
			}

			hash128Fields[i].SetValue( hash, u );
		} 

		return hash;
	}

#endif

	static object _DeserializeDate( System.Type type, JsonParser parser )
	{
		return System.DateTime.ParseExact(
			parser.ReadString(),
			DATE_FORMAT,
			System.Globalization.DateTimeFormatInfo.InvariantInfo,
			System.Globalization.DateTimeStyles.None
			);
	}
}

