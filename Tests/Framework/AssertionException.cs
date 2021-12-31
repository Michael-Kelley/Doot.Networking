﻿using System.Runtime.Serialization;



namespace Doot.Tests
{

    [Serializable]
    public class AssertionException : Exception
    {
        public AssertionException()
        {
        }

        public AssertionException(string message) : base(message)
        {
        }

        public AssertionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected AssertionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
