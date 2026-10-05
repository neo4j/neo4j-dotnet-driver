// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using Pure.DI;

namespace Neo4j.Driver.Internal.QueryApi;

internal static class QueryApiCommonBindings
{
    internal const string Name = "QueryApiCommonBindings";

    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Used to setup DI")]
    private static void Setup()
    {
        DI.Setup(Name, CompositionKind.Internal)
            .Bind<IDriverUri>().To((DriverContext c) => c)

            .Bind<ILogger>()
            .To(
                ctx =>
                {
                    ctx.Inject<ILoggerFactory>(out var loggerFactory);
                    ctx.Inject<ILoggingContextTracker>(out var tracker);
                    return loggerFactory.GetLoggerForType(ctx.ConsumerType, tracker);
                })

            .Bind<IAsyncRetryLogic>()
            .To((DriverContext context, ILogger logger) => new AsyncRetryLogic(context, logger))
            .Bind<IHttpRequestEnricher>(nameof(QueryApiAuthEnricher)).To<QueryApiAuthEnricher>()
            .Bind<IQueryApiClient>().To<QueryApiClient>()
            .Bind<IQueryApiErrorChecker>().To<QueryApiErrorChecker>()
            .Bind<IQueryApiRequestBuilder>().To<QueryApiRequestBuilder>()
            .Bind<IQueryApiRequestHeaderWriter>().To<QueryApiRequestHeaderWriter>()
            .Bind<IQueryApiUrlBuilder>().To<QueryApiUrlBuilder>()
            .Bind<IQueryApiResultCursorBuilder>().To<QueryApiResultCursorBuilder>()
            .Bind<IResultSummaryFactory>().To<QueryApiResultSummaryFactory>()

            .Bind<IBase64Encoder>().Bind<IBase64Decoder>().To<Base64Codec>()
            .Bind<IJsonValueDecoder>().To<JsonValueDecoder>()
            .Bind<IJsonValueEncoder>().To<JsonValueEncoder>()
            .Bind<IQueryApiJsonSerializer>().Bind<IJsonDeserializer>().To<QueryApiJsonSerializer>()
            .Bind<IQueryApiWriteCodecSelector>().To<QueryApiWriteCodecSelector>()
            .Bind<IRequiredMediaVersionCalculator>().To<RequiredMediaVersionCalculator>()

            .Bind<IQueryApiJsonConverter>().To<QueryApiParameterDictionaryConverter>()

            .Bind<IQueryApiTypeCodec>(nameof(QueryApiDateCodec)).To<QueryApiDateCodec>()
            .Bind<IQueryApiTypeCodec>(nameof(QueryApiDurationCodec)).To<QueryApiDurationCodec>()
            .Bind<IQueryApiTypeCodec>(nameof(QueryApiListCodec)).To<QueryApiListCodec>()
            .Bind<IQueryApiTypeCodec>(nameof(QueryApiLocalDateTimeCodec)).To<QueryApiLocalDateTimeCodec>()
            .Bind<IQueryApiTypeCodec>(nameof(QueryApiLocalTimeCodec)).To<QueryApiLocalTimeCodec>()
            .Bind<IQueryApiTypeCodec>(nameof(QueryApiMapCodec)).To<QueryApiMapCodec>()
            .Bind<IQueryApiTypeCodec>(nameof(QueryApiNodeCodec)).To<QueryApiNodeCodec>()
            .Bind<IQueryApiTypeCodec>(nameof(QueryApiOffsetDateTimeCodec)).To<QueryApiOffsetDateTimeCodec>()
            .Bind<IQueryApiTypeCodec>(nameof(QueryApiPathCodec)).To<QueryApiPathCodec>()
            .Bind<IQueryApiTypeCodec>(nameof(QueryApiPointCodec)).To<QueryApiPointCodec>()
            .Bind<IQueryApiTypeCodec>(nameof(QueryApiPrimitiveCodec)).To<QueryApiPrimitiveCodec>()
            .Bind<IQueryApiTypeCodec>(nameof(QueryApiRelationshipCodec)).To<QueryApiRelationshipCodec>()
            .Bind<IQueryApiTypeCodec>(nameof(QueryApiTimeCodec)).To<QueryApiTimeCodec>()
            .Bind<IQueryApiTypeCodec>(nameof(QueryApiVectorCodec)).To<QueryApiVectorCodec>()
            .Bind<IQueryApiTypeCodec>(nameof(QueryApiZonedDateTimeCodec)).To<QueryApiZonedDateTimeCodec>();
    }
}
