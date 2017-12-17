using System.ComponentModel;

namespace VanceStubbs.Tests.Types
{
	interface IGetProperty
	{
		int Value { get; }
	}

	interface ISetProperty
	{
		int Value { set; }
	}
}
