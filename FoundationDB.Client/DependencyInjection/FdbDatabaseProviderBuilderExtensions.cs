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

namespace FoundationDB.DependencyInjection
{
	using FoundationDB.Client;
	using Microsoft.Extensions.DependencyInjection;

	/// <summary>Extension methods for <see cref="IFdbDatabaseProvider"/></summary>
	[PublicAPI]
	public static class FdbDatabaseProviderBuilderExtensions
	{

		/// <summary>Sets the <see cref="FdbDatabaseProviderOptions.ApiVersion"/> for this provider</summary>
		public static IFdbDatabaseProviderBuilder WithApiVersion(this IFdbDatabaseProviderBuilder builder, int apiVersion)
		{
			Contract.GreaterThan(apiVersion, 0, nameof(apiVersion));
			builder.Services.Configure<FdbDatabaseProviderOptions>(c =>
			{
				c.ApiVersion = apiVersion;
			});
			return builder;
		}

		/// <summary>Configures the <see cref="FdbDatabaseProviderOptions.ConnectionOptions"/> for this provider</summary>
		public static IFdbDatabaseProviderBuilder WithConnectionString(this IFdbDatabaseProviderBuilder builder, FdbConnectionOptions options)
		{
			Contract.NotNull(options);
			builder.Services.Configure<FdbDatabaseProviderOptions>(c =>
			{
				c.ConnectionOptions = options;
			});
			return builder;
		}

		/// <summary>Sets the <see cref="FdbConnectionOptions.ClusterFile"/> for this provider</summary>
		public static IFdbDatabaseProviderBuilder WithClusterFile(this IFdbDatabaseProviderBuilder builder, string? clusterFile)
		{
			builder.Services.Configure<FdbDatabaseProviderOptions>(c =>
			{
				c.ConnectionOptions.ClusterFile = clusterFile;
			});
			return builder;
		}

	}

}
