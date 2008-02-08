using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Contract;
using System.AddIn.Pipeline;
using PipelineHints;

//Generally for V1 of your application you'll want to use the same view assembly for both your host and add-in. 
[assembly: ShareViews]

//You should change the assembly name and default namespace for this project before you use it. 
namespace Sample.Blank.Contracts
{
    [AddInContract]
    public interface ISampleAddInContract : IContract
    {
        void DoStuff();
    }
}
