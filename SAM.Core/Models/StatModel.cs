/* Copyright (c) 2024-2026 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using CommunityToolkit.Mvvm.ComponentModel;

namespace SAM.Core.Models;

/// <summary>
/// Represents a Steam statistic (integer or float).
/// </summary>
public abstract partial class StatModel : ObservableObject
{
    public required string Id { get; init; }
    
    [ObservableProperty]
    private string _displayName = string.Empty;
    
    [ObservableProperty]
    private bool _isIncrementOnly;
    
    [ObservableProperty]
    private bool _isProtected;
    
    [ObservableProperty]
    private int _permission;
    
    public abstract object Value { get; set; }
    public abstract bool IsModified { get; set; }
    public abstract double MinValue { get; }
    public abstract double MaxValue { get; }
    
    /// <summary>
    /// Gets or sets the value as a string for binding purposes.
    /// </summary>
    public abstract string StringValue { get; set; }
    
    /// <summary>
    /// Marks the current value as the original (clears modified state).
    /// </summary>
    public abstract void AcceptChanges();
}

public partial class IntStatModel : StatModel
{
    [ObservableProperty]
    private int _intValue;
    
    private int _originalValue;

    public int OriginalValue
    {
        get => _originalValue;
        set => _originalValue = value;
    }

    public override object Value
    {
        get => IntValue;
        set => IntValue = Convert.ToInt32(value);
    }

    public override string StringValue
    {
        get => IntValue.ToString();
        set
        {
            if (int.TryParse(value, out var intVal))
            {
                IntValue = intVal;
            }
        }
    }

    public override bool IsModified
    {
        get => IntValue != OriginalValue;
        set
        {
            if (!value) AcceptChanges();
        }
    }

    public override double MinValue => int.MinValue;
    public override double MaxValue => int.MaxValue;

    public override void AcceptChanges()
    {
        _originalValue = IntValue;
        OnPropertyChanged(nameof(IsModified));
    }

    partial void OnIntValueChanged(int value)
    {
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(StringValue));
        OnPropertyChanged(nameof(IsModified));
    }
}

public partial class FloatStatModel : StatModel
{
    [ObservableProperty]
    private float _floatValue;
    
    private float _originalValue;

    public float OriginalValue
    {
        get => _originalValue;
        set => _originalValue = value;
    }

    public override object Value
    {
        get => FloatValue;
        set => FloatValue = Convert.ToSingle(value);
    }

    public override string StringValue
    {
        get => FloatValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
        set
        {
            if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var floatVal))
            {
                FloatValue = floatVal;
            }
        }
    }

    public override bool IsModified
    {
        get => Math.Abs(FloatValue - OriginalValue) > float.Epsilon;
        set
        {
            if (!value) AcceptChanges();
        }
    }

    public override double MinValue => float.MinValue;
    public override double MaxValue => float.MaxValue;

    public override void AcceptChanges()
    {
        _originalValue = FloatValue;
        OnPropertyChanged(nameof(IsModified));
    }

    partial void OnFloatValueChanged(float value)
    {
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(StringValue));
        OnPropertyChanged(nameof(IsModified));
    }
}
