using System;

namespace ImageProducer.Exceptions
{

    [Serializable]
    public class InternalException : BaseException
    {
        public InternalException() { }
        public InternalException(string message) : base(message) { }
        public InternalException(string message, Exception inner) : base(message, inner) { }
        protected InternalException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
