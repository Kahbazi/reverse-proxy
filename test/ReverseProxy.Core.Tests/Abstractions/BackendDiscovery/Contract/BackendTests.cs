// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Microsoft.ReverseProxy.Core.Abstractions.Tests
{
    public class BackendTests
    {
        [Fact]
        public void Constructor_Works()
        {
            new Backend();
        }

        [Fact]
        public void DeepClone_Works()
        {
            // Arrange
            var backend = new Backend
            {
                CircuitBreakerOptions = new CircuitBreakerOptions(),
                QuotaOptions = new QuotaOptions(),
                PartitioningOptions = new BackendPartitioningOptions(),
                LoadBalancingOptions = new LoadBalancingOptions(),
                HealthCheckOptions = new HealthCheckOptions(),
                Metadata = new Dictionary<string, string>
                {
                    { "key", "value" },
                },
            };

            // Act
            var clone = backend.DeepClone();

            // Assert
            clone.Should().NotBeSameAs(backend);
            clone.CircuitBreakerOptions.Should().NotBeNull();
            clone.CircuitBreakerOptions.Should().NotBeSameAs(backend.CircuitBreakerOptions);
            clone.QuotaOptions.Should().NotBeNull();
            clone.QuotaOptions.Should().NotBeSameAs(backend.QuotaOptions);
            clone.PartitioningOptions.Should().NotBeNull();
            clone.PartitioningOptions.Should().NotBeSameAs(backend.PartitioningOptions);
            clone.LoadBalancingOptions.Should().NotBeNull();
            clone.LoadBalancingOptions.Should().NotBeSameAs(backend.LoadBalancingOptions);
            clone.HealthCheckOptions.Should().NotBeNull();
            clone.HealthCheckOptions.Should().NotBeSameAs(backend.HealthCheckOptions);
            clone.Metadata.Should().NotBeNull();
            clone.Metadata.Should().NotBeSameAs(backend.Metadata);
            clone.Metadata["key"].Should().Be("value");
        }

        [Fact]
        public void DeepClone_Nulls_Works()
        {
            // Arrange
            var backend = new Backend();

            // Act
            var clone = backend.DeepClone();

            // Assert
            clone.Should().NotBeSameAs(backend);
            clone.CircuitBreakerOptions.Should().BeNull();
            clone.QuotaOptions.Should().BeNull();
            clone.PartitioningOptions.Should().BeNull();
            clone.LoadBalancingOptions.Should().BeNull();
            clone.HealthCheckOptions.Should().BeNull();
            clone.Metadata.Should().BeNull();
        }
    }
}
