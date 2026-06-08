using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Neo4j.Driver.Bolt.PackStream.Abstractions;
using Neo4j.Driver.Bolt.PackStream.Abstractions.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Types.ValueDecoding;

namespace Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;

internal class ValueDecoderProvider : IValueDecoderProvider
{
    private readonly ILogger _logger;
    private readonly Dictionary<byte, IValueDecoder> _decoders = new();

    public ValueDecoderProvider(IEnumerable<IValueDecoder> decoders, ILogger logger)
    {
        _logger = logger;
        foreach (var decoder in decoders)
        {
            foreach (var markerByte in decoder.HandledMarkerBytes)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(
                        "Registering decoder {decoder} for marker byte: {markerByte}",
                        decoder.GetType().Name,
                        $"0x{markerByte:X2}");
                }

                _decoders[markerByte] = decoder;
            }
        }
    }

    public bool TryGetDecoder(
        byte markerByte,
        IPackStreamDecoder recursionDecoder,
        [NotNullWhen(true)] out IValueDecoder? decoder)
    {
        var found = _decoders.TryGetValue(markerByte, out var d);
        decoder = d;
        if (found && decoder is IRecursiveValueDecoder recursiveValueDecoder)
        {
            recursiveValueDecoder.SetRecursionDecoder(recursionDecoder);
        }

        return found;
    }
}
