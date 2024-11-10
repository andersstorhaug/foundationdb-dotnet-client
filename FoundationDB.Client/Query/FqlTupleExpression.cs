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

#if NET8_0_OR_GREATER

namespace FoundationDB.Client
{
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.Linq;
	using System.Numerics;
	using System.Runtime.InteropServices;
	using System.Text;
	using Doxense.Collections.Tuples;

	[PublicAPI]
	public enum FqlItemType
	{
		Indirection = -3,
		Variable = -2,
		MaybeMore = -1,

		Invalid = 0,

		Nil,
		Boolean,
		Integer,
		Number,
		String,
		Bytes,
		Uuid,
		Tuple,
	}

	[Flags]
	public enum FqlVariableTypes
	{
		/// <summary>All possible types are allowed</summary>
		Any = -1,

		/// <summary>No type was specified</summary>
		None = 0,

		Nil = 1 << 0,
		Bool = 1 << 1,
		Int = 1 << 2,
		Num = 1 << 3,
		String = 1 << 4,
		Uuid = 1 << 5,
		Bytes = 1 << 6,
		Tuple = 1 << 7,

		/// <summary>Combines the chunks of a large value that was split into several keys</summary>
		Append = 1 << 8,
		Sum = 1 << 9,
		Count = 1 << 10,
	}

	[DebuggerDisplay("{ToString(),nq}")]
	[PublicAPI]
	public readonly struct FqlTupleItem : IFqlExpression,
		// Equality
		IEquatable<FqlTupleItem>,
		IEquatable<FqlTupleExpression>,
		IEquatable<bool>,
		IEquatable<string>,
		IEquatable<int>,
		IEquatable<long>,
		IEquatable<uint>,
		IEquatable<ulong>,
		IEquatable<float>,
		IEquatable<double>,
		IEquatable<decimal>,
		IEquatable<Half>,
		IEquatable<Int128>,
		IEquatable<UInt128>,
		IEquatable<BigInteger>,
#if NET9_0_OR_GREATER
		IEquatable<ReadOnlySpan<char>>,
#endif
		IEquatable<Slice>,
		IEquatable<Guid>,
		IEquatable<Uuid128>,
		IEquatable<IVarTuple>,
		IEquatable<FqlVariableTypes>,
		// Formatting
		IFormattable
	{
		private static readonly object s_false = true;

		private static readonly object s_true = true;

		private static readonly object[] s_smallIntCache = Enumerable.Range(0, 100).Select(i => (object) (long) i).ToArray();

		private static readonly object[] s_smallUIntCache = Enumerable.Range(0, 100).Select(i => (object) (ulong) i).ToArray();

		public readonly FqlItemType Type;

		public readonly object? Value;

		public readonly string? Name;

		public FqlTupleItem(FqlItemType type, object? value = null, string? name = null)
		{
			this.Type = type;
			this.Value = value;
			this.Name = name;
		}

		/// <inheritdoc />
		public bool IsPattern => this.Type is FqlItemType.Variable or FqlItemType.MaybeMore or FqlItemType.Indirection;

		/// <inheritdoc />
		public override bool Equals([NotNullWhen(true)] object? obj) => obj is FqlTupleItem other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode() => this.Type switch
		{
			FqlItemType.Indirection => HashCode.Combine(FqlItemType.Indirection, this.Name),
			FqlItemType.Variable => HashCode.Combine(FqlItemType.Variable, this.Name, (FqlVariableTypes) this.Value!),
			FqlItemType.MaybeMore => HashCode.Combine(FqlItemType.MaybeMore),
			FqlItemType.Nil => HashCode.Combine(FqlItemType.Nil),
			FqlItemType.Boolean => HashCode.Combine(FqlItemType.Bytes, (bool) this.Value!),
			FqlItemType.Integer => this.Value switch
			{
				int x => HashCode.Combine(FqlItemType.Integer, x),
				long x => HashCode.Combine(FqlItemType.Integer, x),
				uint x => HashCode.Combine(FqlItemType.Integer, x),
				ulong x => HashCode.Combine(FqlItemType.Integer, x),
				Int128 x => HashCode.Combine(FqlItemType.Integer, x),
				UInt128 x => HashCode.Combine(FqlItemType.Integer, x),
				_ => throw new InvalidOperationException(),
			},
			FqlItemType.Number => this.Value switch
			{
				double x => HashCode.Combine(FqlItemType.Number, x),
				float x => HashCode.Combine(FqlItemType.Number, x),
				decimal x => HashCode.Combine(FqlItemType.Number, x),
				Half x => HashCode.Combine(FqlItemType.Number, x),
				_ => throw new InvalidOperationException(),
			},
			FqlItemType.String => HashCode.Combine(FqlItemType.String, (string) this.Value!),
			FqlItemType.Bytes => HashCode.Combine(FqlItemType.Bytes, (Slice) this.Value!),
			FqlItemType.Uuid => HashCode.Combine(FqlItemType.Uuid, (Uuid128) this.Value!),
			FqlItemType.Tuple => HashCode.Combine(FqlItemType.Tuple, (FqlTupleExpression) this.Value!),
			_ => HashCode.Combine(this.Type),
		};

		public bool Matches(object? value) => this.Type switch
		{
			FqlItemType.Indirection => throw new NotImplementedException(),
			FqlItemType.Variable => MatchType((FqlVariableTypes) this.Value!, value),
			FqlItemType.MaybeMore => ReferenceEquals(value, null),
			FqlItemType.Nil => ReferenceEquals(value, null),
			FqlItemType.Boolean => value is bool b && Equals(b),
			FqlItemType.Integer => value switch
			{
				int i => Equals(i),
				long l => Equals(l),
				uint ui => Equals(ui),
				ulong ul => Equals(ul),
				Int128 i => Equals(i),
				UInt128 ui => Equals(ui),
				_ => false,
			},
			FqlItemType.Number => value switch
			{
				float f => Equals(f),
				double d => Equals(d),
				Half h => Equals(h),
				decimal d => Equals(d),
				_ => false,
			},
			FqlItemType.String => value is string str && Equals(str),
			FqlItemType.Bytes => value is Slice s && Equals(s),
			FqlItemType.Uuid => value switch
			{
				Uuid128 u => Equals(u),
				Guid g => Equals(g),
				_ => false,
			},
			FqlItemType.Tuple => value switch
			{
				FqlTupleExpression tup => Equals(tup),
				IVarTuple tup => Equals(tup),
				_ => false,
			},
			_ => throw new NotImplementedException(this.Type.ToString())
		};

		public bool Equals(FqlTupleItem other)
		{
			if (this.Type != other.Type) return false;
			return this.Type switch
			{
				FqlItemType.Invalid or FqlItemType.Nil or FqlItemType.MaybeMore => true,
				FqlItemType.Indirection => other.Name == this.Name,
				FqlItemType.Variable => other.Name == this.Name && other.Value switch
				{
					FqlVariableTypes types => this.Equals(types),
					_ => false,
				},
				FqlItemType.Boolean => other.Value switch
				{
					bool b => this.Equals(b),
					_ => false,
				},
				FqlItemType.Integer => other.Value switch
				{
					int x => this.Equals(x),
					long x => this.Equals(x),
					uint x => this.Equals(x),
					ulong x => this.Equals(x),
					Int128 x => this.Equals(x),
					UInt128 x => this.Equals(x),
					BigInteger x => this.Equals(x),
					_ => false,
				},
				FqlItemType.Number => other.Value switch
				{
					double d => this.Equals(d),
					float f => this.Equals(f),
					decimal d => this.Equals(d),
					Half h => this.Equals(h),
					_ => false,
				},
				FqlItemType.String => other.Value switch
				{
					string s => this.Equals(s),
					_ => false,
				},
				FqlItemType.Bytes => other.Value switch
				{
					Slice s => this.Equals(s),
					_ => false,
				},
				FqlItemType.Uuid => other.Value switch
				{
					Guid g => this.Equals(g),
					Uuid128 g => this.Equals(g),
					_ => false,
				},
				FqlItemType.Tuple => other.Value switch
				{
					FqlTupleExpression tup => this.Equals(tup),
					_ => false,
				},
				_ => false
			};
		}

		public bool Equals(bool value) => this.Type == FqlItemType.Boolean && this.Value switch
		{
			bool b => b == value,
			_ => false
		};

		public bool Equals(int value) => this.Type == FqlItemType.Integer && this.Value switch
		{
			int x => x == value,
			uint x => value >= 0 && x == (uint) value,
			long x => x == value,
			ulong x => value >= 0 && x == (ulong) value,
			Int128 x => x == value,
			UInt128 x => value >= 0 && x == (UInt128) value,
			BigInteger x => x == value,
			_ => false
		};

		public bool Equals(long value) => this.Type == FqlItemType.Integer && this.Value switch
		{
			int x => x == value,
			uint x => value >= 0 && x == (ulong) value,
			long x => x == value,
			ulong x => value >= 0 && x == (ulong) value,
			Int128 x => x == value,
			UInt128 x => value >= 0 && x == (UInt128) value,
			BigInteger x => x == value,
			_ => false
		};

		public bool Equals(uint value) => this.Type == FqlItemType.Integer && this.Value switch
		{
			int x => x >= 0 && (uint) x == value,
			uint x => x == value,
			long x => x >= 0 && (ulong) x == value,
			ulong x => x == value,
			Int128 x => x == value,
			UInt128 x => x == value,
			BigInteger x => x == value,
			_ => false
		};

		public bool Equals(ulong value) => this.Type == FqlItemType.Integer && this.Value switch
		{
			int x => x >= 0 && (ulong) x == value,
			uint x => x == value,
			long x => x >= 0 && (ulong) x == value,
			ulong x => x == value,
			Int128 x => x == value,
			UInt128 x => x == value,
			BigInteger x => x == value,
			_ => false
		};

		public bool Equals(Int128 value) => this.Type == FqlItemType.Integer && this.Value switch
		{
			int x => x == value,
			uint x => value >= 0 && x == value,
			long x => x == value,
			ulong x => value >= 0 && x == value,
			Int128 x => x == value,
			UInt128 x => value >= 0 && x == (UInt128) value,
			BigInteger x => x == value,
			_ => false
		};

		public bool Equals(UInt128 value) => this.Type == FqlItemType.Integer && this.Value switch
		{
			int x => x >= 0 && (UInt128) x == value,
			uint x => x == value,
			long x => x >= 0 && (UInt128) x == value,
			ulong x => x == value,
			Int128 x => x >= 0 && (UInt128) x == value,
			UInt128 x => x == value,
			BigInteger x => x == value,
			_ => false
		};

		public bool Equals(BigInteger value) => this.Type == FqlItemType.Integer && this.Value switch
		{
			int x => x >= 0 && (BigInteger) x == value,
			uint x => (BigInteger) x == value,
			long x => x >= 0 && (BigInteger) x == value,
			ulong x => (BigInteger) x == value,
			Int128 x => x >= 0 && x == value,
			UInt128 x => x == value,
			BigInteger x => x == value,
			_ => false
		};

		public bool Equals(float value) => this.Type == FqlItemType.Number && this.Value switch
		{
			// ReSharper disable CompareOfFloatsByEqualityOperator
			float x => float.IsNaN(value) ? float.IsNaN(x) : x == value,
			double x => float.IsNaN(value) ? double.IsNaN(x) : (float) x == value,
			decimal x => !float.IsNaN(value) && (float) x == value,
			Half x => float.IsNaN(value) ? Half.IsNaN(x) : x == (Half) value,
			_ => false,
			// ReSharper restore CompareOfFloatsByEqualityOperator
		};

		public bool Equals(double value) => this.Type == FqlItemType.Number && this.Value switch
		{
			// ReSharper disable CompareOfFloatsByEqualityOperator
			float x => double.IsNaN(value) ? float.IsNaN(x) : x == (float) value,
			double x => double.IsNaN(value) ? double.IsNaN(x) : x == value,
			decimal x => !double.IsNaN(value) && (double) x == value,
			Half x => double.IsNaN(value) ? Half.IsNaN(x) : x == (Half) value,
			_ => false,
			// ReSharper restore CompareOfFloatsByEqualityOperator
		};

		public bool Equals(Half value) => this.Type == FqlItemType.Number && this.Value switch
		{
			// ReSharper disable CompareOfFloatsByEqualityOperator
			float x => Half.IsNaN(value) ? float.IsNaN(x) : (Half) x == value,
			double x => Half.IsNaN(value) ? double.IsNaN(x) : (Half) x == value,
			decimal x => !Half.IsNaN(value) && (Half) x == value,
			Half x => Half.IsNaN(value) ? Half.IsNaN(x) : x == value,
			_ => false,
			// ReSharper restore CompareOfFloatsByEqualityOperator
		};

		public bool Equals(decimal value) => this.Type == FqlItemType.Number && this.Value switch
		{
			// ReSharper disable CompareOfFloatsByEqualityOperator
			float x => !float.IsNaN(x) && x == (float) value,
			double x => !double.IsNaN(x) && x == (double) value,
			decimal x => x == value,
			Half x => !Half.IsNaN(x) && x == (Half) value,
			_ => false,
			// ReSharper restore CompareOfFloatsByEqualityOperator
		};

		public bool Equals(string? value)
		{
			return this.Type == FqlItemType.String && ((string) this.Value!) == value;
		}

		public bool Equals(ReadOnlySpan<char> value)
		{
			return this.Type == FqlItemType.String && value.SequenceEqual((string) this.Value!);
		}

		public bool Equals(Guid value)
		{
			return this.Type == FqlItemType.Uuid && ((Uuid128) this.Value!) == (Uuid128) value;
		}

		public bool Equals(Uuid128 value)
		{
			return this.Type == FqlItemType.Uuid && ((Uuid128) this.Value!) == value;
		}

		public bool Equals(Slice value)
		{
			return this.Type == FqlItemType.Bytes && ((Slice) this.Value!).Equals(value);
		}

		public bool Equals(IVarTuple? value)
		{
			return this.Type == FqlItemType.Tuple && ((FqlTupleExpression) this.Value!).Match(value);
		}

		public bool Equals(FqlVariableTypes types)
		{
			return this.Type == FqlItemType.Variable && ((FqlVariableTypes) this.Value!).Equals(types);
		}

		public bool Equals(FqlTupleExpression? value)
		{
			return this.Type == FqlItemType.Tuple && ((FqlTupleExpression) this.Value!).Equals(value);
		}

		public static bool MatchType(FqlVariableTypes types, object? value)
		{
			return value switch
			{
				null => types.HasFlag(FqlVariableTypes.Nil),
				bool => types.HasFlag(FqlVariableTypes.Bool),
				int => types.HasFlag(FqlVariableTypes.Int),
				uint => types.HasFlag(FqlVariableTypes.Int),
				long => types.HasFlag(FqlVariableTypes.Int),
				ulong => types.HasFlag(FqlVariableTypes.Int),
				Int128 => types.HasFlag(FqlVariableTypes.Int),
				UInt128 => types.HasFlag(FqlVariableTypes.Int),
				float => types.HasFlag(FqlVariableTypes.Num),
				double => types.HasFlag(FqlVariableTypes.Num),
				Half => types.HasFlag(FqlVariableTypes.Num),
				decimal => types.HasFlag(FqlVariableTypes.Num),
				string => types.HasFlag(FqlVariableTypes.String),
				Slice => types.HasFlag(FqlVariableTypes.Bytes),
				Guid => types.HasFlag(FqlVariableTypes.Uuid),
				Uuid128 => types.HasFlag(FqlVariableTypes.Uuid),
				IVarTuple => types.HasFlag(FqlVariableTypes.Tuple),
				_ => false,
			};
		}

		/// <inheritdoc />
		public override string ToString() => this.Type switch
		{
			FqlItemType.Indirection => ":" + this.Name,
			FqlItemType.Variable => "<" + (this.Name != null ? (this.Name + ":") : "") + ToVariableTypeLiteral((FqlVariableTypes) this.Value!) + ">",
			FqlItemType.MaybeMore => "...",
			FqlItemType.Nil => "nil",
			FqlItemType.Boolean => ((bool) this.Value!) ? "true" : "false",
			FqlItemType.Integer => this.Value switch
			{
				int x        => x.ToString(null, CultureInfo.InvariantCulture),
				uint x       => x.ToString(null, CultureInfo.InvariantCulture),
				long x       => x.ToString(null, CultureInfo.InvariantCulture),
				ulong x      => x.ToString(null, CultureInfo.InvariantCulture),
				Int128 x     => x.ToString(null, CultureInfo.InvariantCulture),
				UInt128 x    => x.ToString(null, CultureInfo.InvariantCulture),
				BigInteger x => x.ToString(null, CultureInfo.InvariantCulture),
				_ => throw new InvalidOperationException("Invalid Int storage type"),
			},
			FqlItemType.Number => this.Value switch
			{
				Half x    => x.ToString("R", CultureInfo.InvariantCulture),
				float x   => x.ToString("R", CultureInfo.InvariantCulture),
				double x  => x.ToString("R", CultureInfo.InvariantCulture),
				decimal x => x.ToString("R", CultureInfo.InvariantCulture),
				_ => throw new InvalidOperationException("Invalid Int storage type"),
			},
			FqlItemType.String => $"\"{((string) this.Value!).Replace("\"", "\\\"")}\"",
			FqlItemType.Uuid => ((Uuid128) this.Value!).ToString("D"),
			FqlItemType.Bytes => "0x" + ((Slice) this.Value!).ToString("x"),
			FqlItemType.Tuple => ((FqlTupleExpression) this.Value!).ToString(),
			_ => $"<?{this.Type}?>",
		};

		/// <inheritdoc />
		public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

		public void Explain(ExplanationBuilder builder)
		{
			if (this.Type == FqlItemType.Tuple)
			{
				((FqlTupleExpression) this.Value!).Explain(builder);
				return;
			}

			builder.WriteLine($"{this.Type}: {this.ToString()}");
		}

		private static readonly Dictionary<FqlVariableTypes, string> s_typesLiteralCache = CreateInitialTypeLiteralCache();

		private static Dictionary<FqlVariableTypes, string> CreateInitialTypeLiteralCache() => new()
		{
			[FqlVariableTypes.Any]    = "",
			[FqlVariableTypes.Nil]    = "nil",
			[FqlVariableTypes.Bool]   = "bool",
			[FqlVariableTypes.Int]    = "int",
			[FqlVariableTypes.Num]    = "num",
			[FqlVariableTypes.String] = "str",
			[FqlVariableTypes.Uuid]   = "uuid",
			[FqlVariableTypes.Bytes]  = "bytes",
			[FqlVariableTypes.Tuple]  = "tup",
			[FqlVariableTypes.Append] = "append",
			[FqlVariableTypes.Sum]    = "sum",
			[FqlVariableTypes.Count]  = "count",
		};

		public static FqlVariableTypes ParseVariableTypeLiteral(ReadOnlySpan<char> literal) => literal switch
		{
			""       => FqlVariableTypes.Any,
			"nil"    => FqlVariableTypes.Nil,
			"bool"   => FqlVariableTypes.Bool,
			"int"    => FqlVariableTypes.Int,
			"num"    => FqlVariableTypes.Num,
			"str"    => FqlVariableTypes.String,
			"uuid"   => FqlVariableTypes.Uuid,
			"bytes"  => FqlVariableTypes.Bytes,
			"tup"    => FqlVariableTypes.Tuple,
			"append" => FqlVariableTypes.Append,
			"count"  => FqlVariableTypes.Sum,
			"sum"    => FqlVariableTypes.Count,
			_        => FqlVariableTypes.None
		};

		public static string ToVariableTypeLiteral(FqlVariableTypes types)
		{
			lock (s_typesLiteralCache)
			{
				if (s_typesLiteralCache.TryGetValue(types, out var s))
				{
					return s;
				}

				s = CreateTypeLiteral(types);
				s_typesLiteralCache[types] = s;
				return s;
			}

			static string CreateTypeLiteral(FqlVariableTypes types)
			{
				var sb = new StringBuilder();
				foreach (var x in Enum.GetValues<FqlVariableTypes>())
				{
					if ((types & x) != 0)
					{
						if (sb.Length != 0) sb.Append('|');
						sb.Append(s_typesLiteralCache[x]);
					}
				}

				return sb.ToString();
			}

		}

		public static FqlTupleItem Indirection(string name) => new(FqlItemType.Indirection, null, name);

		public static FqlTupleItem Variable(FqlVariableTypes types, string? name = null) => new(FqlItemType.Variable, types, name);

		public static FqlTupleItem MaybeMore() => new(FqlItemType.MaybeMore);

		public static FqlTupleItem Nil() => new(FqlItemType.Nil);

		public static FqlTupleItem Boolean(bool value) => new(FqlItemType.Boolean, value ? s_true : s_false);

		public static FqlTupleItem Int(int value) => new(FqlItemType.Integer, (uint) value < s_smallIntCache.Length ? s_smallIntCache[value] : (long) value);

		public static FqlTupleItem Int(long value) => new(FqlItemType.Integer, (value >= 0 && value < s_smallIntCache.Length) ? s_smallIntCache[value] : value);

		public static FqlTupleItem Int(uint value) => new(FqlItemType.Integer, value < s_smallUIntCache.Length ? s_smallUIntCache[value] : (ulong) value);

		public static FqlTupleItem Int(ulong value) => new(FqlItemType.Integer, value < (ulong) s_smallUIntCache.Length ? s_smallUIntCache[value] : value);

		public static FqlTupleItem Int(Int128 value) => new(FqlItemType.Integer, value);

		public static FqlTupleItem Int(UInt128 value) => new(FqlItemType.Integer, value);

		public static FqlTupleItem Int(BigInteger value) => new(FqlItemType.Integer, value);

		public static FqlTupleItem Num(float value) => new(FqlItemType.Number, value);

		public static FqlTupleItem Num(double value) => new(FqlItemType.Number, value);

		public static FqlTupleItem Num(Half value) => new(FqlItemType.Number, value);

		public static FqlTupleItem Num(decimal value) => new(FqlItemType.Number, value);

		public static FqlTupleItem String(string value) => new(FqlItemType.String, value);

		public static FqlTupleItem Bytes(Slice value) => new(FqlItemType.Bytes, value);

		public static FqlTupleItem Uuid(Guid value) => new(FqlItemType.Uuid, (Uuid128) value);

		public static FqlTupleItem Uuid(Uuid128 value) => new(FqlItemType.Uuid, value);

		public static FqlTupleItem Tuple(FqlTupleExpression value) => new(FqlItemType.Tuple, value);

	}

	[DebuggerDisplay("{ToString(),nq}")]
	[PublicAPI]
	public sealed class FqlTupleExpression : IEquatable<FqlTupleExpression>, IFqlExpression, IFormattable
	{
		public List<FqlTupleItem> Items { get; } = [];

		/// <inheritdoc />
		public bool IsPattern => this.Items.Any(x => x.IsPattern);

		public bool Match(IVarTuple? tuple)
		{
			if (tuple == null) return false;

			var items = CollectionsMarshal.AsSpan(this.Items);

			// if the last is MaybeMore, we don't need to check
			bool exactSize = true;
			while(items.Length > 0 && items[^1].Type == FqlItemType.MaybeMore)
			{
				exactSize = false;
				items = items[..^1];
			}

			// if the tuple is smaller, it will not match
			if (exactSize)
			{
				if (tuple.Count != items.Length) return false;
			}
			else
			{
				if (tuple.Count < items.Length) return false;
			}

			for (int i = 0; i < items.Length; i++)
			{
				if (!items[i].Matches(tuple[i]))
				{
					return false;
				}
			}
			return true;
		}

		/// <inheritdoc />
		public override bool Equals([NotNullWhen(true)] object? obj) => obj is FqlTupleExpression other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode()
		{
			var hc = new HashCode();
			foreach (var item in this.Items)
			{
				hc.Add(item);
			}
			return hc.ToHashCode();
		}

		public bool Equals(FqlTupleExpression? other)
		{
			if (other is null) return false;
			if (ReferenceEquals(other, this)) return true;

			var items = this.Items;
			var otherItems = other.Items;
			if (items.Count != otherItems.Count) return false;

			for (int i = 0; i < items.Count; i++)
			{
				if (!items[i].Equals(otherItems[i])) return false;
			}

			return true;
		}

		#region Builder Pattern...

		public static FqlTupleExpression Create() => new();

		public FqlTupleExpression Add(FqlTupleItem item)
		{
			this.Items.Add(item);
			return this;
		}

		/// <summary>Adds a <c>...</c> glob, that matches zero or more elements</summary>
		public FqlTupleExpression MaybeMore() => Add(FqlTupleItem.MaybeMore());

		/// <summary>Adds a variable with the given type filter and with an optional name</summary>
		public FqlTupleExpression Var(FqlVariableTypes types, string? name = null) => Add(FqlTupleItem.Variable(types, name));

		/// <summary>Adds a variable that matches any type, and with optional name</summary>
		public FqlTupleExpression VarAny(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Any, name));

		/// <summary>Adds a variable of type <see cref="FqlVariableTypes.Nil"/>, and with optional name</summary>
		public FqlTupleExpression VarNil(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Nil, name));

		/// <summary>Adds a variable of type <see cref="FqlVariableTypes.Bytes"/>, and with optional name</summary>
		public FqlTupleExpression VarBytes(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Bytes, name));

		/// <summary>Adds a variable of type <see cref="FqlVariableTypes.String"/>, and with optional name</summary>
		public FqlTupleExpression VarString(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.String, name));

		/// <summary>Adds a variable of type <see cref="FqlVariableTypes.Bool"/>, and with optional name</summary>
		public FqlTupleExpression VarBoolean(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Bool, name));

		/// <summary>Adds a variable of type <see cref="FqlVariableTypes.Int"/>, and with optional name</summary>
		public FqlTupleExpression VarInteger(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Int, name));

		/// <summary>Adds a variable of type <see cref="FqlVariableTypes.Num"/>, and with optional name</summary>
		public FqlTupleExpression VarNumber(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Num, name));

		/// <summary>Adds a variable of type <see cref="FqlVariableTypes.Uuid"/>, and with optional name</summary>
		public FqlTupleExpression VarUuid(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Uuid, name));

		/// <summary>Adds a variable of type <see cref="FqlVariableTypes.Tuple"/>, and with optional name</summary>
		public FqlTupleExpression VarTuple(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Tuple, name));

		/// <summary>Adds a variable of type <see cref="FqlVariableTypes.Append"/>, and with optional name</summary>
		/// <remarks>This variable is only allowed in values</remarks>
		public FqlTupleExpression VarAppend(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Append, name));

		/// <summary>Adds a variable of type <see cref="FqlVariableTypes.Sum"/>, and with optional name</summary>
		/// <remarks>This variable is only allowed in values</remarks>
		public FqlTupleExpression VarSum(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Sum, name));

		/// <summary>Adds a variable of type <see cref="FqlVariableTypes.Count"/>, and with optional name</summary>
		/// <remarks>This variable is only allowed in values</remarks>
		public FqlTupleExpression VarCount(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Count, name));

		/// <summary>Adds a <see cref="FqlItemType.Nil"/> constant literal</summary>
		public FqlTupleExpression Nil() => Add(FqlTupleItem.Nil());

		/// <summary>Adds a <see cref="FqlItemType.Boolean"/> constant literal</summary>
		public FqlTupleExpression Boolean(bool value) => Add(FqlTupleItem.Boolean(value));

		/// <summary>Adds a constant <see cref="FqlItemType.Integer"/> literal</summary>
		public FqlTupleExpression Integer(int value) => Add(FqlTupleItem.Int(value));

		/// <summary>Adds a constant <see cref="FqlItemType.Integer"/> literal</summary>
		public FqlTupleExpression Integer(long value) => Add(FqlTupleItem.Int(value));

		/// <summary>Adds a constant <see cref="FqlItemType.Integer"/> literal</summary>
		public FqlTupleExpression Integer(uint value) => Add(FqlTupleItem.Int(value));

		/// <summary>Adds a constant <see cref="FqlItemType.Integer"/> literal</summary>
		public FqlTupleExpression Integer(ulong value) => Add(FqlTupleItem.Int(value));

		/// <summary>Adds a constant <see cref="FqlItemType.Integer"/> literal</summary>
		public FqlTupleExpression Integer(Int128 value) => Add(FqlTupleItem.Int(value));

		/// <summary>Adds a constant <see cref="FqlItemType.Integer"/> literal</summary>
		public FqlTupleExpression Integer(UInt128 value) => Add(FqlTupleItem.Int(value));

		/// <summary>Adds a constant <see cref="FqlItemType.Integer"/> literal</summary>
		public FqlTupleExpression Integer(BigInteger value) => Add(FqlTupleItem.Int(value));

		/// <summary>Adds a constant <see cref="FqlItemType.Number"/> literal</summary>
		public FqlTupleExpression Number(float value) => Add(FqlTupleItem.Num(value));

		/// <summary>Adds a constant <see cref="FqlItemType.Number"/> literal</summary>
		public FqlTupleExpression Number(double value) => Add(FqlTupleItem.Num(value));

		/// <summary>Adds a constant <see cref="FqlItemType.Number"/> literal</summary>
		public FqlTupleExpression Number(Half value) => Add(FqlTupleItem.Num(value));

		/// <summary>Adds a constant <see cref="FqlItemType.Number"/> literal</summary>
		public FqlTupleExpression Number(decimal value) => Add(FqlTupleItem.Num(value));

		/// <summary>Adds a constant <see cref="FqlItemType.String"/> literal</summary>
		public FqlTupleExpression String(string value) => Add(FqlTupleItem.String(value));

		/// <summary>Adds a constant <see cref="FqlItemType.Bytes"/> literal</summary>
		public FqlTupleExpression Bytes(Slice value) => Add(FqlTupleItem.Bytes(value));

		/// <summary>Adds a constant <see cref="FqlItemType.Uuid"/> literal</summary>
		public FqlTupleExpression Uuid(Guid value) => Add(FqlTupleItem.Uuid(value));

		/// <summary>Adds a constant <see cref="FqlItemType.Uuid"/> literal</summary>
		public FqlTupleExpression Uuid(Uuid128 value) => Add(FqlTupleItem.Uuid(value));

		/// <summary>Adds an embedded <see cref="FqlItemType.Tuple"/></summary>
		public FqlTupleExpression Tuple(FqlTupleExpression value) => Add(FqlTupleItem.Tuple(value));

		#endregion

		/// <inheritdoc />
		public override string ToString()
		{
			var items = CollectionsMarshal.AsSpan(this.Items);
			if (items.Length == 0)
			{
				return "()";
			}

			if (items.Length == 1)
			{
				return $"({items[0]})";
			}

			var sb = new StringBuilder();
			sb.Append('(');
			sb.Append(items[0].ToString());
			for(int i = 1; i < items.Length; i++)
			{
				sb.Append(',');
				sb.Append(items[i].ToString());
			}
			sb.Append(')');
			return sb.ToString();
		}

		/// <inheritdoc />
		public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

		/// <inheritdoc />
		public void Explain(ExplanationBuilder builder)
		{
			if (!builder.Recursive)
			{
				builder.WriteLine($"Tuple: [{this.Items.Count}] {ToString()}");
				return;
			}

			builder.WriteLine($"Tuple: [{this.Items.Count}]");
			builder.ExplainChildren(this.Items);
		}

	}

}

#endif
