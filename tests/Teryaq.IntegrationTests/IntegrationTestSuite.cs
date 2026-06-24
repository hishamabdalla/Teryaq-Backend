namespace Teryaq.IntegrationTests;

using Xunit;

/// <summary>
/// Shared xUnit collection that groups all integration tests so they run sequentially
/// against a single <see cref="CustomWebApplicationFactory"/> instance.
/// Sequential execution prevents parallel startup races between factories.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestSuite : ICollectionFixture<CustomWebApplicationFactory>
{
    /// <summary>Collection name referenced by <see cref="CollectionAttribute"/> on each test class.</summary>
    public const string Name = "IntegrationTests";
}
