namespace KolikkoControl.Web.Output;

public class OutputBuffer
{
    Message? currentState;
    Message? error;

    public void BadState(string msg)
    {
        error = new Message
        {
            Topic = "/kolikko1/heat/statusmsg",
            Text = "BAD STATE: " + msg,
        };
    }

    public Message? GetErrorMessage()
    {
        return error;
    }

    public void Running(string msg)
    {
        currentState = new Message
        {
            Topic = "/kolikko1/heat/status",
            Text = "ON"
        };
    }

    public void NotRunning(string msg)
    {
        currentState = new Message
        {
            Topic = "/kolikko1/heat/status",
            Text = "OFF"
        };
    }

    public Message? GetMessage()
    {
        return currentState;
    }

    public class Message
    {
        public required string Topic { get; init; }
        public required string Text { get; init; }
    }
}