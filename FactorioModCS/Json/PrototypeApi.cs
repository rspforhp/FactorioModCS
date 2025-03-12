using System.Text.Json.Serialization;
using LuaNt;
using Newtonsoft.Json;

namespace FactorioModCS.Json;

[HideLuaNt]
public class TypedAlias<k,V> where k : TypedAlias<k,V>
{
    public V _;

    public TypedAlias(V val)
    {
        this._ = val;
    }

    [HideLuaNt]
    public To Convert<To>() where To : TypedAlias<To, V>
    {
        return (To)(V)this;
    }

    [HideLuaNt]
    public static implicit operator TypedAlias<k,V>(V str) => new(str);
    [HideLuaNt]
    public static implicit operator V(TypedAlias<k,V> str) => str._;
}


public class PrototypeApi : AbstractApi
{
    public class Property
    {
        public string name { get; set; }
        public int order { get; set; }
        public string description { get; set; }
        public bool @override { get; set; }
        public Value type { get; set; }
        public bool optional { get; set; }

        public class Value
        {
            public string directValue { get; set; }

        public static explicit operator Value(string str)
            {
                return new Value(){directValue = str};
            }
            
            public string complex_type { get; set; }
            public object value { get; set; }
            public string description { get; set; }
            public Value[] options { get; set; }
            public Value[] values { get; set; }
            public Value key { get; set; }
            public bool full_format { get; set; }
        }
        public Value @default { get; set; }
        public bool inline { get; set; }
        
    }
    public class Prototype
    {
        public string name { get; set; }
        public int order { get; set; }
        public string description { get; set; }
        public string[] examples { get; set; }
        public string parent { get; set; }
        public bool @abstract { get; set; }
        public string typename { get; set; }
        public bool deprecated { get; set; }
        public Property[] properties { get; set; }
        
        public Property.Value type { get; set; }
        public EnumValue[] values;
    }

    public class EnumValue
    {
        public string name { get; set; }
        public int order { get; set; }
        public string description { get; set; }
    }
    public Prototype[] prototypes { get; set; }
    public Prototype[] types { get; set; }
    public Prototype[] defines { get; set; }
}