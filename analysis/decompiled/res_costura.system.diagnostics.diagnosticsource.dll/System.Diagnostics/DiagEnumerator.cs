using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics;

internal struct DiagEnumerator<T>(DiagNode<T> head) : IEnumerator<T>, IDisposable, IEnumerator
{
	private static readonly DiagNode<T> s_Empty = new DiagNode<T>(default(T));

	private DiagNode<T> _nextNode = head;

	private DiagNode<T> _currentNode = s_Empty;

	public T Current => _currentNode.Value;

	object IEnumerator.Current => Current;

	public bool MoveNext()
	{
		if (_nextNode == null)
		{
			_currentNode = s_Empty;
			return false;
		}
		_currentNode = _nextNode;
		_nextNode = _nextNode.Next;
		return true;
	}

	public void Reset()
	{
		throw new NotSupportedException();
	}

	public void Dispose()
	{
	}
}
