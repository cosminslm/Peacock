using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security;

namespace System.Diagnostics.Metrics;

[SecurityCritical]
internal static class LabelInstructionCompiler
{
	public static AggregatorLookupFunc<TAggregator> Create<TAggregator>(ref AggregatorStore<TAggregator> aggregatorStore, Func<TAggregator> createAggregatorFunc, ReadOnlySpan<KeyValuePair<string, object>> labels) where TAggregator : Aggregator
	{
		LabelInstruction[] array = Compile(labels);
		Array.Sort(array, (LabelInstruction a, LabelInstruction b) => string.CompareOrdinal(a.LabelName, b.LabelName));
		int expectedLabels = labels.Length;
		switch (array.Length)
		{
		case 0:
		{
			TAggregator defaultAggregator = aggregatorStore.GetAggregator();
			return delegate(ReadOnlySpan<KeyValuePair<string, object>> l, out TAggregator aggregator)
			{
				if (l.Length != expectedLabels)
				{
					aggregator = null;
					return false;
				}
				aggregator = defaultAggregator;
				return true;
			};
		}
		case 1:
		{
			ConcurrentDictionary<ObjectSequence1, TAggregator> labelValuesDictionary2 = aggregatorStore.GetLabelValuesDictionary<StringSequence1, ObjectSequence1>(new StringSequence1(array[0].LabelName));
			return new LabelInstructionInterpreter<ObjectSequence1, TAggregator>(expectedLabels, array, labelValuesDictionary2, createAggregatorFunc).GetAggregator;
		}
		case 2:
		{
			ConcurrentDictionary<ObjectSequence2, TAggregator> labelValuesDictionary4 = aggregatorStore.GetLabelValuesDictionary<StringSequence2, ObjectSequence2>(new StringSequence2(array[0].LabelName, array[1].LabelName));
			return new LabelInstructionInterpreter<ObjectSequence2, TAggregator>(expectedLabels, array, labelValuesDictionary4, createAggregatorFunc).GetAggregator;
		}
		case 3:
		{
			ConcurrentDictionary<ObjectSequence3, TAggregator> labelValuesDictionary3 = aggregatorStore.GetLabelValuesDictionary<StringSequence3, ObjectSequence3>(new StringSequence3(array[0].LabelName, array[1].LabelName, array[2].LabelName));
			return new LabelInstructionInterpreter<ObjectSequence3, TAggregator>(expectedLabels, array, labelValuesDictionary3, createAggregatorFunc).GetAggregator;
		}
		default:
		{
			string[] array2 = new string[array.Length];
			for (int num = 0; num < array.Length; num++)
			{
				array2[num] = array[num].LabelName;
			}
			ConcurrentDictionary<ObjectSequenceMany, TAggregator> labelValuesDictionary = aggregatorStore.GetLabelValuesDictionary<StringSequenceMany, ObjectSequenceMany>(new StringSequenceMany(array2));
			return new LabelInstructionInterpreter<ObjectSequenceMany, TAggregator>(expectedLabels, array, labelValuesDictionary, createAggregatorFunc).GetAggregator;
		}
		}
	}

	private static LabelInstruction[] Compile(ReadOnlySpan<KeyValuePair<string, object>> labels)
	{
		LabelInstruction[] array = new LabelInstruction[labels.Length];
		for (int i = 0; i < labels.Length; i++)
		{
			int num = i;
			int sourceIndex = i;
			KeyValuePair<string, object> keyValuePair = labels[i];
			array[num] = new LabelInstruction(sourceIndex, keyValuePair.Key);
		}
		return array;
	}
}
