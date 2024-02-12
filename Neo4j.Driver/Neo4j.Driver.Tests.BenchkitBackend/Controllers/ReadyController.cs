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

using Microsoft.AspNetCore.Mvc;

namespace Neo4j.Driver.Tests.BenchkitBackend.Controllers;

using ILogger = Microsoft.Extensions.Logging.ILogger;

/// <summary>
/// Check if the service is ready.
/// </summary>
[ApiController]
[Route("[controller]")]
public class ReadyController(
    IDriver driver,
    ILogger logger)
    : ControllerBase
{
    // GET
    /// <summary>
    /// Check if the service is ready.
    /// </summary>
    /// <remarks>
    /// This endpoint can be used to check if the service is ready to receive requests. This obviously includes
    /// the web server, but also whether the backend can successfully connect to the DBMS.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Get()
    {
        try
        {
            await driver.VerifyConnectivityAsync();
            return NoContent();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to verify connectivity to the DBMS");
            return StatusCode(500);
        }
    }
}
