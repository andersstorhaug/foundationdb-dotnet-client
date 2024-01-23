#region Copyright (c) 2023-2023 SnowBank SAS
//
// All rights are reserved. Reproduction or transmission in whole or in part, in
// any form or by any means, electronic, mechanical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
//
#endregion

namespace Aspire.Hosting.ApplicationModel
{
	using System.Linq;
	using System.Net;
	using Doxense.Diagnostics.Contracts;

	public class FdbContainerResource : ContainerResource, IFdbResource, IResourceWithParent<FdbClusterResource>
	{
		public FdbContainerResource(string name, FdbClusterResource parent) : base(name)
		{
			Contract.NotNull(parent);
			this.Parent = parent;
		}

		public FdbClusterResource Parent { get; }

		public required int Port { get; set; }

		public required bool IsCoordinator { get; set; }

		public required string DockerTag { get; set; }

		public string? ProcessClass { get; set; }

		internal EndPoint GetEndpoint()
		{
			if (!this.TryGetAllocatedEndPoints(out var bindings))
			{
				throw new DistributedApplicationException("Expected allocated endpoints!");
			}

			var allocatedEndpoint = bindings.Single();

			//note: we expect the address to be "localhost".
			Contract.Debug.Assert(allocatedEndpoint.Address == "localhost");
			var addr = IPAddress.Loopback;
			var port = allocatedEndpoint.Port;

			return new IPEndPoint(addr, port);
		}

		public string? GetConnectionString()
		{
			return this.Parent.GetConnectionString();
		}

	}

}
