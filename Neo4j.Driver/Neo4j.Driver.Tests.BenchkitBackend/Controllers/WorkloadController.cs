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
using Neo4j.Driver.Tests.BenchkitBackend.Abstractions;
using Neo4j.Driver.Tests.BenchkitBackend.Types;

namespace Neo4j.Driver.Tests.BenchkitBackend.Controllers;
using ILogger = Microsoft.Extensions.Logging.ILogger;

/// <summary>
/// Define and run workloads in the Neo4j driver.
/// </summary>
[ApiController]
[Route("[controller]")]
public class WorkloadController(
        IWorkloadStore workloadStore,
        IWorkloadExecutorSelector workloadExecutorSelector,
        ILogger logger,
        LinkGenerator linkGenerator)
    : ControllerBase
{
    // GET
    /// <summary>Executes a workload.</summary>
    /// <remarks>The driver will load all the records to memory and then discard.</remarks>
    /// <param name="id">The id of the workload to execute.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Workload>> Execute(string id)
    {
        try
        {
            var workload = workloadStore.GetWorkload(id);
            logger.LogInformation(
                "Executing workload with {QueryCount} queries, method {Method} and mode {Mode}",
                workload.Queries.Count,
                workload.Method,
                workload.Mode);

            var executor = workloadExecutorSelector.GetExecutor(workload);
            await executor.ExecuteWorkloadAsync(workload);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // POST
    /// <summary>Creates a driver workload.</summary>
    /// <remarks>This endpoint saves the workload in memory for future execution.</remarks>
    [HttpPost]
    [ProducesResponseType<Workload>(StatusCodes.Status201Created)]
    public ActionResult<string> Create([FromBody] Workload workload)
    {
        var id = workloadStore.CreateWorkload(workload);
        var controllerName = ControllerContext.ActionDescriptor.ControllerName;
        var url = linkGenerator.GetUriByAction(HttpContext, nameof(Execute), controllerName, new { id });
        return new CreatedResult(url, workload);
    }

    // PUT
    /// <summary>Execute supplied drivers workload.</summary>
    [HttpPut]
    [ProducesResponseType<Workload>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Workload>> ExecuteEphemeral([FromBody] Workload workload)
    {
        logger.LogInformation(
            "Executing workload with {QueryCount} queries, method {Method} and mode {Mode}",
            workload.Queries.Count,
            workload.Method,
            workload.Mode);

        var executor = workloadExecutorSelector.GetExecutor(workload);
        await executor.ExecuteWorkloadAsync(workload);
        return NoContent();
    }

    // DELETE
    /// <summary>Deletes a driver workload.</summary>
    /// <remarks>This endpoint deletes the workload from memory. Ongoing executions will not be
    /// canceled or stopped.</remarks>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(string id)
    {
        try
        {
            workloadStore.DeleteWorkload(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // PATCH
    /// <summary>Patches a driver workload.</summary>
    [HttpPatch("{id}")]
    [ProducesResponseType<Workload>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Workload> Patch(string id, [FromBody] Workload workload)
    {
        try
        {
            var updatedWorkload = workloadStore.UpdateWorkload(id, workload);
            return Ok(updatedWorkload);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // GET /workloadStore
    /// <summary>Retrieves the entire workload store.</summary>
    [HttpGet("workloadStore")]
    [ProducesResponseType<IDictionary<string, Workload>>(StatusCodes.Status200OK)]
    public ActionResult<IDictionary<string, Workload>> GetWorkloadStore()
    {
        return Ok(workloadStore.GetAllWorkloads());
    }
}
