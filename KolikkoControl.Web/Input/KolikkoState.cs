namespace KolikkoControl.Web.Input;

/// <summary>
/// Represents the state of <see cref="InputBuffer"/>. The state of the kolikko heater.
/// </summary>
public sealed class KolikkoState
{
    readonly string code;

    KolikkoState(string code)
    {
        this.code = code;
    }

    public static readonly KolikkoState Init = new("INIT");
    public static readonly KolikkoState Off = new("OFF");
    public static readonly KolikkoState On = new("ON");

    public override bool Equals(object? obj)
    {
        return code.Equals((obj as KolikkoState)?.code);
    }

    public override int GetHashCode()
    {
        return code.GetHashCode();
    }

    public override string ToString()
    {
        return code;
    }

    public static bool IsBadInput(string msg)
    {
        return msg == On.code || msg == Off.code;
    }

    public static KolikkoState ParseInputStrict(string msg)
    {
        return msg == On.code
            ? On
            : msg == Off.code
                ? Off
                : throw new Exception("ASSERT FAILED BAD STATE" + msg);
    }
}