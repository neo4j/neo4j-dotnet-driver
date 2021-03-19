using System;
using System.Collections.Generic;
using Newtonsoft.Json;
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
            TransactionResult,
			SessionResult,
            ResultNext,
			ResultConsume,
            RetryablePositive,
            RetryableNegative,
            ProtocolException,
            SessionLastBookmarks,
			VerifyConnectivity,
			CheckMultiDBSupport,
			ResolverResolutionCompleted,

			NumTypes
        };

        private static readonly Dictionary<Types, Tuple<string, Type>> TypeInformation =
                    new Dictionary<Types, Tuple<string, Type>> { { Types.NewDriver,						new Tuple<string, Type>("NewDriver",					typeof(NewDriver)) },    
                                                                 { Types.DriverClose,					new Tuple<string, Type>("DriverClose",					typeof(DriverClose)) },
                                                                 { Types.NewSession,					new Tuple<string, Type>("NewSession",					typeof(NewSession)) },
                                                                 { Types.SessionClose,					new Tuple<string, Type>("SessionClose",					typeof(SessionClose)) },
                                                                 { Types.AuthorizationToken,			new Tuple<string, Type>("AuthorizationToken",			typeof(AuthorizationToken)) },
                                                                 { Types.SessionRun,					new Tuple<string, Type>("SessionRun",					typeof(SessionRun)) },
                                                                 { Types.TransactionRun,				new Tuple<string, Type>("TransactionRun",				typeof(TransactionRun)) },
                                                                 { Types.TransactionCommit,				new Tuple<string, Type>("TransactionCommit",			typeof(TransactionCommit)) },
                                                                 { Types.TransactionRollback,			new Tuple<string, Type>("TransactionRollback",			typeof(TransactionRollback)) },
                                                                 { Types.SessionReadTransaction,		new Tuple<string, Type>("SessionReadTransaction",		typeof(SessionReadTransaction)) },
                                                                 { Types.SessionWriteTransaction,		new Tuple<string, Type>("SessionWriteTransaction",		typeof(SessionWriteTransaction)) },
                                                                 { Types.SessionBeginTransaction,		new Tuple<string, Type>("SessionBeginTransaction",		typeof(SessionBeginTransaction)) },
                                                                 { Types.TransactionResult,				new Tuple<string, Type>("Result",						typeof(TransactionResult))},
																 { Types.SessionResult,					new Tuple<string, Type>("Result",						typeof(SessionResult))},
																 { Types.ResultNext,					new Tuple<string, Type>("ResultNext",					typeof(ResultNext)) },
																 { Types.ResultConsume,					new Tuple<string, Type>("ResultConsume",				typeof(ResultConsume)) },
                                                                 { Types.RetryablePositive,				new Tuple<string, Type>("RetryablePositive",			typeof(RetryablePositive)) },
                                                                 { Types.RetryableNegative,				new Tuple<string, Type>("RetryableNegative",			typeof(RetryableNegative)) },
                                                                 { Types.ProtocolException,				new Tuple<string, Type>("ProtocolException",			typeof(ProtocolException)) },
                                                                 { Types.SessionLastBookmarks,			new Tuple<string, Type>("SessionLastBookmarks",			typeof(SessionLastBookmarks)) },
																 { Types.VerifyConnectivity,			new Tuple<string, Type>("VerifyConnectivity",			typeof(VerifyConnectivity)) },
																 { Types.CheckMultiDBSupport,			new Tuple<string, Type>("CheckMultiDBSupport",			typeof(CheckMultiDBSupport)) },
																 { Types.ResolverResolutionCompleted,	new Tuple<string, Type>("ResolverResolutionCompleted",	typeof(ResolverResolutionCompleted)) } };


		static Protocol()
        {
            if (TypeInformation.Count != (int)Types.NumTypes) throw new Exception("Failure initialising Protocol Types. Mismatch in enum and dictionary counts.");
        }

        public static string Type(Types t) 
        {
            if (!ValidType(t)) throw new Exception($"Attempting to use an unrecognized type: {t.ToString()}");
            return TypeInformation[t].Item1; 
        }
        public static Types Type(string t) 
        {
            if (!ValidType(t)) throw new Exception($"Attempting to use an unrecognized type: {t.ToString()}");
            return TypeInformation.First(x => x.Value.Item1 == t).Key; 
        }
        
        public static bool ValidType(Types t) { return TypeInformation.ContainsKey(t); }

        public static bool ValidType(string typeName) 
        {   
            foreach (var element in TypeInformation)
			{
                if (element.Value.Item1 == typeName)
                    return true;
			}

            return false;
        }

        public static Type GetObjectType(Types t)
		{
            return TypeInformation[t].Item2;
		}

		public static Types GetObjectType<T>()
		{
			foreach(KeyValuePair<Types, Tuple<string, Type>> t in TypeInformation)
			{
				if (t.Value.Item2 == typeof(T))
					return t.Key;
			}

			throw new Exception($"Failure to find matching protocol type of {typeof(T)}");
		}

    }


    internal abstract class IProtocolObject
    {
        public string name { get; set; }
        [JsonProperty("id")]
        public string uniqueId { get; internal set; }    //Only exposes the get option so that the serializer will output it.  Don't want to read in on deserialization.
        [JsonIgnore]
        protected ProtocolObjectManager ObjManager { get; set; }        
        public event EventHandler ProtocolEvent;

        public void SetObjectManager(ProtocolObjectManager objManager)
        {
            ObjManager = objManager;
        }

        public void SetUniqueId(string id) { uniqueId = id; }


        public virtual async Task Process()
		{
            await Task.CompletedTask;
        }

        public virtual async Task Process(Controller controller)    //Default is to not use the controller object. But option to override this method and use it if necessary.
		{
            await Process();
        }

        public string Encode()      
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public virtual string Respond()
        {
            return Encode();
        }

        protected void TriggerEvent()
		{
            ProtocolEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}
