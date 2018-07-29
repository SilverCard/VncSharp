﻿using System;
using System.Collections.Generic;
using System.Text;

namespace VncSharp
{
    public class MessageMemberAttribute : Attribute
    {
        public int Index { get; private set; }
        public int Size { get; private set; }

        public MessageMemberAttribute(int index, int size = 0)
        {
            Index = index;
            Size = size;
        }
    }
}
