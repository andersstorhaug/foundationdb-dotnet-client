#region Copyright (c) 2005-2023 Doxense SAS
//
// All rights are reserved. Reproduction or transmission in whole or in part, in
// any form or by any means, electronic, mechanical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
//
#endregion

namespace Doxense.Networking.Http
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using System.Net.Http;
	using System.Runtime.CompilerServices;
	using System.Runtime.ExceptionServices;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Xml.Linq;
	using Doxense.Diagnostics.Contracts;
	using Doxense.IO;
	using Doxense.Serialization.Json;
	using Microsoft.IO;
	using OpenTelemetry.Trace;

	/// <summary>Represents the context of an HTTP request being executed</summary>
	[DebuggerDisplay("{ToString(),nq}")]
	public class BetterHttpClientContext
	{

		private static readonly ActivitySource ActivitySource = new("Doxense.Networking.Http");

		/// <summary>Instance of the <see cref="BetterHttpClient">client</see> executing this request</summary>
		public BetterHttpClient Client { get; init; }

		/// <summary>Unique ID of this request (for logging purpose)</summary>
		public string Id { get; init; }

		/// <summary>Cancellation token attached to the lifetime of this request</summary>
		public CancellationToken Cancellation { get; init; }

		public BetterHttpClientStage Stage { get; private set; }

		public BetterHttpClientStage? FailedStage { get; internal set; }

		/// <summary>Bag of items that will be available throughout the lifetime of the request</summary>
		public Dictionary<string, object?> State { get; init; }

		/// <summary>Request that will be send to the remote HTTP server</summary>
		public HttpRequestMessage Request { get; init; }

		internal HttpResponseMessage? OriginalResponse { get; set; }

		/// <summary>Response that was received from the remote HTTP server</summary>
		public HttpResponseMessage Response
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.OriginalResponse ?? FailErrorNotAvailable();
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private HttpResponseMessage FailErrorNotAvailable() => throw new InvalidOperationException("The response message is not available.");

		/// <summary>Box that captured any error that happened during the processing of the request</summary>
		public ExceptionDispatchInfo? Error { get; internal set; }

		internal void SetStage(BetterHttpClientStage stage)
		{
			this.Stage = stage;
			this.Client.Options.Hooks?.OnStageChanged(this, stage);
		}

		/// <summary>Set (or clear) an item in the <see cref="State"/> dictionary</summary>
		/// <typeparam name="TState"></typeparam>
		/// <param name="key">Key of the item</param>
		/// <param name="state">New value for this item. If null, the item is removed</param>
		public void SetState<TState>(string key, TState? state)
		{
			Contract.Debug.Requires(key != null);
			if (state is null)
			{
				this.State.Remove(key);
			}
			else
			{
				this.State[key] = state;
			}
		}

		/// <summary>Try to get back an item that was previously stored in the <see cref="State"/> dictionary</summary>
		public bool TryGetState<TState>(string key, [MaybeNullWhen(false)] out TState state)
		{
			Contract.Debug.Requires(key != null);
			if (!this.State.TryGetValue(key, out var obj) || obj is not TState value)
			{
				state = default;
				return false;
			}

			state = value;
			return true;
		}

		public HttpResponseMessage EnsureHasResponse()
		{
			return this.Response ?? throw new InvalidOperationException("HTTP context does not yet have a valid response message.");
		}

		public void EnsureSuccessStatusCode()
		{
			EnsureHasResponse().EnsureSuccessStatusCode();
		}

		/// <summary>Gets a value that indicates if the response was successful</summary>
		public bool IsSuccessStatusCode => this.Response?.IsSuccessStatusCode ?? false;

		/// <summary>Read the response body as a string</summary>
		public Task<string> ReadAsStringAsync()
		{
			return EnsureHasResponse().Content.ReadAsStringAsync(this.Cancellation);
		}

		/// <summary>Return a stream that can be used to read the response body</summary>
		public Task<Stream> ReadAsStreamAsync()
		{
			return EnsureHasResponse().Content.ReadAsStreamAsync(this.Cancellation);
		}

		/// <summary>Copy the response body into the provided stream</summary>
		public Task CopyToAsync(Stream stream)
		{
			return EnsureHasResponse().Content.CopyToAsync(stream, this.Cancellation);
		}

		#region Helpers...

		//REVIEW: c'est trop fragile comme m�thode!!
		public bool IsLikelyJson()
		{
			//TODO: meilleur heuristique! Probl�me: on a pas le body en m�moire donc c'est difficile d'inspecter le body !
			if (this.Response == null) return false;
			if (this.Response.Content.Headers.ContentType?.MediaType == "application/json")
			{
				return true;
			}

			return false;
		}

		private static readonly RecyclableMemoryStreamManager DefaultPool = new RecyclableMemoryStreamManager();

		public async Task<JsonValue> ReadAsJsonAsync(CrystalJsonSettings? settings = null)
		{
			this.Cancellation.ThrowIfCancellationRequested();
			using var activity = ActivitySource.StartActivity("JSON Parse");

			try
			{
				//BUGBUG: PERF: tant qu'on n'a pas de read async json, on est oblig� de buffer dans un MemoryStream!!
				using (var ms = DefaultPool.GetStream())
				{
					await this.CopyToAsync(ms);
					return CrystalJson.Parse(ms.ToSlice(), settings);
				}
			}
			catch (Exception ex)
			{
				activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
				activity?.RecordException(ex);
				throw;
			}
		}

		public async Task<JsonObject?> ReadAsJsonObjectAsync(CrystalJsonSettings? settings = null)
		{
			this.Cancellation.ThrowIfCancellationRequested();
			using var activity = ActivitySource.StartActivity("JSON Parse");

			try
			{
				//BUGBUG: PERF: tant qu'on n'a pas de read async json, on est oblig� de buffer dans un MemoryStream!!
				using (var ms = DefaultPool.GetStream())
				{
					await CopyToAsync(ms);
					activity?.SetTag("json.length", ms.Length);
					return CrystalJson.ParseObject(ms.ToSlice(), settings);
				}
			}
			catch (Exception ex)
			{
				activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
				activity?.RecordException(ex);
				throw;
			}
		}

		public async Task<JsonArray?> ReadAsJsonArrayAsync(CrystalJsonSettings? settings = null)
		{
			this.Cancellation.ThrowIfCancellationRequested();
			using var activity = ActivitySource.StartActivity("JSON Parse");

			try
			{
				//BUGBUG: PERF: tant qu'on n'a pas de read async json, on est oblig� de buffer dans un MemoryStream!!
				using (var ms = DefaultPool.GetStream())
				{
					await CopyToAsync(ms);
					return CrystalJson.ParseArray(ms.ToSlice(), settings);
				}
			}
			catch (Exception ex)
			{
				activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
				activity?.RecordException(ex);
				throw;
			}
		}

		public async Task<TResult?> ReadAsJsonAsync<TResult>(CrystalJsonSettings? settings = null, ICrystalJsonTypeResolver? resolver = null)
		{
			this.Cancellation.ThrowIfCancellationRequested();
			using var activity = ActivitySource.StartActivity("JSON Parse");

			try
			{
				//BUGBUG: PERF: tant qu'on n'a pas de read async json, on est oblig� de buffer dans un MemoryStream!!
				using (var ms = DefaultPool.GetStream())
				{
					await CopyToAsync(ms);
					return CrystalJson.Deserialize<TResult>(ms.ToSlice(), settings, resolver);
				}
			}
			catch (Exception ex)
			{
				activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
				activity?.RecordException(ex);
				throw;
			}
		}

		#endregion

		#region XML Helpers...

		public bool IsLikelyXml()
		{
			//TODO: meilleur heuristique! Probl�me: on a pas le body en m�moire donc c'est difficile d'inspecter le body !
			if (this.Response == null) return false;
			if (this.Response.Content.Headers.ContentType?.MediaType == "text/xml")
			{
				return true;
			}
			return false;
		}

		public async Task<XDocument?> ReadAsXmlAsync(LoadOptions options = LoadOptions.None)
		{
			this.Cancellation.ThrowIfCancellationRequested();
			using var activity = ActivitySource.StartActivity("XML Parse");

			try
			{
				var response = EnsureHasResponse();
				var stream = await response.Content.ReadAsStreamAsync(this.Cancellation);
				//note: do NOT dispose this stream here!

				return await XDocument.LoadAsync(stream, options, this.Cancellation);
			}
			catch (Exception ex)
			{
				activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
				activity?.RecordException(ex);
				throw;
			}
		}

		#endregion

		public override string ToString()
		{
			return $"{this.Request.Method} {this.Request.RequestUri} => {(this.Response != null ? $"{(int) this.Response.StatusCode} {this.Response.ReasonPhrase}" : "<no response>")}";
		}

	}

}
