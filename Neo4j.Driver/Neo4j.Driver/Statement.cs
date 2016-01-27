using System.Collections.Generic;
using System.Dynamic;

namespace Neo4j.Driver
{
    public class Statement
    {
        public string Template { get; }
        public IDictionary<string, object> Parameters { get; }

        public Statement(string temp, IDictionary<string, object> parameters = null)
        {
            Template = temp;
            // TODO a complete copy of parameters
            Parameters = parameters == null ? new Dictionary<string, object>() : new Dictionary<string, object>(parameters);
        }

        public Statement WithTemplate(string newTemplate)
        {
            return new Statement(newTemplate, Parameters);
        }

        public Statement WithParameters(IDictionary<string, object> newParams)
        {
            return new Statement(Template, newParams);
        }

        // TODO clone parameters at the construcor so that we always work on a new copy
        //public Statement WithUpdatedParameters() 
        
    }

}