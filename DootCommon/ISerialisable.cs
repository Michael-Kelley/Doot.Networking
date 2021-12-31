
namespace Doot
{
    public interface ISerialisable
    {
        void Serialise(MessageSerialiser serialiser);
        void Deserialise(MessageDeserialiser deserialiser);
    }
}
