using System;

namespace Ditto.Translation.Exceptions
{
  public class GoogleTranslateIPBannedException: Exception
  {
    public enum Operation { TokenGeneration, Translation }

    public Operation OperationBanned { get; }
    
    public GoogleTranslateIPBannedException(string message, Operation operation)
      :base("Google translate has banned this IP. " + message)
    {
      OperationBanned = operation;
    }

    public GoogleTranslateIPBannedException(Operation operation)
      :this(String.Empty, operation) { }
  }
}