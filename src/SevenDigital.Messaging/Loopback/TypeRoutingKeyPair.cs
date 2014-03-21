using System;

namespace SevenDigital.Messaging.Loopback
{
	/// <summary>
	/// Represents a RabbitMq exchange binding - exchange name (Type) and its routing key.
	/// </summary>
	public class TypeRoutingKeyPair : IEquatable<TypeRoutingKeyPair>
	{
		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		public bool Equals(TypeRoutingKeyPair other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return string.Equals(_routingKey, other._routingKey) && Equals(_type, other._type);
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((TypeRoutingKeyPair) obj);
		}

		/// <summary>
		/// Serves as the default hash function.
		/// </summary>
		public override int GetHashCode()
		{
			unchecked
			{
				return ((_routingKey != null ? _routingKey.GetHashCode() : 0)*397) ^ (_type != null ? _type.GetHashCode() : 0);
			}
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		public static bool operator ==(TypeRoutingKeyPair left, TypeRoutingKeyPair right)
		{
			return Equals(left, right);
		}

		/// <summary>
		/// Determines whether the specified object is different from the current object.
		/// </summary>
		public static bool operator !=(TypeRoutingKeyPair left, TypeRoutingKeyPair right)
		{
			return !Equals(left, right);
		}

		readonly string _routingKey;
		readonly Type _type;

		/// <summary>
		/// Creates a new TypeRoutingKeyPair
		/// </summary>
		public TypeRoutingKeyPair(Type type, string routingKey)
		{
			_type = type;
			_routingKey = routingKey;
		}

		/// <summary>
		/// Mapped type, used as the exchange name
		/// </summary>
		public Type Type
		{
			get { return _type; }
		}

		/// <summary>
		/// Exchange routing key
		/// </summary>
		public string RoutingKey
		{
			get { return _routingKey; }
		}
	}
}