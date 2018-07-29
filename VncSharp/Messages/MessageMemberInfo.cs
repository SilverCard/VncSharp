using System;
using System.Reflection;

namespace VncSharp.Messages
{
    public class MessageMemberInfo
    {
        public Object Value { get; private set; }
        public MemberInfo MemberInfo { get; private set; }
        public MessageMemberAttribute MessageMemberAttribute { get; private set; }
        public Type Type { get; private set; }

        public static MessageMemberInfo FromPropertyInfo(PropertyInfo pi, Object obj)
        {
            return new MessageMemberInfo()
            {
                MessageMemberAttribute = pi.GetCustomAttribute<MessageMemberAttribute>(),
                Value = pi.GetValue(obj),
                MemberInfo = pi,
                Type = pi.PropertyType
            };           

        }
    }
}
