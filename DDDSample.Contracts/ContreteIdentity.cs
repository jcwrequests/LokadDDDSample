using System;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace DDDSample
{
    public static class IdentityConvert
    {
        public static string ToStream(IIdentity identity)
        {
            return identity.GetTag() + "-" + identity.GetId();
        }

        public static string ToTransportable(IIdentity identity)
        {
            return identity.GetTag() + "-" + identity.GetId();
        }
    }
    
    [DataContract(Namespace="Sample")]
    public sealed class CustomerId : AbstractIdentity<int>
    {
        public const string TagValue = "customer";

        public CustomerId() { }
        public CustomerId(int id)
        {
            Contract.Requires(id > 0);
            this.Id = id;
        }
        public override int Id {get; protected set;}
        
        public override string GetTag()
        {
            return TagValue;
        }
    }

}