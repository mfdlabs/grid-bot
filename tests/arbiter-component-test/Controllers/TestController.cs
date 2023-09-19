namespace Grid.Arbiter.Service.Tests.Component.Controllers;

using System;
using System.Text;

using Google.Protobuf;

using Microsoft.AspNetCore.Mvc;

using V1;

[ApiController]
public class TestController : Controller
{
    private readonly ScriptManagementAPI.ScriptManagementAPIClient _ScriptManagementClient;

    public TestController(
        ScriptManagementAPI.ScriptManagementAPIClient scriptManagementClient
    )
    {
        _ScriptManagementClient = scriptManagementClient ?? throw new ArgumentNullException(nameof(scriptManagementClient));
    }

    [HttpPost("/v1/test/{scriptType}/{scriptName}")]
    public IActionResult WriteScript(string scriptName, ScriptType scriptType, string content)
    {
        try
        {
            var result = _ScriptManagementClient.WriteScript(
                new()
                {
                    Name = scriptName,
                    Type = scriptType,
                    Content = ByteString.FromBase64(Convert.ToBase64String(Encoding.UTF8.GetBytes(content)))
                }
            );

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return Content(result.ScriptPath);
        }
        catch (Exception ex)
        {
            Response.StatusCode = 500;

            return Content(
                ex.ToString()
            );
        }
    }

    [HttpGet("/v1/test/{scriptType}/{scriptName}")]
    public IActionResult ReadScript(string scriptName, ScriptType scriptType)
    {
        try
        {
            var result = _ScriptManagementClient.ReadScript(
                new()
                {
                    Name = scriptName,
                    Type = scriptType
                }
            );

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return Content(result.ScriptContents);
        }
        catch (Exception ex)
        {
            Response.StatusCode = 500;

            return Content(
                ex.ToString()
            );
        }
    }

    [HttpDelete("/v1/test/{scriptType}/{scriptName}")]
    public IActionResult DeleteScript(string scriptName, ScriptType scriptType)
    {
        try
        {
            var result = _ScriptManagementClient.DeleteScript(
                new()
                {
                    Name = scriptName,
                    Type = scriptType
                }
            );

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return Content(result.ScriptPath);
        }
        catch (Exception ex)
        {
            Response.StatusCode = 500;

            return Content(
                ex.ToString()
            );
        }
    }
}
