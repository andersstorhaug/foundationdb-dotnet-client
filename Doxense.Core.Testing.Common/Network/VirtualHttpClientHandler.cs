﻿#region Copyright (c) 2005-2023 Doxense SAS
//
// All rights are reserved. Reproduction or transmission in whole or in part, in
// any form or by any means, electronic, mechanical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
//
#endregion

namespace Doxense.Networking
{
	using System;
	using System.Net;
	using System.Net.Http;
	using System.Threading;
	using System.Threading.Tasks;
	using Doxense.Networking.Http;

	internal class VirtualHttpClientHandler : DelegatingHandler
	{

		public VirtualHttpClientHandler(VirtualNetworkMap map, Uri baseAddress, BetterHttpClientOptions options)
		{
			this.Map = map;
			this.BaseAddress = baseAddress;
			this.Options = options;
		}

		public VirtualNetworkMap Map { get; }

		public Uri BaseAddress { get; }

		public BetterHttpClientOptions Options { get; }

		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			throw new NotImplementedException("Non async client not supported. Why do you need it anyway?");
		}

		public static Exception SimulateNameResolutionError(string hostName, string debugReason)
		{
			var webEx = new System.Net.WebException($"The remote name could not be resolved: '{hostName}'", WebExceptionStatus.NameResolutionFailure);
			return new HttpRequestException($"An error occurred while sending the request. [{debugReason}]", webEx);
		}

		public static Exception SimulatePortNotBoundFailure(string hostName, int port, string debugReason)
		{
			var webEx = new System.Net.WebException($"No connection could be made because the target machine actively refused it {hostName}:{port}", WebExceptionStatus.ConnectFailure);
			return new HttpRequestException($"An error occurred while sending the request. [{debugReason}]", webEx);
		}

		public static Exception SimulateConnectFailure(string debugReason)
		{
			var sockEx = new System.Net.Sockets.SocketException(10060); // TimedOut
			var webEx = new System.Net.WebException("Unable to connect to the remove server", sockEx, WebExceptionStatus.ConnectFailure, null);
			return new HttpRequestException($"An error occurred while sending the request. [{debugReason}]", webEx);
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var uri = request.RequestUri;
			if (uri == null)
			{
				uri = this.BaseAddress;
			}
			else if (!uri.IsAbsoluteUri)
			{
				uri = new Uri(this.BaseAddress, uri);
			}

			// a partir de l'hostname, on va regarder si on peut resolve l'ip, puis si on y a acces, puis on va récupérer le TestClient correspondant (ou sinon, simuler une exception adaptée)
			//REVIEW: je sais pas s'il faut utiliser .DnsSafeHost ou .Host ?
			string hostName = uri.DnsSafeHost;

			var host = this.Map.FindHost(hostName);
			if (host == null)
			{
				throw SimulateNameResolutionError(hostName, $"Found no matching host for name '{hostName}' visible from simulated host '{this.Map.Host.Id}' ({this.Map.Host.Fqdn})");
			}

			if (host.Passthrough)
			{ // ce host existe en vrai, on va faire la requête pour de vrai!
				var handler = new HttpClientHandler()
				{
					// ??
				};
				this.Options.Configure(handler);
				var invoker = new HttpMessageInvoker(handler);
				return invoker.SendAsync(request, cancellationToken);
			}

			var (local, remote) = this.Map.FindNetworkPath(host, hostName);
			if (local != null && remote != null)
			{
				//TODO: check si les deux hosts veulent se parler quand meme!
				int port = uri.Port;
				var factory = host.FindHandler(remote, port);
				if (factory != null)
				{
					var handler = factory();
					handler = this.Options.Configure(handler);
					var invoker = new HttpMessageInvoker(handler);
					return invoker.SendAsync(request, cancellationToken);
				}

				throw SimulatePortNotBoundFailure(hostName, port, $"Found no port {port} bound on location '{remote}' of target host '{host.Id}', visible from host '{this.Map.Host.Id}' ({this.Map.Host.Fqdn})");
			}

			if (IPAddress.TryParse(hostName, out var ip))
				throw SimulateConnectFailure($"Found not matching host for IP {ip} visible from host '{this.Map.Host.Id}' ({this.Map.Host.Fqdn})");
			else
				throw SimulateNameResolutionError(hostName, $"Found no matching host for name '{hostName}' visible from simulated host '{this.Map.Host.Id}' ({this.Map.Host.Fqdn})");
		}

	}

}
