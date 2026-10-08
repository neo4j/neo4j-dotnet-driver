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

namespace Neo4j.Driver.Preview.Encryption;

/// <summary>
/// The stage of building a decrypt request where the additional authenticated data (AAD) to reproduce must be
/// chosen - either supplied explicitly, or taken from the encrypted value itself. This
/// interface is part of the Encryption Preview feature, and is subject to change or removal.
/// </summary>
public interface IDecryptRequestAadStep
{
    /// <summary>
    /// Supplies the additional authenticated data (AAD) to reproduce, binding the decryption to external context.
    /// This method is part of the Encryption Preview feature, and is subject to change or removal.
    /// </summary>
    /// <param name="aad">
    /// The AAD value: a <see cref="bool"/>, an integer, a <see cref="string"/>, a <see cref="byte"/> array, a
    /// <see cref="LocalDate"/> or <see cref="System.DateOnly"/>, a <see cref="LocalTime"/> or
    /// <see cref="System.TimeOnly"/>, an <see cref="OffsetTime"/>, a <see cref="Point"/>, or a
    /// <see cref="System.Guid"/>.
    /// </param>
    /// <remarks>
    /// Decryption needs the same AAD bytes, and the driver does not normalise values to produce them. Consider
    /// normalising a <see cref="string"/> AAD, for example to Unicode Normalization Form C (NFC) with
    /// <see cref="System.Text.NormalizationForm.FormC"/>, both when encrypting and when decrypting.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException"><paramref name="aad"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="aad"/> is of a type not listed above.</exception>
    /// <returns>The next stage of the request.</returns>
    IDecryptRequestExecuteStep WithAad(object aad);

    /// <summary>
    /// Decrypts without external additional authenticated data (AAD), using the AAD configuration recorded in the
    /// encrypted value: the AAD persisted at encryption, or none if none was used. The value is then not bound to any
    /// external context, so it still decrypts if it is moved elsewhere.
    /// This method is part of the Encryption Preview feature, and is subject to change or removal.
    /// </summary>
    /// <returns>The next stage of the request.</returns>
    IDecryptRequestExecuteStep WithoutExternalAad();
}
