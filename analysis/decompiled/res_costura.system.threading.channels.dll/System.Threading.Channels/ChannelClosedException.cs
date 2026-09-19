namespace System.Threading.Channels;

public class ChannelClosedException : InvalidOperationException
{
	public ChannelClosedException()
		: base(System.SR.ChannelClosedException_DefaultMessage)
	{
	}

	public ChannelClosedException(string? message)
		: base(message ?? System.SR.ChannelClosedException_DefaultMessage)
	{
	}

	public ChannelClosedException(Exception? innerException)
		: base(System.SR.ChannelClosedException_DefaultMessage, innerException)
	{
	}

	public ChannelClosedException(string? message, Exception? innerException)
		: base(message ?? System.SR.ChannelClosedException_DefaultMessage, innerException)
	{
	}
}
