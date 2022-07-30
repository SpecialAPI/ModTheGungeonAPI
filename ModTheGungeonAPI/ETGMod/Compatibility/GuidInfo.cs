using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Represents a pair consisting of a guid and info.
/// </summary>
public struct GuidInfo
{
    public GuidInfo(string guid, string info)
    {
        this.guid = guid;
        this.info = info;
    }

    public override string ToString()
    {
        return $"{guid}.{info}";
    }

    public string guid;
    public string info;
}
