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

namespace FoundationDB.Linq.Providers
{
	using FoundationDB.Layers.Indexing;

	/// <summary>Database query</summary>
	/// <remarks>Reads data directly from a database</remarks>
	public sealed class FdbIndexQuery<TId, TValue> : FdbAsyncQuery<FdbIndexQuery<TId, TValue>>, IFdbIndexQueryable<TId, TValue>
	{
		internal FdbIndexQuery(IFdbDatabase db, FdbIndex<TId, TValue> index)
			: base(db)
		{
			this.Index = index;
		}

		/// <summary>Index used by this query</summary>
		public FdbIndex<TId, TValue> Index { get; }

		/// <summary>Execute the query and return the result in the expected type</summary>
		protected override Task<object?> ExecuteInternal(FdbQueryExpression expression, Type resultType, CancellationToken ct)
		{
			throw new InvalidOperationException("You cannot execute this operation on the whole index. Try calling Lookup() on this query to lookup specific values from the index.");
		}

	}

}
