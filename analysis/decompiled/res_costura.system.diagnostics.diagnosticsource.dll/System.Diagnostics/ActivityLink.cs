using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics;

public readonly struct ActivityLink(ActivityContext context, ActivityTagsCollection? tags = null) : IEquatable<ActivityLink>
{
	private readonly Activity.TagsLinkedList _tags = ((tags != null && tags.Count > 0) ? new Activity.TagsLinkedList(tags) : null);

	public ActivityContext Context { get; } = context;

	public IEnumerable<KeyValuePair<string, object?>>? Tags => _tags;

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is ActivityLink value)
		{
			return Equals(value);
		}
		return false;
	}

	public bool Equals(ActivityLink value)
	{
		if (Context == value.Context)
		{
			return value.Tags == Tags;
		}
		return false;
	}

	public static bool operator ==(ActivityLink left, ActivityLink right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ActivityLink left, ActivityLink right)
	{
		return !left.Equals(right);
	}

	public Activity.Enumerator<KeyValuePair<string, object?>> EnumerateTagObjects()
	{
		return new Activity.Enumerator<KeyValuePair<string, object>>(_tags?.First);
	}

	public override int GetHashCode()
	{
		if (this == default(ActivityLink))
		{
			return 0;
		}
		int num = 5381;
		num = (num << 5) + num + Context.GetHashCode();
		if (Tags != null)
		{
			foreach (KeyValuePair<string, object> tag in Tags)
			{
				num = (num << 5) + num + tag.Key.GetHashCode();
				if (tag.Value != null)
				{
					num = (num << 5) + num + tag.Value.GetHashCode();
				}
			}
		}
		return num;
	}
}
