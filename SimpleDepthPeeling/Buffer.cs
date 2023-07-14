public class Buffer<Type>
{
    private Type _value;

    public Buffer(Type value)
    {
        _value = value;
    }

    public Type Read() => _value;

    public void Set(Type v) => _value = v;
}
