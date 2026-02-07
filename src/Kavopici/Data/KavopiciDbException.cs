namespace Kavopici.Data;

public class KavopiciDbException : Exception
{
    public KavopiciDbException(string message) : base(message) { }
    public KavopiciDbException(string message, Exception innerException) : base(message, innerException) { }
}
