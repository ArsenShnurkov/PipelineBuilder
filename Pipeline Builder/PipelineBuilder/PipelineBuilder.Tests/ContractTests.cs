using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using NUnit.Framework;

namespace PipelineBuilder.Tests
{
    [TestFixture]
    public class ContractTests : AssertionHelper
    {
        

        private static void BuildPipeline( Assembly source, String root, bool print )
        {
            PipelineBuilder tpb = new PipelineBuilder( source.Location );
            List<PipelineSegmentSource> pipelines = tpb.BuildPipeline( );
            
            foreach ( PipelineSegmentSource pipline in pipelines )
            {
                String compRoot = root + "\\" + pipline.Name;
                
                foreach ( SourceFile file in pipline.Files )
                {
                    
                }
            }
        }

        [Test]
        public void TestMissingContractAssembly( )
        {
            try
            {
                Assembly.LoadFrom( "..\\" );
            }
            catch ( Exception ex )
            {
                Assert.True( ex is FileNotFoundException );
            }
        }

        [Test]
        public void TestEventInBaseContract( )
        {   
            string sSourcePath = "..\\..\\..\\TestAssemblies\\TestEventInBaseContract\\bin\\release\\Contracts.dll";
            string sDestPath = "..\\..\\..\\TestAssemblies\\TestEventInBaseContract\\output\\";

            if ( Debugger.IsAttached || !File.Exists(sSourcePath) )
            {
                sSourcePath = sSourcePath.Replace( "release", "debug" );
            }

            try
            {
                BuildPipeline( Assembly.LoadFrom( sSourcePath ), sDestPath, true );
            }
            catch ( InvalidOperationException )
            {
                // before fix, an InvalidOperationException "Can not find matching unsubscribe method for method:" would be thrown.
                throw;
            }
        }
    }

}