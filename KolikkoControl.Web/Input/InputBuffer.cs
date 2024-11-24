using KolikkoControl.Web.Output;

namespace KolikkoControl.Web.Input;

public class InputBuffer(ILogger<InputBuffer> logger, OutputBuffer outputBuffer)
{
    // Does this need to persist state to disk in order to support restarts?
    // - Not doing this. Make it external thermostat's responsibility to periodically send update messages. State
    //   will then auto-heal and we eliminate double bookkeeping of persistent state.

    public KolikkoState State { get; private set; } = KolikkoState.Init;

    public void Handle(string msg)
    {
        if (KolikkoState.IsBadInput(msg))
        {
            logger.LogWarning("Attempt to set unknown state {msg}. Ignoring and keeping {currentStatus}", msg, State);
            outputBuffer.BadState(msg);
            return;
        }

        State = KolikkoState.ParseInputStrict(msg);
    }
}