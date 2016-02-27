using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Neo4j.Driver.Extensions;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.Tck.Tests.TCK
{
    public class CypherRecordParser
    {
        private readonly Regex _pathPattern;
        private readonly Regex _nodePattern;
        private readonly Regex _relPattern;
        private readonly Regex _intPattern;
        private readonly Regex _strPattern;
        private readonly Regex _nullPattern;
        private readonly Regex _boolPattern;
        private readonly Regex _floatPattern;
        private readonly Regex _mapPattern;
        private readonly Regex _listPattern;

        #region Consts
        private const string Path = @"^<(.*)>$";
        private const string Node = @"^\((.*?)({.*})?\s*\)$";
        private const string Relationship = @"^\[\s*:(.+?)({.*})?\s*\]$";
        private const string Integer = "^([-+]?[0-9]+)$";
        private const string String = "^\"(.*)\"$";
        private const string Null = "^(?i)null$";
        private const string Boolean = "^(?i)(false|true)$";
        private const string Map = "^{(.*)}$";
        // TEST LIST AFTER RELATIONSHIP
        private const string List = @"^\[(.*)\]$";
        // TEST FLOAT AFTER INT
        private const string Float = @"^([-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?)$";
        #endregion Consts

        public CypherRecordParser()
        {
            // create all regex
            _pathPattern = new Regex(Path);
            _nodePattern = new Regex(Node);
            _relPattern = new Regex(Relationship);
            _strPattern = new Regex(String);
            _nullPattern = new Regex(Null);
            _boolPattern = new Regex(Boolean);
            _intPattern = new Regex(Integer);
            _floatPattern = new Regex(Float);
            _mapPattern = new Regex(Map);
            _listPattern = new Regex(List);
        }

        internal dynamic ParseBasicTypes(string input)
        {
            Match match = _nullPattern.Match(input);
            if (match.Success)
            {
                return null;
            }

            match = _boolPattern.Match(input);
            if (match.Success)
            {
                return match.Groups[1].Value.ToLower().Equals("true");
            }

            match = _strPattern.Match(input);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            match = _intPattern.Match(input);
            if (match.Success)
            {
                return Convert.ToInt64(match.Groups[1].Value);
            }

            match = _floatPattern.Match(input);
            if (match.Success)
            {
                return Convert.ToDouble(match.Groups[1].Value);
            }
            throw new NotSupportedException($"Failed to parse to {nameof(Null)}/{nameof(Boolean)}/{nameof(String)}/{nameof(Integer)}/{nameof(Float)} from input: {input}");
        }

        public dynamic Parse(string input)
        {
            try
            {
                if (_nodePattern.IsMatch(input))
                {
                    return ParseNode(input);
                }
                if (_relPattern.IsMatch(input))
                {
                    return ParseRelationship(input);
                }
                if (_pathPattern.IsMatch(input))
                {

                    return ParsePath(input);
                }
                if (_listPattern.IsMatch(input))
                {
                    return ParseList(input);
                }

                return ParseBasicTypes(input);
            }
            catch (NotSupportedException ex)
            {
                throw new NotSupportedException($"Failed to parse to {nameof(Null)}/{nameof(Boolean)}/{nameof(String)}/{nameof(Integer)}/{nameof(Float)}" +
                                                $"/{nameof(Node)}/{nameof(Relationship)}/{nameof(Path)} from input: {input}", ex);
            }
            // map?
        }

        internal IList<object> ParseList(string input)
        {
            var match = _listPattern.Match(input);
            if (!match.Success)
            {
                throw new NotSupportedException($"Failed to parse to {nameof(List)} from input {input}");
            }

            var items = match.Groups[1].Value.Trim().Split(new [] {','}, StringSplitOptions.RemoveEmptyEntries);
            return items.Select(item => Parse(item.Trim())).Cast<object>().ToList();
        }

        internal IPath ParsePath(string input)
        {
            var match = _pathPattern.Match(input);
            if (!match.Success)
            {
                throw new NotSupportedException($"Failed to parse to {nameof(Path)} from input {input}");
            }

            input = match.Groups[1].Value.Trim();// remove <>

            var nodes = new List<INode>();
            var relationships = new List<IRelationship>();
            var segments = new List<ISegment>();

            var segs = input.Split(new [] { '(', ')', '[', ']'}, StringSplitOptions.RemoveEmptyEntries);
            var start = ParseNode("(" + segs[0] + ")");
            nodes.Add(start);
            for (int i = 1; i < segs.Length; i+= 4)
            {
                var relationship = (Relationship)ParseRelationship("[" + segs[i+1] + "]");
                var end = ParseNode("(" + segs[i + 3] + ")");
                if (segs[i].Trim().Equals("-") && segs[i + 2].Trim().Equals("->"))
                {
                    relationship.SetStartAndEnd(start.Identity, end.Identity);
                }
                else
                {
                    relationship.SetStartAndEnd(end.Identity, start.Identity);
                }
                segments.Add(new Segment(start, relationship, end));
                relationships.Add(relationship);
                nodes.Add(end);
                start = end;
            }
            return new Path(segments, nodes, relationships);
        }

        internal IRelationship ParseRelationship(string input)
        {
            var match = _relPattern.Match(input);
            if (!match.Success)
            {
                throw new NotSupportedException($"Failed to parse to {nameof(Relationship)} from input {input}");
            }
            
            string type = match.Groups[1].Value.Trim();
            string props = match.Groups[2].Value.Trim();
            var propMap = props.Equals(string.Empty) ? new Dictionary<string, object>() : ParseMap(props);
            return new Relationship(type.GetHashCode() & propMap.GetHashCode(), -1, -1, type, propMap);
        }

        internal INode ParseNode(string input)
        {
            var match = _nodePattern.Match(input);
            if (!match.Success)
            {
                throw new NotSupportedException($"Failed to parse to {nameof(Node)} from input {input}");
            }
            string labels = match.Groups[1].Value.Trim();
            string props = match.Groups[2].Value.Trim();

            var propMap = props.Equals(string.Empty) ? new Dictionary<string, object>() : ParseMap(props);
            var labelList = GetLabels(labels);
            return new Node(labelList.GetHashCode() & propMap.GetHashCode(), labelList, propMap);
        }

        internal IReadOnlyDictionary<string, object> ParseMap(string input)
        {
            Match matcher = _mapPattern.Match(input);
            if (!matcher.Success)
            {
                throw new NotSupportedException($"Failed to parse to {nameof(Map)} from input: {input}");
            }
            var map = new Dictionary<string, object>();
            var props = input.Split(new[] { ':', ' ', '{', '}' }, StringSplitOptions.RemoveEmptyEntries);
            if (props.Length%2 != 0)
            {
                throw new NotSupportedException($"Failed to parse to {nameof(Map)} from input: {input}");
            }
            for (int i = 0; i < props.Length; i+=2)
            {
                string key = ParseBasicTypes(props[i]);
                var value = ParseBasicTypes(props[i+1]);
                map.Add(key, value);
            }
            return map;
        }

        private IReadOnlyList<string> GetLabels(string value)
        {
            var labels = value.Split(new[] {':', ' '}, StringSplitOptions.RemoveEmptyEntries);
            return labels.ToList();
        }

        public static string PathToString(IPath path)
        {
            var str = "<";
            INode start, end = path.Start;
            var i = 0;
            foreach (var rel in path.Relationships)
            {
                start = path.Nodes[i];
                end = path.Nodes[i + 1];

                if (rel.Start.Equals(start.Identity))
                {
                    str += NodeToString(start) + "-" + RelToString(rel) + "->";
                }
                else
                {
                    str += NodeToString(start) + "<-" + RelToString(rel) + "-";
                }
                i++;
            }

            str += NodeToString(end) + ">";
            return str;
        }

        public static string RelToString(IRelationship rel)
        {
            return $"[{rel.Type} {rel.Properties.ToContentString()}]";
        }

        public static string NodeToString(INode node)
        {
            return $"({node.Labels.ToContentString()} {node.Properties.ToContentString()})";
        }
    }
}