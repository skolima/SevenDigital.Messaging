using System;

namespace SevenDigital.Messaging.Integration.Tests._Helpers.Messages
{
	public class JokerMessage : IVillainMessage
	{
		public JokerMessage()
		{
			CorrelationId = Guid.NewGuid();
		}
		public Guid CorrelationId {get; set;}
		public string Text
		{
			get { return "Superman"; }
		}
	}
}