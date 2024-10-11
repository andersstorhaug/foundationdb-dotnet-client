﻿#region Copyright (c) 2023-2024 SnowBank SAS, (c) 2005-2023 Doxense SAS
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 	* Redistributions of source code must retain the above copyright
// 	  notice, this list of conditions and the following disclaimer.
// 	* Redistributions in binary form must reproduce the above copyright
// 	  notice, this list of conditions and the following disclaimer in the
// 	  documentation and/or other materials provided with the distribution.
// 	* Neither the name of SnowBank nor the
// 	  names of its contributors may be used to endorse or promote products
// 	  derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL SNOWBANK SAS BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

//#define DEBUG_JSON_PARSER

// ReSharper disable ArrangeStaticMemberQualifier

namespace Doxense.Serialization.Json
{
	using System.Buffers;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using System.Reflection;
	using System.Runtime.CompilerServices;
	using System.Text;
	using Doxense.Collections.Caching;
	using Doxense.Runtime;

	/// <summary>Helper class to serialize, parse or deserialize JSON documents</summary>
	[PublicAPI]
	[DebuggerNonUserCode]
	public static class CrystalJson
	{

		public static readonly CrystalJsonTypeResolver DefaultResolver = new();

		public static readonly UTF8Encoding Utf8NoBom = CrystalJsonFormatter.Utf8NoBom;

		public static void Warmup()
		{
			PlatformHelpers.PreJit(
				typeof(CrystalJsonSettings), typeof(CrystalJsonNodaPatterns),
				typeof(JsonNull), typeof(JsonBoolean), typeof(JsonString), typeof(JsonNumber), typeof(JsonArray), typeof(JsonObject), typeof(JsonNull), typeof(JsonValue), typeof(JsonDateTime),
				typeof(CrystalJsonVisitor), typeof(CrystalJsonTypeVisitor), 
				typeof(CrystalJsonStreamReader), typeof(CrystalJsonStreamWriter), typeof(CrystalJsonParser), typeof(CrystalJsonDomWriter), typeof(CrystalJsonFormatter),
				typeof(CrystalJson)
			);
		}

		[Flags]
		public enum SaveOptions
		{
			None = 0,
			/// <summary>If the file already exists, save first into a temporary file, and swap it with the previous one in a single step</summary>
			AtomicSave = 1,
			/// <summary>If the file already exists, a backup copy will be created (with the ".bak" extension)</summary>
			KeepBackup = 2,
			/// <summary>Append to the end of the file (create it if necessary), instead of overwriting it. Should only be used to JSON fragments, or JSON logs</summary>
			Append = 4,
		}

		[Flags]
		public enum LoadOptions
		{
			None = 0,
			/// <summary>If the file does not exist, return the default value of the type (null, 0, false, ...)</summary>
			ReturnNullIfMissing = 1,
			/// <summary>If the source is using streaming (socket, ...), do not wait to reach the end of the file, and stop once a complete top-level value as been consumed.</summary>
			Streaming = 2
		}

		#region Serialization...

		/// <summary>Serializes a boxed value (of any type)</summary>
		/// <param name="value">Instance to serialize (can be null)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns><c>`123`</c>, <c>`true`</c>, <c>`"ABC"`</c>, <c>`{ "foo":..., "bar": ... }`</c>, <c>`[ ... ]`</c>, ...</returns>
		/// <exception cref="Doxense.Serialization.Json.JsonSerializationException">If the object fails to serialize properly (non-serializable type, loop in the object graph, ...)</exception>
		[Pure]
		public static string Serialize(object? value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			return SerializeToString(value, typeof(object), settings, resolver);
		}

		/// <summary>Serializes a boxed value (of any type)</summary>
		/// <param name="value">Instance to serialize (can be null)</param>
		/// <param name="declaredType">Type of the field or property, as declared in the parent type.</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns><c>`123`</c>, <c>`true`</c>, <c>`"ABC"`</c>, <c>`{ "foo":..., "bar": ... }`</c>, <c>`[ ... ]`</c>, ...</returns>
		/// <exception cref="Doxense.Serialization.Json.JsonSerializationException">If the object fails to serialize properly (non-serializable type, loop in the object graph, ...)</exception>
		[Pure]
		public static string Serialize(object? value, Type declaredType, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			return SerializeToString(value, declaredType, settings, resolver);
		}

		/// <summary>Serializes a value (of any type)</summary>
		/// <param name="value">Instance to serialize (can be null)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns><c>`123`</c>, <c>`true`</c>, <c>`"ABC"`</c>, <c>`{ "foo":..., "bar": ... }`</c>, <c>`[ ... ]`</c>, ...</returns>
		/// <exception cref="Doxense.Serialization.Json.JsonSerializationException">If the object fails to serialize properly (non-serializable type, loop in the object graph, ...)</exception>
		[Pure]
		public static string SerializeJson(JsonValue value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			switch (value)
			{
				case null or JsonNull:
				{
					return "null";
				}
				case JsonString str:
				{
					return str.ToJson(settings);
				}
				case JsonBoolean b:
				{
					return b.Value ? JsonTokens.True : JsonTokens.False;
				}
				case JsonNumber n:
				{
					return n.ToJson(settings);
				}
				case JsonDateTime dt:
				{
					return dt.ToJson(settings);
				}
				default:
				{ // Object or Array
					var writer = WriterPool.Allocate();
					try
					{
						writer.Initialize(0, settings, resolver);

						value.JsonSerialize(writer);

						return writer.GetString();
					}
					finally
					{
						writer.Dispose();
						WriterPool.Free(writer);
					}
				}
			}
		}

		/// <summary>Serializes a value that implements <see cref="IJsonSerializable"/></summary>
		/// <param name="value">Instance to serialize (can be null)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns><c>`123`</c>, <c>`true`</c>, <c>`"ABC"`</c>, <c>`{ "foo":..., "bar": ... }`</c>, <c>`[ ... ]`</c>, ...</returns>
		/// <exception cref="Doxense.Serialization.Json.JsonSerializationException">If the object fails to serialize properly (non-serializable type, loop in the object graph, ...)</exception>
		public static string SerializeJson(IJsonSerializable? value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			if (value == null)
			{ // special case for null instances
				return JsonTokens.Null;
			}

			var writer = WriterPool.Allocate();
			try
			{
				writer.Initialize(0, settings, resolver);

				value.JsonSerialize(writer);

				return writer.GetString();
			}
			finally
			{
				writer.Dispose();
				WriterPool.Free(writer);
			}
		}

		/// <summary>Serializes a value of type <typeparamref name="T"/> into a string literal</summary>
		/// <param name="value">Instance to serialize (can be null)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns><c>`123`</c>, <c>`true`</c>, <c>`"ABC"`</c>, <c>`{ "foo":..., "bar": ... }`</c>, <c>`[ ... ]`</c>, ...</returns>
		/// <exception cref="Doxense.Serialization.Json.JsonSerializationException">If the object fails to serialize properly (non-serializable type, loop in the object graph, ...)</exception>
		[Pure]
		public static string Serialize<T>(T? value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			if (value == null)
			{ // special case for null instances
				return JsonTokens.Null;
			}

			var writer = WriterPool.Allocate();
			try
			{
				writer.Initialize(0, settings, resolver);

				CrystalJsonVisitor.VisitValue<T>(value, writer);

				return writer.GetString();
			}
			finally
			{
				writer.Dispose();
				WriterPool.Free(writer);
			}
		}

		/// <summary>Serializes a value of type <typeparamref name="T"/> into a string literal, using a customer serializer</summary>
		/// <param name="value">Instance to serialize (can be null)</param>
		/// <param name="serializer">Custom serializer for this type</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns><c>`123`</c>, <c>`true`</c>, <c>`"ABC"`</c>, <c>`{ "foo":..., "bar": ... }`</c>, <c>`[ ... ]`</c>, ...</returns>
		/// <exception cref="Doxense.Serialization.Json.JsonSerializationException">If the object fails to serialize properly (non-serializable type, loop in the object graph, ...)</exception>
		[Pure]
		public static string Serialize<T>(T? value, IJsonSerializer<T>? serializer, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			if (value == null)
			{ // special case for null instances
				return JsonTokens.Null;
			}

			var writer = WriterPool.Allocate();
			try
			{
				writer.Initialize(0, settings, resolver);

				if (serializer != null)
				{
					serializer.Serialize(writer, value);
				}
				else
				{
					CrystalJsonVisitor.VisitValue<T>(value, writer);
				}

				return writer.GetString();
			}
			finally
			{
				writer.Dispose();
				WriterPool.Free(writer);
			}
		}

		/// <summary>Performs a custom seriazation operation, using a pooled <see cref="CrystalJsonWriter"/>.</summary>
		/// <typeparam name="TState">Type of the state that is forwarded to <paramref name="handler"/></typeparam>
		/// <param name="state">State that is forwarded to <paramref name="handler"/></param>
		/// <param name="handler">Handler that receives a pooled writer, and forwarded <paramref name="state"/>, and will use the writer to produce some JSON and consume the result.</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <remarks>
		/// <para>The handler is expected to call any of the <see cref="CrystalJsonWriter.GetString"/>, <see cref="CrystalJsonWriter.GetUtf8Slice(ArrayPool{byte})"/> or similar methods, before returning.</para>
		/// <para>The handler *MUST NOT* expose the pooled writer to the outside! Doing this would break the application</para>
		/// </remarks>
		public static void Convert<TState>(TState state, Action<CrystalJsonWriter, TState> handler, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			Contract.NotNull(handler);

			var writer = WriterPool.Allocate();
			try
			{
				writer.Initialize(0, settings, resolver);

				handler(writer, state);
			}
			finally
			{
				writer.Dispose();
				WriterPool.Free(writer);
			}
		}

		/// <summary>Performs a custom serialization operation, using a pooled <see cref="CrystalJsonWriter"/>.</summary>
		/// <typeparam name="TState">Type of the state that is forwarded to <paramref name="handler"/></typeparam>
		/// <typeparam name="TResult">Type of the result returned by the handler</typeparam>
		/// <param name="state">State that is forwarded to <paramref name="handler"/></param>
		/// <param name="handler">Handler that receives a pooled writer, and forwarded <paramref name="state"/>, and will use the writer to produce some JSON and consume the result.</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>The result of the custom serialization (usually a <see cref="string"/>, <see cref="Slice"/> or any other value)</returns>
		/// <remarks>
		/// <para>The handler is expected to call any of the <see cref="CrystalJsonWriter.GetString()"/>, <see cref="CrystalJsonWriter.GetUtf8Slice(ArrayPool{byte})"/> or similar methods, to extract the content of the writer.</para>
		/// <para>The handler *MUST NOT* expose the pooled writer to the outside! Doing this would break the application</para>
		/// </remarks>
		public static TResult Convert<TState, TResult>(TState state, Func<CrystalJsonWriter, TState, TResult> handler, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			Contract.NotNull(handler);

			var writer = WriterPool.Allocate();
			try
			{
				writer.Initialize(0, settings, resolver);

				return handler(writer, state);
			}
			finally
			{
				writer.Dispose();
				WriterPool.Free(writer);
			}
		}

		/// <summary>Performs a custom seriazation operation, using a pooled <see cref="CrystalJsonWriter"/>.</summary>
		/// <typeparam name="TState">Type of the state that is forwarded to <paramref name="handler"/></typeparam>
		/// <param name="state">State that is forwarded to <paramref name="handler"/></param>
		/// <param name="handler">Handler that receives a pooled writer, and forwarded <paramref name="state"/>, and will use the writer to produce some JSON and consume the result.</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <remarks>
		/// <para>The handler is expected to call any of the <see cref="CrystalJsonWriter.GetString"/>, <see cref="CrystalJsonWriter.GetUtf8Slice(ArrayPool{byte})"/> or similar methods, before returning.</para>
		/// <para>The handler *MUST NOT* expose the pooled writer to the outside! Doing this would break the application</para>
		/// </remarks>
		public static async Task ConvertAsync<TState>(TState state, Func<CrystalJsonWriter, TState, Task> handler, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			Contract.NotNull(handler);

			var writer = WriterPool.Allocate();
			try
			{
				writer.Initialize(0, settings, resolver);

				await handler(writer, state).ConfigureAwait(false);
			}
			finally
			{
				writer.Dispose();
				WriterPool.Free(writer);
			}
		}

		/// <summary>Performs a custom serialization operation, using a pooled <see cref="CrystalJsonWriter"/>.</summary>
		/// <typeparam name="TState">Type of the state that is forwarded to <paramref name="handler"/></typeparam>
		/// <typeparam name="TResult">Type of the result returned by the handler</typeparam>
		/// <param name="state">State that is forwarded to <paramref name="handler"/></param>
		/// <param name="handler">Handler that receives a pooled writer, and forwarded <paramref name="state"/>, and will use the writer to produce some JSON and consume the result.</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>The result of the custom serialization (usually a <see cref="string"/>, <see cref="Slice"/> or any other value)</returns>
		/// <remarks>
		/// <para>The handler is expected to call any of the <see cref="CrystalJsonWriter.GetString"/>, <see cref="CrystalJsonWriter.GetUtf8Slice(ArrayPool{byte})"/> or similar methods, to extract the content of the writer.</para>
		/// <para>The handler *MUST NOT* expose the pooled writer to the outside! Doing this would break the application</para>
		/// </remarks>
		public static async Task<TResult> ConvertAsync<TState, TResult>(TState state, Func<CrystalJsonWriter, TState, Task<TResult>> handler, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			Contract.NotNull(handler);

			var writer = WriterPool.Allocate();
			try
			{
				writer.Initialize(0, settings, resolver);

				return await handler(writer, state).ConfigureAwait(false);
			}
			finally
			{
				writer.Dispose();
				WriterPool.Free(writer);
			}
		}

		/// <summary>Serializes a boxed value (of any type) into the specified buffer</summary>
		/// <param name="value">Instance to serialize (can be null)</param>
		/// <param name="buffer">Destination buffer (created automatically if null)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>The value of <paramref name="buffer"/>, for call chaining</returns>
		/// <exception cref="Doxense.Serialization.Json.JsonSerializationException">If the object fails to serialize properly (non-serializable type, loop in the object graph, ...)</exception>
		[Pure]
		public static StringBuilder Serialize(object? value, StringBuilder? buffer, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			if (value == null)
			{ // special case for null instances
				return buffer?.Append(JsonTokens.Null) ?? new StringBuilder(JsonTokens.Null);
			}

			// grab a new buffer if needed
			buffer ??= CreateBufferFromSettings(settings);

			var writer = WriterPool.Allocate();
			try
			{
				writer.Initialize(0, settings, resolver);

				CrystalJsonVisitor.VisitValue(value, typeof(object), writer);

				writer.CopyTo(buffer);
			}
			finally
			{
				writer.Dispose();
				WriterPool.Free(writer);
			}

			return buffer;
		}

		/// <summary>Creates a new empty buffer with the appropriate size</summary>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <returns>Empty StringBuilder, with an initial capacity that depends on the settings</returns>
		[Pure]
		private static StringBuilder CreateBufferFromSettings(CrystalJsonSettings? settings)
		{
			int capacity = settings?.OptimizeForLargeData == true ? 4096 : 512;
			return new StringBuilder(capacity);
		}

		/// <summary>Serializes a boxed value (of any type) into the specified buffer</summary>
		/// <param name="value">Class, struct, Enumerable, Nullable&lt;T&gt;, ...</param>
		/// <param name="declaredType"></param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		[Pure]
		private static string SerializeToString(object? value, Type declaredType, CrystalJsonSettings? settings, ICrystalJsonTypeResolver? resolver)
		{
			if (value == null)
			{ // special case for null instances
				return JsonTokens.Null;
			}

			var writer = WriterPool.Allocate();
			try
			{
				writer.Initialize(0, settings, resolver);

				CrystalJsonVisitor.VisitValue(value, declaredType, writer);

				return writer.GetString();
			}
			finally
			{
				writer.Dispose();
				WriterPool.Free(writer);
			}
		}

		/// <summary>Serializes a boxed value (of any type) into the specified output</summary>
		/// <param name="output">Output for the JSON document</param>
		/// <param name="value">Instance to serialize (of any type)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>The <paramref name="output"/> instance, for call chaining</returns>
		/// <exception cref="Doxense.Serialization.Json.JsonSerializationException">If the object fails to serialize properly (non-serializable type, loop in the object graph, ...)</exception>
		public static TextWriter SerializeTo(TextWriter output, object? value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			return SerializeToTextWriter(output, value, typeof(object), settings, resolver);
		}

		/// <summary>Serializes a value (of any type) into the specified output</summary>
		/// <param name="output">Output for the JSON document</param>
		/// <param name="value">Instance to serialize (of any type)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>The <paramref name="output"/> instance, for call chaining</returns>
		/// <exception cref="Doxense.Serialization.Json.JsonSerializationException">If the object fails to serialize properly (non-serializable type, loop in the object graph, ...)</exception>
		public static TextWriter SerializeTo<T>(TextWriter output, T value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			return SerializeToTextWriter(output, value, typeof(T), settings, resolver);
		}

		/// <summary>Serializes a value (of any type) into the specified stream</summary>
		/// <param name="output">Output for the JSON document</param>
		/// <param name="value">Instance to serialize (of any type)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <exception cref="Doxense.Serialization.Json.JsonSerializationException">If the object fails to serialize properly (non-serializable type, loop in the object graph, ...)</exception>
		public static void SerializeTo<T>(Stream output, T? value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			Contract.NotNull(output);

			SerializeToStream(output, value, typeof(T), settings, resolver);
		}

		/// <summary>Serializes a boxed <paramref name="value"/> (of any type) into the file at the specified <paramref name="path"/></summary>
		/// <param name="path">Path to the output file</param>
		/// <param name="value">Instance to serialize (of any type)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <param name="options">Save settings</param>
		/// <exception cref="Doxense.Serialization.Json.JsonSerializationException">If the object fails to serialize properly (non-serializable type, loop in the object graph, ...)</exception>
		public static void SaveTo(string path, object? value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null, SaveOptions options = SaveOptions.None)
		{
			SerializeAndSaveInternal(path, value, typeof(object), settings, resolver, options);
		}

		/// <summary>Serializes a <paramref name="value"/> (of any type) into the file at the specified <paramref name="path"/></summary>
		/// <param name="path">Path to the output file</param>
		/// <param name="value">Instance to serialize (of any type)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <param name="options">Save settings</param>
		/// <exception cref="Doxense.Serialization.Json.JsonSerializationException">If the object fails to serialize properly (non-serializable type, loop in the object graph, ...)</exception>
		public static void SaveTo<T>(string path, T? value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null, SaveOptions options = SaveOptions.None)
		{
			SerializeAndSaveInternal(path, value, typeof(T), settings, resolver, options);
		}

		/// <summary>Serializes a boxed value into an indented JSON string, suitable for humans (logging, troubleshooting, ...)</summary>
		[Pure]
		public static string Dump(object? value)
		{
			return Serialize(value, CrystalJsonSettings.JsonIndented);
		}

		/// <summary>Serializes a value into an indented JSON string, suitable for humans (logging, troubleshooting, ...)</summary>
		[Pure]
		public static string Dump<TValue>(TValue? value)
		{
			return Serialize(value, CrystalJsonSettings.JsonIndented);
		}

		/// <summary>Serializes a boxed value into an in-memory buffer</summary>
		/// <returns>Byte array that contains the resulting JSON document</returns>
		[Pure]
		public static byte[] ToBytes(object? value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			return ToSlice(value, settings, resolver).ToArray();
		}

		/// <summary>Serializes a value into an in-memory buffer</summary>
		/// <returns>Byte array that contains the resulting JSON document</returns>
		[Pure]
		public static byte[] ToBytes<T>(T? value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			return ToSlice<T>(value, settings, resolver).ToArray();
		}

		/// <summary>Serializes a value into an in-memory buffer</summary>
		/// <returns>Byte array that contains the resulting JSON document</returns>
		[Pure]
		public static byte[] ToBytes<T>(T? value, IJsonSerializer<T> serializer, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			return ToSlice<T>(value, serializer, settings, resolver).ToArray();
		}

		/// <summary>Serializes a boxed value into an UTF-8 encoded Slice</summary>
		/// <param name="value">Instance to serialize (of any type)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>Slice of memory that contains the utf-8 encoded JSON document</returns>
		/// <exception cref="Doxense.Serialization.Json.JsonSerializationException">if the serialization fails</exception>
		[Pure]
		public static Slice ToSlice(object? value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
			=> ToSlice(value, typeof(object), settings, resolver);

		private static ObjectPool<CrystalJsonWriter> WriterPool = new(() => new CrystalJsonWriter());

		/// <summary>Serializes a value into an UTF-8 encoded Slice</summary>
		/// <typeparam name="T">Advertized type of the instance.</typeparam>
		/// <param name="value">Instance to serialize (of any type)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>Slice of memory that contains the utf-8 encoded JSON document</returns>
		/// <remarks>
		/// <para>If <typeparamref name="T"/> is an interface or abstract class, or if <paramref name="value"/> is a derived type of <typeparamref name="T"/>, the serialized document may include an additional attribute with the original type name, which may not be recognized by other libraries or platforms.</para>
		/// </remarks>
		[Pure]
		public static Slice ToSlice<T>(T? value, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			var writer = WriterPool.Allocate();
			try
			{
				writer.Initialize(0, settings, resolver);

				CrystalJsonVisitor.VisitValue(value, writer);

				return writer.GetUtf8Slice();
			}
			finally
			{
				writer.Dispose();
				WriterPool.Free(writer);
			}
		}

		/// <summary>Serializes a JSON value into an UTF-8 encoded Slice</summary>
		/// <param name="value">Instance to serialize (of any type)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <returns>Slice of memory that contains the utf-8 encoded JSON document</returns>
		[Pure]
		public static Slice ToJsonSlice(JsonValue? value, CrystalJsonSettings? settings = null)
		{
			var writer = WriterPool.Allocate();
			try
			{
				writer.Initialize(0, settings, null);

				(value ?? JsonNull.Null).JsonSerialize(writer);

				return writer.GetUtf8Slice();
			}
			finally
			{
				writer.Dispose();
				WriterPool.Free(writer);
			}
		}

		/// <summary>Serializes a JSON value into an UTF-8 encoded Slice</summary>
		/// <param name="value">Instance to serialize (of any type)</param>
		/// <param name="pool">Pool used to allocate the content of the slice (use <see cref="ArrayPool{T}.Shared"/> if <see langword="null"/>)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if <see langword="null"/>)</param>
		/// <returns>Slice of memory that contains the utf-8 encoded JSON document</returns>
		[Pure]
		public static SliceOwner ToJsonSlice(JsonValue? value, ArrayPool<byte>? pool, CrystalJsonSettings? settings = null)
		{
			var writer = WriterPool.Allocate();
			try
			{
				writer.Initialize(0, settings, null);

				(value ?? JsonNull.Null).JsonSerialize(writer);

				return writer.GetUtf8Slice(pool);
			}
			finally
			{
				writer.Dispose();
				WriterPool.Free(writer);
			}
		}

		/// <summary>Serializes a value of type <typeparamref name="T"/> into a <see cref="Slice"/>, using a customer serializer</summary>
		/// <param name="serializer">Custom serializer</param>
		/// <param name="value">Instance to serialize (can be null)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns><c>`123`</c>, <c>`true`</c>, <c>`"ABC"`</c>, <c>`{ "foo":..., "bar": ... }`</c>, <c>`[ ... ]`</c>, ...</returns>
		/// <exception cref="JsonSerializationException">If the object fails to serialize properly (non-serializable type, loop in the object graph, ...)</exception>
		public static Slice ToSlice<T>(T? value, IJsonSerializer<T>? serializer, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			var writer = WriterPool.Allocate();
			try
			{
				writer.Initialize(0, settings, resolver);

				if (serializer != null)
				{
					serializer.Serialize(writer, value);
				}
				else
				{
					CrystalJsonVisitor.VisitValue(value, writer);
				}

				return writer.GetUtf8Slice();
			}
			finally
			{
				writer.Dispose();
				WriterPool.Free(writer);
			}
		}

		/// <summary>Serializes a value of type <typeparamref name="T"/> into a <see cref="SliceOwner"/> using the specified <see cref="ArrayPool{T}">pool</see></summary>
		/// <param name="value">Instance to serialize (can be null)</param>
		/// <param name="pool">Pool used to allocate the content of the slice (use <see cref="ArrayPool{T}.Shared"/> if <see langword="null"/>)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns><c>`123`</c>, <c>`true`</c>, <c>`"ABC"`</c>, <c>`{ "foo":..., "bar": ... }`</c>, <c>`[ ... ]`</c>, ...</returns>
		/// <exception cref="JsonSerializationException">If the object fails to serialize properly (non-serializable type, loop in the object graph, ...)</exception>
		/// <remarks>
		/// <para>The <see cref="SliceOwner"/> returned <b>MUST</b> be disposed; otherwise, the rented buffer will not be returned to the <paramref name="pool"/>.</para>
		/// </remarks>
		public static SliceOwner ToSlice<T>(T? value, ArrayPool<byte>? pool, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			var writer = WriterPool.Allocate();
			try
			{
				writer.Initialize(0, settings, resolver);

				CrystalJsonVisitor.VisitValue(value, writer);
				
				return writer.GetUtf8Slice(pool);
			}
			finally
			{
				writer.Dispose();
				WriterPool.Free(writer);
			}
		}

		/// <summary>Serializes a value of type <typeparamref name="T"/> into a <see cref="SliceOwner"/>, using a customer serializer, and the specified <see cref="ArrayPool{T}">pool</see></summary>
		/// <param name="value">Instance to serialize (can be null)</param>
		/// <param name="serializer">Custom serializer</param>
		/// <param name="pool">Pool used to allocate the content of the slice (use <see cref="ArrayPool{T}.Shared"/> if <see langword="null"/>)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns><c>`123`</c>, <c>`true`</c>, <c>`"ABC"`</c>, <c>`{ "foo":..., "bar": ... }`</c>, <c>`[ ... ]`</c>, ...</returns>
		/// <exception cref="JsonSerializationException">If the object fails to serialize properly (non-serializable type, loop in the object graph, ...)</exception>
		/// <remarks>
		/// <para>The <see cref="SliceOwner"/> returned <b>MUST</b> be disposed; otherwise, the rented buffer will not be returned to the <paramref name="pool"/>.</para>
		/// </remarks>
		public static SliceOwner ToSlice<T>(T? value, IJsonSerializer<T>? serializer, ArrayPool<byte>? pool, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			var writer = WriterPool.Allocate();
			try
			{
				writer.Initialize(0, settings, resolver);
				if (serializer != null)
				{
					serializer.Serialize(writer, value);
				}
				else if (value is JsonValue j)
				{
					j.JsonSerialize(writer);
				}
				else
				{
					CrystalJsonVisitor.VisitValue(value, writer);
				}

				return writer.GetUtf8Slice(pool);
			}
			finally
			{
				writer.Dispose();
				WriterPool.Free(writer);
			}
		}

		/// <summary>Serializes a boxed value into an UTF-8 encoded Slice</summary>
		/// <param name="value">Instance to serialize (of any type)</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <param name="pool">Pool used to allocate the content of the slice (use <see cref="ArrayPool{T}.Shared"/> if <see langword="null"/>)</param>
		/// <returns>Slice of memory that contains the utf-8 encoded JSON document</returns>
		/// <exception cref="Doxense.Serialization.Json.JsonSerializationException">if the serialization fails</exception>
		/// <remarks>
		/// <para>The <see cref="SliceOwner"/> returned <b>MUST</b> be disposed; otherwise, the rented buffer will not be returned to the <paramref name="pool"/>.</para>
		/// </remarks>
		[Pure]
		public static SliceOwner ToSlice(object? value, ArrayPool<byte>? pool, CrystalJsonSettings? settings, ICrystalJsonTypeResolver? resolver)
		{
			return ToSlice(value, typeof(object), pool, settings, resolver);
		}

		/// <summary>Serializes a boxed value into an UTF-8 encoded Slice</summary>
		/// <param name="value">Instance to serialize (of any type)</param>
		/// <param name="type">Advertized type of the instance, or <see langword="null"/> if it is not known.</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <param name="pool">Pool used to allocate the content of the slice (use <see cref="ArrayPool{T}.Shared"/> if <see langword="null"/>)</param>
		/// <returns>Slice of memory that contains the utf-8 encoded JSON document</returns>
		/// <exception cref="Doxense.Serialization.Json.JsonSerializationException">if the serialization fails</exception>
		/// <remarks>
		/// <para>The <see cref="SliceOwner"/> returned <b>MUST</b> be disposed; otherwise, the rented buffer will not be returned to the <paramref name="pool"/>.</para>
		/// <para>If <paramref name="type"/> is an interface or abstract class, or if <paramref name="value"/> is a derived type of <paramref name="type"/>, the serialized document may include an additional attribute with the original type name, which may not be recognized by other libraries or platforms.</para>
		/// </remarks>
		[Pure]
		public static SliceOwner ToSlice(object? value, Type? type, ArrayPool<byte>? pool, CrystalJsonSettings? settings, ICrystalJsonTypeResolver? resolver)
		{
			var writer = WriterPool.Allocate();
			try
			{
				writer.Initialize(0, settings, resolver);

				if (value is JsonValue j)
				{
					j.JsonSerialize(writer);
				}
				else
				{
					CrystalJsonVisitor.VisitValue(value, writer);
				}
				return writer.GetUtf8Slice(pool);
			}
			finally
			{
				writer.Dispose();
				WriterPool.Free(writer);
			}
		}

		/// <summary>Serializes a boxed value into an UTF-8 encoded Slice</summary>
		/// <param name="value">Instance to serialize (of any type)</param>
		/// <param name="type">Advertized type of the instance, or <see langword="null"/> if it is not known.</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>Slice of memory that contains the utf-8 encoded JSON document</returns>
		/// <exception cref="Doxense.Serialization.Json.JsonSerializationException">if the serialization fails</exception>
		/// <remarks>
		/// <para>If <paramref name="type"/> is an interface or abstract class, or if <paramref name="value"/> is a derived type of <paramref name="type"/>, the serialized document may include an additional attribute with the original type name, which may not be recognized by other libraries or platforms.</para>
		/// </remarks>
		[Pure]
		public static Slice ToSlice(object? value, Type? type, CrystalJsonSettings? settings, ICrystalJsonTypeResolver? resolver)
		{
			var writer = WriterPool.Allocate();
			try
			{
				writer.Initialize(0, settings, resolver);

				if (value is JsonValue j)
				{
					j.JsonSerialize(writer);
				}
				else
				{
					CrystalJsonVisitor.VisitValue(value, type ?? typeof(object), writer);
				}
				return writer.GetUtf8Slice();
			}
			finally
			{
				writer.Dispose();
				WriterPool.Free(writer);
			}
		}

		/// <summary>Serializes a boxed value to a <see cref="TextWriter"/></summary>
		/// <param name="output">Destination where the serialized JSON document will be written</param>
		/// <param name="value">Instance to serialize, can be <see langword="null"/></param>
		/// <param name="declaredType">Advertized type of the instance. Use <c>typeof(object)</c> is unknown.</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>The same instance as <paramref name="output"/></returns>
		private static TextWriter SerializeToTextWriter(TextWriter output, object? value, Type declaredType, CrystalJsonSettings? settings, ICrystalJsonTypeResolver? resolver)
		{
			Contract.NotNull(output);

			if (value == null)
			{ // cas spécial pour null
				output.Write(JsonTokens.Null);
			}
			else
			{
				var writer = WriterPool.Allocate();
				try
				{
					//REVIEW: what value for auto-flush ?
					writer.Initialize(output, 65536, settings, resolver);

					CrystalJsonVisitor.VisitValue(value, declaredType, writer);
					
					writer.Flush(last: true);
				}
				finally
				{
					writer.Dispose();
					WriterPool.Free(writer);
				}
			}
			return output;
		}

		/// <summary>Serializes a boxed value to a <see cref="Stream"/></summary>
		/// <param name="output">Destination where the serialized JSON document will be written</param>
		/// <param name="value">Instance to serialize, can be <see langword="null"/></param>
		/// <param name="declaredType">Advertized type of the instance. Use <c>typeof(object)</c> is unknown.</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>The same instance as <paramref name="output"/></returns>
		private static Stream SerializeToStream(Stream output, object? value, Type declaredType, CrystalJsonSettings? settings, ICrystalJsonTypeResolver? resolver)
		{
			Contract.NotNull(output);

			if (ReferenceEquals(output, Stream.Null))
			{
				return output;
			}

			if (value == null)
			{ // cas spécial pour null
				output.Write("null"u8);
			}
			else
			{
				var writer = WriterPool.Allocate();
				try
				{
					//REVIEW: what value for auto-flush ?
					writer.Initialize(output, 65536, settings, resolver);

					CrystalJsonVisitor.VisitValue(value, declaredType, writer);
					
					writer.Flush(last: true);
				}
				finally
				{
					writer.Dispose();
					WriterPool.Free(writer);
				}
			}
			return output;
		}

		/// <summary>Creates a new <see cref="StreamWriter"/> that can be used to serialize JSON into a file, with the specified settings</summary>
		/// <param name="path">Path to the output file</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <returns>Writer that will output bytes into the specified file</returns>
		/// <remarks>The stream will be tuned according to the serialization settings used, especially if planning to write a large amount of data.</remarks>
		private static Stream OpenJsonStream(string path, CrystalJsonSettings? settings)
		{
			var bufferSize = 0x400;
			if (settings != null && settings.OptimizeForLargeData)
			{
				bufferSize = 0x14000; // 80K
			}
			//note: le StreamWriter va allouer un char[bufferSize] et un byte[3*bufferSize + 3]!

			return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize << 2, FileOptions.SequentialScan | FileOptions.WriteThrough);
		}

		private static void SerializeAndSaveInternal(string path, object? value, Type declaredType, CrystalJsonSettings? settings, ICrystalJsonTypeResolver? resolver, SaveOptions options)
		{
			Contract.NotNullOrEmpty(path);
			path = Path.GetFullPath(path);

			string savePath = path;
			string? bakPath = null;

			if ((options & SaveOptions.Append) == SaveOptions.Append)
			{
				throw new NotSupportedException("Append save is not supported");
			}

			#region Settings...

			bool doAtomicUpdate = false;
			if (File.Exists(path))
			{ // A file with the same name already exists, we will need to swap or backup
				if ((options & SaveOptions.AtomicSave) == SaveOptions.AtomicSave)
				{
					doAtomicUpdate = true;
					savePath += ".new";
				}
				if ((options & SaveOptions.KeepBackup) == SaveOptions.KeepBackup)
				{
					bakPath = savePath + ".bak";
				}
			}
			else
			{ // No file with this name was found

				// this could be due to the fact that the folder does not exist
				string parent = Path.GetDirectoryName(path)!;
				Contract.Debug.Assert(parent != null);
				if (!Directory.Exists(parent))
				{
					// create the path!
					//REVIEW: TODO: should we have an option for this ? AutoCreatePath = true|false ?
					Directory.CreateDirectory(parent);
				}
			}

			#endregion

			if (doAtomicUpdate)
			{ // Replace the file using "atomic" semantics
			  // - Save to a temporary ".new" file (in the same folder)
			  // - If serialization fails, the ".new" file is deleted
			  // - Replace the previous file with the ".new" file
			  // - The previous file maybe be renamed into a ".bak" if the KeepBackup options is enabled.

				try
				{
					using (var output = OpenJsonStream(savePath, settings))
					{
						SerializeToStream(output, value, declaredType, settings, resolver);

						// some Stream implementations don't property Flush after the last byte, so we do an explicit flush here
						output.Flush();
					}
				}
				catch (Exception)
				{
					if (File.Exists(savePath)) File.Delete(savePath);
					throw;
				}

				// Swap the old and new files
				File.Replace(savePath, path, bakPath);
			}
			else if (bakPath != null)
			{ // Overwrite the previous file, but keep a backup of the previous file
				// - Rename the previous file into ".bak" (deleted any previous ".bak" file)
				// - Save the new data into the destination file
				// - In case of a serialization error, we try to rename the ".bak" into the original name (which _could_ fail!)

				bool swapped = false;
				try
				{
					File.Replace(savePath, bakPath, null);
					swapped = true;

					using (var output = OpenJsonStream(savePath, settings))
					{
						SerializeToStream(output, value, declaredType, settings, resolver);

						output.Flush();
					}
				}
				catch (Exception)
				{
					if (swapped)
					{ // Revert using the backup file!
						File.Replace(bakPath, savePath, null);
					}
					throw;
				}

			}
			else
			{ // The file does not exist on disk
				// - Save directly to disk
				// - In case of serialization error, delete the (incomplete) file

				try
				{
					using (var output = OpenJsonStream(savePath, settings))
					{
						SerializeToStream(output, value, declaredType, settings, resolver);
						output.Flush();
					}
				}
				catch (Exception)
				{
					if (File.Exists(savePath)) File.Delete(savePath);
					throw;
				}
			}
		}

		#endregion

		#region Parsing...

		/// <summary>Parses a JSON text literal, and returns the corresponding JSON value</summary>
		/// <param name="jsonText">JSON text document to parse</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <returns>Corresponding JSON value. If <paramref name="jsonText"/> is empty, will return <see cref="JsonNull.Missing"/></returns>
		/// <remarks>
		/// <para>The value may be mutable (for objects and arrays) and can be modified. If you require an immutable thread-safe value, please configure the <paramref name="settings"/> accordingly.</para>
		/// <para>If the result is always expected to be an Array or an Object, please call <see cref="JsonValueExtensions.AsArray"/> or <see cref="JsonValueExtensions.AsObject"/> on the result.</para>
		/// </remarks>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static JsonValue Parse(
#if NET8_0_OR_GREATER
			[StringSyntax("json")]
#endif
			string? jsonText,
			CrystalJsonSettings? settings = null
		)
		{
#if DEBUG_JSON_PARSER
			Debug.WriteLine("CrystalJson.Parse('" + jsonText + "', ...)");
#endif
			return ParseFromReader(new JsonStringReader(jsonText), settings);
		}

		/// <summary>Parses a JSON buffer, and returns the corresponding JSON value</summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <returns>Corresponding JSON value. If <paramref name="jsonBytes"/> is empty, will return <see cref="JsonNull.Missing"/></returns>
		/// <remarks>
		/// <para>The value may be mutable (for objects and arrays) and can be modified. If you require an immutable thread-safe value, please configure the <paramref name="settings"/> accordingly.</para>
		/// <para>If the result is always expected to be an Array or an Object, please call <see cref="JsonValueExtensions.AsArray"/> or <see cref="JsonValueExtensions.AsObject"/> on the result.</para>
		/// </remarks>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static JsonValue Parse(byte[]? jsonBytes, CrystalJsonSettings? settings = null)
		{
			return Parse(jsonBytes.AsSlice(), settings);
		}

		/// <summary>Parses a JSON buffer, and returns the corresponding JSON value</summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <returns>Corresponding JSON value. If <paramref name="jsonBytes"/> is empty, will return <see cref="JsonNull.Missing"/></returns>
		/// <remarks>
		/// <para>The value may be mutable (for objects and arrays) and can be modified. If you require an immutable thread-safe value, please configure the <paramref name="settings"/> accordingly.</para>
		/// <para>If the result is always expected to be an Array or an Object, please call <see cref="JsonValueExtensions.AsArray"/> or <see cref="JsonValueExtensions.AsObject"/> on the result.</para>
		/// </remarks>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static JsonValue Parse(Slice jsonBytes, CrystalJsonSettings? settings = null)
		{
			return ParseFromReader(new JsonSliceReader(jsonBytes), settings);
		}

		/// <summary>Parses a JSON buffer, and returns the corresponding JSON value</summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <returns>Corresponding JSON value. If <paramref name="jsonBytes"/> is empty, will return <see cref="JsonNull.Missing"/></returns>
		/// <remarks>
		/// <para>The value may be mutable (for objects and arrays) and can be modified. If you require an immutable thread-safe value, please configure the <paramref name="settings"/> accordingly.</para>
		/// <para>If the result is always expected to be an Array or an Object, please call <see cref="JsonValueExtensions.AsArray"/> or <see cref="JsonValueExtensions.AsObject"/> on the result.</para>
		/// </remarks>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		[Pure]
		public static JsonValue Parse(ReadOnlySpan<byte> jsonBytes, CrystalJsonSettings? settings = null)
		{
			unsafe
			{
				fixed (byte* first = jsonBytes)
				{
					return ParseFromReader(new JsonUnmanagedReader(first, jsonBytes.Length), settings);
				}
			}
		}

		/// <summary>Parses a JSON buffer, and returns the corresponding JSON value</summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <returns>Corresponding JSON value. If <paramref name="jsonBytes"/> is empty, will return <see cref="JsonNull.Missing"/></returns>
		/// <remarks>
		/// <para>The value may be mutable (for objects and arrays) and can be modified. If you require an immutable thread-safe value, please configure the <paramref name="settings"/> accordingly.</para>
		/// <para>If the result is always expected to be an Array or an Object, please call <see cref="JsonValueExtensions.AsArray"/> or <see cref="JsonValueExtensions.AsObject"/> on the result.</para>
		/// </remarks>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static JsonValue Parse(ReadOnlyMemory<byte> jsonBytes, CrystalJsonSettings? settings = null)
		{
			return Parse(jsonBytes.Span, settings);
		}

		/// <summary>Parses a JSON sequence of buffer, and returns the corresponding JSON value</summary>
		/// <param name="jsonBytes">Sequence of buffers containing the UTF-8 encoded JSON document to parse</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <returns>Corresponding JSON value. If <paramref name="jsonBytes"/> is empty, will return <see cref="JsonNull.Missing"/></returns>
		/// <remarks>
		/// <para>The value may be mutable (for objects and arrays) and can be modified. If you require an immutable thread-safe value, please configure the <paramref name="settings"/> accordingly.</para>
		/// <para>If the result is always expected to be an Array or an Object, please call <see cref="JsonValueExtensions.AsArray"/> or <see cref="JsonValueExtensions.AsObject"/> on the result.</para>
		/// </remarks>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		[Pure]
		public static JsonValue Parse(ref ReadOnlySequence<byte> jsonBytes, CrystalJsonSettings? settings = null)
		{
			if (jsonBytes.IsSingleSegment)
			{
				return Parse(jsonBytes.First.Span, settings);
			}

			//TODO: un reader de ReadOnlySequence<byte>?
			// en attendant, on va copier les data dans un buffer pooled...
			long len = jsonBytes.Length;
			if (len > int.MaxValue) throw new NotSupportedException("Cannot parse sequence of bytes larger than 2 GiB.");
			using (var scratch = MemoryPool<byte>.Shared.Rent((int) len))
			{
				var mem = scratch.Memory.Span;
				int p = 0;
				foreach (var chunk in jsonBytes)
				{
					chunk.Span.CopyTo(mem.Slice(p, chunk.Length));
					p += chunk.Length;
				}
				Contract.Debug.Assert(p == mem.Length);
				return Parse(mem, settings);
			}
		}

		/// <summary>Parses a JSON text literal, and returns the corresponding JSON value</summary>
		/// <param name="jsonText">JSON text document to parse</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <returns>Corresponding JSON value. If <paramref name="jsonText"/> is empty, will return <see cref="JsonNull.Missing"/></returns>
		/// <remarks>
		/// <para>The value may be mutable (for objects and arrays) and can be modified. If you require an immutable thread-safe value, please configure the <paramref name="settings"/> accordingly.</para>
		/// <para>If the result is always expected to be an Array or an Object, please call <see cref="JsonValueExtensions.AsArray"/> or <see cref="JsonValueExtensions.AsObject"/> on the result.</para>
		/// </remarks>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		[Pure]
		public static JsonValue Parse(ReadOnlySpan<char> jsonText, CrystalJsonSettings? settings = null)
		{
			unsafe
			{
				fixed (char* first = jsonText)
				{
					return ParseFromReader(new JsonCharReader(first, jsonText.Length), settings);
				}
			}
		}

		/// <summary>Parses a JSON text literal, and returns the corresponding JSON value</summary>
		/// <param name="jsonText">JSON text document to parse</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <returns>Corresponding JSON value. If <paramref name="jsonText"/> is empty, will return <see cref="JsonNull.Missing"/></returns>
		/// <remarks>
		/// <para>The value may be mutable (for objects and arrays) and can be modified. If you require an immutable thread-safe value, please configure the <paramref name="settings"/> accordingly.</para>
		/// <para>If the result is always expected to be an Array or an Object, please call <see cref="JsonValueExtensions.AsArray"/> or <see cref="JsonValueExtensions.AsObject"/> on the result.</para>
		/// </remarks>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static JsonValue Parse(ReadOnlyMemory<char> jsonText, CrystalJsonSettings? settings = null)
		{
			return Parse(jsonText.Span, settings);
		}

		/// <summary>Parses a JSON text literal sequence, and returns the corresponding JSON value</summary>
		/// <param name="jsonText">Sequence of buffers containing the JSON text document to parse</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <returns>Corresponding JSON value. If <paramref name="jsonText"/> is empty, will return <see cref="JsonNull.Missing"/></returns>
		/// <remarks>
		/// <para>The value may be mutable (for objects and arrays) and can be modified. If you require an immutable thread-safe value, please configure the <paramref name="settings"/> accordingly.</para>
		/// <para>If the result is always expected to be an Array or an Object, please call <see cref="JsonValueExtensions.AsArray"/> or <see cref="JsonValueExtensions.AsObject"/> on the result.</para>
		/// </remarks>
		/// <exception cref="FormatException">If the JSON document is not syntaxically correct.</exception>
		[Pure]
		public static JsonValue Parse(ref ReadOnlySequence<char> jsonText, CrystalJsonSettings? settings = null)
		{
			if (jsonText.IsSingleSegment)
			{
				return Parse(jsonText.First.Span, settings);
			}

			//TODO: un reader de ReadOnlySequence<char>?
			// en attendant, on va copier les data dans un buffer pooled...
			long len = jsonText.Length;
			if (len > int.MaxValue) throw new NotSupportedException("Cannot parse sequence of chars larger than 4 GiB.");
			using (var scratch = MemoryPool<char>.Shared.Rent((int) len))
			{
				var mem = scratch.Memory.Span;
				int p = 0;
				foreach (var chunk in jsonText)
				{
					chunk.Span.CopyTo(mem.Slice(p, chunk.Length));
					p += chunk.Length;
				}
				Contract.Debug.Assert(p == mem.Length);
				return Parse(mem, settings);
			}
		}

		/// <summary>Reads the content of a reader, and returns the corresponding JSON value</summary>
		/// <param name="reader">Instance from which to read the JSON text document</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <returns>Corresponding JSON value. If <paramref name="reader"/> is empty, will return <see cref="JsonNull.Missing"/></returns>
		/// <remarks>
		/// <para>The value may be mutable (for objects and arrays) and can be modified. If you require an immutable thread-safe value, please configure the <paramref name="settings"/> accordingly.</para>
		/// <para>If the result is always expected to be an Array or an Object, please call <see cref="JsonValueExtensions.AsArray"/> or <see cref="JsonValueExtensions.AsObject"/> on the result.</para>
		/// </remarks>
		/// <exception cref="FormatException">If the JSON document is not syntaxically correct.</exception>
		[Pure]
		public static JsonValue ParseFrom(TextReader reader, CrystalJsonSettings? settings = null)
		{
			Contract.NotNull(reader);
			return ParseFromReader(new JsonTextReader(reader), settings);
		}

		/// <summary>Reads the content of a reader, and returns the corresponding JSON value</summary>
		/// <param name="source">Instance from which to read the JSON text document</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <returns>Corresponding JSON value. If <paramref name="source"/> is empty, will return <see cref="JsonNull.Missing"/></returns>
		/// <remarks>
		/// <para>The value may be mutable (for objects and arrays) and can be modified. If you require an immutable thread-safe value, please configure the <paramref name="settings"/> accordingly.</para>
		/// <para>If the result is always expected to be an Array or an Object, please call <see cref="JsonValueExtensions.AsArray"/> or <see cref="JsonValueExtensions.AsObject"/> on the result.</para>
		/// </remarks>
		/// <exception cref="FormatException">If the JSON document is not syntaxically correct.</exception>
		[Pure]
		internal static JsonValue ParseFromReader<TReader>(TReader source, CrystalJsonSettings? settings = null)
			where TReader : struct, IJsonReader
		{
			Contract.NotNull(source);

			var tokenizer = default(CrystalJsonTokenizer<TReader>);
			try
			{
				tokenizer = new CrystalJsonTokenizer<TReader>(source, settings ?? CrystalJsonSettings.Json);
				return CrystalJsonParser<TReader>.ParseJsonValue(ref tokenizer) ?? JsonNull.Missing;
			}
			finally
			{
				tokenizer.Dispose();
			}
		}

		/// <summary>Reads the content of a file, and returns the corresponding JSON value</summary>
		/// <param name="path">Instance from which to read the JSON document</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="options">Options used for streaming operations.</param>
		/// <returns>Corresponding JSON value. If <paramref name="path"/> is empty, will return <see cref="JsonNull.Missing"/></returns>
		/// <remarks>
		/// <para>The value may be mutable (for objects and arrays) and can be modified. If you require an immutable thread-safe value, please configure the <paramref name="settings"/> accordingly.</para>
		/// <para>If the result is always expected to be an Array or an Object, please call <see cref="JsonValueExtensions.AsArray"/> or <see cref="JsonValueExtensions.AsObject"/> on the result.</para>
		/// </remarks>
		/// <exception cref="FormatException">If the JSON document is not syntaxically correct.</exception>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static JsonValue ParseFrom(string path, CrystalJsonSettings? settings = null, LoadOptions options = LoadOptions.None)
		{
			return LoadAndParseInternal(path, settings, options);
		}

		/// <summary>Reads the content of a file, and returns the corresponding JSON value</summary>
		/// <param name="source">Instance from which to read the JSON document</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <returns>Corresponding JSON value. If <paramref name="source"/> is empty, will return <see cref="JsonNull.Missing"/></returns>
		/// <remarks>
		/// <para>The value may be mutable (for objects and arrays) and can be modified. If you require an immutable thread-safe value, please configure the <paramref name="settings"/> accordingly.</para>
		/// <para>If the result is always expected to be an Array or an Object, please call <see cref="JsonValueExtensions.AsArray"/> or <see cref="JsonValueExtensions.AsObject"/> on the result.</para>
		/// </remarks>
		/// <exception cref="FormatException">If the JSON document is not syntaxically correct.</exception>
		[Pure]
		public static JsonValue ParseFrom(Stream source, CrystalJsonSettings? settings = null)
		{
			Contract.NotNull(source);

			using var reader = new StreamReader(source, Encoding.UTF8, true);

			return ParseFromReader(new JsonTextReader(reader), settings);
		}

		[Pure, ContractAnnotation("null => false")]
		public static bool MaybeJsonDocument(byte[]? jsonBytes)
		{
			return jsonBytes != null && MaybeJsonDocument(jsonBytes.AsSlice());
		}

		[Pure, ContractAnnotation("jsonBytes:null => false")]
		public static bool MaybeJsonDocument(byte[]? jsonBytes, int offset, int count)
		{
			return jsonBytes != null && MaybeJsonDocument(jsonBytes.AsSlice(offset, count));
		}

		/// <summary>Essayes de déterminer si le buffer contient un document JSON (object ou array)</summary>
		/// <param name="jsonBytes">Buffer contenant un document JSON (encodé en UTF-8 ou ASCII)</param>
		/// <returns>True si le document pourrait être du JSON (object "{...}" ou array "[...]")</returns>
		/// <remarks>Attention: L'heuristique ne garantit pas qu'il s'agit d'un document valide!</remarks>
		[Pure]
		public static bool MaybeJsonDocument(Slice jsonBytes)
		{
			if (jsonBytes.Count < 2) return false;

			// cela peut "null"
			if (jsonBytes.Count == 4
			 && jsonBytes[0] == 110 /*'n'*/
			 && jsonBytes[1] == 117 /*'u'*/
			 && jsonBytes[2] == 108 /*'l'*/
			 && jsonBytes[3] == 108 /*'l'*/)
				return true;

			// on recup le premier et dernier caractère valide (en skippant les espaces de chaque coté)
			int p = jsonBytes.Offset;
			int end = jsonBytes.Offset + jsonBytes.Count;
			char first = (char) jsonBytes.Array[p++];
			while (char.IsWhiteSpace(first) && p < end)
			{
				first = (char) jsonBytes.Array[p++];
			}

			p = end - 1;
			char last = (char) jsonBytes.Array[p--];
			while (char.IsWhiteSpace(last) && p >= jsonBytes.Offset)
			{
				last = (char) jsonBytes.Array[p--];
			}

			// il faut que ca commence par "{" ou "["
			return (first == '{' && last == '}') || (first == '[' && last == ']');
		}

		/// <summary>Crée un reader sur un fichier sur le disque</summary>
		/// <param name="path">Chemin du fichier à lire</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <returns>Reader prêt à lire depuis le fichier</returns>
		[Pure]
		private static StreamReader OpenJsonStreamReader(string path, CrystalJsonSettings? settings)
		{
			var bufferSize = 0x1000; // x4 = 16K
			if (settings != null && settings.OptimizeForLargeData)
			{
				bufferSize = 0x8000; // x4 = 128k
			}

			FileStream? fileStream = null;
			try
			{
				fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize << 2, FileOptions.SequentialScan);

				// note: C'est le StreamWriter va fermera le FileStream quand il sera Dispose()
				return new StreamReader(fileStream, Encoding.UTF8, true, bufferSize);
			}
			catch (Exception)
			{
				fileStream?.Dispose();
				throw;
			}
		}

		[Pure]
		private static JsonValue LoadAndParseInternal(string path, CrystalJsonSettings? settings, LoadOptions options)
		{
			Contract.NotNullOrEmpty(path);
			path = Path.GetFullPath(path);

			if (!File.Exists(path))
			{ // The file does not exist

				if ((options & LoadOptions.ReturnNullIfMissing) == LoadOptions.ReturnNullIfMissing)
				{ // Treat a FileNotFound as if it was present with a "null" value
					return JsonNull.Missing;
				}
				// 404'ed !
				throw new FileNotFoundException("Specified JSON file could not be found", path);
			}

			using var reader = OpenJsonStreamReader(path, settings);
			return ParseFromReader(new JsonTextReader(reader), settings);
		}

		#endregion

		#region Deserialization...

		#region Désérialisation directe...

		/// <summary><b>DO NOT USE!</b></summary>
		[Pure]
		[Obsolete("Please avoid doing untyped deserialization!")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object? DeserializeBoxed(string jsonText, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			return BindBoxed(Parse(jsonText, settings), null, resolver);
		}

		/// <summary><b>DO NOT USE!</b></summary>
		[Pure]
		[Obsolete("Please avoid doing untyped deserialization!")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object? BindBoxed(JsonValue? value, Type? type, ICrystalJsonTypeResolver? resolver = null)
		{
			return value == null ? null : (resolver ?? CrystalJson.DefaultResolver).BindJsonValue(type, value);
		}

		#endregion

		#region Désérialisation vers un type défini

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonText">JSON text document to parse</param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		/// <exception cref="InvalidOperationException">If the JSON document is <c>"null"</c></exception>
		[Pure]
		public static TValue Deserialize<TValue>(
#if NET8_0_OR_GREATER
			[StringSyntax("json")]
#endif
			string jsonText
		) where TValue : notnull
		{
			return Parse(jsonText).Required<TValue>(resolver: null);
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonText">JSON text document to parse</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		/// <exception cref="InvalidOperationException">If the JSON document is <c>"null"</c></exception>
		[Pure]
		public static TValue Deserialize<TValue>(
#if NET8_0_OR_GREATER
			[StringSyntax("json")]
#endif
			string jsonText,
			CrystalJsonSettings? settings,
			ICrystalJsonTypeResolver? resolver = null
		) where TValue : notnull
		{
			return Parse(jsonText, settings).Required<TValue>(resolver);
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonText">JSON text document to parse</param>
		/// <param name="serializer"></param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		/// <exception cref="InvalidOperationException">If the JSON document is <c>"null"</c></exception>
		[Pure]
		public static TValue Deserialize<TValue>(
#if NET8_0_OR_GREATER
			[StringSyntax("json")]
#endif
			string jsonText,
			IJsonDeserializer<TValue>? serializer,
			CrystalJsonSettings? settings = null,
			ICrystalJsonTypeResolver? resolver = null
		) where TValue : notnull
		{
			var parsed = Parse(jsonText, settings);
			if (serializer != null)
			{
				return serializer.Unpack(parsed, resolver);
			}
			else
			{
				return parsed.Required<TValue>(resolver);
			}
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonText">JSON text document to parse</param>
		/// <param name="defaultValue"></param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		/// <exception cref="InvalidOperationException">If the JSON document is <c>"null"</c></exception>
		[Pure]
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static TValue? Deserialize<TValue>(
#if NET8_0_OR_GREATER
			[StringSyntax("json")]
#endif
			string jsonText,
			TValue defaultValue)
		{
			return Parse(jsonText).As(defaultValue);
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonText">JSON text document to parse</param>
		/// <param name="defaultValue"></param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		/// <exception cref="InvalidOperationException">If the JSON document is <c>"null"</c></exception>
		[Pure]
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static TValue? Deserialize<TValue>(
#if NET8_0_OR_GREATER
			[StringSyntax("json")]
#endif
			string jsonText,
			TValue defaultValue,
			CrystalJsonSettings? settings,
			ICrystalJsonTypeResolver? resolver = null
		)
		{
			return Parse(jsonText, settings).As(defaultValue, resolver);
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		/// <exception cref="InvalidOperationException">If the JSON document is <c>"null"</c></exception>
		[Pure]
		public static TValue Deserialize<TValue>(byte[] jsonBytes) where TValue : notnull
		{
			return Parse(jsonBytes).Required<TValue>();
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		/// <exception cref="InvalidOperationException">If the JSON document is <c>"null"</c></exception>
		[Pure]
		public static TValue Deserialize<TValue>(byte[] jsonBytes, CrystalJsonSettings? settings, ICrystalJsonTypeResolver? resolver = null) where TValue : notnull
		{
			return Parse(jsonBytes, settings).Required<TValue>(resolver);
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <param name="defaultValue"></param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		/// <exception cref="InvalidOperationException">If the JSON document is <c>"null"</c></exception>
		[Pure]
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static TValue? Deserialize<TValue>(byte[] jsonBytes, TValue defaultValue)
		{
			return Parse(jsonBytes).As(defaultValue);
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <param name="defaultValue"></param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		/// <exception cref="InvalidOperationException">If the JSON document is <c>"null"</c></exception>
		[Pure]
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static TValue? Deserialize<TValue>(byte[] jsonBytes, TValue defaultValue, CrystalJsonSettings? settings, ICrystalJsonTypeResolver? resolver = null)
		{
			return Parse(jsonBytes, settings).As(defaultValue, resolver);
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		[Pure]
		public static TValue Deserialize<TValue>(Slice jsonBytes) where TValue : notnull
		{
			return Parse(jsonBytes).Required<TValue>(resolver: null);
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		[Pure]
		public static TValue Deserialize<TValue>(Slice jsonBytes, CrystalJsonSettings? settings, ICrystalJsonTypeResolver? resolver = null) where TValue : notnull
		{
			return Parse(jsonBytes, settings).Required<TValue>(resolver);
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <param name="defaultValue"></param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		[Pure]
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static TValue? Deserialize<TValue>(Slice jsonBytes, TValue defaultValue)
		{
			return Parse(jsonBytes).As(defaultValue);
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <param name="defaultValue"></param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		[Pure]
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static TValue? Deserialize<TValue>(Slice jsonBytes, TValue defaultValue, CrystalJsonSettings? settings, ICrystalJsonTypeResolver? resolver = null)
		{
			return Parse(jsonBytes, settings).As(defaultValue, resolver);
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		[Pure]
		public static TValue Deserialize<TValue>(ReadOnlySpan<byte> jsonBytes, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null) where TValue : notnull
		{
			return Parse(jsonBytes, settings).Required<TValue>(resolver);
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <param name="defaultValue"></param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		[Pure]
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> jsonBytes, TValue defaultValue)
		{
			return Parse(jsonBytes).As(defaultValue);
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <param name="defaultValue"></param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		[Pure]
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> jsonBytes, TValue defaultValue, CrystalJsonSettings? settings, ICrystalJsonTypeResolver? resolver = null)
		{
			return Parse(jsonBytes, settings).As(defaultValue, resolver);
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		/// <exception cref="InvalidOperationException">If the JSON document is <c>"null"</c></exception>
		[Pure]
		public static TValue Deserialize<TValue>(ReadOnlyMemory<byte> jsonBytes, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null) where TValue : notnull
		{
			return Parse(jsonBytes, settings).Required<TValue>(resolver);
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <param name="defaultValue"></param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		[Pure]
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static TValue? Deserialize<TValue>(ReadOnlyMemory<byte> jsonBytes, TValue defaultValue)
		{
			return Parse(jsonBytes).As(defaultValue);
		}

		/// <summary>De-serializes a JSON text literal into a value of type <typeparamref name="TValue"/></summary>
		/// <param name="jsonBytes">UTF-8 encoded JSON document to parse</param>
		/// <param name="defaultValue"></param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		[Pure]
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static TValue? Deserialize<TValue>(ReadOnlyMemory<byte> jsonBytes, TValue defaultValue, CrystalJsonSettings? settings, ICrystalJsonTypeResolver? resolver = null)
		{
			return Parse(jsonBytes, settings).As(defaultValue, resolver);
		}

		/// <summary>Deserializes the content of a source of JSON text into an instance of type <typeparamref name="TValue"/></summary>
		/// <param name="source">Source of text to parse</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		/// <exception cref="InvalidOperationException">If the JSON document is <c>"null"</c></exception>
		[Pure]
		public static TValue? LoadFrom<TValue>(TextReader source, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null) where TValue : notnull
		{
			return ParseFrom(source, settings).As<TValue?>(default, resolver);
		}

		/// <summary>Deserializes the content of a stream into an instance of type <typeparamref name="TValue"/></summary>
		/// <param name="source">File to read</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		/// <exception cref="InvalidOperationException">If the JSON document is <c>"null"</c></exception>
		[Pure]
		public static TValue? LoadFrom<TValue>(Stream source, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null) where TValue : notnull
		{
			Contract.NotNull(source);

			using var sr = new StreamReader(source, Encoding.UTF8, true);

			return ParseFromReader(new JsonTextReader(sr), settings).As<TValue?>(default, resolver);
		}

		/// <summary>Deserializes the content of a file on disk into an instance of type <typeparamref name="TValue"/></summary>
		/// <param name="path">Path to the file to read</param>
		/// <param name="settings">Serialization settings (use default JSON settings if null)</param>
		/// <param name="resolver">Custom type resolver (use default behavior if null)</param>
		/// <param name="options">Read options</param>
		/// <returns>Deserialized instance</returns>
		/// <exception cref="FormatException">If the JSON document is not syntactically correct.</exception>
		/// <exception cref="InvalidOperationException">If the JSON document is <c>"null"</c></exception>
		[Pure]
		public static TValue? LoadFrom<TValue>(string path, CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null, LoadOptions options = LoadOptions.None) where TValue : notnull
		{
			return LoadAndParseInternal(path, settings ?? CrystalJsonSettings.Json, options).As<TValue?>(default, resolver);
		}

		#endregion

		#endregion

		#region Helpers...

		public const long Ticks1970Jan1 = 621355968000000000; // = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

		/// <summary>Retourne le nombre de ticks JavaScript correspondant à une date</summary>
		internal static long DateToJavaScriptTicks(DateTime date)
		{
			long ticks = ((date != DateTime.MinValue && date != DateTime.MaxValue && date.Kind != DateTimeKind.Utc) ? date.ToUniversalTime() : date).Ticks;
			return (ticks - Ticks1970Jan1) / TimeSpan.TicksPerMillisecond;
		}

		internal static long DateToJavaScriptTicks(DateTimeOffset date)
		{
			return (date.UtcTicks - Ticks1970Jan1) / TimeSpan.TicksPerMillisecond;
		}

		/// <summary>Retourne la date correspondant à un nombre de ticks JavaScript</summary>
		internal static DateTime JavaScriptTicksToDate(long ticks)
		{
			return new DateTime((ticks * TimeSpan.TicksPerMillisecond) + Ticks1970Jan1, DateTimeKind.Utc);
		}

		/// <summary>Encode une chaîne en JSON, et append le résultat à un StringBuilder</summary>
		/// <param name="sb">Buffer où écrire le résultat</param>
		/// <param name="text">Chaîne à encoder</param>
		/// <returns>Le StringBuilder passé en paramètre (pour chainage)</returns>
		/// <remarks>Note: Ajoute "null" si text==null && includeQuotes==true</remarks>
		public static StringBuilder StringAppend(StringBuilder sb, string? text)
		{
			return JsonEncoding.Append(sb, text);
		}

		/// <summary>Test if the resolver is the default resolver (<see langword="true"/>) or a customized resolver (<see langword="false"/>)</summary>
		/// <param name="resolver"></param>
		/// <returns></returns>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool IsDefaultResolver([NotNullWhen(false)] ICrystalJsonTypeResolver? resolver)
		{
			return resolver == null || ReferenceEquals(resolver, DefaultResolver);
		}

		#endregion

		#region Error Handling...

		internal static class Errors
		{

			#region Serialization Errors...

			[Pure, MethodImpl(MethodImplOptions.NoInlining)]
			internal static JsonSerializationException Serialization_FailTooDeep(int depth, object? current) => new($"Reached maximum depth of {depth} while serializing child object of type '{current?.GetType().GetFriendlyName() ?? "<null>"}'. Top object is too complex to be serialized this way!");

			[Pure, MethodImpl(MethodImplOptions.NoInlining)]
			internal static InvalidOperationException Serialization_ObjectRecursionIsNotAllowed(IEnumerable<object?> visited, object? value, int depth) => new JsonSerializationException($"Object of type '{value?.GetType().FullName}' at depth {depth} already serialized before! Recursive object graphs not supported. Visited path: {string.Join(" <- ", visited.Select(v => v?.GetType().FullName ?? "<null>"))}");

			[Pure, MethodImpl(MethodImplOptions.NoInlining)]
			internal static JsonSerializationException Serialization_InternalDepthInconsistent() => new("Internal depth is inconsistent.");

			[Pure, MethodImpl(MethodImplOptions.NoInlining)]
			internal static JsonSerializationException Serialization_LeaveNotSameThanMark(int depth, object? current) => new($"De-synchronization of the visited object stack: Leave() was called with a different value of type '{current?.GetType().GetFriendlyName() ?? "<null>"}' than MarkVisited() at depth {depth}.");

			[Pure, MethodImpl(MethodImplOptions.NoInlining)]
			internal static JsonSerializationException Serialization_DoesNotKnowHowToSerializeType(Type type) => new($"Doesn't know how to serialize values of type '{type.GetFriendlyName()}'.");

			[Pure, MethodImpl(MethodImplOptions.NoInlining)]
			internal static JsonSerializationException Serialization_DoesNotKnowHowToSerializeNullableType(Type type) => new($"Doesn't know how to serialize Nullable type '{type.GetFriendlyName()}'.");

			[Pure, MethodImpl(MethodImplOptions.NoInlining)]
			internal static JsonSerializationException Serialization_CouldNotResolveTypeDefinition(Type type) => new($"Could not get the members list for type '{type.GetFriendlyName()}'.");

			[Pure, MethodImpl(MethodImplOptions.NoInlining)]
			internal static JsonSerializationException Serialization_StaticJsonSerializeMethodInvalidSignature(Type type, MethodInfo method) => new($"Static serialization method '{type.GetFriendlyName()}.{method.Name}' must take two parameters.");

			[Pure, MethodImpl(MethodImplOptions.NoInlining)]
			internal static JsonSerializationException Serialization_StaticJsonSerializeMethodInvalidFirstParam(Type type, MethodInfo method, Type prmType)
			{
#if DEBUG
				if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
#endif
				return new JsonSerializationException($"First parameter of static method '{type.GetFriendlyName()}.{method.Name}' must be assignable to type '{type.GetFriendlyName()}' (it was '{prmType.GetFriendlyName()}').");
			}

			[Pure, MethodImpl(MethodImplOptions.NoInlining)]
			internal static JsonSerializationException Serialization_StaticJsonSerializeMethodInvalidSecondParam(Type type, MethodInfo method, Type prmType)
			{
#if DEBUG
				if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
#endif
				return new JsonSerializationException($"Second parameter of static method '{type.GetFriendlyName()}.{method.Name}' must be a {nameof(CrystalJsonWriter)} object (it was '{prmType.GetFriendlyName()}').");
			}

			[Pure, MethodImpl(MethodImplOptions.NoInlining)]
			internal static JsonSerializationException Serialization_InstanceJsonPackMethodInvalidSignature(Type type, MethodInfo method)
			{
#if DEBUG
				if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
#endif
				return new JsonSerializationException($"Static serialization method '{type.GetFriendlyName()}.{method.Name}' must take two parameters.");
			}

			[Pure, MethodImpl(MethodImplOptions.NoInlining)]
			internal static JsonSerializationException Serialization_CouldNotGetDefaultValueForMember(Type type, MemberInfo info, Exception? error)
			{
				var memberType = info is PropertyInfo pi ? pi.PropertyType : info is FieldInfo fi ? fi.FieldType : typeof(object);
				return memberType.IsByRefLike
					? new JsonSerializationException($"Cannot serialize {(info is PropertyInfo ? "property" : "field")} {type.GetFriendlyName()}.{info.Name} with type {memberType.GetFriendlyName()}: ref-like types are NOT supported.", error)
					: new JsonSerializationException($"Cannot generate default value for {(info is PropertyInfo ? "property" : "field")} {type.GetFriendlyName()}.{info.Name} with type {memberType.GetFriendlyName()}.", error);
			}


			#endregion

			#region Parsing Errors...

			[Pure, MethodImpl(MethodImplOptions.NoInlining)]
			internal static JsonBindingException Parsing_CannotCastToJsonObject(JsonValue? value) => new($"Cannot parse JSON {(value ?? JsonNull.Missing).Type} as an Object.", value);

			[Pure, MethodImpl(MethodImplOptions.NoInlining)]
			internal static JsonBindingException Parsing_CannotCastToJsonArray(JsonValue? value) => new($"Cannot parse JSON {(value ?? JsonNull.Missing).Type} as an Array.", value);

			[Pure, MethodImpl(MethodImplOptions.NoInlining)]
			internal static JsonBindingException Parsing_CannotCastToJsonNumber(JsonValue? value) => new($"Cannot parse JSON {(value ?? JsonNull.Missing).Type} as a Number.", value);

			[Pure, MethodImpl(MethodImplOptions.NoInlining)]
			internal static JsonBindingException Parsing_CannotCastToJsonString(JsonValue? value) => new($"Cannot parse JSON {(value ?? JsonNull.Missing).Type} as a String.", value);

			#endregion

		}

		#endregion
	}

}
