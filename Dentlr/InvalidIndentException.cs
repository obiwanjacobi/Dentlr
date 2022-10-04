using Antlr4.Runtime;

namespace Dentlr
{

	[Serializable]
	public class InvalidIndentException : Exception
    {
		public InvalidIndentException() { }
		public InvalidIndentException(string message) : base(message) { }
		public InvalidIndentException(string message, Exception inner) : base(message, inner) { }
		protected InvalidIndentException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
