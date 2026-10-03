using System.Security.Claims;
using AshaNandanvan.Application.Analytics;
using AshaNandanvan.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AshaNandanvan.Web.Services;

/// <summary>
/// Records page views and conversions from inside a live Blazor circuit, where there is no
/// HttpContext left to read. Every call is best effort: analytics must never break a page.
/// </summary>
public sealed class VisitTracker : IVisitTracker
{
    private readonly IVisitStore _store;
    private readonly NavigationManager _navigation;
    private readonly AuthenticationStateProvider _auth;
    private readonly ILogger<VisitTracker> _logger;

    // Components initialise before the layout can run its JS handshake, so anything they
    // report on the very first render is held here until the visit is attached.
    private readonly List<(VisitEventKind Kind, string Path, string? Detail, decimal? Value)> _waiting = [];

    private int? _sessionId;
    private bool _attached;

    public VisitTracker(
        IVisitStore store,
        NavigationManager navigation,
        AuthenticationStateProvider auth,
        ILogger<VisitTracker> logger)
    {
        _store = store;
        _navigation = navigation;
        _auth = auth;
        _logger = logger;
    }

    public async Task BeginAsync(Guid sessionKey, int? viewportWidth, CancellationToken cancellationToken = default)
    {
        if (sessionKey == Guid.Empty || _attached)
        {
            return;
        }

        _attached = true;

        try
        {
            _sessionId = await _store.FindBySessionKeyAsync(sessionKey, cancellationToken);
            if (_sessionId is not int id)
            {
                return;
            }

            if (viewportWidth is int width)
            {
                await _store.SetViewportAsync(id, width, cancellationToken);
            }

            foreach (var held in _waiting)
            {
                await _store.RecordEventAsync(
                    id,
                    held.Kind,
                    held.Path,
                    held.Detail,
                    held.Value,
                    await UserIdAsync(),
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Swallow(ex, "attach to a visit");
        }
        finally
        {
            _waiting.Clear();
        }
    }

    public async Task PageViewAsync(string path, CancellationToken cancellationToken = default)
    {
        if (_sessionId is not int id)
        {
            return;
        }

        try
        {
            _sessionId = await _store.RecordPageViewAsync(id, path, await UserIdAsync(), cancellationToken);
        }
        catch (Exception ex)
        {
            Swallow(ex, "record a page view");
        }
    }

    public async Task EventAsync(
        VisitEventKind kind,
        string? detail = null,
        decimal? value = null,
        CancellationToken cancellationToken = default)
    {
        if (_sessionId is not int id)
        {
            if (!_attached && _waiting.Count < 10)
            {
                _waiting.Add((kind, CurrentPath, detail, value));
            }

            return;
        }

        try
        {
            await _store.RecordEventAsync(id, kind, CurrentPath, detail, value, await UserIdAsync(), cancellationToken);
        }
        catch (Exception ex)
        {
            Swallow(ex, $"record {kind}");
        }
    }

    private string CurrentPath => "/" + _navigation.ToBaseRelativePath(_navigation.Uri);

    private async Task<string?> UserIdAsync()
    {
        var state = await _auth.GetAuthenticationStateAsync();
        return state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private void Swallow(Exception ex, string what) =>
        _logger.LogDebug(ex, "Visit tracking could not {What}.", what);
}
