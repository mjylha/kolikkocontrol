namespace KolikkoControl.Web.Output;

public class OutputBuffer(OutputBuffer.OutputTopicConfig config)
{
    public class OutputTopicConfig
    {
        public required string ProblemTopic { get; init; }
        public required string StatusOutputTopic { get; init; }
    }

    Message? currentState;
    Message? error;

    public void BadState(string msg)
    {
        error = new Message
        {
            Topic = config.ProblemTopic,
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
            Topic = config.StatusOutputTopic,
            Text = "ON"
        };
        error = null;
    }

    public void NotRunning(string msg)
    {
        currentState = new Message
        {
            Topic = config.StatusOutputTopic,
            Text = "OFF"
        };
        error = null;
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