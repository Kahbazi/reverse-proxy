﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FluentAssertions;
using Xunit;

namespace Microsoft.ReverseProxy.Core.Abstractions.Tests
{
    public class BackendPartitioningOptionsTests
    {
        [Fact]
        public void Constructor_Works()
        {
            new BackendPartitioningOptions();
        }

        [Fact]
        public void DeepClone_Works()
        {
            // Arrange
            var sut = new BackendPartitioningOptions
            {
                PartitionCount = 10,
                PartitionKeyExtractor = "Header('x-ms-org-id')",
                PartitioningAlgorithm = "alg1",
            };

            // Act
            var clone = sut.DeepClone();

            // Assert
            clone.Should().NotBeSameAs(sut);
            clone.PartitionCount.Should().Be(sut.PartitionCount);
            clone.PartitionKeyExtractor.Should().Be(sut.PartitionKeyExtractor);
            clone.PartitioningAlgorithm.Should().Be(sut.PartitioningAlgorithm);
        }
    }
}
