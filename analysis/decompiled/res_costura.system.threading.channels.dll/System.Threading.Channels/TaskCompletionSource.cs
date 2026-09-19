using System.Threading.Tasks;

namespace System.Threading.Channels;

internal sealed class TaskCompletionSource : TaskCompletionSource<VoidResult>
{
	public TaskCompletionSource(TaskCreationOptions creationOptions)
		: base(creationOptions)
	{
	}

	public bool TrySetResult()
	{
		return TrySetResult(default(VoidResult));
	}
}
