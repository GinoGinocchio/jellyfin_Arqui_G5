﻿using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Emby.Server.Implementations;
using Jellyfin.Networking.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.PreStartupRoutines;

/// <inheritdoc />
public class MigrateNetworkConfiguration : IMigrationRoutine
{
    private readonly ServerApplicationPaths _applicationPaths;
    private readonly ILogger<MigrateNetworkConfiguration> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrateNetworkConfiguration"/> class.
    /// </summary>
    /// <param name="applicationPaths">An instance of <see cref="ServerApplicationPaths"/>.</param>
    /// <param name="loggerFactory">An instance of the <see cref="ILoggerFactory"/> interface.</param>
    public MigrateNetworkConfiguration(ServerApplicationPaths applicationPaths, ILoggerFactory loggerFactory)
    {
        _applicationPaths = applicationPaths;
        _logger = loggerFactory.CreateLogger<MigrateNetworkConfiguration>();
    }

    /// <inheritdoc />
    public Guid Id => Guid.Parse("4FB5C950-1991-11EE-9B4B-0800200C9A66");

    /// <inheritdoc />
    public string Name => nameof(MigrateNetworkConfiguration);

    /// <inheritdoc />
    public bool PerformOnNewInstall => false;

    /// <inheritdoc />
    public void Perform()
    {
        string path = Path.Combine(_applicationPaths.ConfigurationDirectoryPath, "network.xml");
        var oldNetworkConfigSerializer = new XmlSerializer(typeof(OldNetworkConfiguration), new XmlRootAttribute("NetworkConfiguration"));
        using var xmlReader = XmlReader.Create(path);
        var oldNetworkConfiguration = (OldNetworkConfiguration?)oldNetworkConfigSerializer.Deserialize(xmlReader);

        if (oldNetworkConfiguration is not null)
        {
            // Migrate network config values to new config schema
            var networkConfiguration = new NetworkConfiguration();
            networkConfiguration.AutoDiscovery = oldNetworkConfiguration.AutoDiscovery;
            networkConfiguration.BaseUrl = oldNetworkConfiguration.BaseUrl;
            networkConfiguration.CertificatePassword = oldNetworkConfiguration.CertificatePassword;
            networkConfiguration.CertificatePath = oldNetworkConfiguration.CertificatePath;
            networkConfiguration.EnableHttps = oldNetworkConfiguration.EnableHttps;
            networkConfiguration.EnableIPv4 = oldNetworkConfiguration.EnableIPV4;
            networkConfiguration.EnableIPv6 = oldNetworkConfiguration.EnableIPV6;
            networkConfiguration.EnablePublishedServerUriByRequest = oldNetworkConfiguration.EnablePublishedServerUriByRequest;
            networkConfiguration.EnableRemoteAccess = oldNetworkConfiguration.EnableRemoteAccess;
            networkConfiguration.EnableUPnP = oldNetworkConfiguration.EnableUPnP;
            networkConfiguration.IgnoreVirtualInterfaces = oldNetworkConfiguration.IgnoreVirtualInterfaces;
            networkConfiguration.InternalHttpPort = oldNetworkConfiguration.HttpServerPortNumber;
            networkConfiguration.InternalHttpsPort = oldNetworkConfiguration.HttpsPortNumber;
            networkConfiguration.IsRemoteIPFilterBlacklist = oldNetworkConfiguration.IsRemoteIPFilterBlacklist;
            networkConfiguration.KnownProxies = oldNetworkConfiguration.KnownProxies;
            networkConfiguration.LocalNetworkAddresses = oldNetworkConfiguration.LocalNetworkAddresses;
            networkConfiguration.LocalNetworkSubnets = oldNetworkConfiguration.LocalNetworkSubnets;
            networkConfiguration.PublicHttpPort = oldNetworkConfiguration.PublicPort;
            networkConfiguration.PublicHttpsPort = oldNetworkConfiguration.PublicHttpsPort;
            networkConfiguration.PublishedServerUriBySubnet = oldNetworkConfiguration.PublishedServerUriBySubnet;
            networkConfiguration.RemoteIPFilter = oldNetworkConfiguration.RemoteIPFilter;
            networkConfiguration.RequireHttps = oldNetworkConfiguration.RequireHttps;

            // Migrate old virtual interface name schema
            var oldVirtualInterfaceNames = oldNetworkConfiguration.VirtualInterfaceNames;
            if (oldVirtualInterfaceNames.Equals("vEthernet*", StringComparison.OrdinalIgnoreCase))
            {
                networkConfiguration.VirtualInterfaceNames = new string[] { "veth" };
            }
            else
            {
                networkConfiguration.VirtualInterfaceNames = oldVirtualInterfaceNames.Replace("*", string.Empty, StringComparison.OrdinalIgnoreCase).Split(',');
            }

            var networkConfigSerializer = new XmlSerializer(typeof(NetworkConfiguration));
            var xmlWriterSettings = new XmlWriterSettings { Indent = true };
            using var xmlWriter = XmlWriter.Create(path, xmlWriterSettings);
            networkConfigSerializer.Serialize(xmlWriter, networkConfiguration);
        }
    }

#pragma warning disable
    public sealed class OldNetworkConfiguration
    {
        public const int DefaultHttpPort = 8096;

        public const int DefaultHttpsPort = 8920;

        private string _baseUrl = string.Empty;

        public bool RequireHttps { get; set; }

        public string CertificatePath { get; set; } = string.Empty;

        public string CertificatePassword { get; set; } = string.Empty;

        public string BaseUrl
        {
            get => _baseUrl;
            set
            {
                // Normalize the start of the string
                if (string.IsNullOrWhiteSpace(value))
                {
                    // If baseUrl is empty, set an empty prefix string
                    _baseUrl = string.Empty;
                    return;
                }

                if (value[0] != '/')
                {
                    // If baseUrl was not configured with a leading slash, append one for consistency
                    value = "/" + value;
                }

                // Normalize the end of the string
                if (value[^1] == '/')
                {
                    // If baseUrl was configured with a trailing slash, remove it for consistency
                    value = value.Remove(value.Length - 1);
                }

                _baseUrl = value;
            }
        }

        public int PublicHttpsPort { get; set; } = DefaultHttpsPort;

        public int HttpServerPortNumber { get; set; } = DefaultHttpPort;

        public int HttpsPortNumber { get; set; } = DefaultHttpsPort;

        public bool EnableHttps { get; set; }

        public int PublicPort { get; set; } = DefaultHttpPort;

        public bool UPnPCreateHttpPortMap { get; set; }

        public string UDPPortRange { get; set; } = string.Empty;

        public bool EnableIPV6 { get; set; }

        public bool EnableIPV4 { get; set; } = true;

        public bool EnableSSDPTracing { get; set; }

        public string SSDPTracingFilter { get; set; } = string.Empty;

        public int UDPSendCount { get; set; } = 2;

        public int UDPSendDelay { get; set; } = 100;

        public bool IgnoreVirtualInterfaces { get; set; } = true;

        public string VirtualInterfaceNames { get; set; } = "vEthernet*";

        public int GatewayMonitorPeriod { get; set; } = 60;

        public bool EnableMultiSocketBinding { get; } = true;

        public bool TrustAllIP6Interfaces { get; set; }

        public string HDHomerunPortRange { get; set; } = string.Empty;

        public string[] PublishedServerUriBySubnet { get; set; } = Array.Empty<string>();

        public bool AutoDiscoveryTracing { get; set; }

        public bool AutoDiscovery { get; set; } = true;

        public string[] RemoteIPFilter { get; set; } = Array.Empty<string>();

        public bool IsRemoteIPFilterBlacklist { get; set; }

        public bool EnableUPnP { get; set; }

        public bool EnableRemoteAccess { get; set; } = true;

        public string[] LocalNetworkSubnets { get; set; } = Array.Empty<string>();

        public string[] LocalNetworkAddresses { get; set; } = Array.Empty<string>();
        public string[] KnownProxies { get; set; } = Array.Empty<string>();

        public bool EnablePublishedServerUriByRequest { get; set; } = false;
    }
#pragma warning restore
}
