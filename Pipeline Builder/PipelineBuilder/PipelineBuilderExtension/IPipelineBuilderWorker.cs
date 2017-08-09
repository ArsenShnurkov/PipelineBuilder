/// Copyright (c) Microsoft Corporation.  All rights reserved.
namespace PipelineBuilderExtension
{
    interface IPipelineBuilderWorker
    {
        System.Collections.Generic.List<PipelineBuilder.PipelineSegmentSource> BuildPipeline(string sourceFile);
    }
}
