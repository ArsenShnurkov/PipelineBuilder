using System;
using System.Collections.Generic;
using PipelineBuilder;

namespace PipelineBuilderExtension
{
	internal class PipelineBuilderWorker : MarshalByRefObject, IPipelineBuilderWorker
	{
		public List<PipelineSegmentSource> BuildPipeline(String sourceFile)
		{
			var builder = new PipelineBuilder.PipelineBuilder(sourceFile);
			return builder.BuildPipeline();
		}
	}
}