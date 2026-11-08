namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Ordered basemap layer stack (CMD-28.2). Pure UI-local state — not sim, not DecisionLog.
/// Mutations return a new instance; defaults via <see cref="WithDefaults"/>.
/// </summary>
public sealed class MapLayerStackState
{
    private readonly MapLayerEntry[] _layers;

    public MapLayerStackState(IReadOnlyList<MapLayerEntry> layers)
    {
        if (layers is null)
        {
            throw new ArgumentNullException(nameof(layers));
        }

        _layers = new MapLayerEntry[layers.Count];
        for (var i = 0; i < layers.Count; i++)
        {
            var layer = layers[i]
                ?? throw new ArgumentException("Layer list must not contain null entries.", nameof(layers));
            _layers[i] = layer;
        }
    }

    /// <summary>Ordered stack (draw order top-to-bottom as listed).</summary>
    public IReadOnlyList<MapLayerEntry> Layers => _layers;

    public int Count => _layers.Length;

    public int VisibleCount
    {
        get
        {
            var n = 0;
            foreach (var layer in _layers)
            {
                if (layer.IsVisible)
                {
                    n++;
                }
            }

            return n;
        }
    }

    /// <summary>Default stack: all layers on except DayNight off.</summary>
    public static MapLayerStackState WithDefaults() => MapLayerStackProjection.DefaultStack();

    public MapLayerStackState Toggle(MapLayerId id)
    {
        var copy = new MapLayerEntry[_layers.Length];
        var found = false;
        for (var i = 0; i < _layers.Length; i++)
        {
            var layer = _layers[i];
            if (layer.Id == id)
            {
                copy[i] = layer with { IsVisible = !layer.IsVisible };
                found = true;
            }
            else
            {
                copy[i] = layer;
            }
        }

        if (!found)
        {
            return this;
        }

        return new MapLayerStackState(copy);
    }

    public MapLayerStackState SetVisible(MapLayerId id, bool isVisible)
    {
        var copy = new MapLayerEntry[_layers.Length];
        var found = false;
        for (var i = 0; i < _layers.Length; i++)
        {
            var layer = _layers[i];
            if (layer.Id == id)
            {
                if (layer.IsVisible == isVisible)
                {
                    return this;
                }

                copy[i] = layer with { IsVisible = isVisible };
                found = true;
            }
            else
            {
                copy[i] = layer;
            }
        }

        if (!found)
        {
            return this;
        }

        return new MapLayerStackState(copy);
    }

    public bool TryGet(MapLayerId id, out MapLayerEntry entry)
    {
        foreach (var layer in _layers)
        {
            if (layer.Id == id)
            {
                entry = layer;
                return true;
            }
        }

        entry = null!;
        return false;
    }

    /// <summary>
    /// Apply visibility overrides from a string→bool bag (keys are <see cref="MapLayerId"/> names).
    /// Unknown keys are ignored; missing keys keep current visibility.
    /// </summary>
    public MapLayerStackState ApplyVisibilitySnapshot(IReadOnlyDictionary<string, bool>? snapshot)
    {
        if (snapshot is null || snapshot.Count == 0)
        {
            return this;
        }

        var copy = new MapLayerEntry[_layers.Length];
        var changed = false;
        for (var i = 0; i < _layers.Length; i++)
        {
            var layer = _layers[i];
            if (snapshot.TryGetValue(layer.Id.ToString(), out var visible) && layer.IsVisible != visible)
            {
                copy[i] = layer with { IsVisible = visible };
                changed = true;
            }
            else
            {
                copy[i] = layer;
            }
        }

        return changed ? new MapLayerStackState(copy) : this;
    }

    /// <summary>Export visibility as string→bool bag for UI-local persistence.</summary>
    public Dictionary<string, bool> ToVisibilitySnapshot()
    {
        var bag = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var layer in _layers)
        {
            bag[layer.Id.ToString()] = layer.IsVisible;
        }

        return bag;
    }
}
