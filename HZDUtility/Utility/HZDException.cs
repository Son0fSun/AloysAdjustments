﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace HZDUtility.Utility
{
    public class HzdException : Exception
    {
        public HzdException() { }

        public HzdException(string message) 
            : base(message) { }

        public HzdException(string message, Exception innerException) 
            : base(message, innerException) { }

        protected HzdException(SerializationInfo info, StreamingContext context) 
            : base(info, context) { }
    }
}