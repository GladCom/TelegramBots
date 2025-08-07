using DirectumCoffee.InterestsSimilarity;

namespace DirectumCoffee.Tests;

public class Tests
{
  [SetUp]
  public void Setup()
  {
  }

  [Test]
  public void Test1()
  {
    var comparer = new InterestsComparer();
    var result = comparer.CompareInterests("cat", "dog");
    Assert.Pass();
  }
}