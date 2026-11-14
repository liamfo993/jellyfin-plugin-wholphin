using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ICU4N.Util;
using Jellyfin.Plugin.Wholphin.Configuration;
using Jellyfin.Plugin.Wholphin.Models;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Wholphin.Api;

/// <summary>
/// API endpoints for Wholphin.
/// </summary>
[ApiController]
[Authorize]
[Route("wholphin")]
public class WholphinController : ControllerBase
{
    private readonly ILogger<WholphinController> logger;
    private readonly ILoggerFactory loggerFactory;
    private readonly ILibraryManager libraryManager;

    public WholphinController(ILoggerFactory loggerFactory, ILibraryManager libraryManager)
    {
        this.loggerFactory = loggerFactory;
        this.libraryManager = libraryManager;
        logger = loggerFactory.CreateLogger<WholphinController>();
    }

    [AllowAnonymous]
    [HttpGet("public")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult Public()
    {
        // TODO return version?
        return Ok();
    }

    [Authorize]
    [HttpGet("homesettings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<HomePageSettings> GetHomeSettings()
    {
        var config = WholphinPlugin.Instance!.Configuration;
        var settings = config.HomeConfig.HomePageSettings;
        if (settings != null)
        {
            if (settings.Rows.OfType<KidsMovies>().Any(row =>
                string.IsNullOrWhiteSpace(row.Title)
                || (!HasFilterValues(row.Genres) && !HasFilterValues(row.Studios))
                || row.SortBy is not ("SortName" or "DateCreated" or "PremiereDate" or "Random")))
            {
                logger.LogWarning("A kids movies row has an invalid title, filter, or sort order");
                return BadRequest("Each kids movies row requires a title, a genre or studio, and a valid sort order.");
            }

            return CreateHomeSettingsResponse(settings, Request.PathBase);
        }

        return null;
    }

    internal HomePageSettings CreateHomeSettingsResponse(HomePageSettings settings, PathString pathBase)
    {
        return new HomePageSettings
        {
            Version = settings.Version,
            Rows = settings.Rows.Select(row =>
            {
                var responseRow = row is KidsMovies kids ? ToCustomEndpoint(kids, pathBase) : row;
                return responseRow.ViewOptions is { HeightDp: < 64 }
                    ? responseRow.WithViewOptions(null)
                    : responseRow;
            }).ToArray(),
        };
    }

    private static bool HasFilterValues(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length > 0;
    }

    private CustomEndpoint ToCustomEndpoint(KidsMovies row, PathString pathBase)
    {
        var query = new List<KeyValueEntry>
        {
            new() { Key = "includeItemTypes", Value = "Movie" },
            new() { Key = "recursive", Value = "true" },
            new() { Key = "limit", Value = "100" },
            new() { Key = "sortBy", Value = row.SortBy },
            new() { Key = "sortOrder", Value = row.SortBy is "DateCreated" or "PremiereDate" ? "Descending" : "Ascending" },
            new() { Key = "enableUserData", Value = "true" },
        };
        if (HasFilterValues(row.Genres))
        {
            query.Add(new() { Key = "genres", Value = string.Join("|", row.Genres.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) });
        }

        if (HasFilterValues(row.Studios))
        {
            var studioIds = row.Studios.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(libraryManager.GetStudioId);
            query.Add(new() { Key = "studioIds", Value = string.Join(",", studioIds) });
        }

        if (!string.IsNullOrWhiteSpace(row.MaxOfficialRating))
        {
            query.Add(new() { Key = "maxOfficialRating", Value = row.MaxOfficialRating.Trim() });
        }

        if (row.ParentId.HasValue && row.ParentId != Guid.Empty)
        {
            query.Add(new() { Key = "parentId", Value = row.ParentId.Value.ToString() });
        }

        return new CustomEndpoint
        {
            Title = row.Title.Trim(),
            Endpoint = pathBase.Add("/Items").Value!,
            Query = query,
            ViewOptions = row.ViewOptions,
        };
    }

    [HttpGet("config")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetConfig()
    {
        var config = WholphinPlugin.Instance!.Configuration;
        return Ok(config);
    }

    [HttpGet("config/home")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<Response> GetHomeSettingsConfig()
    {
        var config = WholphinPlugin.Instance!.Configuration;
        var settings = config.HomeConfig.HomePageSettings;
        if (settings != null)
        {
            var result = WholphinPlugin.YamlSerializer.Serialize(settings);
            return new Response { Result = result };
        }
        else
        {
            return new Response { };
        }
    }

    [HttpPost("config/home")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<Response> SaveHomeSettingsConfig([FromBody, Required] ConfigValue config)
    {
        HomePageSettings p;
        try
        {
            p = WholphinPlugin.YamlDeserializer.Deserialize<HomePageSettings>(config.Value);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error parsing");
            return BadRequest(e.Data.ToString());
        }
        var toSave = WholphinPlugin.Instance!.Configuration;
        toSave.HomeConfig.HomePageSettings = p;
        WholphinPlugin.Instance.UpdateConfiguration(toSave);
        return Ok();
    }

    [Authorize]
    [HttpGet("seerrsettings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<SeerrConfig> GetSeerrSettings()
    {
        var seerr = WholphinPlugin.Instance!.Configuration.SeerrConfig;
        if (string.IsNullOrWhiteSpace(seerr.ServerUrl))
        {
            return NotFound();
        }
        return seerr;
    }

    [HttpGet("config/seerr")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<Response> GetSeerrSettingsConfig()
    {
        var seerr = WholphinPlugin.Instance!.Configuration.SeerrConfig;
        var result = WholphinPlugin.YamlSerializer.Serialize(seerr);
        return new Response { Result = result };
    }

    [HttpGet("config/seerr/settings")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<SeerrConfig> GetDashboardSeerrSettings()
    {
        return WholphinPlugin.Instance!.Configuration.SeerrConfig;
    }

    [HttpPost("config/seerr/settings")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult SaveDashboardSeerrSettings([FromBody, Required] SeerrConfig settings)
    {
        settings.ServerUrl = settings.ServerUrl?.Trim();
        var toSave = WholphinPlugin.Instance!.Configuration;
        toSave.SeerrConfig = settings;
        WholphinPlugin.Instance.UpdateConfiguration(toSave);
        return Ok();
    }

    [HttpPost("config/seerr")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<Response> SaveSeerrSettingsConfig([FromBody, Required] ConfigValue config)
    {
        SeerrConfig s;
        try
        {
            s = WholphinPlugin.YamlDeserializer.Deserialize<SeerrConfig>(config.Value);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error parsing seerr settings");
            return BadRequest(e.Message);
        }
        var toSave = WholphinPlugin.Instance!.Configuration;
        toSave.SeerrConfig = s;
        WholphinPlugin.Instance.UpdateConfiguration(toSave);
        return Ok();
    }
}

public class ConfigValue
{
    public string Value { get; set; } = default!;
}

public class Response
{
    public string? Error { get; set; } = null;
    public string? Result { get; set; } = null!;
}
