#if NET8_0_OR_GREATER

namespace Doxense.Serialization.Json.Tests
{
	using Doxense.Serialization.Json;

	// ReSharper disable GrammarMistakeInComment
	// ReSharper disable InconsistentNaming
	// ReSharper disable JoinDeclarationAndInitializer
	// ReSharper disable PartialTypeWithSinglePart
	// ReSharper disable RedundantNameQualifier

	public partial class GeneratedSerializers
	{

		#region Person ...

		/// <summary>JSON converter for type <see cref="Doxense.Serialization.Json.Tests.Person">Person</see></summary>
		public static PersonJsonConverter Person => m_cachedPerson ??= new();

		private static PersonJsonConverter? m_cachedPerson;

		/// <summary>Converts instances of type <see cref="T:Doxense.Serialization.Json.Tests.Person">Person</see> to and from JSON.</summary>
		[global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
		[global::System.CodeDom.Compiler.GeneratedCode("CrystalJsonSourceGenerator", "0.1")]
		[global::System.Diagnostics.DebuggerNonUserCode()]
		public sealed class PersonJsonConverter : IJsonConverter<Person>
		{

			#region Serialization...

			private static readonly JsonEncodedPropertyName _firstName = new("firstName");
			private static readonly JsonEncodedPropertyName _familyName = new("familyName");

			public void Serialize(CrystalJsonWriter writer, global::Doxense.Serialization.Json.Tests.Person? instance)
			{
				if (instance is null)
				{
					writer.WriteNull();
					return;
				}

				if (instance.GetType() != typeof(global::Doxense.Serialization.Json.Tests.Person))
				{
					CrystalJsonVisitor.VisitValue(instance, typeof(global::Doxense.Serialization.Json.Tests.Person), writer);
					return;
				}

				var state = writer.BeginObject();

				// string FirstName => "firstName"
				// fast!
				writer.WriteField(_firstName, instance.FirstName);

				// string FamilyName => "familyName"
				// fast!
				writer.WriteField(_familyName, instance.FamilyName);

				writer.EndObject(state);
			}

			#endregion

			#region Packing...

			public JsonValue Pack(global::Doxense.Serialization.Json.Tests.Person? instance, CrystalJsonSettings? settings = default, ICrystalJsonTypeResolver? resolver = default)
			{
				if (instance is null)
				{
					return JsonNull.Null;
				}

				if (instance.GetType() != typeof(global::Doxense.Serialization.Json.Tests.Person))
				{
					return JsonValue.FromValue(instance);
				}

				JsonValue? value;
				var readOnly = settings?.ReadOnly ?? false;
				var keepNulls = settings?.ShowNullMembers ?? false;

				var obj = new JsonObject(2);

				// string FirstName => "firstName"
				value = JsonString.Return(instance.FirstName);
				if (keepNulls || value is not null or JsonNull)
				{
					obj["firstName"] = value;
				}

				// string FamilyName => "familyName"
				value = JsonString.Return(instance.FamilyName);
				if (keepNulls || value is not null or JsonNull)
				{
					obj["familyName"] = value;
				}
				if (readOnly)
				{
					return FreezeUnsafe(obj);
				}

				return obj;
			}

			#endregion

			#region Deserialization...

			// FirstName { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<FirstName>k__BackingField")]
			private static extern ref string? FirstNameAccessor(global::Doxense.Serialization.Json.Tests.Person instance);

			// FamilyName { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<FamilyName>k__BackingField")]
			private static extern ref string? FamilyNameAccessor(global::Doxense.Serialization.Json.Tests.Person instance);

			public global::Doxense.Serialization.Json.Tests.Person Unpack(JsonValue value, ICrystalJsonTypeResolver? resolver = default)
			{
				var obj = value.AsObject();
				var instance = global::System.Activator.CreateInstance<global::Doxense.Serialization.Json.Tests.Person>();

				foreach (var kv in obj)
				{
					switch (kv.Key)
					{
						case "firstName": FirstNameAccessor(instance) = kv.Value.ToStringOrDefault(); break;
						case "familyName": FamilyNameAccessor(instance) = kv.Value.ToStringOrDefault(); break;
					}
				}

				return instance;
			}

			#endregion

		}

		/// <summary>Wraps a <see cref="JsonObject"/> into something that looks like a Person</summary>
		public readonly record struct PersonReadOnly : IJsonReadOnlyProxy<global::Doxense.Serialization.Json.Tests.Person, PersonReadOnly, PersonMutable>
		{

			/// <summary>JSON Object that is wrapped</summary>
			private readonly JsonObject m_obj;

			public PersonReadOnly(JsonValue value) => m_obj = value.AsObject();

			/// <inheritdoc />
			public static PersonReadOnly Create(JsonValue value, IJsonConverter<Person>? converter = null) => new(value.AsObject());

			/// <inheritdoc />
			public static PersonReadOnly Create(global::Doxense.Serialization.Json.Tests.Person value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null) => new(GeneratedSerializers.Person.Pack(value, settings.AsReadOnly(), resolver));

			/// <inheritdoc />
			public static IJsonConverter<Person> Converter => GeneratedSerializers.Person;

			/// <inheritdoc />
			public global::Doxense.Serialization.Json.Tests.Person ToValue() => GeneratedSerializers.Person.Unpack(m_obj);

			/// <inheritdoc />
			public JsonValue ToJson() => m_obj;

			/// <inheritdoc />
			public PersonMutable ToMutable() => new(m_obj.Copy());

			/// <inheritdoc />
			public PersonReadOnly With(Action<PersonMutable> modifier)
			{
				var copy = m_obj.Copy();
				modifier(new(copy));
				return new(FreezeUnsafe(copy));
			}

			/// <inheritdoc />
			void IJsonSerializable.JsonSerialize(CrystalJsonWriter writer) => m_obj.JsonSerialize(writer);

			/// <inheritdoc />
			JsonValue IJsonPackable.JsonPack(CrystalJsonSettings settings, ICrystalJsonTypeResolver resolver) => m_obj;

			/// <inheritdoc cref="Person.FirstName" />
			public string? FirstName => m_obj.Get<string?>("firstName", null);

			/// <inheritdoc cref="Person.FamilyName" />
			public string? FamilyName => m_obj.Get<string?>("familyName", null);

		}

		/// <summary>Wraps a <see cref="JsonObject"/> into something that looks like a Person</summary>
		public sealed record PersonMutable : IJsonMutableProxy<global::Doxense.Serialization.Json.Tests.Person, PersonMutable, PersonReadOnly>
		{

			private readonly JsonObject m_obj;

			public PersonMutable(JsonValue value) => m_obj = value.AsObject();

			/// <inheritdoc />
			public static PersonMutable Create(JsonValue value, IJsonConverter<Person>? converter = null) => new(value.AsObject());

			/// <inheritdoc />
			public static PersonMutable Create(global::Doxense.Serialization.Json.Tests.Person value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null) => new(GeneratedSerializers.Person.Pack(value, settings.AsMutable(), resolver));

			/// <inheritdoc />
			public static IJsonConverter<Person> Converter => GeneratedSerializers.Person;

			/// <summary>Pack an instance of <see cref="global::Doxense.Serialization.Json.Tests.Person"/> into a mutable JSON proxy</summary>
			public static PersonMutable FromValue(global::Doxense.Serialization.Json.Tests.Person value)
			{
				global::Doxense.Diagnostics.Contracts.Contract.NotNull(value);
				return new((JsonObject) GeneratedSerializers.Person.Pack(value, CrystalJsonSettings.Json));
			}

			/// <inheritdoc />
			public global::Doxense.Serialization.Json.Tests.Person ToValue() => GeneratedSerializers.Person.Unpack(m_obj);

			/// <inheritdoc />
			public JsonValue ToJson() => m_obj;

			/// <inheritdoc />
			public PersonReadOnly ToReadOnly() => new (m_obj.ToReadOnly());

			/// <inheritdoc />
			void IJsonSerializable.JsonSerialize(CrystalJsonWriter writer) => m_obj.JsonSerialize(writer);

			/// <inheritdoc />
			JsonValue IJsonPackable.JsonPack(CrystalJsonSettings settings, ICrystalJsonTypeResolver resolver) => settings.IsReadOnly() ? m_obj.ToReadOnly() : m_obj;

			/// <inheritdoc cref="Person.FirstName" />
			public string? FirstName
			{
				get => m_obj.Get<string?>("firstName", null);
				set => m_obj["firstName"] = JsonString.Return(value);
			}

			/// <inheritdoc cref="Person.FamilyName" />
			public string? FamilyName
			{
				get => m_obj.Get<string?>("familyName", null);
				set => m_obj["familyName"] = JsonString.Return(value);
			}

		}

		#endregion

		#region MyAwesomeUser ...

		/// <summary>JSON converter for type <see cref="Doxense.Serialization.Json.Tests.MyAwesomeUser">MyAwesomeUser</see></summary>
		public static MyAwesomeUserJsonConverter MyAwesomeUser => m_cachedMyAwesomeUser ??= new();

		private static MyAwesomeUserJsonConverter? m_cachedMyAwesomeUser;

		/// <summary>Converts instances of type <see cref="T:Doxense.Serialization.Json.Tests.MyAwesomeUser">MyAwesomeUser</see> to and from JSON.</summary>
		[global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
		[global::System.CodeDom.Compiler.GeneratedCode("CrystalJsonSourceGenerator", "0.1")]
		[global::System.Diagnostics.DebuggerNonUserCode()]
		public sealed class MyAwesomeUserJsonConverter : IJsonConverter<MyAwesomeUser>
		{

			#region Serialization...

			private static readonly JsonEncodedPropertyName _id = new("id");
			private static readonly JsonEncodedPropertyName _displayName = new("displayName");
			private static readonly JsonEncodedPropertyName _email = new("email");
			private static readonly JsonEncodedPropertyName _type = new("type");
			private static readonly JsonEncodedPropertyName _roles = new("roles");
			private static readonly JsonEncodedPropertyName _metadata = new("metadata");
			private static readonly JsonEncodedPropertyName _items = new("items");
			private static readonly JsonEncodedPropertyName _devices = new("devices");
			private static readonly JsonEncodedPropertyName _extras = new("extras");

			public void Serialize(CrystalJsonWriter writer, global::Doxense.Serialization.Json.Tests.MyAwesomeUser? instance)
			{
				if (instance is null)
				{
					writer.WriteNull();
					return;
				}

				var state = writer.BeginObject();

				// string Id => "id"
				// fast!
				writer.WriteField(_id, instance.Id);

				// string DisplayName => "displayName"
				// fast!
				writer.WriteField(_displayName, instance.DisplayName);

				// string Email => "email"
				// fast!
				writer.WriteField(_email, instance.Email);

				// int Type => "type"
				// fast!
				writer.WriteField(_type, instance.Type);

				// string[] Roles => "roles"
				// fast array!
				writer.WriteFieldArray(_roles, instance.Roles);

				// MyAwesomeMetadata Metadata => "metadata"
				// custom!
				writer.WriteField(_metadata, instance.Metadata, GeneratedSerializers.MyAwesomeMetadata);

				// List<MyAwesomeStruct> Items => "items"
				// custom array!
				writer.WriteFieldArray(_items, instance.Items, GeneratedSerializers.MyAwesomeStruct);

				// Dictionary<string, MyAwesomeDevice> Devices => "devices"
				// dictionary with string key
				writer.WriteFieldDictionary(_devices, instance.Devices, GeneratedSerializers.MyAwesomeDevice);

				// JsonObject Extras => "extras"
				// fast!
				writer.WriteField(_extras, instance.Extras);

				writer.EndObject(state);
			}

			#endregion

			#region Packing...

			public JsonValue Pack(global::Doxense.Serialization.Json.Tests.MyAwesomeUser? instance, CrystalJsonSettings? settings = default, ICrystalJsonTypeResolver? resolver = default)
			{
				if (instance is null)
				{
					return JsonNull.Null;
				}

				JsonValue? value;
				var readOnly = settings?.ReadOnly ?? false;
				var keepNulls = settings?.ShowNullMembers ?? false;

				var obj = new JsonObject(9);

				// string Id => "id"
				value = JsonString.Return(instance.Id);
				obj["id"] = value;

				// string DisplayName => "displayName"
				value = JsonString.Return(instance.DisplayName);
				obj["displayName"] = value;

				// string Email => "email"
				value = JsonString.Return(instance.Email);
				obj["email"] = value;

				// int Type => "type"
				// fast!
				value = JsonNumber.Return(instance.Type);
				obj["type"] = value;

				// string[] Roles => "roles"
				value = JsonSerializerExtensions.JsonPackArray(instance.Roles, settings, resolver);
				if (keepNulls || value is not null or JsonNull)
				{
					obj["roles"] = value;
				}

				// MyAwesomeMetadata Metadata => "metadata"
				// custom!
				value = GeneratedSerializers.MyAwesomeMetadata.Pack(instance.Metadata, settings, resolver);
				obj["metadata"] = value;

				// List<MyAwesomeStruct> Items => "items"
				value = GeneratedSerializers.MyAwesomeStruct.JsonPackList(instance.Items, settings, resolver);
				if (keepNulls || value is not null or JsonNull)
				{
					obj["items"] = value;
				}

				// Dictionary<string, MyAwesomeDevice> Devices => "devices"
				value = GeneratedSerializers.MyAwesomeDevice.JsonPackObject(instance.Devices, settings, resolver);
				if (keepNulls || value is not null or JsonNull)
				{
					obj["devices"] = value;
				}

				// JsonObject Extras => "extras"
				value = readOnly ? instance.Extras?.ToReadOnly() : instance.Extras;
				if (keepNulls || value is not null or JsonNull)
				{
					obj["extras"] = value;
				}
				if (readOnly)
				{
					return FreezeUnsafe(obj);
				}

				return obj;
			}

			#endregion

			#region Deserialization...

			// Id { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<Id>k__BackingField")]
			private static extern ref string IdAccessor(global::Doxense.Serialization.Json.Tests.MyAwesomeUser instance);

			// DisplayName { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<DisplayName>k__BackingField")]
			private static extern ref string DisplayNameAccessor(global::Doxense.Serialization.Json.Tests.MyAwesomeUser instance);

			// Email { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<Email>k__BackingField")]
			private static extern ref string EmailAccessor(global::Doxense.Serialization.Json.Tests.MyAwesomeUser instance);

			// Type { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<Type>k__BackingField")]
			private static extern ref int TypeAccessor(global::Doxense.Serialization.Json.Tests.MyAwesomeUser instance);

			// Roles { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<Roles>k__BackingField")]
			private static extern ref string[]? RolesAccessor(global::Doxense.Serialization.Json.Tests.MyAwesomeUser instance);

			// Metadata { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<Metadata>k__BackingField")]
			private static extern ref global::Doxense.Serialization.Json.Tests.MyAwesomeMetadata MetadataAccessor(global::Doxense.Serialization.Json.Tests.MyAwesomeUser instance);

			// Items { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<Items>k__BackingField")]
			private static extern ref global::System.Collections.Generic.List<global::Doxense.Serialization.Json.Tests.MyAwesomeStruct>? ItemsAccessor(global::Doxense.Serialization.Json.Tests.MyAwesomeUser instance);

			// Devices { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<Devices>k__BackingField")]
			private static extern ref global::System.Collections.Generic.Dictionary<string, global::Doxense.Serialization.Json.Tests.MyAwesomeDevice>? DevicesAccessor(global::Doxense.Serialization.Json.Tests.MyAwesomeUser instance);

			// Extras { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<Extras>k__BackingField")]
			private static extern ref JsonObject? ExtrasAccessor(global::Doxense.Serialization.Json.Tests.MyAwesomeUser instance);

			public global::Doxense.Serialization.Json.Tests.MyAwesomeUser Unpack(JsonValue value, ICrystalJsonTypeResolver? resolver = default)
			{
				var obj = value.AsObject();
				var instance = global::System.Activator.CreateInstance<global::Doxense.Serialization.Json.Tests.MyAwesomeUser>();

				foreach (var kv in obj)
				{
					switch (kv.Key)
					{
						case "id": IdAccessor(instance) = kv.Value.RequiredField("Id").ToString(); break;
						case "displayName": DisplayNameAccessor(instance) = kv.Value.RequiredField("DisplayName").ToString(); break;
						case "email": EmailAccessor(instance) = kv.Value.RequiredField("Email").ToString(); break;
						case "type": TypeAccessor(instance) = kv.Value.ToInt32(); break;
						case "roles": RolesAccessor(instance) = kv.Value.AsArrayOrDefault()?.ToArray<string>(null, resolver)!; break;
						case "metadata": MetadataAccessor(instance) = GeneratedSerializers.MyAwesomeMetadata.UnpackRequired(kv.Value, resolver: resolver, fieldName: "Metadata"); break;
						case "items": ItemsAccessor(instance) = GeneratedSerializers.MyAwesomeStruct.JsonDeserializeListRequired(kv.Value, resolver: resolver, fieldName: "Items"); break;
						case "devices": DevicesAccessor(instance) = GeneratedSerializers.MyAwesomeDevice.JsonDeserializeDictionary(kv.Value, resolver: resolver)!; break;
						case "extras": ExtrasAccessor(instance) = kv.Value.AsObjectOrDefault(); break;
					}
				}

				return instance;
			}

			#endregion

		}

		/// <summary>Wraps a <see cref="JsonObject"/> into something that looks like a MyAwesomeUser</summary>
		public readonly record struct MyAwesomeUserReadOnly : IJsonReadOnlyProxy<global::Doxense.Serialization.Json.Tests.MyAwesomeUser, MyAwesomeUserReadOnly, MyAwesomeUserMutable>
		{

			/// <summary>JSON Object that is wrapped</summary>
			private readonly JsonObject m_obj;

			public MyAwesomeUserReadOnly(JsonValue value) => m_obj = value.AsObject();

			/// <inheritdoc />
			public static MyAwesomeUserReadOnly Create(JsonValue value, IJsonConverter<MyAwesomeUser>? converter = null) => new(value.AsObject());

			/// <inheritdoc />
			public static MyAwesomeUserReadOnly Create(global::Doxense.Serialization.Json.Tests.MyAwesomeUser value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null) => new(GeneratedSerializers.MyAwesomeUser.Pack(value, settings.AsReadOnly(), resolver));

			/// <inheritdoc />
			public static IJsonConverter<MyAwesomeUser> Converter => GeneratedSerializers.MyAwesomeUser;

			/// <inheritdoc />
			public global::Doxense.Serialization.Json.Tests.MyAwesomeUser ToValue() => GeneratedSerializers.MyAwesomeUser.Unpack(m_obj);

			/// <inheritdoc />
			public JsonValue ToJson() => m_obj;

			/// <inheritdoc />
			public MyAwesomeUserMutable ToMutable() => new(m_obj.Copy());

			/// <inheritdoc />
			public MyAwesomeUserReadOnly With(Action<MyAwesomeUserMutable> modifier)
			{
				var copy = m_obj.Copy();
				modifier(new(copy));
				return new(FreezeUnsafe(copy));
			}

			/// <inheritdoc />
			void IJsonSerializable.JsonSerialize(CrystalJsonWriter writer) => m_obj.JsonSerialize(writer);

			/// <inheritdoc />
			JsonValue IJsonPackable.JsonPack(CrystalJsonSettings settings, ICrystalJsonTypeResolver resolver) => m_obj;

			/// <inheritdoc cref="MyAwesomeUser.Id" />
			public string Id => m_obj.Get<string>("id");

			/// <inheritdoc cref="MyAwesomeUser.DisplayName" />
			public string DisplayName => m_obj.Get<string>("displayName");

			/// <inheritdoc cref="MyAwesomeUser.Email" />
			public string Email => m_obj.Get<string>("email");

			/// <inheritdoc cref="MyAwesomeUser.Type" />
			public int Type => m_obj.Get<int>("type", 0);

			/// <inheritdoc cref="MyAwesomeUser.Roles" />
			public JsonReadOnlyProxyArray<string> Roles => new(m_obj.GetArrayOrEmpty("roles"));

			/// <inheritdoc cref="MyAwesomeUser.Metadata" />
			public GeneratedSerializers.MyAwesomeMetadataReadOnly Metadata => new(m_obj.GetObject("metadata"));

			/// <inheritdoc cref="MyAwesomeUser.Items" />
			public JsonReadOnlyProxyArray<global::Doxense.Serialization.Json.Tests.MyAwesomeStruct> Items => new(m_obj.GetArray("items"), GeneratedSerializers.MyAwesomeStruct);

			/// <inheritdoc cref="MyAwesomeUser.Devices" />
			public JsonReadOnlyProxyObject<global::Doxense.Serialization.Json.Tests.MyAwesomeDevice> Devices => new(m_obj.GetObjectOrEmpty("devices"), GeneratedSerializers.MyAwesomeDevice);

			/// <inheritdoc cref="MyAwesomeUser.Extras" />
			public JsonObject? Extras => m_obj.GetObjectOrDefault("extras");

		}

		/// <summary>Wraps a <see cref="JsonObject"/> into something that looks like a MyAwesomeUser</summary>
		public sealed record MyAwesomeUserMutable : IJsonMutableProxy<global::Doxense.Serialization.Json.Tests.MyAwesomeUser, MyAwesomeUserMutable, MyAwesomeUserReadOnly>
		{

			private readonly JsonObject m_obj;

			public MyAwesomeUserMutable(JsonValue value) => m_obj = value.AsObject();

			/// <inheritdoc />
			public static MyAwesomeUserMutable Create(JsonValue value, IJsonConverter<MyAwesomeUser>? converter = null) => new(value.AsObject());

			/// <inheritdoc />
			public static MyAwesomeUserMutable Create(global::Doxense.Serialization.Json.Tests.MyAwesomeUser value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null) => new(GeneratedSerializers.MyAwesomeUser.Pack(value, settings.AsMutable(), resolver));

			/// <inheritdoc />
			public static IJsonConverter<MyAwesomeUser> Converter => GeneratedSerializers.MyAwesomeUser;

			/// <summary>Pack an instance of <see cref="global::Doxense.Serialization.Json.Tests.MyAwesomeUser"/> into a mutable JSON proxy</summary>
			public static MyAwesomeUserMutable FromValue(global::Doxense.Serialization.Json.Tests.MyAwesomeUser value)
			{
				global::Doxense.Diagnostics.Contracts.Contract.NotNull(value);
				return new((JsonObject) GeneratedSerializers.MyAwesomeUser.Pack(value, CrystalJsonSettings.Json));
			}

			/// <inheritdoc />
			public global::Doxense.Serialization.Json.Tests.MyAwesomeUser ToValue() => GeneratedSerializers.MyAwesomeUser.Unpack(m_obj);

			/// <inheritdoc />
			public JsonValue ToJson() => m_obj;

			/// <inheritdoc />
			public MyAwesomeUserReadOnly ToReadOnly() => new (m_obj.ToReadOnly());

			/// <inheritdoc />
			void IJsonSerializable.JsonSerialize(CrystalJsonWriter writer) => m_obj.JsonSerialize(writer);

			/// <inheritdoc />
			JsonValue IJsonPackable.JsonPack(CrystalJsonSettings settings, ICrystalJsonTypeResolver resolver) => settings.IsReadOnly() ? m_obj.ToReadOnly() : m_obj;

			/// <inheritdoc cref="MyAwesomeUser.Id" />
			public string Id
			{
				get => m_obj.Get<string>("id");
				set => m_obj["id"] = JsonString.Return(value);
			}

			/// <inheritdoc cref="MyAwesomeUser.DisplayName" />
			public string DisplayName
			{
				get => m_obj.Get<string>("displayName");
				set => m_obj["displayName"] = JsonString.Return(value);
			}

			/// <inheritdoc cref="MyAwesomeUser.Email" />
			public string Email
			{
				get => m_obj.Get<string>("email");
				set => m_obj["email"] = JsonString.Return(value);
			}

			/// <inheritdoc cref="MyAwesomeUser.Type" />
			public int Type
			{
				get => m_obj.Get<int>("type", 0);
				set => m_obj["type"] = JsonNumber.Return(value);
			}

			/// <inheritdoc cref="MyAwesomeUser.Roles" />
			public string[]? Roles
			{
				get => m_obj.Get<string[]?>("roles", null);
				set => m_obj.Set<string[]?>("roles", value);
			}

			/// <inheritdoc cref="MyAwesomeUser.Metadata" />
			public GeneratedSerializers.MyAwesomeMetadataMutable Metadata
			{
				get => new(m_obj.GetObject("metadata"));
				set => m_obj["metadata"] = value.ToJson();
			}

			/// <inheritdoc cref="MyAwesomeUser.Items" />
			public JsonMutableProxyArray<global::Doxense.Serialization.Json.Tests.MyAwesomeStruct, GeneratedSerializers.MyAwesomeStructMutable> Items
			{
				get => new(m_obj["items"]);
				set => m_obj["items"] = value.ToJson();
			}

			/// <inheritdoc cref="MyAwesomeUser.Devices" />
			public JsonMutableProxyObject<global::Doxense.Serialization.Json.Tests.MyAwesomeDevice, GeneratedSerializers.MyAwesomeDeviceMutable> Devices
			{
				get => new(m_obj["devices"]);
				set => m_obj["devices"] = value.ToJson();
			}

			/// <inheritdoc cref="MyAwesomeUser.Extras" />
			public JsonObject? Extras
			{
				get => m_obj.GetObjectOrDefault("extras")?.ToMutable();
				set => m_obj["extras"] = value ?? JsonNull.Null;
			}

		}

		#endregion

		#region MyAwesomeMetadata ...

		/// <summary>JSON converter for type <see cref="Doxense.Serialization.Json.Tests.MyAwesomeMetadata">MyAwesomeMetadata</see></summary>
		public static MyAwesomeMetadataJsonConverter MyAwesomeMetadata => m_cachedMyAwesomeMetadata ??= new();

		private static MyAwesomeMetadataJsonConverter? m_cachedMyAwesomeMetadata;

		/// <summary>Converts instances of type <see cref="T:Doxense.Serialization.Json.Tests.MyAwesomeMetadata">MyAwesomeMetadata</see> to and from JSON.</summary>
		[global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
		[global::System.CodeDom.Compiler.GeneratedCode("CrystalJsonSourceGenerator", "0.1")]
		[global::System.Diagnostics.DebuggerNonUserCode()]
		public sealed class MyAwesomeMetadataJsonConverter : IJsonConverter<MyAwesomeMetadata>
		{

			#region Serialization...

			private static readonly JsonEncodedPropertyName _accountCreated = new("accountCreated");
			private static readonly JsonEncodedPropertyName _accountModified = new("accountModified");
			private static readonly JsonEncodedPropertyName _accountDisabled = new("accountDisabled");

			public void Serialize(CrystalJsonWriter writer, global::Doxense.Serialization.Json.Tests.MyAwesomeMetadata? instance)
			{
				if (instance is null)
				{
					writer.WriteNull();
					return;
				}

				var state = writer.BeginObject();

				// DateTimeOffset AccountCreated => "accountCreated"
				// fast!
				writer.WriteField(_accountCreated, instance.AccountCreated);

				// DateTimeOffset AccountModified => "accountModified"
				// fast!
				writer.WriteField(_accountModified, instance.AccountModified);

				// DateTimeOffset? AccountDisabled => "accountDisabled"
				// fast!
				writer.WriteField(_accountDisabled, instance.AccountDisabled);

				writer.EndObject(state);
			}

			#endregion

			#region Packing...

			public JsonValue Pack(global::Doxense.Serialization.Json.Tests.MyAwesomeMetadata? instance, CrystalJsonSettings? settings = default, ICrystalJsonTypeResolver? resolver = default)
			{
				if (instance is null)
				{
					return JsonNull.Null;
				}

				JsonValue? value;
				var readOnly = settings?.ReadOnly ?? false;
				var keepNulls = settings?.ShowNullMembers ?? false;

				var obj = new JsonObject(3);

				// DateTimeOffset AccountCreated => "accountCreated"
				// fast!
				value = JsonDateTime.Return(instance.AccountCreated);
				obj["accountCreated"] = value;

				// DateTimeOffset AccountModified => "accountModified"
				// fast!
				value = JsonDateTime.Return(instance.AccountModified);
				obj["accountModified"] = value;

				// DateTimeOffset? AccountDisabled => "accountDisabled"
				// fast!
				{
					var tmp = instance.AccountDisabled;
					value = tmp.HasValue ? JsonDateTime.Return(tmp.Value) : null;
					if (keepNulls || value is not null or JsonNull)
					{
						obj["accountDisabled"] = value;
					}
				}
				if (readOnly)
				{
					return FreezeUnsafe(obj);
				}

				return obj;
			}

			#endregion

			#region Deserialization...

			// AccountCreated { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<AccountCreated>k__BackingField")]
			private static extern ref global::System.DateTimeOffset AccountCreatedAccessor(global::Doxense.Serialization.Json.Tests.MyAwesomeMetadata instance);

			// AccountModified { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<AccountModified>k__BackingField")]
			private static extern ref global::System.DateTimeOffset AccountModifiedAccessor(global::Doxense.Serialization.Json.Tests.MyAwesomeMetadata instance);

			// AccountDisabled { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<AccountDisabled>k__BackingField")]
			private static extern ref global::System.DateTimeOffset? AccountDisabledAccessor(global::Doxense.Serialization.Json.Tests.MyAwesomeMetadata instance);

			public global::Doxense.Serialization.Json.Tests.MyAwesomeMetadata Unpack(JsonValue value, ICrystalJsonTypeResolver? resolver = default)
			{
				var obj = value.AsObject();
				var instance = global::System.Activator.CreateInstance<global::Doxense.Serialization.Json.Tests.MyAwesomeMetadata>();

				foreach (var kv in obj)
				{
					switch (kv.Key)
					{
						case "accountCreated": AccountCreatedAccessor(instance) = kv.Value.ToDateTimeOffset(); break;
						case "accountModified": AccountModifiedAccessor(instance) = kv.Value.ToDateTimeOffset(); break;
						case "accountDisabled": AccountDisabledAccessor(instance) = kv.Value.ToDateTimeOffsetOrDefault(); break;
					}
				}

				return instance;
			}

			#endregion

		}

		/// <summary>Wraps a <see cref="JsonObject"/> into something that looks like a MyAwesomeMetadata</summary>
		public readonly record struct MyAwesomeMetadataReadOnly : IJsonReadOnlyProxy<global::Doxense.Serialization.Json.Tests.MyAwesomeMetadata, MyAwesomeMetadataReadOnly, MyAwesomeMetadataMutable>
		{

			/// <summary>JSON Object that is wrapped</summary>
			private readonly JsonObject m_obj;

			public MyAwesomeMetadataReadOnly(JsonValue value) => m_obj = value.AsObject();

			/// <inheritdoc />
			public static MyAwesomeMetadataReadOnly Create(JsonValue value, IJsonConverter<MyAwesomeMetadata>? converter = null) => new(value.AsObject());

			/// <inheritdoc />
			public static MyAwesomeMetadataReadOnly Create(global::Doxense.Serialization.Json.Tests.MyAwesomeMetadata value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null) => new(GeneratedSerializers.MyAwesomeMetadata.Pack(value, settings.AsReadOnly(), resolver));

			/// <inheritdoc />
			public static IJsonConverter<MyAwesomeMetadata> Converter => GeneratedSerializers.MyAwesomeMetadata;

			/// <inheritdoc />
			public global::Doxense.Serialization.Json.Tests.MyAwesomeMetadata ToValue() => GeneratedSerializers.MyAwesomeMetadata.Unpack(m_obj);

			/// <inheritdoc />
			public JsonValue ToJson() => m_obj;

			/// <inheritdoc />
			public MyAwesomeMetadataMutable ToMutable() => new(m_obj.Copy());

			/// <inheritdoc />
			public MyAwesomeMetadataReadOnly With(Action<MyAwesomeMetadataMutable> modifier)
			{
				var copy = m_obj.Copy();
				modifier(new(copy));
				return new(FreezeUnsafe(copy));
			}

			/// <inheritdoc />
			void IJsonSerializable.JsonSerialize(CrystalJsonWriter writer) => m_obj.JsonSerialize(writer);

			/// <inheritdoc />
			JsonValue IJsonPackable.JsonPack(CrystalJsonSettings settings, ICrystalJsonTypeResolver resolver) => m_obj;

			/// <inheritdoc cref="MyAwesomeMetadata.AccountCreated" />
			public global::System.DateTimeOffset AccountCreated => m_obj.Get<global::System.DateTimeOffset>("accountCreated", DateTimeOffset.MinValue);

			/// <inheritdoc cref="MyAwesomeMetadata.AccountModified" />
			public global::System.DateTimeOffset AccountModified => m_obj.Get<global::System.DateTimeOffset>("accountModified", DateTimeOffset.MinValue);

			/// <inheritdoc cref="MyAwesomeMetadata.AccountDisabled" />
			public global::System.DateTimeOffset? AccountDisabled => m_obj.Get<global::System.DateTimeOffset?>("accountDisabled", default!);

		}

		/// <summary>Wraps a <see cref="JsonObject"/> into something that looks like a MyAwesomeMetadata</summary>
		public sealed record MyAwesomeMetadataMutable : IJsonMutableProxy<global::Doxense.Serialization.Json.Tests.MyAwesomeMetadata, MyAwesomeMetadataMutable, MyAwesomeMetadataReadOnly>
		{

			private readonly JsonObject m_obj;

			public MyAwesomeMetadataMutable(JsonValue value) => m_obj = value.AsObject();

			/// <inheritdoc />
			public static MyAwesomeMetadataMutable Create(JsonValue value, IJsonConverter<MyAwesomeMetadata>? converter = null) => new(value.AsObject());

			/// <inheritdoc />
			public static MyAwesomeMetadataMutable Create(global::Doxense.Serialization.Json.Tests.MyAwesomeMetadata value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null) => new(GeneratedSerializers.MyAwesomeMetadata.Pack(value, settings.AsMutable(), resolver));

			/// <inheritdoc />
			public static IJsonConverter<MyAwesomeMetadata> Converter => GeneratedSerializers.MyAwesomeMetadata;

			/// <summary>Pack an instance of <see cref="global::Doxense.Serialization.Json.Tests.MyAwesomeMetadata"/> into a mutable JSON proxy</summary>
			public static MyAwesomeMetadataMutable FromValue(global::Doxense.Serialization.Json.Tests.MyAwesomeMetadata value)
			{
				global::Doxense.Diagnostics.Contracts.Contract.NotNull(value);
				return new((JsonObject) GeneratedSerializers.MyAwesomeMetadata.Pack(value, CrystalJsonSettings.Json));
			}

			/// <inheritdoc />
			public global::Doxense.Serialization.Json.Tests.MyAwesomeMetadata ToValue() => GeneratedSerializers.MyAwesomeMetadata.Unpack(m_obj);

			/// <inheritdoc />
			public JsonValue ToJson() => m_obj;

			/// <inheritdoc />
			public MyAwesomeMetadataReadOnly ToReadOnly() => new (m_obj.ToReadOnly());

			/// <inheritdoc />
			void IJsonSerializable.JsonSerialize(CrystalJsonWriter writer) => m_obj.JsonSerialize(writer);

			/// <inheritdoc />
			JsonValue IJsonPackable.JsonPack(CrystalJsonSettings settings, ICrystalJsonTypeResolver resolver) => settings.IsReadOnly() ? m_obj.ToReadOnly() : m_obj;

			/// <inheritdoc cref="MyAwesomeMetadata.AccountCreated" />
			public global::System.DateTimeOffset AccountCreated
			{
				get => m_obj.Get<global::System.DateTimeOffset>("accountCreated", DateTimeOffset.MinValue);
				set => m_obj["accountCreated"] = JsonDateTime.Return(value);
			}

			/// <inheritdoc cref="MyAwesomeMetadata.AccountModified" />
			public global::System.DateTimeOffset AccountModified
			{
				get => m_obj.Get<global::System.DateTimeOffset>("accountModified", DateTimeOffset.MinValue);
				set => m_obj["accountModified"] = JsonDateTime.Return(value);
			}

			/// <inheritdoc cref="MyAwesomeMetadata.AccountDisabled" />
			public global::System.DateTimeOffset? AccountDisabled
			{
				get => m_obj.Get<global::System.DateTimeOffset?>("accountDisabled", default!);
				set => m_obj["accountDisabled"] = JsonDateTime.Return(value);
			}

		}

		#endregion

		#region MyAwesomeStruct ...

		/// <summary>JSON converter for type <see cref="Doxense.Serialization.Json.Tests.MyAwesomeStruct">MyAwesomeStruct</see></summary>
		public static MyAwesomeStructJsonConverter MyAwesomeStruct => m_cachedMyAwesomeStruct ??= new();

		private static MyAwesomeStructJsonConverter? m_cachedMyAwesomeStruct;

		/// <summary>Converts instances of type <see cref="T:Doxense.Serialization.Json.Tests.MyAwesomeStruct">MyAwesomeStruct</see> to and from JSON.</summary>
		[global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
		[global::System.CodeDom.Compiler.GeneratedCode("CrystalJsonSourceGenerator", "0.1")]
		[global::System.Diagnostics.DebuggerNonUserCode()]
		public sealed class MyAwesomeStructJsonConverter : IJsonConverter<MyAwesomeStruct>
		{

			#region Serialization...

			private static readonly JsonEncodedPropertyName _id = new("id");
			private static readonly JsonEncodedPropertyName _level = new("level");
			private static readonly JsonEncodedPropertyName _path = new("path");
			private static readonly JsonEncodedPropertyName _disabled = new("disabled");

			public void Serialize(CrystalJsonWriter writer, global::Doxense.Serialization.Json.Tests.MyAwesomeStruct instance)
			{
				var state = writer.BeginObject();

				// string Id => "id"
				// fast!
				writer.WriteField(_id, instance.Id);

				// int Level => "level"
				// fast!
				writer.WriteField(_level, instance.Level);

				// JsonPath Path => "path"
				// TODO: unsupported enumerable type: Doxense.Serialization.Json.JsonPath
				// unknown type
				writer.WriteField(_path, instance.Path);

				// bool? Disabled => "disabled"
				// fast!
				writer.WriteField(_disabled, instance.Disabled);

				writer.EndObject(state);
			}

			#endregion

			#region Packing...

			public JsonValue Pack(global::Doxense.Serialization.Json.Tests.MyAwesomeStruct instance, CrystalJsonSettings? settings = default, ICrystalJsonTypeResolver? resolver = default)
			{
				JsonValue? value;
				var readOnly = settings?.ReadOnly ?? false;
				var keepNulls = settings?.ShowNullMembers ?? false;

				var obj = new JsonObject(4);

				// string Id => "id"
				value = JsonString.Return(instance.Id);
				obj["id"] = value;

				// int Level => "level"
				// fast!
				value = JsonNumber.Return(instance.Level);
				obj["level"] = value;

				// JsonPath Path => "path"
				// fast!
				value = JsonValue.FromValue<JsonPath>(instance.Path);
				obj["path"] = value;

				// bool? Disabled => "disabled"
				// fast!
				{
					var tmp = instance.Disabled;
					value = tmp.HasValue ? JsonBoolean.Return(tmp.Value) : null;
					if (keepNulls || value is not null or JsonNull)
					{
						obj["disabled"] = value;
					}
				}
				if (readOnly)
				{
					return FreezeUnsafe(obj);
				}

				return obj;
			}

			#endregion

			#region Deserialization...

			// Id { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<Id>k__BackingField")]
			private static extern ref string IdAccessor(ref global::Doxense.Serialization.Json.Tests.MyAwesomeStruct instance);

			// Level { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<Level>k__BackingField")]
			private static extern ref int LevelAccessor(ref global::Doxense.Serialization.Json.Tests.MyAwesomeStruct instance);

			// Path { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<Path>k__BackingField")]
			private static extern ref JsonPath PathAccessor(ref global::Doxense.Serialization.Json.Tests.MyAwesomeStruct instance);

			// Disabled { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<Disabled>k__BackingField")]
			private static extern ref bool? DisabledAccessor(ref global::Doxense.Serialization.Json.Tests.MyAwesomeStruct instance);

			public global::Doxense.Serialization.Json.Tests.MyAwesomeStruct Unpack(JsonValue value, ICrystalJsonTypeResolver? resolver = default)
			{
				var obj = value.AsObject();
				var instance = global::System.Activator.CreateInstance<global::Doxense.Serialization.Json.Tests.MyAwesomeStruct>();

				foreach (var kv in obj)
				{
					switch (kv.Key)
					{
						case "id": IdAccessor(ref instance) = kv.Value.RequiredField("Id").ToString(); break;
						case "level": LevelAccessor(ref instance) = kv.Value.ToInt32(); break;
						case "path": PathAccessor(ref instance) = JsonSerializerExtensions.UnpackRequired<JsonPath>(kv.Value, resolver: resolver, fieldName: "Path"); break;
						case "disabled": DisabledAccessor(ref instance) = kv.Value.ToBooleanOrDefault(); break;
					}
				}

				return instance;
			}

			#endregion

		}

		/// <summary>Wraps a <see cref="JsonObject"/> into something that looks like a MyAwesomeStruct</summary>
		public readonly record struct MyAwesomeStructReadOnly : IJsonReadOnlyProxy<global::Doxense.Serialization.Json.Tests.MyAwesomeStruct, MyAwesomeStructReadOnly, MyAwesomeStructMutable>
		{

			/// <summary>JSON Object that is wrapped</summary>
			private readonly JsonObject m_obj;

			public MyAwesomeStructReadOnly(JsonValue value) => m_obj = value.AsObject();

			/// <inheritdoc />
			public static MyAwesomeStructReadOnly Create(JsonValue value, IJsonConverter<MyAwesomeStruct>? converter = null) => new(value.AsObject());

			/// <inheritdoc />
			public static MyAwesomeStructReadOnly Create(global::Doxense.Serialization.Json.Tests.MyAwesomeStruct value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null) => new(GeneratedSerializers.MyAwesomeStruct.Pack(value, settings.AsReadOnly(), resolver));

			/// <inheritdoc />
			public static IJsonConverter<MyAwesomeStruct> Converter => GeneratedSerializers.MyAwesomeStruct;

			/// <inheritdoc />
			public global::Doxense.Serialization.Json.Tests.MyAwesomeStruct ToValue() => GeneratedSerializers.MyAwesomeStruct.Unpack(m_obj);

			/// <inheritdoc />
			public JsonValue ToJson() => m_obj;

			/// <inheritdoc />
			public MyAwesomeStructMutable ToMutable() => new(m_obj.Copy());

			/// <inheritdoc />
			public MyAwesomeStructReadOnly With(Action<MyAwesomeStructMutable> modifier)
			{
				var copy = m_obj.Copy();
				modifier(new(copy));
				return new(FreezeUnsafe(copy));
			}

			/// <inheritdoc />
			void IJsonSerializable.JsonSerialize(CrystalJsonWriter writer) => m_obj.JsonSerialize(writer);

			/// <inheritdoc />
			JsonValue IJsonPackable.JsonPack(CrystalJsonSettings settings, ICrystalJsonTypeResolver resolver) => m_obj;

			/// <inheritdoc cref="MyAwesomeStruct.Id" />
			public string Id => m_obj.Get<string>("id");

			/// <inheritdoc cref="MyAwesomeStruct.Level" />
			public int Level => m_obj.Get<int>("level");

			/// <inheritdoc cref="MyAwesomeStruct.Path" />
			public JsonPath Path => m_obj.Get<JsonPath>("path");

			/// <inheritdoc cref="MyAwesomeStruct.Disabled" />
			public bool? Disabled => m_obj.Get<bool?>("disabled", default!);

		}

		/// <summary>Wraps a <see cref="JsonObject"/> into something that looks like a MyAwesomeStruct</summary>
		public sealed record MyAwesomeStructMutable : IJsonMutableProxy<global::Doxense.Serialization.Json.Tests.MyAwesomeStruct, MyAwesomeStructMutable, MyAwesomeStructReadOnly>
		{

			private readonly JsonObject m_obj;

			public MyAwesomeStructMutable(JsonValue value) => m_obj = value.AsObject();

			/// <inheritdoc />
			public static MyAwesomeStructMutable Create(JsonValue value, IJsonConverter<MyAwesomeStruct>? converter = null) => new(value.AsObject());

			/// <inheritdoc />
			public static MyAwesomeStructMutable Create(global::Doxense.Serialization.Json.Tests.MyAwesomeStruct value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null) => new(GeneratedSerializers.MyAwesomeStruct.Pack(value, settings.AsMutable(), resolver));

			/// <inheritdoc />
			public static IJsonConverter<MyAwesomeStruct> Converter => GeneratedSerializers.MyAwesomeStruct;

			/// <summary>Pack an instance of <see cref="global::Doxense.Serialization.Json.Tests.MyAwesomeStruct"/> into a mutable JSON proxy</summary>
			public static MyAwesomeStructMutable FromValue(global::Doxense.Serialization.Json.Tests.MyAwesomeStruct value)
			{
				global::Doxense.Diagnostics.Contracts.Contract.NotNull(value);
				return new((JsonObject) GeneratedSerializers.MyAwesomeStruct.Pack(value, CrystalJsonSettings.Json));
			}

			/// <inheritdoc />
			public global::Doxense.Serialization.Json.Tests.MyAwesomeStruct ToValue() => GeneratedSerializers.MyAwesomeStruct.Unpack(m_obj);

			/// <inheritdoc />
			public JsonValue ToJson() => m_obj;

			/// <inheritdoc />
			public MyAwesomeStructReadOnly ToReadOnly() => new (m_obj.ToReadOnly());

			/// <inheritdoc />
			void IJsonSerializable.JsonSerialize(CrystalJsonWriter writer) => m_obj.JsonSerialize(writer);

			/// <inheritdoc />
			JsonValue IJsonPackable.JsonPack(CrystalJsonSettings settings, ICrystalJsonTypeResolver resolver) => settings.IsReadOnly() ? m_obj.ToReadOnly() : m_obj;

			/// <inheritdoc cref="MyAwesomeStruct.Id" />
			public string Id
			{
				get => m_obj.Get<string>("id");
				set => m_obj["id"] = JsonString.Return(value);
			}

			/// <inheritdoc cref="MyAwesomeStruct.Level" />
			public int Level
			{
				get => m_obj.Get<int>("level");
				set => m_obj["level"] = JsonNumber.Return(value);
			}

			/// <inheritdoc cref="MyAwesomeStruct.Path" />
			public JsonPath Path
			{
				get => m_obj.Get<JsonPath>("path");
				set => m_obj.Set<JsonPath>("path", value);
			}

			/// <inheritdoc cref="MyAwesomeStruct.Disabled" />
			public bool? Disabled
			{
				get => m_obj.Get<bool?>("disabled", default!);
				set => m_obj["disabled"] = JsonBoolean.Return(value);
			}

		}

		#endregion

		#region MyAwesomeDevice ...

		/// <summary>JSON converter for type <see cref="Doxense.Serialization.Json.Tests.MyAwesomeDevice">MyAwesomeDevice</see></summary>
		public static MyAwesomeDeviceJsonConverter MyAwesomeDevice => m_cachedMyAwesomeDevice ??= new();

		private static MyAwesomeDeviceJsonConverter? m_cachedMyAwesomeDevice;

		/// <summary>Converts instances of type <see cref="T:Doxense.Serialization.Json.Tests.MyAwesomeDevice">MyAwesomeDevice</see> to and from JSON.</summary>
		[global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
		[global::System.CodeDom.Compiler.GeneratedCode("CrystalJsonSourceGenerator", "0.1")]
		[global::System.Diagnostics.DebuggerNonUserCode()]
		public sealed class MyAwesomeDeviceJsonConverter : IJsonConverter<MyAwesomeDevice>
		{

			#region Serialization...

			private static readonly JsonEncodedPropertyName _id = new("id");
			private static readonly JsonEncodedPropertyName _model = new("model");
			private static readonly JsonEncodedPropertyName _lastSeen = new("lastSeen");
			private static readonly JsonEncodedPropertyName _lastAddress = new("lastAddress");

			public void Serialize(CrystalJsonWriter writer, global::Doxense.Serialization.Json.Tests.MyAwesomeDevice? instance)
			{
				if (instance is null)
				{
					writer.WriteNull();
					return;
				}

				var state = writer.BeginObject();

				// string Id => "id"
				// fast!
				writer.WriteField(_id, instance.Id);

				// string Model => "model"
				// fast!
				writer.WriteField(_model, instance.Model);

				// DateTimeOffset? LastSeen => "lastSeen"
				// fast!
				writer.WriteField(_lastSeen, instance.LastSeen);

				// IPAddress LastAddress => "lastAddress"
				// unknown type
				writer.WriteField(_lastAddress, instance.LastAddress);

				writer.EndObject(state);
			}

			#endregion

			#region Packing...

			public JsonValue Pack(global::Doxense.Serialization.Json.Tests.MyAwesomeDevice? instance, CrystalJsonSettings? settings = default, ICrystalJsonTypeResolver? resolver = default)
			{
				if (instance is null)
				{
					return JsonNull.Null;
				}

				JsonValue? value;
				var readOnly = settings?.ReadOnly ?? false;
				var keepNulls = settings?.ShowNullMembers ?? false;

				var obj = new JsonObject(4);

				// string Id => "id"
				value = JsonString.Return(instance.Id);
				obj["id"] = value;

				// string Model => "model"
				value = JsonString.Return(instance.Model);
				obj["model"] = value;

				// DateTimeOffset? LastSeen => "lastSeen"
				// fast!
				{
					var tmp = instance.LastSeen;
					value = tmp.HasValue ? JsonDateTime.Return(tmp.Value) : null;
					if (keepNulls || value is not null or JsonNull)
					{
						obj["lastSeen"] = value;
					}
				}

				// IPAddress LastAddress => "lastAddress"
				value = JsonValue.FromValue<global::System.Net.IPAddress>(instance.LastAddress);
				if (keepNulls || value is not null or JsonNull)
				{
					obj["lastAddress"] = value;
				}
				if (readOnly)
				{
					return FreezeUnsafe(obj);
				}

				return obj;
			}

			#endregion

			#region Deserialization...

			// Id { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<Id>k__BackingField")]
			private static extern ref string IdAccessor(global::Doxense.Serialization.Json.Tests.MyAwesomeDevice instance);

			// Model { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<Model>k__BackingField")]
			private static extern ref string ModelAccessor(global::Doxense.Serialization.Json.Tests.MyAwesomeDevice instance);

			// LastSeen { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<LastSeen>k__BackingField")]
			private static extern ref global::System.DateTimeOffset? LastSeenAccessor(global::Doxense.Serialization.Json.Tests.MyAwesomeDevice instance);

			// LastAddress { get; init; }
			[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "<LastAddress>k__BackingField")]
			private static extern ref global::System.Net.IPAddress? LastAddressAccessor(global::Doxense.Serialization.Json.Tests.MyAwesomeDevice instance);

			public global::Doxense.Serialization.Json.Tests.MyAwesomeDevice Unpack(JsonValue value, ICrystalJsonTypeResolver? resolver = default)
			{
				var obj = value.AsObject();
				var instance = global::System.Activator.CreateInstance<global::Doxense.Serialization.Json.Tests.MyAwesomeDevice>();

				foreach (var kv in obj)
				{
					switch (kv.Key)
					{
						case "id": IdAccessor(instance) = kv.Value.RequiredField("Id").ToString(); break;
						case "model": ModelAccessor(instance) = kv.Value.RequiredField("Model").ToString(); break;
						case "lastSeen": LastSeenAccessor(instance) = kv.Value.ToDateTimeOffsetOrDefault(); break;
						case "lastAddress": LastAddressAccessor(instance) = kv.Value.As<global::System.Net.IPAddress>(resolver: resolver)!; break;
					}
				}

				return instance;
			}

			#endregion

		}

		/// <summary>Wraps a <see cref="JsonObject"/> into something that looks like a MyAwesomeDevice</summary>
		public readonly record struct MyAwesomeDeviceReadOnly : IJsonReadOnlyProxy<global::Doxense.Serialization.Json.Tests.MyAwesomeDevice, MyAwesomeDeviceReadOnly, MyAwesomeDeviceMutable>
		{

			/// <summary>JSON Object that is wrapped</summary>
			private readonly JsonObject m_obj;

			public MyAwesomeDeviceReadOnly(JsonValue value) => m_obj = value.AsObject();

			/// <inheritdoc />
			public static MyAwesomeDeviceReadOnly Create(JsonValue value, IJsonConverter<MyAwesomeDevice>? converter = null) => new(value.AsObject());

			/// <inheritdoc />
			public static MyAwesomeDeviceReadOnly Create(global::Doxense.Serialization.Json.Tests.MyAwesomeDevice value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null) => new(GeneratedSerializers.MyAwesomeDevice.Pack(value, settings.AsReadOnly(), resolver));

			/// <inheritdoc />
			public static IJsonConverter<MyAwesomeDevice> Converter => GeneratedSerializers.MyAwesomeDevice;

			/// <inheritdoc />
			public global::Doxense.Serialization.Json.Tests.MyAwesomeDevice ToValue() => GeneratedSerializers.MyAwesomeDevice.Unpack(m_obj);

			/// <inheritdoc />
			public JsonValue ToJson() => m_obj;

			/// <inheritdoc />
			public MyAwesomeDeviceMutable ToMutable() => new(m_obj.Copy());

			/// <inheritdoc />
			public MyAwesomeDeviceReadOnly With(Action<MyAwesomeDeviceMutable> modifier)
			{
				var copy = m_obj.Copy();
				modifier(new(copy));
				return new(FreezeUnsafe(copy));
			}

			/// <inheritdoc />
			void IJsonSerializable.JsonSerialize(CrystalJsonWriter writer) => m_obj.JsonSerialize(writer);

			/// <inheritdoc />
			JsonValue IJsonPackable.JsonPack(CrystalJsonSettings settings, ICrystalJsonTypeResolver resolver) => m_obj;

			/// <inheritdoc cref="MyAwesomeDevice.Id" />
			public string Id => m_obj.Get<string>("id");

			/// <inheritdoc cref="MyAwesomeDevice.Model" />
			public string Model => m_obj.Get<string>("model");

			/// <inheritdoc cref="MyAwesomeDevice.LastSeen" />
			public global::System.DateTimeOffset? LastSeen => m_obj.Get<global::System.DateTimeOffset?>("lastSeen", default!);

			/// <inheritdoc cref="MyAwesomeDevice.LastAddress" />
			public global::System.Net.IPAddress? LastAddress => m_obj.Get<global::System.Net.IPAddress?>("lastAddress", null);

		}

		/// <summary>Wraps a <see cref="JsonObject"/> into something that looks like a MyAwesomeDevice</summary>
		public sealed record MyAwesomeDeviceMutable : IJsonMutableProxy<global::Doxense.Serialization.Json.Tests.MyAwesomeDevice, MyAwesomeDeviceMutable, MyAwesomeDeviceReadOnly>
		{

			private readonly JsonObject m_obj;

			public MyAwesomeDeviceMutable(JsonValue value) => m_obj = value.AsObject();

			/// <inheritdoc />
			public static MyAwesomeDeviceMutable Create(JsonValue value, IJsonConverter<MyAwesomeDevice>? converter = null) => new(value.AsObject());

			/// <inheritdoc />
			public static MyAwesomeDeviceMutable Create(global::Doxense.Serialization.Json.Tests.MyAwesomeDevice value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null) => new(GeneratedSerializers.MyAwesomeDevice.Pack(value, settings.AsMutable(), resolver));

			/// <inheritdoc />
			public static IJsonConverter<MyAwesomeDevice> Converter => GeneratedSerializers.MyAwesomeDevice;

			/// <summary>Pack an instance of <see cref="global::Doxense.Serialization.Json.Tests.MyAwesomeDevice"/> into a mutable JSON proxy</summary>
			public static MyAwesomeDeviceMutable FromValue(global::Doxense.Serialization.Json.Tests.MyAwesomeDevice value)
			{
				global::Doxense.Diagnostics.Contracts.Contract.NotNull(value);
				return new((JsonObject) GeneratedSerializers.MyAwesomeDevice.Pack(value, CrystalJsonSettings.Json));
			}

			/// <inheritdoc />
			public global::Doxense.Serialization.Json.Tests.MyAwesomeDevice ToValue() => GeneratedSerializers.MyAwesomeDevice.Unpack(m_obj);

			/// <inheritdoc />
			public JsonValue ToJson() => m_obj;

			/// <inheritdoc />
			public MyAwesomeDeviceReadOnly ToReadOnly() => new (m_obj.ToReadOnly());

			/// <inheritdoc />
			void IJsonSerializable.JsonSerialize(CrystalJsonWriter writer) => m_obj.JsonSerialize(writer);

			/// <inheritdoc />
			JsonValue IJsonPackable.JsonPack(CrystalJsonSettings settings, ICrystalJsonTypeResolver resolver) => settings.IsReadOnly() ? m_obj.ToReadOnly() : m_obj;

			/// <inheritdoc cref="MyAwesomeDevice.Id" />
			public string Id
			{
				get => m_obj.Get<string>("id");
				set => m_obj["id"] = JsonString.Return(value);
			}

			/// <inheritdoc cref="MyAwesomeDevice.Model" />
			public string Model
			{
				get => m_obj.Get<string>("model");
				set => m_obj["model"] = JsonString.Return(value);
			}

			/// <inheritdoc cref="MyAwesomeDevice.LastSeen" />
			public global::System.DateTimeOffset? LastSeen
			{
				get => m_obj.Get<global::System.DateTimeOffset?>("lastSeen", default!);
				set => m_obj["lastSeen"] = JsonDateTime.Return(value);
			}

			/// <inheritdoc cref="MyAwesomeDevice.LastAddress" />
			public global::System.Net.IPAddress? LastAddress
			{
				get => m_obj.Get<global::System.Net.IPAddress?>("lastAddress", null);
				set => m_obj.Set<global::System.Net.IPAddress?>("lastAddress", value);
			}

		}

		#endregion

		#region Helpers...

		[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "FreezeUnsafe")]
		private static extern JsonObject FreezeUnsafe(JsonObject instance);

		[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "FreezeUnsafe")]
		private static extern JsonArray FreezeUnsafe(JsonArray instance);

		#endregion

	}

}

#endif
