using NUnit.Framework;

namespace PipelineBuilder.Tests
{
	[TestFixture]
	public class ExtensionsTests
	{
		[Test]
		public void CanUseFoldL()
		{
			var sum = 10;
			var ints = new[] {1.0, 3.0, 4.0, 2.0};
			Assert.That(ints.FoldL<double, double>(0, (index, item, curr) => 
				curr + item),
				Is.EqualTo(sum));
		}

		[Test]
		public void CanUseAllTrue()
		{
			Assert.That(new[] { true, false, true }.AllTrue((i, item) => item), Is.False);
			Assert.That(new[] { true, true, true }.AllTrue((i, item) => item), Is.True);
		}

		[Test]
		public void CanUseNotAllTrue()
		{
			Assert.That(new[]{true, true, true, false}.NotAllTrue(i=>i));
			Assert.That(new[] { "Sometimes", "words", "slip", "in"}.NotAllTrue(i => i == "slip"));
		}
	}
}
