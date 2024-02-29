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

namespace Doxense.Serialization.Json
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using Doxense.Diagnostics.Contracts;
	using JetBrains.Annotations;

	[PublicAPI]
	[DebuggerNonUserCode]
	public static class JsonValueExtensions
	{

		/// <summary>Test si une valeur JSON est null, ou équivalente à null</summary>
		/// <param name="value">Valeur JSON</param>
		/// <returns>True si <paramref name="value"/> est null, ou une instance de type <see cref="JsonNull"/></returns>
		[Pure, ContractAnnotation("null=>true"), MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNullOrMissing([NotNullWhen(false)] this JsonValue? value)
		{
			return value is (null or JsonNull);
		}

		/// <summary>Test si une valeur JSON est l'équivalent logique de 'missing'</summary>
		/// <param name="value">Valeur JSON</param>
		/// <returns>True si <paramref name="value"/> est null, ou égal à <see cref="JsonNull.Missing"/>.</returns>
		/// <remarks><see cref="JsonNull.Null"/> n'est pas considéré comme manquant (c'est un null explicite)</remarks>
		[Pure, ContractAnnotation("null=>true"), MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsMissing([NotNullWhen(false)] this JsonValue? value)
		{
			//note: JsonNull.Error est un singleton, donc on peut le comparer par référence!
			return ReferenceEquals(value, JsonNull.Missing);
		}


		/// <summary>Test si une valeur JSON est manquant pour cause d'une erreur de parsing</summary>
		/// <param name="value">Valeur JSON</param>
		/// <returns>True si <paramref name="value"/> est null, ou égal à <see cref="JsonNull.Error"/>.</returns>
		/// <remarks><see cref="JsonNull.Null"/> n'est pas considéré comme manquant (c'est un null explicite)</remarks>
		[Pure, ContractAnnotation("null=>true")]
		public static bool IsError([NotNullWhen(false)] this JsonValue? value)
		{
			//note: JsonNull.Error est un singleton, donc on peut le comparer par référence!
			return ReferenceEquals(value, JsonNull.Error);
		}

		/// <summary>Vérifie qu'une valeur JSON est bien présente</summary>
		/// <param name="value">Valeur JSON qui ne doit pas être null ou manquante</param>
		/// <returns>La valeur JSON si elle existe. Ou une exception si elle est null ou manquante</returns>
		/// <exception cref="InvalidOperationException">If the value is null or missing</exception>
		[ ContractAnnotation("null => halt"), MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static JsonValue Required(this JsonValue? value) => value is not (null or JsonNull) ? value : FailValueIsNullOrMissing();

		/// <summary>Vérifie qu'une valeur JSON est bien présente dans une array</summary>
		/// <param name="value">Valeur JSON qui ne doit pas être null ou manquante</param>
		/// <param name="index">Index dans l'array qui doit être présent</param>
		/// <returns>La valeur JSON si elle existe. Ou une exception si elle est null ou manquante</returns>
		/// <exception cref="InvalidOperationException">If the value is null or missing</exception>
		[Pure, ContractAnnotation("value:null => halt"), MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static JsonValue RequiredIndex(this JsonValue? value, int index, string? message = null) => value is not (null or JsonNull) ? value : FailIndexIsNullOrMissing(index, message);

		/// <summary>Vérifie qu'une valeur JSON est bien présente dans une array</summary>
		/// <param name="value">Valeur JSON qui ne doit pas être null ou manquante</param>
		/// <param name="index">Index dans l'array qui doit être présent</param>
		/// <returns>La valeur JSON si elle existe. Ou une exception si elle est null ou manquante</returns>
		/// <exception cref="InvalidOperationException">If the value is null or missing</exception>
		[Pure, ContractAnnotation("value:null => halt"), MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static JsonValue RequiredIndex(this JsonValue? value, Index index, string? message = null) => value is not (null or JsonNull) ? value : FailIndexIsNullOrMissing(index, message);

		/// <summary>Ensures that the value of a field in a JSON Object is not null or missing</summary>
		/// <param name="value">Value of the <paramref name="field"/> in the parent object.</param>
		/// <param name="field">Name of the field in the parent object.</param>
		/// <param name="message">Message of the exception thrown if the value is null or missing</param>
		/// <returns>The same value, if it is not null or missing; otherwise, an exception is thrown</returns>
		/// <exception cref="InvalidOperationException">If the value is null or missing</exception>
		[ContractAnnotation("value:null => halt"), MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static JsonValue RequiredField(this JsonValue? value, string field, string? message = null) => value is not (null or JsonNull) ? value : FailFieldIsNullOrMissing(field, message);

		/// <summary>Vérifie qu'une valeur JSON est bien présente</summary>
		/// <param name="value">Valeur JSON qui ne doit pas être null ou manquante</param>
		/// <param name="path">Chemin vers le champ qui doit être présent</param>
		/// <returns>La valeur JSON si elle existe. Ou une exception si elle est null ou manquante</returns>
		/// <exception cref="InvalidOperationException">If the value is null or missing</exception>
		[ContractAnnotation("value:null => halt"), MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static JsonValue RequiredPath(this JsonValue? value, string path) => value is not (null or JsonNull) ? value : FailPathIsNullOrMissing(path);

		/// <summary>Vérifie qu'une valeur JSON est bien présente</summary>
		/// <param name="value">Valeur JSON qui ne doit pas être null ou manquante</param>
		/// <returns>La valeur JSON si elle existe. Ou une exception si elle est null ou manquante</returns>
		/// <exception cref="InvalidOperationException">If the value is null or missing</exception>
		[ContractAnnotation("null => halt")]
		public static JsonArray Required(this JsonArray? value) => value ?? FailArrayIsNullOrMissing();

		/// <summary>Vérifie qu'une valeur JSON est bien présente</summary>
		/// <param name="value">Valeur JSON qui ne doit pas être null ou manquante</param>
		/// <returns>La valeur JSON si elle existe. Ou une exception si elle est null ou manquante</returns>
		/// <exception cref="InvalidOperationException">If the value is null or missing</exception>
		[ContractAnnotation("null => halt")]
		public static JsonObject Required(this JsonObject? value) => value ?? FailObjectIsNullOrMissing();

		[Pure]
		internal static InvalidOperationException ErrorValueIsNullOrMissing() => new("Required JSON value was null or missing.");

		[DoesNotReturn]
		internal static JsonValue FailValueIsNullOrMissing() => throw ErrorValueIsNullOrMissing();

		[DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
		internal static JsonArray FailArrayIsNullOrMissing() => throw new InvalidOperationException("Required JSON array was null or missing.");

		[DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
		internal static JsonArray FailValueIsNotAnArray(JsonValue value) => throw CrystalJson.Errors.Parsing_CannotCastToJsonArray(value.Type);

		[DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
		internal static JsonValue FailIndexIsNullOrMissing(int index, string? message = null) => throw new InvalidOperationException(message ?? $"Required JSON field at index {index} was null or missing.");

		[DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
		internal static JsonValue FailIndexIsNullOrMissing(Index index, string? message = null) => throw new InvalidOperationException(message ?? $"Required JSON field at index {index} was null or missing.");

		[Pure]
		internal static InvalidOperationException ErrorFieldIsNullOrMissing(string field, string? message) => new(message ?? $"Required JSON field '{field}' was null or missing.");

		[DoesNotReturn]
		internal static JsonValue FailFieldIsNullOrMissing(string field, string? message = null) => throw ErrorFieldIsNullOrMissing(field, message);

		[DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
		internal static JsonValue FailFieldIsEmpty(string field) => throw new InvalidOperationException($"Required JSON field '{field}' was empty.");

		[DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
		internal static JsonValue FailPathIsNullOrMissing(string path) => throw new InvalidOperationException($"Required JSON path '{path}' was null or missing.");

		#region ToStuff(...)

		/// <summary>Sérialise cette valeur JSON en texte le plus compact possible (pour du stockage)</summary>
		/// <remarks>Note: si le JSON doit être envoyés en HTTP ou sauvé sur disque, préférer <see cref="ToJsonBuffer(JsonValue)"/> ou <see cref="ToJsonBytes(JsonValue)"/></remarks>
		[Pure]
		public static string ToJsonCompact(this JsonValue? value) => value?.ToJson(CrystalJsonSettings.JsonCompact) ?? JsonTokens.Null;

		/// <summary>Sérialise cette valeur JSON en texte au format indenté (pratique pour des logs ou en mode debug)</summary>
		/// <remarks>Note: si le JSON doit être envoyés en HTTP ou sauvé sur disque, préférer <see cref="ToJsonBuffer(JsonValue)"/> ou <see cref="ToJsonBytes(JsonValue)"/></remarks>
		[Pure]
		public static string ToJsonIndented(this JsonValue? value) => value?.ToJson(CrystalJsonSettings.JsonIndented) ?? JsonTokens.Null;

		/// <summary>Sérialise cette valeur JSON en un tableau de bytes</summary>
		/// <returns>Buffer contenant le texte JSON encodé en UTF-8</returns>
		/// <remarks>A n'utiliser que si l'appelant veut absolument un tableau. Pour de l'IO, préférer <see cref="ToJsonBuffer(JsonValue)"/> qui permet d'éviter une copie inutile en mémoire</remarks>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] ToJsonBytes(this JsonValue? value) => CrystalJson.ToBytes(value);

		/// <summary>Sérialise cette valeur JSON en un tableau de bytes</summary>
		/// <returns>Buffer contenant le texte JSON encodé en UTF-8</returns>
		/// <remarks>A n'utiliser que si l'appelant veut absolument un tableau. Pour de l'IO, préférer <see cref="ToJsonBuffer(JsonValue, CrystalJsonSettings)"/> qui permet d'éviter une copie inutile en mémoire</remarks>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] ToJsonBytes(this JsonValue? value, CrystalJsonSettings? settings) => CrystalJson.ToBytes(value, settings);

		/// <summary>Sérialise cette valeur JSON en un buffer de bytes</summary>
		/// <returns>Buffer contenant le texte JSON encodé en UTF-8</returns>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Slice ToJsonBuffer(this JsonValue? value) => CrystalJson.ToBuffer(value);

		/// <summary>Sérialise cette valeur JSON en un buffer de bytes</summary>
		/// <returns>Buffer contenant le texte JSON encodé en UTF-8</returns>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Slice ToJsonBuffer(this JsonValue? value, CrystalJsonSettings? settings) => CrystalJson.ToBuffer(value, settings);

		#endregion

		#region As<T>...

		/// <summary>Convert this value into a the specified CLR type.</summary>
		/// <typeparam name="TValue">Target CLR type</typeparam>
		/// <exception cref="JsonBindingException">If the value cannot be bound to the specified type.</exception>
		/// <remarks>If the value is <see langword="null"/> or "null-like", this will return the default <typeparam name="TValue"/> value (<see langword="0"/>, <see langword="false"/>, <see langword="null"/>, ...)</remarks>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Obsolete("Please use _As<T>(default) instead")]
		public static TValue? As<TValue>(this JsonValue? value) //BUGBUG: will become _REQUIRED_ !
		{
			if (value == null)
			{
				return default(TValue) == null ? JsonNull.Default<TValue>(value) : default;
			}

			#region <JIT_HACK>
			// pattern recognized and optimized by the JIT, only in Release build
#if !DEBUG
			if (typeof(TValue) == typeof(bool)) return (TValue) (object) value.ToBoolean();
			if (typeof(TValue) == typeof(char)) return (TValue) (object) value.ToChar();
			if (typeof(TValue) == typeof(byte)) return (TValue) (object) value.ToByte();
			if (typeof(TValue) == typeof(sbyte)) return (TValue) (object) value.ToSByte();
			if (typeof(TValue) == typeof(short)) return (TValue) (object) value.ToInt16();
			if (typeof(TValue) == typeof(ushort)) return (TValue) (object) value.ToUInt16();
			if (typeof(TValue) == typeof(int)) return (TValue) (object) value.ToInt32();
			if (typeof(TValue) == typeof(uint)) return (TValue) (object) value.ToUInt32();
			if (typeof(TValue) == typeof(long)) return (TValue) (object) value.ToInt64();
			if (typeof(TValue) == typeof(ulong)) return (TValue) (object) value.ToUInt64();
			if (typeof(TValue) == typeof(float)) return (TValue) (object) value.ToSingle();
			if (typeof(TValue) == typeof(double)) return (TValue) (object) value.ToDouble();
			if (typeof(TValue) == typeof(decimal)) return (TValue) (object) value.ToDecimal();
			if (typeof(TValue) == typeof(Guid)) return (TValue) (object) value.ToGuid();
			if (typeof(TValue) == typeof(Uuid128)) return (TValue) (object) value.ToUuid128();
			if (typeof(TValue) == typeof(Uuid96)) return (TValue) (object) value.ToUuid96();
			if (typeof(TValue) == typeof(Uuid80)) return (TValue) (object) value.ToUuid80();
			if (typeof(TValue) == typeof(Uuid64)) return (TValue) (object) value.ToUuid64();
			if (typeof(TValue) == typeof(TimeSpan)) return (TValue) (object) value.ToTimeSpan();
			if (typeof(TValue) == typeof(DateTime)) return (TValue) (object) value.ToDateTime();
			if (typeof(TValue) == typeof(DateTimeOffset)) return (TValue) (object) value.ToDateTimeOffset();
			if (typeof(TValue) == typeof(NodaTime.Instant)) return (TValue) (object) value.ToInstant();
			if (typeof(TValue) == typeof(NodaTime.Duration)) return (TValue) (object) value.ToDuration();

			//note: value peut être un JsonNull, donc on doit invoquer les ...OrDefault() !
			if (typeof(TValue) == typeof(bool?)) return (TValue?) (object?) value.ToBooleanOrDefault();
			if (typeof(TValue) == typeof(char?)) return (TValue?) (object?) value.ToCharOrDefault();
			if (typeof(TValue) == typeof(byte?)) return (TValue?) (object?) value.ToByteOrDefault();
			if (typeof(TValue) == typeof(sbyte?)) return (TValue?) (object?) value.ToSByteOrDefault();
			if (typeof(TValue) == typeof(short?)) return (TValue?) (object?) value.ToInt16OrDefault();
			if (typeof(TValue) == typeof(ushort?)) return (TValue?) (object?) value.ToUInt16OrDefault();
			if (typeof(TValue) == typeof(int?)) return (TValue?) (object?) value.ToInt32OrDefault();
			if (typeof(TValue) == typeof(uint?)) return (TValue?) (object?) value.ToUInt32OrDefault();
			if (typeof(TValue) == typeof(long?)) return (TValue?) (object?) value.ToInt64OrDefault();
			if (typeof(TValue) == typeof(ulong?)) return (TValue?) (object?) value.ToUInt64OrDefault();
			if (typeof(TValue) == typeof(float?)) return (TValue?) (object?) value.ToSingleOrDefault();
			if (typeof(TValue) == typeof(double?)) return (TValue?) (object?) value.ToDoubleOrDefault();
			if (typeof(TValue) == typeof(decimal?)) return (TValue?) (object?) value.ToDecimalOrDefault();
			if (typeof(TValue) == typeof(Guid?)) return (TValue?) (object?) value.ToGuidOrDefault();
			if (typeof(TValue) == typeof(Uuid128?)) return (TValue?) (object?) value.ToUuid128OrDefault();
			if (typeof(TValue) == typeof(Uuid96?)) return (TValue?) (object?) value.ToUuid96OrDefault();
			if (typeof(TValue) == typeof(Uuid80?)) return (TValue?) (object?) value.ToUuid80OrDefault();
			if (typeof(TValue) == typeof(Uuid64?)) return (TValue?) (object?) value.ToUuid64OrDefault();
			if (typeof(TValue) == typeof(TimeSpan?)) return (TValue?) (object?) value.ToTimeSpanOrDefault();
			if (typeof(TValue) == typeof(DateTime?)) return (TValue?) (object?) value.ToDateTimeOrDefault();
			if (typeof(TValue) == typeof(DateTimeOffset?)) return (TValue?) (object?) value.ToDateTimeOffsetOrDefault();
			if (typeof(TValue) == typeof(NodaTime.Instant?)) return (TValue?) (object?) value.ToInstantOrDefault();
			if (typeof(TValue) == typeof(NodaTime.Duration?)) return (TValue?) (object?) value.ToDurationOrDefault();
#endif
			#endregion </JIT_HACK>

			return value.Bind<TValue>();
		}

		/// <summary>Convert this value into a the specified CLR type.</summary>
		/// <typeparam name="TValue">Target CLR type</typeparam>
		/// <exception cref="JsonBindingException">If the value cannot be bound to the specified type.</exception>
		/// <remarks>If the value is <see langword="null"/> or "null-like", this will return the default <typeparam name="TValue"/> value (<see langword="0"/>, <see langword="false"/>, <see langword="null"/>, ...)</remarks>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Obsolete("Please use _As<T>(T, default) instead")]
		public static TValue? As<TValue>(this JsonValue? value, ICrystalJsonTypeResolver? resolver)
		{
			if (value == null)
			{
				return default(TValue) == null ? JsonNull.Default<TValue>(value) : default;
			}

			#region <JIT_HACK>

			// En mode RELEASE, le JIT reconnaît les patterns "if (typeof(T) == typeof(VALUETYPE)) { ... }" dans une méthode générique Foo<T> quand T est un ValueType,
			// et les remplace par des "if (true) { ...}" ce qui permet d'éliminer le reste du code (très efficace si le if contient un return!)
			// Egalement, le JIT optimise le "(VALUE_TYPE)(object)value" si T == VALUE_TYPE pour éviter le boxing inutile (le cast intermédiaire en object est pour faire taire le compilateur)
			// => pour le vérifier, il faut inspecter l'asm généré par le JIT au runtime (en mode release, en dehors du debugger, etc...) ce qui n'est pas facile...
			// => vérifié avec .NET 4.6.1 + RyuJIT x64, la méthode FromValue<int> est directement inlinée en l'appel à JsonNumber.Return(...) !

#if !DEBUG

			//note: si value est un JsonNull, toutes les versions de ToVALUETYPE() retourne le type attendu!

			if (typeof(TValue) == typeof(bool)) return (TValue) (object) value.ToBoolean();
			if (typeof(TValue) == typeof(char)) return (TValue) (object) value.ToChar();
			if (typeof(TValue) == typeof(byte)) return (TValue) (object) value.ToByte();
			if (typeof(TValue) == typeof(sbyte)) return (TValue) (object) value.ToSByte();
			if (typeof(TValue) == typeof(short)) return (TValue) (object) value.ToInt16();
			if (typeof(TValue) == typeof(ushort)) return (TValue) (object) value.ToUInt16();
			if (typeof(TValue) == typeof(int)) return (TValue) (object) value.ToInt32();
			if (typeof(TValue) == typeof(uint)) return (TValue) (object) value.ToUInt32();
			if (typeof(TValue) == typeof(long)) return (TValue) (object) value.ToInt64();
			if (typeof(TValue) == typeof(ulong)) return (TValue) (object) value.ToUInt64();
			if (typeof(TValue) == typeof(float)) return (TValue) (object) value.ToSingle();
			if (typeof(TValue) == typeof(double)) return (TValue) (object) value.ToDouble();
			if (typeof(TValue) == typeof(decimal)) return (TValue) (object) value.ToDecimal();
			if (typeof(TValue) == typeof(Guid)) return (TValue) (object) value.ToGuid();
			if (typeof(TValue) == typeof(Uuid128)) return (TValue) (object) value.ToUuid128();
			if (typeof(TValue) == typeof(Uuid96)) return (TValue) (object) value.ToUuid96();
			if (typeof(TValue) == typeof(Uuid80)) return (TValue) (object) value.ToUuid80();
			if (typeof(TValue) == typeof(Uuid64)) return (TValue) (object) value.ToUuid64();
			if (typeof(TValue) == typeof(TimeSpan)) return (TValue) (object) value.ToTimeSpan();
			if (typeof(TValue) == typeof(DateTime)) return (TValue) (object) value.ToDateTime();
			if (typeof(TValue) == typeof(DateTimeOffset)) return (TValue) (object) value.ToDateTimeOffset();
			if (typeof(TValue) == typeof(NodaTime.Instant)) return (TValue) (object) value.ToInstant();
			if (typeof(TValue) == typeof(NodaTime.Duration)) return (TValue) (object) value.ToDuration();

			//note: value peut être un JsonNull, donc on doit invoquer les ...OrDefault() !
			if (typeof(TValue) == typeof(bool?)) return (TValue?) (object?) value.ToBooleanOrDefault();
			if (typeof(TValue) == typeof(char?)) return (TValue?) (object?) value.ToCharOrDefault();
			if (typeof(TValue) == typeof(byte?)) return (TValue?) (object?) value.ToByteOrDefault();
			if (typeof(TValue) == typeof(sbyte?)) return (TValue?) (object?) value.ToSByteOrDefault();
			if (typeof(TValue) == typeof(short?)) return (TValue?) (object?) value.ToInt16OrDefault();
			if (typeof(TValue) == typeof(ushort?)) return (TValue?) (object?) value.ToUInt16OrDefault();
			if (typeof(TValue) == typeof(int?)) return (TValue?) (object?) value.ToInt32OrDefault();
			if (typeof(TValue) == typeof(uint?)) return (TValue?) (object?) value.ToUInt32OrDefault();
			if (typeof(TValue) == typeof(long?)) return (TValue?) (object?) value.ToInt64OrDefault();
			if (typeof(TValue) == typeof(ulong?)) return (TValue?) (object?) value.ToUInt64OrDefault();
			if (typeof(TValue) == typeof(float?)) return (TValue?) (object?) value.ToSingleOrDefault();
			if (typeof(TValue) == typeof(double?)) return (TValue?) (object?) value.ToDoubleOrDefault();
			if (typeof(TValue) == typeof(decimal?)) return (TValue?) (object?) value.ToDecimalOrDefault();
			if (typeof(TValue) == typeof(Guid?)) return (TValue?) (object?) value.ToGuidOrDefault();
			if (typeof(TValue) == typeof(Uuid128?)) return (TValue?) (object?) value.ToUuid128OrDefault();
			if (typeof(TValue) == typeof(Uuid96?)) return (TValue?) (object?) value.ToUuid96OrDefault();
			if (typeof(TValue) == typeof(Uuid80?)) return (TValue?) (object?) value.ToUuid80OrDefault();
			if (typeof(TValue) == typeof(Uuid64?)) return (TValue?) (object?) value.ToUuid64OrDefault();
			if (typeof(TValue) == typeof(TimeSpan?)) return (TValue?) (object?) value.ToTimeSpanOrDefault();
			if (typeof(TValue) == typeof(DateTime?)) return (TValue?) (object?) value.ToDateTimeOrDefault();
			if (typeof(TValue) == typeof(DateTimeOffset?)) return (TValue?) (object?) value.ToDateTimeOffsetOrDefault();
			if (typeof(TValue) == typeof(NodaTime.Instant?)) return (TValue?) (object?) value.ToInstantOrDefault();
			if (typeof(TValue) == typeof(NodaTime.Duration?)) return (TValue?) (object?) value.ToDurationOrDefault();
#endif

			#endregion </JIT_HACK>

			return value.Bind<TValue>(resolver);
		}

		/// <summary>Convert this value into a the specified CLR type.</summary>
		/// <typeparam name="TValue">Target CLR type</typeparam>
		/// <exception cref="JsonBindingException">If the value cannot be bound to the specified type.</exception>
		/// <remarks>If the value is <see langword="null"/> or "null-like", this will return the default <typeparam name="TValue"/> value (<see langword="0"/>, <see langword="false"/>, <see langword="null"/>, ...) if <paramref name="required"/> is <see langword="false"/>, or an exception if it is <see langword="true"/>.</remarks>
		[Pure, ContractAnnotation("required:true => notnull")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Obsolete("Please use _As<T>(T) if required, or As<T>(T, default) if optional")]
		public static TValue? As<TValue>(this JsonValue? value, bool required, ICrystalJsonTypeResolver? resolver = null)
		{
			if (value == null)
			{
				return required ? FailRequiredValueIsNullOrMissing<TValue>() : default(TValue) == null ? JsonNull.Default<TValue>(value) : default;
			}
			if (required && value.IsNull)
			{
				return FailRequiredValueIsNullOrMissing<TValue>();
			}

			#region <JIT_HACK>

			// En mode RELEASE, le JIT reconnaît les patterns "if (typeof(T) == typeof(VALUETYPE)) { ... }" dans une méthode générique Foo<T> quand T est un ValueType,
			// et les remplace par des "if (true) { ...}" ce qui permet d'éliminer le reste du code (très efficace si le if contient un return!)
			// Egalement, le JIT optimise le "(VALUE_TYPE)(object)value" si T == VALUE_TYPE pour éviter le boxing inutile (le cast intermédiaire en object est pour faire taire le compilateur)
			// => pour le vérifier, il faut inspecter l'asm généré par le JIT au runtime (en mode release, en dehors du debugger, etc...) ce qui n'est pas facile...
			// => vérifié avec .NET 4.6.1 + RyuJIT x64, la méthode FromValue<int> est directement inlinée en l'appel à JsonNumber.Return(...) !

#if !DEBUG // trop lent en debug !

			//note: si value est un JsonNull, toutes les versions de ToVALUETYPE() retourne le type attendu!

			if (typeof(TValue) == typeof(bool)) return (TValue) (object) value.ToBoolean();
			if (typeof(TValue) == typeof(char)) return (TValue) (object) value.ToChar();
			if (typeof(TValue) == typeof(byte)) return (TValue) (object) value.ToByte();
			if (typeof(TValue) == typeof(sbyte)) return (TValue) (object) value.ToSByte();
			if (typeof(TValue) == typeof(short)) return (TValue) (object) value.ToInt16();
			if (typeof(TValue) == typeof(ushort)) return (TValue) (object) value.ToUInt16();
			if (typeof(TValue) == typeof(int)) return (TValue) (object) value.ToInt32();
			if (typeof(TValue) == typeof(uint)) return (TValue) (object) value.ToUInt32();
			if (typeof(TValue) == typeof(long)) return (TValue) (object) value.ToInt64();
			if (typeof(TValue) == typeof(ulong)) return (TValue) (object) value.ToUInt64();
			if (typeof(TValue) == typeof(float)) return (TValue) (object) value.ToSingle();
			if (typeof(TValue) == typeof(double)) return (TValue) (object) value.ToDouble();
			if (typeof(TValue) == typeof(decimal)) return (TValue) (object) value.ToDecimal();
			if (typeof(TValue) == typeof(Guid)) return (TValue) (object) value.ToGuid();
			if (typeof(TValue) == typeof(Uuid128)) return (TValue) (object) value.ToUuid128();
			if (typeof(TValue) == typeof(Uuid96)) return (TValue) (object) value.ToUuid96();
			if (typeof(TValue) == typeof(Uuid80)) return (TValue) (object) value.ToUuid80();
			if (typeof(TValue) == typeof(Uuid64)) return (TValue) (object) value.ToUuid64();
			if (typeof(TValue) == typeof(TimeSpan)) return (TValue) (object) value.ToTimeSpan();
			if (typeof(TValue) == typeof(DateTime)) return (TValue) (object) value.ToDateTime();
			if (typeof(TValue) == typeof(DateTimeOffset)) return (TValue) (object) value.ToDateTimeOffset();
			if (typeof(TValue) == typeof(NodaTime.Instant)) return (TValue) (object) value.ToInstant();
			if (typeof(TValue) == typeof(NodaTime.Duration)) return (TValue) (object) value.ToDuration();

			//note: value peut être un JsonNull, donc on doit invoquer les ...OrDefault() !
			if (typeof(TValue) == typeof(bool?)) return (TValue?) (object?) value.ToBooleanOrDefault();
			if (typeof(TValue) == typeof(char?)) return (TValue?) (object?) value.ToCharOrDefault();
			if (typeof(TValue) == typeof(byte?)) return (TValue?) (object?) value.ToByteOrDefault();
			if (typeof(TValue) == typeof(sbyte?)) return (TValue?) (object?) value.ToSByteOrDefault();
			if (typeof(TValue) == typeof(short?)) return (TValue?) (object?) value.ToInt16OrDefault();
			if (typeof(TValue) == typeof(ushort?)) return (TValue?) (object?) value.ToUInt16OrDefault();
			if (typeof(TValue) == typeof(int?)) return (TValue?) (object?) value.ToInt32OrDefault();
			if (typeof(TValue) == typeof(uint?)) return (TValue?) (object?) value.ToUInt32OrDefault();
			if (typeof(TValue) == typeof(long?)) return (TValue?) (object?) value.ToInt64OrDefault();
			if (typeof(TValue) == typeof(ulong?)) return (TValue?) (object?) value.ToUInt64OrDefault();
			if (typeof(TValue) == typeof(float?)) return (TValue?) (object?) value.ToSingleOrDefault();
			if (typeof(TValue) == typeof(double?)) return (TValue?) (object?) value.ToDoubleOrDefault();
			if (typeof(TValue) == typeof(decimal?)) return (TValue?) (object?) value.ToDecimalOrDefault();
			if (typeof(TValue) == typeof(Guid?)) return (TValue?) (object?) value.ToGuidOrDefault();
			if (typeof(TValue) == typeof(Uuid128?)) return (TValue?) (object?) value.ToUuid128OrDefault();
			if (typeof(TValue) == typeof(Uuid96?)) return (TValue?) (object?) value.ToUuid96OrDefault();
			if (typeof(TValue) == typeof(Uuid80?)) return (TValue?) (object?) value.ToUuid80OrDefault();
			if (typeof(TValue) == typeof(Uuid64?)) return (TValue?) (object?) value.ToUuid64OrDefault();
			if (typeof(TValue) == typeof(TimeSpan?)) return (TValue?) (object?) value.ToTimeSpanOrDefault();
			if (typeof(TValue) == typeof(DateTime?)) return (TValue?) (object?) value.ToDateTimeOrDefault();
			if (typeof(TValue) == typeof(DateTimeOffset?)) return (TValue?) (object?) value.ToDateTimeOffsetOrDefault();
			if (typeof(TValue) == typeof(NodaTime.Instant?)) return (TValue?) (object?) value.ToInstantOrDefault();
			if (typeof(TValue) == typeof(NodaTime.Duration?)) return (TValue?) (object?) value.ToDurationOrDefault();
#endif

			#endregion </JIT_HACK>

			return value.Bind<TValue>(resolver);
		}

		[DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
		internal static T FailRequiredValueIsNullOrMissing<T>() => throw new InvalidOperationException($"Required JSON value of type {typeof(T).GetFriendlyName()} was null or missing");

		#endregion

		#region OrDefault...

		/// <summary>Convert this required JSON value into an instance of the specified type.</summary>
		/// <typeparam name="TValue">Target managed type</typeparam>
		/// <param name="value">JSON value to be converted</param>
		/// <param name="resolver">Optional type resolver used to bind the value into a managed CLR type (<see cref="CrystalJson.DefaultResolver"/> is omitted)</param>
		/// <exception cref="InvalidOperationException">If <paramref name="value"/> is <see langword="null"/> or <see cref="JsonNull">null-like</see></exception>
		/// <exception cref="JsonBindingException">If the value cannot be bound to the specified type.</exception>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TValue Required<TValue>(this JsonValue? value, ICrystalJsonTypeResolver? resolver = null) where TValue : notnull
		{
			if (value is null or JsonNull)
			{
				FailValueIsNullOrMissing();
			}

			#region <JIT_HACK>
			// pattern recognized and optimized by the JIT, only in Release build
#if !DEBUG
			// value types are safe because they can never be null
			if (typeof(TValue) == typeof(bool)) return (TValue) (object) value.ToBoolean();
			if (typeof(TValue) == typeof(char)) return (TValue) (object) value.ToChar();
			if (typeof(TValue) == typeof(byte)) return (TValue) (object) value.ToByte();
			if (typeof(TValue) == typeof(sbyte)) return (TValue) (object) value.ToSByte();
			if (typeof(TValue) == typeof(short)) return (TValue) (object) value.ToInt16();
			if (typeof(TValue) == typeof(ushort)) return (TValue) (object) value.ToUInt16();
			if (typeof(TValue) == typeof(int)) return (TValue) (object) value.ToInt32();
			if (typeof(TValue) == typeof(uint)) return (TValue) (object) value.ToUInt32();
			if (typeof(TValue) == typeof(long)) return (TValue) (object) value.ToInt64();
			if (typeof(TValue) == typeof(ulong)) return (TValue) (object) value.ToUInt64();
			if (typeof(TValue) == typeof(float)) return (TValue) (object) value.ToSingle();
			if (typeof(TValue) == typeof(double)) return (TValue) (object) value.ToDouble();
			if (typeof(TValue) == typeof(Half)) return (TValue) (object) value.ToHalf();
			if (typeof(TValue) == typeof(decimal)) return (TValue) (object) value.ToDecimal();
			if (typeof(TValue) == typeof(Guid)) return (TValue) (object) value.ToGuid();
			if (typeof(TValue) == typeof(Uuid128)) return (TValue) (object) value.ToUuid128();
			if (typeof(TValue) == typeof(Uuid96)) return (TValue) (object) value.ToUuid96();
			if (typeof(TValue) == typeof(Uuid80)) return (TValue) (object) value.ToUuid80();
			if (typeof(TValue) == typeof(Uuid64)) return (TValue) (object) value.ToUuid64();
			if (typeof(TValue) == typeof(TimeSpan)) return (TValue) (object) value.ToTimeSpan();
			if (typeof(TValue) == typeof(DateTime)) return (TValue) (object) value.ToDateTime();
			if (typeof(TValue) == typeof(DateTimeOffset)) return (TValue) (object) value.ToDateTimeOffset();
			if (typeof(TValue) == typeof(NodaTime.Instant)) return (TValue) (object) value.ToInstant();
			if (typeof(TValue) == typeof(NodaTime.Duration)) return (TValue) (object) value.ToDuration();
			// Nullable variants don't really make sense here since null will always throw.

#endif
			#endregion </JIT_HACK>

			if (default(TValue) != null)
			{ // value type
				return value.Bind<TValue>(resolver)!;
			}

			return value.Bind<TValue>(resolver)!;
		}

		/// <summary>Convert this value into a the specified CLR type, with a fallback value if it is null or missing.</summary>
		/// <typeparam name="TValue">Target CLR type</typeparam>
		/// <exception cref="JsonBindingException">If the value cannot be bound to the specified type.</exception>
		/// <remarks>If the value is <see langword="null"/> or "null-like", this will return the <paramref name="defaultValue"/>.</remarks>
		[Pure]
		public static TValue? OrDefault<TValue>(this JsonValue? value, ICrystalJsonTypeResolver? resolver = null)
		{
			if (value is null or JsonNull)
			{
				return default(TValue) == null ? JsonNull.Default<TValue>(value)! : default;
			}

			#region <JIT_HACK>
			// pattern recognized and optimized by the JIT, only in Release build
#if !DEBUG
			if (typeof(TValue) == typeof(bool) || typeof(TValue) == typeof(bool?)) return (TValue?) (object?) value.ToBooleanOrDefault();
			if (typeof(TValue) == typeof(char) || typeof(TValue) == typeof(char?)) return (TValue?) (object?) value.ToCharOrDefault();
			if (typeof(TValue) == typeof(byte) || typeof(TValue) == typeof(byte?)) return (TValue?) (object?) value.ToByteOrDefault();
			if (typeof(TValue) == typeof(sbyte) || typeof(TValue) == typeof(sbyte?)) return (TValue?) (object?) value.ToSByteOrDefault();
			if (typeof(TValue) == typeof(short) || typeof(TValue) == typeof(short?)) return (TValue?) (object?) value.ToInt16OrDefault();
			if (typeof(TValue) == typeof(ushort) || typeof(TValue) == typeof(ushort?)) return (TValue?) (object?) value.ToUInt16OrDefault();
			if (typeof(TValue) == typeof(int) || typeof(TValue) == typeof(int?)) return (TValue?) (object?) value.ToInt32OrDefault();
			if (typeof(TValue) == typeof(uint) || typeof(TValue) == typeof(uint?)) return (TValue?) (object?) value.ToUInt32OrDefault();
			if (typeof(TValue) == typeof(long) || typeof(TValue) == typeof(long?)) return (TValue?) (object?) value.ToInt64OrDefault();
			if (typeof(TValue) == typeof(ulong) || typeof(TValue) == typeof(ulong?)) return (TValue?) (object?) value.ToUInt64OrDefault();
			if (typeof(TValue) == typeof(float) || typeof(TValue) == typeof(float?)) return (TValue?) (object?) value.ToSingleOrDefault();
			if (typeof(TValue) == typeof(double) || typeof(TValue) == typeof(double?)) return (TValue?) (object?) value.ToDoubleOrDefault();
			if (typeof(TValue) == typeof(Half) || typeof(TValue) == typeof(Half?)) return (TValue?) (object?) value.ToHalfOrDefault();
			if (typeof(TValue) == typeof(decimal) || typeof(TValue) == typeof(decimal?)) return (TValue?) (object?) value.ToDecimalOrDefault();
			if (typeof(TValue) == typeof(Guid) || typeof(TValue) == typeof(Guid?)) return (TValue?) (object?) value.ToGuidOrDefault();
			if (typeof(TValue) == typeof(Uuid128) || typeof(TValue) == typeof(Uuid128?)) return (TValue?) (object?) value.ToUuid128OrDefault();
			if (typeof(TValue) == typeof(Uuid96) || typeof(TValue) == typeof(Uuid96?)) return (TValue?) (object?) value.ToUuid96OrDefault();
			if (typeof(TValue) == typeof(Uuid80) || typeof(TValue) == typeof(Uuid80?)) return (TValue?) (object?) value.ToUuid80OrDefault();
			if (typeof(TValue) == typeof(Uuid64) || typeof(TValue) == typeof(Uuid64?)) return (TValue?) (object?) value.ToUuid64OrDefault();
			if (typeof(TValue) == typeof(TimeSpan) || typeof(TValue) == typeof(TimeSpan?)) return (TValue?) (object?) value.ToTimeSpanOrDefault();
			if (typeof(TValue) == typeof(DateTime) || typeof(TValue) == typeof(DateTime?)) return (TValue?) (object?) value.ToDateTimeOrDefault();
			if (typeof(TValue) == typeof(DateTimeOffset) || typeof(TValue) == typeof(DateTimeOffset?)) return (TValue?) (object?) value.ToDateTimeOffsetOrDefault();
			if (typeof(TValue) == typeof(NodaTime.Instant) || typeof(TValue) == typeof(NodaTime.Instant?)) return (TValue?) (object?) value.ToInstantOrDefault();
			if (typeof(TValue) == typeof(NodaTime.Duration) || typeof(TValue) == typeof(NodaTime.Duration?)) return (TValue?) (object?) value.ToDurationOrDefault();
#endif
			#endregion </JIT_HACK>

			if (default(TValue) == null)
			{ // value type
				return value.Bind<TValue>()!;
			}

			return value.Bind<TValue>(resolver);
		}

		/// <summary>Convert this value into a the specified CLR type, with a fallback value if it is null or missing.</summary>
		/// <typeparam name="TValue">Target CLR type</typeparam>
		/// <exception cref="JsonBindingException">If the value cannot be bound to the specified type.</exception>
		/// <remarks>If the value is <see langword="null"/> or "null-like", this will return the <paramref name="defaultValue"/>.</remarks>
		[Pure]
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static TValue? OrDefault<TValue>(this JsonValue? value, TValue defaultValue, ICrystalJsonTypeResolver? resolver = null)
		{
			if (value is null or JsonNull)
			{
				return default(TValue) == null ? (defaultValue == null ? JsonNull.Default<TValue>(value)! : defaultValue) : defaultValue;
			}

			#region <JIT_HACK>
			// pattern recognized and optimized by the JIT, only in Release build
#if !DEBUG
			if (typeof(TValue) == typeof(bool)) return (TValue) (object) value.ToBooleanOrDefault((bool) (object) defaultValue!);
			if (typeof(TValue) == typeof(char)) return (TValue) (object) value.ToCharOrDefault((char) (object) defaultValue!);
			if (typeof(TValue) == typeof(byte)) return (TValue) (object) value.ToByteOrDefault((byte) (object) defaultValue!);
			if (typeof(TValue) == typeof(sbyte)) return (TValue) (object) value.ToSByteOrDefault((sbyte) (object) defaultValue!);
			if (typeof(TValue) == typeof(short)) return (TValue) (object) value.ToInt16OrDefault((short) (object) defaultValue!);
			if (typeof(TValue) == typeof(ushort)) return (TValue) (object) value.ToUInt16OrDefault((ushort) (object) defaultValue!);
			if (typeof(TValue) == typeof(int)) return (TValue) (object) value.ToInt32OrDefault((int) (object) defaultValue!);
			if (typeof(TValue) == typeof(uint)) return (TValue) (object) value.ToUInt32OrDefault((uint) (object) defaultValue!);
			if (typeof(TValue) == typeof(long)) return (TValue) (object) value.ToInt64OrDefault((long) (object) defaultValue!);
			if (typeof(TValue) == typeof(ulong)) return (TValue) (object) value.ToUInt64OrDefault((ulong) (object) defaultValue!);
			if (typeof(TValue) == typeof(float)) return (TValue) (object) value.ToSingleOrDefault((float) (object) defaultValue!);
			if (typeof(TValue) == typeof(double)) return (TValue) (object) value.ToDoubleOrDefault((double) (object) defaultValue!);
			if (typeof(TValue) == typeof(Half)) return (TValue) (object) value.ToHalfOrDefault((Half) (object) defaultValue!);
			if (typeof(TValue) == typeof(decimal)) return (TValue) (object) value.ToDecimalOrDefault((decimal) (object) defaultValue!);
			if (typeof(TValue) == typeof(Guid)) return (TValue) (object) value.ToGuidOrDefault((Guid) (object) defaultValue!);
			if (typeof(TValue) == typeof(Uuid128)) return (TValue) (object) value.ToUuid128OrDefault((Uuid128) (object) defaultValue!);
			if (typeof(TValue) == typeof(Uuid96)) return (TValue) (object) value.ToUuid96OrDefault((Uuid96) (object) defaultValue!);
			if (typeof(TValue) == typeof(Uuid80)) return (TValue) (object) value.ToUuid80OrDefault((Uuid80) (object) defaultValue!);
			if (typeof(TValue) == typeof(Uuid64)) return (TValue) (object) value.ToUuid64OrDefault((Uuid64) (object) defaultValue!);
			if (typeof(TValue) == typeof(TimeSpan)) return (TValue) (object) value.ToTimeSpanOrDefault((TimeSpan) (object) defaultValue!);
			if (typeof(TValue) == typeof(DateTime)) return (TValue) (object) value.ToDateTimeOrDefault((DateTime) (object) defaultValue!);
			if (typeof(TValue) == typeof(DateTimeOffset)) return (TValue) (object) value.ToDateTimeOffsetOrDefault((DateTimeOffset) (object) defaultValue!);
			if (typeof(TValue) == typeof(NodaTime.Instant)) return (TValue) (object) value.ToInstantOrDefault((NodaTime.Instant) (object) defaultValue!);
			if (typeof(TValue) == typeof(NodaTime.Duration)) return (TValue) (object) value.ToDurationOrDefault((NodaTime.Duration) (object) defaultValue!);
			//
			if (typeof(TValue) == typeof(bool?)) return (TValue?) (object?) value.ToBooleanOrDefault((bool?) (object?) defaultValue);
			if (typeof(TValue) == typeof(char?)) return (TValue?) (object?) value.ToCharOrDefault((char?) (object?) defaultValue);
			if (typeof(TValue) == typeof(byte?)) return (TValue?) (object?) value.ToByteOrDefault((byte?) (object?) defaultValue);
			if (typeof(TValue) == typeof(sbyte?)) return (TValue?) (object?) value.ToSByteOrDefault((sbyte?) (object?) defaultValue);
			if (typeof(TValue) == typeof(short?)) return (TValue?) (object?) value.ToInt16OrDefault((short?) (object?) defaultValue);
			if (typeof(TValue) == typeof(ushort?)) return (TValue?) (object?) value.ToUInt16OrDefault((ushort?) (object?) defaultValue);
			if (typeof(TValue) == typeof(int?)) return (TValue?) (object?) value.ToInt32OrDefault((int?) (object?) defaultValue);
			if (typeof(TValue) == typeof(uint?)) return (TValue?) (object?) value.ToUInt32OrDefault((uint?) (object?) defaultValue);
			if (typeof(TValue) == typeof(long?)) return (TValue?) (object?) value.ToInt64OrDefault((long?) (object?) defaultValue);
			if (typeof(TValue) == typeof(ulong?)) return (TValue?) (object?) value.ToUInt64OrDefault((ulong?) (object?) defaultValue);
			if (typeof(TValue) == typeof(float?)) return (TValue?) (object?) value.ToSingleOrDefault((float?) (object?) defaultValue);
			if (typeof(TValue) == typeof(double?)) return (TValue?) (object?) value.ToDoubleOrDefault((double?) (object?) defaultValue);
			if (typeof(TValue) == typeof(Half?)) return (TValue?) (object?) value.ToHalfOrDefault((Half?) (object?) defaultValue);
			if (typeof(TValue) == typeof(decimal?)) return (TValue?) (object?) value.ToDecimalOrDefault((decimal?) (object?) defaultValue);
			if (typeof(TValue) == typeof(Guid?)) return (TValue?) (object?) value.ToGuidOrDefault((Guid?) (object?) defaultValue);
			if (typeof(TValue) == typeof(Uuid128?)) return (TValue?) (object?) value.ToUuid128OrDefault((Uuid128?) (object?) defaultValue);
			if (typeof(TValue) == typeof(Uuid96?)) return (TValue?) (object?) value.ToUuid96OrDefault((Uuid96?) (object?) defaultValue);
			if (typeof(TValue) == typeof(Uuid80?)) return (TValue?) (object?) value.ToUuid80OrDefault((Uuid80?) (object?) defaultValue);
			if (typeof(TValue) == typeof(Uuid64?)) return (TValue?) (object?) value.ToUuid64OrDefault((Uuid64?) (object?) defaultValue);
			if (typeof(TValue) == typeof(TimeSpan?)) return (TValue?) (object?) value.ToTimeSpanOrDefault((TimeSpan?) (object?) defaultValue);
			if (typeof(TValue) == typeof(DateTime?)) return (TValue?) (object?) value.ToDateTimeOrDefault((DateTime?) (object?) defaultValue);
			if (typeof(TValue) == typeof(DateTimeOffset?)) return (TValue?) (object?) value.ToDateTimeOffsetOrDefault((DateTimeOffset?) (object?) defaultValue);
			if (typeof(TValue) == typeof(NodaTime.Instant?)) return (TValue?) (object?) value.ToInstantOrDefault((NodaTime.Instant?) (object?) defaultValue);
			if (typeof(TValue) == typeof(NodaTime.Duration?)) return (TValue?) (object?) value.ToDurationOrDefault((NodaTime.Duration?) (object?) defaultValue);
#endif
			#endregion </JIT_HACK>

			if (default(TValue) == null)
			{ // value type
				return value.Bind<TValue>()!;
			}

			return value.Bind<TValue>(resolver) ?? defaultValue;
		}

		/// <summary>Returns the converted value, or a fallback value if it is missing</summary>
		/// <param name="value">JSON value to convert</param>
		/// <param name="missingValue">Fallback value</param>
		/// <returns>The converted value, or <paramref name="missingValue"/> if it is <see langword="null"/> or missing</returns>
		[Pure]
		public static JsonValue OrDefault(this JsonValue? value, JsonValue? missingValue) => (value is JsonNull ? null : value) ?? missingValue ?? JsonNull.Missing;

		/// <summary>Returns the converted value, or a fallback value if it is missing</summary>
		/// <param name="value">JSON value to convert</param>
		/// <param name="missingValue">Fallback value</param>
		/// <returns>The converted value, or <paramref name="missingValue"/> if it is <see langword="null"/> or missing</returns>
		[Pure]
		public static string OrDefault(this JsonValue? value, string missingValue) => value?.ToStringOrDefault() ?? missingValue;

		/// <summary>Returns the converted value, or a fallback value if it is missing</summary>
		/// <param name="value">JSON value to convert</param>
		/// <param name="missingValue">Fallback value</param>
		/// <returns>The converted value, or <paramref name="missingValue"/> if it is <see langword="null"/> or missing</returns>
		[Pure]
		public static bool OrDefault(this JsonValue? value, bool missingValue) => value?.ToBooleanOrDefault() ?? missingValue;

		/// <summary>Returns the converted value, or a fallback value if it is missing</summary>
		/// <param name="value">JSON value to convert</param>
		/// <param name="missingValue">Fallback value</param>
		/// <returns>The converted value, or <paramref name="missingValue"/> if it is <see langword="null"/> or missing</returns>
		[Pure]
		public static int OrDefault(this JsonValue? value, int missingValue) => value?.ToInt32OrDefault() ?? missingValue;

		/// <summary>Returns the converted value, or a fallback value if it is missing</summary>
		/// <param name="value">JSON value to convert</param>
		/// <param name="missingValue">Fallback value</param>
		/// <returns>The converted value, or <paramref name="missingValue"/> if it is <see langword="null"/> or missing</returns>
		[Pure]
		public static long OrDefault(this JsonValue? value, long missingValue) => value?.ToInt64OrDefault() ?? missingValue;

		/// <summary>Returns the converted value, or a fallback value if it is missing</summary>
		/// <param name="value">JSON value to convert</param>
		/// <param name="missingValue">Fallback value</param>
		/// <returns>The converted value, or <paramref name="missingValue"/> if it is <see langword="null"/> or missing</returns>
		[Pure]
		public static double OrDefault(this JsonValue? value, double missingValue) => value?.ToDoubleOrDefault() ?? missingValue;

		/// <summary>Returns the converted value, or a fallback value if it is missing</summary>
		/// <param name="value">JSON value to convert</param>
		/// <param name="missingValue">Fallback value</param>
		/// <returns>The converted value, or <paramref name="missingValue"/> if it is <see langword="null"/> or missing</returns>
		[Pure]
		public static float OrDefault(this JsonValue? value, float missingValue) => value?.ToSingleOrDefault() ?? missingValue;

		/// <summary>Returns the converted value, or a fallback value if it is missing</summary>
		/// <param name="value">JSON value to convert</param>
		/// <param name="missingValue">Fallback value</param>
		/// <returns>The converted value, or <paramref name="missingValue"/> if it is <see langword="null"/> or missing</returns>
		[Pure]
		public static Half OrDefault(this JsonValue? value, Half missingValue) => value?.ToHalfOrDefault() ?? missingValue;

		/// <summary>Returns the converted value, or a fallback value if it is missing</summary>
		/// <param name="value">JSON value to convert</param>
		/// <param name="missingValue">Fallback value</param>
		/// <returns>The converted value, or <paramref name="missingValue"/> if it is <see langword="null"/> or missing</returns>
		[Pure]
		public static Guid OrDefault(this JsonValue? value, Guid missingValue) => value is not (null or JsonNull) ? value.ToGuid() : missingValue;

		/// <summary>Returns the converted value, or a fallback value if it is missing</summary>
		/// <param name="value">JSON value to convert</param>
		/// <param name="missingValue">Fallback value</param>
		/// <returns>The converted value, or <paramref name="missingValue"/> if it is <see langword="null"/> or missing</returns>
		[Pure]
		public static Uuid128 OrDefault(this JsonValue? value, Uuid128 missingValue) => value is not (null or JsonNull) ? value.ToUuid128() : missingValue;

		/// <summary>Returns the converted value, or a fallback value if it is missing</summary>
		/// <param name="value">JSON value to convert</param>
		/// <param name="missingValue">Fallback value</param>
		/// <returns>The converted value, or <paramref name="missingValue"/> if it is <see langword="null"/> or missing</returns>
		[Pure]
		public static Uuid64 OrDefault(this JsonValue? value, Uuid64 missingValue) => value is not (null or JsonNull) ? value.ToUuid64() : missingValue;

		/// <summary>Returns the converted value, or a fallback value if it is missing</summary>
		/// <param name="value">JSON value to convert</param>
		/// <param name="missingValue">Fallback value</param>
		/// <returns>The converted value, or <paramref name="missingValue"/> if it is <see langword="null"/> or missing</returns>
		[Pure]
		public static TimeSpan OrDefault(this JsonValue? value, TimeSpan missingValue) => value is not (null or JsonNull) ? value.ToTimeSpan() : missingValue;

		/// <summary>Returns the converted value, or a fallback value if it is missing</summary>
		/// <param name="value">JSON value to convert</param>
		/// <param name="missingValue">Fallback value</param>
		/// <returns>The converted value, or <paramref name="missingValue"/> if it is <see langword="null"/> or missing</returns>
		[Pure]
		public static DateTime OrDefault(this JsonValue? value, DateTime missingValue) => value is not (null or JsonNull) ? value.ToDateTime() : missingValue;

		/// <summary>Returns the converted value, or a fallback value if it is missing</summary>
		/// <param name="value">JSON value to convert</param>
		/// <param name="missingValue">Fallback value</param>
		/// <returns>The converted value, or <paramref name="missingValue"/> if it is <see langword="null"/> or missing</returns>
		[Pure]
		public static DateTimeOffset OrDefault(this JsonValue? value, DateTimeOffset missingValue) => value is not (null or JsonNull) ? value.ToDateTimeOffset() : missingValue;

		/// <summary>Returns the converted value, or a fallback value if it is missing</summary>
		/// <param name="value">JSON value to convert</param>
		/// <param name="missingValue">Fallback value</param>
		/// <returns>The converted value, or <paramref name="missingValue"/> if it is <see langword="null"/> or missing</returns>
		[Pure]
		public static NodaTime.Instant OrDefault(this JsonValue? value, NodaTime.Instant missingValue) => value is not (null or JsonNull) ? value.ToInstant() : missingValue;

		/// <summary>Returns the converted value, or a fallback value if it is missing</summary>
		/// <param name="value">JSON value to convert</param>
		/// <param name="missingValue">Fallback value</param>
		/// <returns>The converted value, or <paramref name="missingValue"/> if it is <see langword="null"/> or missing</returns>
		[Pure]
		public static NodaTime.Duration OrDefault(this JsonValue? value, NodaTime.Duration missingValue) => value is not (null or JsonNull) ? value.ToDuration() : missingValue;

		#endregion

		#region Object Helpers...

		// magic cast entre JsonValue et JsonObject
		// le but est de réduire les faux positifs de nullref avec des outils d'analyse statique de code (R#, ..)

		/// <summary>Vérifie que la valeur n'est pas vide, et qu'il s'agit bien d'un JsonObject.</summary>
		/// <param name="value">Valeur JSON qui doit être un object</param>
		/// <returns>Valeur castée en JsonObject si elle existe. Une exception si la valeur est null, missing, ou n'est pas une array.</returns>
		/// <exception cref="System.InvalidOperationException">Si <paramref name="value"/> est null, missing, ou n'est pas un object.</exception>
		[Pure]
		[Obsolete("OLD_API: Please use _AsObject() if required, or _AsObjectOrDefault(...) if optional", error: true)]
		public static JsonObject AsObject(this JsonValue? value)
		{
			if (value.IsNullOrMissing()) return FailObjectIsNullOrMissing();
			if (value.Type != JsonType.Object) return FailValueIsNotAnObject(value); // => throws
			return (JsonObject) value;
		}

		/// <summary>Vérifie que la valeur n'est pas vide, et qu'il s'agit bien d'un JsonObject.</summary>
		/// <param name="value">Valeur JSON qui doit être un object</param>
		/// <param name="required">Si true, une exception sera générée si l'objet est null.</param>
		/// <returns>Valeur castée en JsonObject si elle existe. Une exception si la valeur est null, missing, ou n'est pas une array.</returns>
		/// <exception cref="System.InvalidOperationException">Si <paramref name="value"/> est null, missing, ou n'est pas un object.</exception>
		[Pure, ContractAnnotation("required:true => notnull")]
		[Obsolete("OLD_API: Please use _AsObject() if required, or _AsObjectOrDefault(...) if optional", error: true)]
		public static JsonObject? AsObject(this JsonValue? value, bool required)
		{
			if (value is null || value.IsNull)
			{
				return !required ? null : FailObjectIsNullOrMissing();
			}
			if (value.Type != JsonType.Object)
			{
				return FailValueIsNotAnObject(value);
			}
			return (JsonObject) value;
		}

		/// <summary>Vérifie que la valeur n'est pas vide, et qu'il s'agit bien d'un JsonObject.</summary>
		/// <param name="value">Valeur JSON qui doit être un object</param>
		/// <returns>Valeur castée en JsonObject si elle existe. Une exception si la valeur est null, missing, ou n'est pas une array.</returns>
		/// <exception cref="System.InvalidOperationException">Si <paramref name="value"/> est null, missing, ou n'est pas un object.</exception>
		[Pure]
		public static JsonObject _AsObject(this JsonValue? value)
		{
			if (value is null or JsonNull)
			{
				return FailObjectIsNullOrMissing();
			}
			if (value.Type != JsonType.Object)
			{
				return FailValueIsNotAnObject(value);
			}
			return (JsonObject) value;
		}

		/// <summary>Vérifie que la valeur n'est pas vide, et qu'il s'agit bien d'un JsonObject.</summary>
		/// <param name="value">Valeur JSON qui doit être un object</param>
		/// <returns>Valeur castée en JsonObject si elle existe. Une exception si la valeur est null, missing, ou n'est pas une array.</returns>
		/// <exception cref="System.InvalidOperationException">Si <paramref name="value"/> est null, missing, ou n'est pas un object.</exception>
		[Pure]
		public static JsonObject? _AsObjectOrDefault(this JsonValue? value)
		{
			if (value is null or JsonNull)
			{
				return null;
			}
			if (value.Type != JsonType.Object)
			{
				return FailValueIsNotAnObject(value);
			}
			return (JsonObject) value;
		}

		[DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
		internal static JsonObject FailObjectIsNullOrMissing() => throw new InvalidOperationException("Required JSON object was null or missing.");

		[DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
		internal static JsonObject FailValueIsNotAnObject(JsonValue value) => throw CrystalJson.Errors.Parsing_CannotCastToJsonObject(value.Type);

		[Pure]
		public static JsonObject ToJsonObject([InstantHandle] this IEnumerable<KeyValuePair<string, JsonValue>> items, IEqualityComparer<string>? comparer = null)
		{
			return JsonObject.Create(items!, comparer);
		}

		[Pure]
		public static JsonObject ToJsonObject<TValue>([InstantHandle] this IEnumerable<KeyValuePair<string, TValue>> items, IEqualityComparer<string>? comparer = null)
		{
			Contract.NotNull(items);

			//TODO: move this as JsonObject.FromValues(...)

			comparer ??= JsonObject.ExtractKeyComparer(items) ?? StringComparer.Ordinal;

			var map = new Dictionary<string, JsonValue>(items.TryGetNonEnumeratedCount(out var count) ? count : 0, comparer);
			var context = new CrystalJsonDomWriter.VisitingContext();
			foreach (var item in items)
			{
				map.Add(item.Key, JsonValue.FromValue(CrystalJsonDomWriter.Default, ref context, item.Value));
			}
			return new JsonObject(map, readOnly: false);
		}

		[Pure]
		public static JsonObject ToJsonObject<TValue>([InstantHandle] this IEnumerable<KeyValuePair<string, TValue>> items, [InstantHandle] Func<TValue, JsonValue?> valueSelector, IEqualityComparer<string>? comparer = null)
		{
			Contract.NotNull(items);
			Contract.NotNull(valueSelector);

			//TODO: move this as JsonObject.FromValues(...)

			comparer ??= JsonObject.ExtractKeyComparer(items) ?? StringComparer.Ordinal;

			var map = new Dictionary<string, JsonValue>(items.TryGetNonEnumeratedCount(out var count) ? count : 0, comparer);
			foreach (var item in items)
			{
				Contract.Debug.Assert(item.Key != null, "Item cannot have a null key");
				map.Add(item.Key, valueSelector(item.Value) ?? JsonNull.Null);
			}
			return new JsonObject(map, readOnly: false);
		}

		[Pure]
		public static JsonObject ToJsonObject<TElement>([InstantHandle] this IEnumerable<TElement> source, [InstantHandle] Func<TElement, string> keySelector, [InstantHandle] Func<TElement, JsonValue?> valueSelector, IEqualityComparer<string>? comparer = null)
		{
			Contract.NotNull(source);
			Contract.NotNull(keySelector);
			Contract.NotNull(valueSelector);

			//TODO: move this as JsonObject.FromValues(...)

			var map = new Dictionary<string, JsonValue>(source.TryGetNonEnumeratedCount(out var count) ? count : 0, comparer ?? StringComparer.Ordinal);
			foreach (var item in source)
			{
				var key = keySelector(item);
				Contract.Debug.Assert(key != null, "key selector should not return null");
				var child = valueSelector(item) ?? JsonNull.Null;
				map.Add(key, child);
			}
			return new JsonObject(map, readOnly: false);
		}

		[Pure]
		public static JsonObject ToJsonObject<TElement, TValue>([InstantHandle] this IEnumerable<TElement> source, [InstantHandle] Func<TElement, string> keySelector, [InstantHandle] Func<TElement, TValue> valueSelector, IEqualityComparer<string>? comparer = null)
		{
			Contract.NotNull(source);
			Contract.NotNull(keySelector);
			Contract.NotNull(valueSelector);

			//TODO: move this as JsonObject.FromValues(...)

			var map = new Dictionary<string, JsonValue>(source.TryGetNonEnumeratedCount(out var count) ? count : 0, comparer ?? StringComparer.Ordinal);
			var context = new CrystalJsonDomWriter.VisitingContext();
			foreach (var item in source)
			{
				var key = keySelector(item);
				Contract.Debug.Assert(key != null, "key selector should not return null");
				var child = valueSelector(item);
				map.Add(key, JsonValue.FromValue(CrystalJsonDomWriter.Default, ref context, child));
			}
			return new JsonObject(map, readOnly: false);
		}

		#endregion

		#region Array Helpers...

		/// <summary>Vérifie que la valeur n'est pas vide, et qu'il s'agit bien d'une JsonArray.</summary>
		/// <param name="value">Valeur JSON qui doit être une array</param>
		/// <returns>Valeur castée en JsonArray si elle existe. Une exception si la valeur est null, missing, ou n'est pas une array.</returns>
		/// <exception cref="System.InvalidOperationException">Si <paramref name="value"/> est null, missing, ou n'est pas une array.</exception>
		[Pure, ContractAnnotation("null => halt"), MethodImpl(MethodImplOptions.AggressiveInlining)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static JsonArray _AsArray(this JsonValue? value) => value is null or JsonNull ? FailArrayIsNullOrMissing() : value as JsonArray ?? FailValueIsNotAnArray(value);

		/// <summary>Retourne la valeur JSON sous forme d'array, ou null si elle est null ou manquante.</summary>
		/// <param name="value">Valeur JSON qui doit être soit une array, soit null/missing.</param>
		/// <returns>Valeur castée en JsonArray si elle existe, ou null si la valeur null ou missing. Une exception si la valeur est d'un type différent.</returns>
		/// <exception cref="System.InvalidOperationException">Si <paramref name="value"/> n'est ni null, ni une array.</exception>
		[Pure, ContractAnnotation("null => null"), MethodImpl(MethodImplOptions.AggressiveInlining)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static JsonArray? _AsArrayOrDefault(this JsonValue? value) => value.IsNullOrMissing() ? null : value as JsonArray ?? FailValueIsNotAnArray(value);

		/// <summary>Vérifie que la valeur n'est pas vide, et qu'il s'agit bien d'une JsonArray.</summary>
		/// <param name="value">Valeur JSON qui doit être une array</param>
		/// <param name="required">Si true, throw une exception si le document JSON parsé est équivalent à null (vide, 'null', ...)</param>
		/// <returns>Valeur castée en JsonArray si elle existe, ou null si la valeur est null/missing et que <paramref name="required"/> vaut false. Throw dans tous les autres cas</returns>
		/// <exception cref="InvalidOperationException">Si <paramref name="value"/> est null ou missing et que <paramref name="required"/> vaut true. Ou si <paramref name="value"/> n'est pas une array.</exception>
		[Pure, ContractAnnotation("required:true => notnull")]
		[Obsolete("OLD_API: Use AsArray() if required, or AsArrayOrDefault() if optional", error: true)]
		public static JsonArray? AsArray(this JsonValue? value, bool required) => required ? _AsArray(value) : _AsArrayOrDefault(value);

		#endregion

		#region AsNumber...

		// magic cast entre JsonValue et JsonNumber
		// le but est de réduire les faux positifs de nullref avec des outils d'analyse statique de code (R#, ..)

		/// <summary>Vérifie que la valeur n'est pas vide, et qu'il s'agit bien d'une JsonNumber.</summary>
		/// <param name="value">Valeur JSON qui doit être un number</param>
		/// <returns>Valeur castée en JsonNumber si elle existe. Une exception si la valeur est null, missing, ou n'est pas un number.</returns>
		/// <exception cref="System.InvalidOperationException">Si <paramref name="value"/> est null, missing, ou n'est pas un number.</exception>
		[Pure, ContractAnnotation("null => halt")]
		public static JsonNumber AsNumber(this JsonValue? value)
		{
			if (value == null || value.Type != JsonType.Number) return FailValueIsNotANumber(value);
			return (JsonNumber)value;
		}

		/// <summary>Retourne la valeur JSON sous forme d'un number, ou null si elle est null ou manquante.</summary>
		/// <param name="value">Valeur JSON qui doit être soit un number, soit null/missing.</param>
		/// <returns>Valeur castée en JsonNumber si elle existe, ou null si la valeur null ou missing. Une exception si la valeur est d'un type différent.</returns>
		/// <exception cref="System.InvalidOperationException">Si <paramref name="value"/> n'est ni null, ni un number.</exception>
		[Pure]
		public static JsonNumber? AsNumberOrDefault(this JsonValue? value)
		{
			if (value.IsNullOrMissing()) return null;
			if (value.Type != JsonType.Number) return FailValueIsNotANumber(value);
			return (JsonNumber)value;
		}

		[DoesNotReturn]
		private static JsonNumber FailValueIsNotANumber(JsonValue? value)
		{
			if (value.IsNullOrMissing()) ThrowHelper.ThrowInvalidOperationException("Expected JSON number was either null or missing.");
			throw CrystalJson.Errors.Parsing_CannotCastToJsonNumber(value.Type);
		}

		#endregion

		#region AsString...

		// magic cast entre JsonValue et JsonNumber
		// le but est de réduire les faux positifs de nullref avec des outils d'analyse statique de code (R#, ..)

		/// <summary>Vérifie que la valeur n'est pas vide, et qu'il s'agit bien d'une JsonNumber.</summary>
		/// <param name="value">Valeur JSON qui doit être un number</param>
		/// <returns>Valeur castée en JsonNumber si elle existe. Une exception si la valeur est null, missing, ou n'est pas un number.</returns>
		/// <exception cref="System.InvalidOperationException">Si <paramref name="value"/> est null, missing, ou n'est pas un number.</exception>
		[Pure, ContractAnnotation("null => halt")]
		public static JsonString AsString(this JsonValue? value)
		{
			if (value == null || value.Type != JsonType.String) return FailValueIsNotAString(value);
			return (JsonString)value;
		}

		/// <summary>Retourne la valeur JSON sous forme d'un number, ou null si elle est null ou manquante.</summary>
		/// <param name="value">Valeur JSON qui doit être soit un number, soit null/missing.</param>
		/// <returns>Valeur castée en JsonNumber si elle existe, ou null si la valeur null ou missing. Une exception si la valeur est d'un type différent.</returns>
		/// <exception cref="System.InvalidOperationException">Si <paramref name="value"/> n'est ni null, ni un number.</exception>
		[Pure]
		public static JsonString? AsStringOrDefault(this JsonValue? value)
		{
			if (value.IsNullOrMissing()) return null;
			if (value.Type != JsonType.String) return FailValueIsNotAString(value);
			return (JsonString)value;
		}

		[DoesNotReturn]
		private static JsonString FailValueIsNotAString(JsonValue? value)
		{
			if (value.IsNullOrMissing()) ThrowHelper.ThrowInvalidOperationException("Expected JSON string was either null or missing.");
			throw CrystalJson.Errors.Parsing_CannotCastToJsonString(value.Type);
		}

		#endregion

		#region Getters...

		/// <summary>Return the value of the <b>required</b> field with the specified name, converted into an array with elements of type <typeparamref name="TValue"/></summary>
		/// <typeparam name="TValue">Type of the elements of the array</typeparam>
		/// <param name="key">Name of the field</param>
		/// <returns>Array of values converted into instances of type <typeparamref name="TValue"/></returns>
		/// <exception cref="InvalidOperationException">The field is null or missing</exception>
		/// <exception cref="JsonBindingException">If a value cannot be bound to the specified type.</exception>
		[Pure]
		public static TValue[] _GetArray<TValue>(this JsonValue self, string key)
		{
			var value = self._GetValue(key);
			if (value is not JsonArray arr)
			{
				throw CrystalJson.Errors.Parsing_CannotCastToJsonArray(value.Type);
			}
			return arr.ToArray<TValue>()!;
		}

		/// <summary>Return the value of the <b>required</b> field with the specified name, converted into an array with elements of type <typeparamref name="TValue"/></summary>
		/// <typeparam name="TValue">Type of the elements of the array</typeparam>
		/// <param name="key">Name of the field</param>
		/// <param name="resolver">Optional custom type resolver</param>
		/// <param name="message">Optional error message if the required array is null or missing</param>
		/// <returns>Array of values converted into instances of type <typeparamref name="TValue"/></returns>
		/// <exception cref="InvalidOperationException">The field is null or missing</exception>
		/// <exception cref="JsonBindingException">If a value cannot be bound to the specified type.</exception>
		[Pure]
		public static TValue[] _GetArray<TValue>(this JsonValue self, string key, ICrystalJsonTypeResolver? resolver = null, string? message = null)
		{
			var value = self.GetValueOrDefault(key).RequiredField(key, message);
			if (value is not JsonArray arr)
			{
				throw CrystalJson.Errors.Parsing_CannotCastToJsonArray(value.Type);
			}
			return arr.ToArray<TValue>(resolver)!;
		}

		/// <summary>Return the value of the <i>optional</i> field with the specified name, converted into an array with elements of type <typeparamref name="TValue"/></summary>
		/// <typeparam name="TValue">Type of the elements of the array</typeparam>
		/// <param name="key">Name of the field</param>
		/// <param name="defaultValue">Values returned if the field is null or missing.</param>
		/// <returns>Array of values converted into instances of type <typeparamref name="TValue"/>, or <paramref name="defaultValue"/> if the field was null or missing.</returns>
		/// <exception cref="InvalidOperationException">The field is null or missing</exception>
		/// <exception cref="JsonBindingException">If a value cannot be bound to the specified type.</exception>
		[Pure]
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static TValue[]? _GetArray<TValue>(this JsonValue? self, string key, TValue[]? defaultValue) => _GetArray(self, key, defaultValue, null);

		/// <summary>Return the value of the <i>optional</i> field with the specified name, converted into an array with elements of type <typeparamref name="TValue"/></summary>
		/// <typeparam name="TValue">Type of the elements of the array</typeparam>
		/// <param name="key">Name of the field</param>
		/// <param name="defaultValue">Values returned if the field is null or missing.</param>
		/// <param name="resolver">Optional custom type resolver</param>
		/// <returns>Array of values converted into instances of type <typeparamref name="TValue"/>, or <paramref name="defaultValue"/> if the field was null or missing.</returns>
		/// <exception cref="InvalidOperationException">The field is null or missing</exception>
		/// <exception cref="JsonBindingException">If a value cannot be bound to the specified type.</exception>
		[Pure]
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static TValue[]? _GetArray<TValue>(this JsonValue? self, string key, TValue[]? defaultValue, ICrystalJsonTypeResolver? resolver)
		{
			var value = self?.GetValueOrDefault(key);
			switch (value)
			{
				case null or JsonNull:
				{
					return defaultValue;
				}
				case JsonArray arr:
				{
					return arr.ToArray<TValue>()!;
				}
				default:
				{
					throw CrystalJson.Errors.Parsing_CannotCastToJsonArray(value.Type);
				}
			}
		}

		/// <summary>Return the value of the <i>optional</i> field with the specified name, converted into an array with elements of type <typeparamref name="TValue"/></summary>
		/// <typeparam name="TValue">Type of the elements of the array</typeparam>
		/// <param name="key">Name of the field</param>
		/// <param name="defaultValue">Values returned if the field is null or missing.</param>
		/// <returns>Array of values converted into instances of type <typeparamref name="TValue"/>, or <paramref name="defaultValue"/> if the field was null or missing.</returns>
		/// <exception cref="InvalidOperationException">The field is null or missing</exception>
		/// <exception cref="JsonBindingException">If a value cannot be bound to the specified type.</exception>
		[Pure]
		public static TValue[] _GetArray<TValue>(this JsonValue? self, string key, ReadOnlySpan<TValue> defaultValue) => _GetArray(self, key, defaultValue, null);

		/// <summary>Return the value of the <i>optional</i> field with the specified name, converted into an array with elements of type <typeparamref name="TValue"/></summary>
		/// <typeparam name="TValue">Type of the elements of the array</typeparam>
		/// <param name="key">Name of the field</param>
		/// <param name="defaultValue">Values returned if the field is null or missing.</param>
		/// <param name="resolver">Optional custom type resolver</param>
		/// <returns>Array of values converted into instances of type <typeparamref name="TValue"/>, or <paramref name="defaultValue"/> if the field was null or missing.</returns>
		/// <exception cref="InvalidOperationException">The field is null or missing</exception>
		/// <exception cref="JsonBindingException">If a value cannot be bound to the specified type.</exception>
		[Pure]
		public static TValue[] _GetArray<TValue>(this JsonValue? self, string key, ReadOnlySpan<TValue> defaultValue, ICrystalJsonTypeResolver? resolver)
		{
			var value = self?.GetValueOrDefault(key);
			switch (value)
			{
				case null or JsonNull:
				{
					return defaultValue.ToArray();
				}
				case JsonArray arr:
				{
					return arr.ToArray<TValue>()!;
				}
				default:
				{
					throw CrystalJson.Errors.Parsing_CannotCastToJsonArray(value.Type);
				}
			}
		}

		/// <summary>Return the value of the <b>required</b> field with the specified name, converted into a list with elements of type <typeparamref name="TValue"/></summary>
		/// <typeparam name="TValue">Type of the elements of the array</typeparam>
		/// <param name="key">Name of the field</param>
		/// <returns>List of values converted into instances of type <typeparamref name="TValue"/></returns>
		/// <exception cref="InvalidOperationException">The field is null or missing</exception>
		/// <exception cref="JsonBindingException">If a value cannot be bound to the specified type.</exception>
		[Pure]
		public static List<TValue> _GetList<TValue>(this JsonValue self, string key)
		{
			Contract.NotNull(self);
			var value = self._GetValue(key);
			if (value is not JsonArray arr) throw CrystalJson.Errors.Parsing_CannotCastToJsonArray(value.Type);
			return arr.ToList<TValue>()!;
		}

		/// <summary>Return the value of the <b>required</b> field with the specified name, converted into a list with elements of type <typeparamref name="TValue"/></summary>
		/// <typeparam name="TValue">Type of the elements of the array</typeparam>
		/// <param name="key">Name of the field</param>
		/// <param name="resolver">Optional custom type resolver</param>
		/// <param name="message">Optional error message if the required array is null or missing</param>
		/// <returns>List of values converted into instances of type <typeparamref name="TValue"/></returns>
		/// <exception cref="InvalidOperationException">The field is null or missing</exception>
		/// <exception cref="JsonBindingException">If a value cannot be bound to the specified type.</exception>
		[Pure]
		public static List<TValue> _GetList<TValue>(this JsonValue self, string key, ICrystalJsonTypeResolver? resolver = null, string? message = null)
		{
			Contract.NotNull(self);
			var value = self.GetValueOrDefault(key).RequiredField(key, message);
			if (value is not JsonArray arr) throw CrystalJson.Errors.Parsing_CannotCastToJsonArray(value.Type);
			return arr.ToList<TValue>(resolver)!;
		}

		/// <summary>Return the value of the <i>optional</i> field with the specified name, converted into a list with elements of type <typeparamref name="TValue"/></summary>
		/// <typeparam name="TValue">Type of the elements of the array</typeparam>
		/// <param name="key">Name of the field</param>
		/// <param name="defaultValue">List returned if the field is null or missing.</param>
		/// <returns>Array of values converted into instances of type <typeparamref name="TValue"/>, or <paramref name="defaultValue"/> if the field was null or missing.</returns>
		/// <exception cref="InvalidOperationException">The field is null or missing</exception>
		/// <exception cref="JsonBindingException">If a value cannot be bound to the specified type.</exception>
		[Pure]
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static List<TValue>? _GetList<TValue>(this JsonValue? self, string key, List<TValue>? defaultValue) => _GetList(self, key, defaultValue, null);

		/// <summary>Return the value of the <i>optional</i> field with the specified name, converted into a list with elements of type <typeparamref name="TValue"/></summary>
		/// <typeparam name="TValue">Type of the elements of the array</typeparam>
		/// <param name="key">Name of the field</param>
		/// <param name="defaultValue">List returned if the field is null or missing.</param>
		/// <param name="resolver">Optional custom type resolver</param>
		/// <returns>Array of values converted into instances of type <typeparamref name="TValue"/>, or <paramref name="defaultValue"/> if the field was null or missing.</returns>
		/// <exception cref="InvalidOperationException">The field is null or missing</exception>
		/// <exception cref="JsonBindingException">If a value cannot be bound to the specified type.</exception>
		[Pure]
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static List<TValue>? _GetList<TValue>(this JsonValue? self, string key, List<TValue>? defaultValue, ICrystalJsonTypeResolver? resolver)
		{
			var value = self?.GetValueOrDefault(key);
			switch (value)
			{
				case null or JsonNull:
				{
					return defaultValue;
				}
				case JsonArray arr:
				{
					return arr.ToList<TValue>()!;
				}
				default:
				{
					throw CrystalJson.Errors.Parsing_CannotCastToJsonArray(value.Type);
				}
			}
		}

		/// <summary>Return the value of the <b>required</b> field with the specified name, converted into a dictionary with keys of type <typeparamref name="TKey"/> and elements of type <typeparamref name="TValue"/></summary>
		/// <typeparam name="TKey">Type of the keys of the dictionary</typeparam>
		/// <typeparam name="TValue">Type of the elements of the dictionary</typeparam>
		/// <param name="key">Name of the field</param>
		/// <param name="resolver">Optional custom type resolver</param>
		/// <param name="message">Optional error message if the required array is null or missing</param>
		/// <returns>Dictionary of keys and values converted into instances of type <typeparamref name="TKey"/> and <typeparamref name="TValue"/> respectively.</returns>
		/// <exception cref="InvalidOperationException">The field is null or missing</exception>
		/// <exception cref="JsonBindingException">If a key or value cannot be bound to the specified type.</exception>
		public static Dictionary<TKey, TValue> _GetDictionary<TKey, TValue>(this JsonValue self, string key, ICrystalJsonTypeResolver? resolver = null, string? message = null) where TKey : notnull
		{
			Contract.NotNull(self);
			var value = self._GetValue(key);
			if (value is not JsonObject obj) throw CrystalJson.Errors.Parsing_CannotCastToJsonObject(value.Type);
			return obj.ToDictionary<TKey, TValue>(resolver);
		}

		public static Dictionary<TKey, TValue> _GetDictionary<TKey, TValue>(this JsonValue self, string key) where TKey : notnull
			=> _GetDictionary<TKey, TValue>(self, key, resolver: null, message: null);

		/// <summary>Return the value of the <i>optional</i> field with the specified name, converted into a dictionary with keys of type <typeparamref name="TKey"/> and elements of type <typeparamref name="TValue"/></summary>
		/// <typeparam name="TKey">Type of the keys of the dictionary</typeparam>
		/// <typeparam name="TValue">Type of the elements of the dictionary</typeparam>
		/// <param name="key">Name of the field</param>
		/// <param name="defaultValue">Dictionary returned if the field is null or missing.</param>
		/// <param name="resolver">Optional custom type resolver</param>
		/// <returns>Dictionary of keys and values converted into instances of type <typeparamref name="TKey"/> and <typeparamref name="TValue"/> respectively, or <paramref name="defaultValue"/> if the field was null or missing.</returns>
		/// <exception cref="InvalidOperationException">The field is null or missing</exception>
		/// <exception cref="JsonBindingException">If a key or value cannot be bound to the specified type.</exception>
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static Dictionary<TKey, TValue>? _GetDictionary<TKey, TValue>(this JsonValue? self, string key, Dictionary<TKey, TValue>? defaultValue, ICrystalJsonTypeResolver? resolver) where TKey : notnull
		{
			var value = self?.GetValueOrDefault(key);
			switch (value)
			{
				case null or JsonNull:
				{
					return defaultValue;
				}
				case JsonObject obj:
				{
					return obj.ToDictionary<TKey, TValue>(resolver);
				}
				default:
				{
					throw CrystalJson.Errors.Parsing_CannotCastToJsonArray(value.Type);
				}
			}
		}

		public static Dictionary<TKey, TValue>? _GetDictionary<TKey, TValue>(this JsonValue? self, string key, Dictionary<TKey, TValue>? defaultValue) where TKey : notnull
			=> _GetDictionary<TKey, TValue>(self, key, defaultValue, null);

		#endregion

	}

}
