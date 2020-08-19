using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend
{
    public static class Protocol
    {
        public enum Types
        {
            NewDriver,
            DriverClose,
            NewSession,
            SessionClose,
            AuthorizationToken,
            SessionRun,
            TransactionRun,
            TransactionCommit,
            TransactionRollback,            
            SessionReadTransaction,
            SessionWriteTransaction,
            SessionBeginTransaction,
            Result,
            ResultNext,
            RetryablePositive,
            RetryableNegative,
            ProtocolException,

            NumTypes
        }

        private static readonly Dictionary<Types, string> TypeNames =
                    new Dictionary<Types, string> { { Types.NewDriver, "NewDriver" },
                                                    { Types.DriverClose, "DriverClose" },
                                                    { Types.NewSession, "NewSession" },
                                                    { Types.SessionClose, "SessionClose" },
                                                    { Types.AuthorizationToken, "AuthorizationToken" },
                                                    { Types.SessionRun, "SessionRun" },
                                                    { Types.TransactionRun, "TransactionRun" },
                                                    { Types.TransactionCommit, "TransactionCommit" },
                                                    { Types.TransactionRollback, "TransactionRollback" },
                                                    { Types.SessionReadTransaction, "SessionReadTransaction" },
                                                    { Types.SessionWriteTransaction, "SessionWriteTransaction" },
                                                    { Types.SessionBeginTransaction, "SessionBeginTransaction" },
                                                    { Types.Result, "Result" },
                                                    { Types.ResultNext, "ResultNext" },
                                                    { Types.RetryablePositive, "RetryablePositive" },
                                                    { Types.RetryableNegative, "RetryableNegative" },
                                                    { Types.ProtocolException, "ProtocolException" } };

        static Protocol()
        {
            if (TypeNames.Count != (int)Types.NumTypes) throw new Exception("Failure initialising Protocol Types. Mismatch in enum and dictionary counts.");
        }

        public static string Type(Types t) 
        {
            if (!ValidType(t)) throw new Exception($"Attempting to use an unrecognized type: {t.ToString()}");
            return TypeNames[t]; 
        }
        public static Types Type(string t) 
        {
            if (!ValidType(t)) throw new Exception($"Attempting to use an unrecognized type: {t.ToString()}");
            return TypeNames.First(x => x.Value == t).Key; 
        }
        public static bool ValidType(string typeName) { return TypeNames.ContainsValue(typeName); }
        public static bool ValidType(Types t) { return TypeNames.ContainsKey(t); }

    }


    internal abstract class IProtocolObject
    {
        public string name { get; set; }
        [JsonPropertyName("id")]
        public string uniqueId { get; internal set; }    //Only exposes the get option so that the serializer will output it.  Don't want to read in on deserialization.
        protected ProtocolObjectManager ObjManager { get; set; }

        public void SetObjectManager(ProtocolObjectManager objManager)
        {
            ObjManager = objManager;
        }

        public void SetUniqueId(string id) { uniqueId = id; }

        public abstract Task Process();        

        public string Encode()      
        {
           return JsonSerializer.Serialize<object>(this); 
        }

        public virtual string Respond()
        {
            return Encode();
        }

        protected async Task AysncVoidReturn()
        {
            await Task.Run(() => { });
        }
    }
}
